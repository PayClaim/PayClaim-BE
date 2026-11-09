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
        var claim = await _uow.Claims.GetByIdAsync(
            new object[] { command.ClaimId },
            ct);


        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{command.ClaimId}' was not found.");
        }
        byte currentLevel;
        string requiredRole;

        if (claim.Status == ClaimStatus.Submitted)

        if (claim.Status == ClaimStatus.Submitted)
        {
            throw new BusinessRuleException($"Cannot reject a claim that is already in '{claim.Status}' status.");
        var decision = command.Request.Decision switch
        {
            ApprovalDecision.Rejected =>
                ApprovalDecision.Rejected,
        {
            ApprovalDecision.Rejected =>
                ApprovalDecision.Rejected,

            ApprovalDecision.NeedsRevision =>
                ApprovalDecision.NeedsRevision,

            _ => throw new BusinessRuleException(
                "Invalid rejection decision.")
        };

        var hasRequiredRole = command.ApproverRoles.Contains(
            requiredRole,
            StringComparer.OrdinalIgnoreCase);

        if (!hasRequiredRole)
        {
            throw new ForbiddenException(
        var approver = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == command.LoggedInApproverId,
                ct);

            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == command.LoggedInApproverId,
                ct);

        if (approver == null)
        {
        if (!approver.IsActive)
        {
            throw new ForbiddenException(
                "Inactive employees cannot reject claims.");
        }

        if (claim.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot reject or request revision for your own claim.");

        // Karyawan tidak boleh memproses Claim miliknya sendiri.
        if (requiredRole == "Manager")
        {
            var isManagerOfSubmitter = await _context.Employees
                .AnyAsync(
                    e => e.Id == claim.EmployeeId &&
                         e.ManagerId == approver.Id,
                    ct);

            if (!isManagerOfSubmitter)
            {
                throw new ForbiddenException(
                    "You are not the manager of the employee who submitted this claim.");
            }
        }

            if (!isManagerOfSubmitter)
            {
                throw new ForbiddenException(
                    "You are not the manager of the employee who submitted this claim.");
            }
        }
        claim.Status = decision == ApprovalDecision.NeedsRevision
            ? ClaimStatus.NeedsRevision
            : ClaimStatus.Rejected;

        _uow.Claims.Update(claim);

        // 8. Catat riwayat keputusan.
        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = decision,
            ApprovalLevel = currentLevel,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.ClaimApprovals.AddAsync(approvalRecord, ct);

        // 9. Simpan perubahan.
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
