using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
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

    public RejectClaimHandler(IUnitOfWork uow, PayClaimDbContext context)
    {
        _uow = uow;
        _context = context;
    }

    public async Task<ClaimApprovalHistoryDto> Handle(RejectClaimCommand command, CancellationToken ct = default)
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
            throw new BusinessRuleException($"Cannot reject a claim that is already in '{claim.Status}' status.");
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

        // 4. Tentukan keputusan (Rejected atau NeedsRevision)
        var decision = command.Request.Decision == ApprovalDecision.NeedsRevision
            ? ApprovalDecision.NeedsRevision
            : ApprovalDecision.Rejected;

        claim.Status = decision == ApprovalDecision.NeedsRevision
            ? ClaimStatus.NeedsRevision
            : ClaimStatus.Rejected;

        _uow.Claims.Update(claim);

        // 5. Buat riwayat jejak approval (audit trail)
        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = decision,
            Notes = command.Request.Notes,
            ApprovalLevel = 1,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.ClaimApprovals.AddAsync(approvalRecord, ct);

        // 6. Simpan seluruh perubahan
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
