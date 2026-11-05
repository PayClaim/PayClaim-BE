using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
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

    public ApproveClaimHandler(IUnitOfWork uow, PayClaimDbContext context)
    {
        _uow = uow;
        _context = context;
    }

    public async Task<ClaimApprovalHistoryDto> Handle(ApproveClaimCommand command, CancellationToken ct = default)
    {
        // 1. Ambil data klaim
        var claim = await _uow.Claims.GetByIdAsync(new object[] { command.ClaimId }, ct);
        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{command.ClaimId}' was not found.");
        }

        // 2. Validasi status klaim
        if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderReview)
        {
            throw new BusinessRuleException($"Cannot approve a claim that is already in '{claim.Status}' status.");
        }

        // 3. Resolusi Approver
        var approverId = command.Request.ApproverId ?? command.LoggedInApproverId;
        Employee? approver = null;

        if (approverId.HasValue)
        {
            approver = await _uow.Employees.GetByIdAsync(new object[] { approverId.Value }, ct);
        }

        if (approver == null)
        {
            // Fallback untuk kemudahan development: cari manager/lead/finance atau employee pertama
            approver = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Position.Title.Contains("Manager") || e.Position.Title.Contains("Lead") || e.Position.Title.Contains("Finance"), ct)
                ?? await _context.Employees.FirstOrDefaultAsync(ct);

            if (approver == null)
            {
                throw new NotFoundException("No eligible approver found in the system.");
            }
        }

        // 4. Logika Finansial: Potong Saldo jika jenis klaim adalah Benefit
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
                throw new BusinessRuleException($"Cannot approve claim: Insufficient benefit quota balance. Available balance is Rp {empBenefit.Balance:N0}, but claim amount requested is Rp {claim.Amount:N0}.");
            }

            empBenefit.Balance -= claim.Amount;
            _uow.EmployeeBenefits.Update(empBenefit);
        }

        // 5. Update status klaim menjadi Approved
        claim.Status = ClaimStatus.Approved;
        _uow.Claims.Update(claim);

        // 6. Buat riwayat jejak approval (audit trail)
        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = ApprovalDecision.Approved,
            Notes = command.Request.Notes,
            ApprovalLevel = 1,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.ClaimApprovals.AddAsync(approvalRecord, ct);

        // 7. Simpan seluruh perubahan dalam satu transaksi atomic
        await _uow.SaveChangesAsync(ct);

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
