using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Claim;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Commands.RejectClaim;

public record RejectClaimCommand(Guid ClaimId, ClaimApprovalActionDto Request, Guid? LoggedInApproverId = null);

public class RejectClaimHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;
    private readonly IEmailService _emailService;

    public RejectClaimHandler(IUnitOfWork uow, PayClaimDbContext context, IEmailService emailService)
    {
        _uow = uow;
        _context = context;
        _emailService = emailService;
    }

    public async Task<ClaimApprovalHistoryDto> Handle(RejectClaimCommand command, CancellationToken ct = default)
    {
        // 1. Ambil data klaim
        var claim = await _uow.Claims.GetByIdAsync(new object[] { command.ClaimId }, ct);
        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{command.ClaimId}' was not found.");
        }

        // 2. Validasi status klaim yang berhak ditolak
        if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderReview)
        {
            throw new BusinessRuleException($"Cannot reject a claim that is already in '{claim.Status}' status.");
        }

        // 3. Tentukan level penolakan (Level 1 Manager jika Submitted, Level 2 Finance jika UnderReview)
        byte currentLevel = claim.Status == ClaimStatus.Submitted ? (byte)1 : (byte)2;
        string approverRoleKeyword = currentLevel == 1 ? "Manager" : "Finance";

        // 4. Resolusi Approver
        var approverId = command.Request.ApproverId ?? command.LoggedInApproverId;
        Employee? approver = null;

        if (approverId.HasValue)
        {
            approver = await _uow.Employees.GetByIdAsync(new object[] { approverId.Value }, ct);
        }

        if (approver == null)
        {
            approver = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Position.Title.Contains(approverRoleKeyword) || e.Position.Title.Contains("Lead") || e.Position.Title.Contains("Director"), ct)
                ?? await _context.Employees.FirstOrDefaultAsync(ct);

            if (approver == null)
            {
                throw new NotFoundException($"No eligible approver ({approverRoleKeyword}) found in the system.");
            }
        }

        // 5. Tentukan keputusan (Rejected atau NeedsRevision)
        var decision = command.Request.Decision == ApprovalDecision.NeedsRevision
            ? ApprovalDecision.NeedsRevision
            : ApprovalDecision.Rejected;

        claim.Status = decision == ApprovalDecision.NeedsRevision
            ? ClaimStatus.NeedsRevision
            : ClaimStatus.Rejected;

        _uow.Claims.Update(claim);

        // 6. Buat riwayat jejak approval (audit trail)
        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = decision,
            Notes = command.Request.Notes,
            ApprovalLevel = currentLevel,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.ClaimApprovals.AddAsync(approvalRecord, ct);

        // 7. Simpan seluruh perubahan
        await _uow.SaveChangesAsync(ct);

        // 8. Kirim notifikasi email penolakan / revisi ke karyawan
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
