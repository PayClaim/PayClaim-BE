using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Claim;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Commands.ApproveClaim;

public record ApproveClaimCommand(Guid ClaimId, ClaimApprovalActionDto Request, Guid? LoggedInApproverId = null);

public class ApproveClaimHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;
    private readonly IEmailService _emailService;

    public ApproveClaimHandler(IUnitOfWork uow, PayClaimDbContext context, IEmailService emailService)
    {
        _uow = uow;
        _context = context;
        _emailService = emailService;
    }

    public async Task<ClaimApprovalHistoryDto> Handle(ApproveClaimCommand command, CancellationToken ct = default)
    {
        // 1. Ambil data klaim
        var claim = await _uow.Claims.GetByIdAsync(new object[] { command.ClaimId }, ct);
        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{command.ClaimId}' was not found.");
        }

        // 2. Validasi status klaim yang berhak di-approve
        if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderReview)
        {
            throw new BusinessRuleException(
                $"Cannot approve claim with status '{claim.Status}'. Only claims in 'Submitted' (Manager Review) or 'UnderReview' (Finance Review) can be approved.");
        }

        byte currentLevel;
        ClaimStatus nextStatus;
        string approverRoleKeyword;

        // 3. Tentukan Tingkat Approval (Level 1 Manager vs Level 2 Finance)
        if (claim.Status == ClaimStatus.Submitted)
        {
            // Level 1: Persetujuan Manager
            currentLevel = 1;
            nextStatus = ClaimStatus.UnderReview; // Menunggu verifikasi Finance
            approverRoleKeyword = "Manager";
        }
        else // claim.Status == ClaimStatus.UnderReview
        {
            // Level 2: Verifikasi & Persetujuan Dokumen oleh Finance
            currentLevel = 2;
            nextStatus = ClaimStatus.Approved; // Disetujui final
            approverRoleKeyword = "Finance";

            // LOGIKA FINANSIAL: Potong Saldo Benefit saat Finance menyetujui klaim
            if (claim.ClaimType == ClaimType.Benefit)
            {
                if (!claim.EmployeeBenefitId.HasValue)
                {
                    throw new BusinessRuleException("Benefit claim does not have an associated EmployeeBenefit quota.");
                }

                var empBenefit = await _context.EmployeeBenefits
                    .FirstOrDefaultAsync(eb => eb.Id == claim.EmployeeBenefitId.Value, ct);

                if (empBenefit == null)
                {
                    throw new NotFoundException($"Employee benefit quota with ID '{claim.EmployeeBenefitId.Value}' was not found.");
                }

                if (empBenefit.Balance < claim.Amount)
                {
                    throw new BusinessRuleException(
                        $"Cannot approve claim: Insufficient benefit quota balance. Available balance is Rp {empBenefit.Balance:N0}, but claim amount requested is Rp {claim.Amount:N0}.");
                }

                empBenefit.Balance -= claim.Amount;
                _uow.EmployeeBenefits.Update(empBenefit);
            }
        }

        // 4. Resolusi Approver
        var approverId = command.Request.ApproverId ?? command.LoggedInApproverId;
        Employee? approver = null;

        if (approverId.HasValue)
        {
            approver = await _uow.Employees.GetByIdAsync(new object[] { approverId.Value }, ct);
        }

        if (approver == null)
        {
            // Fallback untuk kemudahan development: cari employee sesuai keyword role (Manager atau Finance)
            approver = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Position.Title.Contains(approverRoleKeyword) || e.Position.Title.Contains("Lead") || e.Position.Title.Contains("Director"), ct)
                ?? await _context.Employees.FirstOrDefaultAsync(ct);

            if (approver == null)
            {
                throw new NotFoundException($"No eligible approver ({approverRoleKeyword}) found in the system.");
            }
        }

        // 5. Update Status Klaim
        claim.Status = nextStatus;
        _uow.Claims.Update(claim);

        // 6. Buat Riwayat Jejak Approval (Audit Trail)
        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = ApprovalDecision.Approved,
            Notes = command.Request.Notes,
            ApprovalLevel = currentLevel,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.ClaimApprovals.AddAsync(approvalRecord, ct);

        // 7. Simpan seluruh transaksi ke database
        await _uow.SaveChangesAsync(ct);

        // 8. Kirim notifikasi email ke karyawan
        var claimant = await _uow.Employees.GetByIdAsync(new object[] { claim.EmployeeId }, ct);
        if (claimant != null && !string.IsNullOrWhiteSpace(claimant.Email))
        {
            await _emailService.SendClaimStatusEmailAsync(
                claimant.Email,
                claimant.FullName,
                claim.Description,
                claim.Amount,
                claim.Status.ToString(),
                approver.FullName,
                command.Request.Notes,
                ct);
        }

        return new ClaimApprovalHistoryDto
        {
            Id = approvalRecord.Id,
            ApproverId = approvalRecord.ApproverId,
            ApproverName = approvalRecord.ApproverName,
            Decision = approvalRecord.Decision,
            Notes = approvalRecord.Notes,
            ApprovalLevel = approvalRecord.ApprovalLevel,
            DecidedAt = approvalRecord.DecidedAt
        };
    }
}
