using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Claim;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Commands.RejectClaim;

public record RejectClaimCommand(
    Guid ClaimId,
    ClaimApprovalActionDto Request,
    Guid LoggedInApproverId,
    IReadOnlyCollection<string> ApproverRoles);

public class RejectClaimHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;
    private readonly IEmailService _emailService;

    public RejectClaimHandler(
        IUnitOfWork uow,
        PayClaimDbContext context,
        IEmailService emailService)
    {
        _uow = uow;
        _context = context;
        _emailService = emailService;
    }

    public async Task<ClaimApprovalHistoryDto> Handle(
        RejectClaimCommand command,
        CancellationToken ct = default)
    {
        // 1. Ambil Claim.
        var claim = await _uow.Claims.GetByIdAsync(
            new object[] { command.ClaimId },
            ct);

        var claim = await _uow.Claims.GetByIdAsync(
            new object[] { command.ClaimId },
            ct);


        if (claim == null)
        {
            throw new NotFoundException(
                $"Claim with ID '{command.ClaimId}' was not found.");
        }
        byte currentLevel;
        string requiredRole;

        // 2. Tentukan tahap approval berdasarkan status Claim.
        byte currentLevel;
        string requiredRole;

        if (claim.Status == ClaimStatus.Submitted)
        if (claim.Status == ClaimStatus.Submitted)

        if (claim.Status == ClaimStatus.Submitted)
        {
            currentLevel = 1;
            requiredRole = "Manager";
        }
        else if (claim.Status == ClaimStatus.UnderReview)
        {
            currentLevel = 2;
            requiredRole = "Finance";
        }
        else
        {
            throw new BusinessRuleException(
                $"Cannot reject Claim with status '{claim.Status}'. Only Submitted or UnderReview claims can be rejected.");
        }
            throw new BusinessRuleException($"Cannot reject a claim that is already in '{claim.Status}' status.");
        var decision = command.Request.Decision switch
        {
            ApprovalDecision.Rejected =>
                ApprovalDecision.Rejected,
        {
            ApprovalDecision.Rejected =>
                ApprovalDecision.Rejected,

        // 3. Pastikan keputusan merupakan Rejected atau NeedsRevision.
        var decision = command.Request.Decision switch
        {
            ApprovalDecision.Rejected =>
                ApprovalDecision.Rejected,
            ApprovalDecision.NeedsRevision =>
                ApprovalDecision.NeedsRevision,

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

            _ => throw new BusinessRuleException(
                "Invalid rejection decision.")
        };

        // 4. Pastikan approver memiliki role yang sesuai.
        var hasRequiredRole = command.ApproverRoles.Contains(
            requiredRole,
            StringComparer.OrdinalIgnoreCase);

        if (!hasRequiredRole)
        if (approver == null)
        {
        if (!approver.IsActive)
        {
            throw new ForbiddenException(
                $"Only {requiredRole} can reject or request revision for this claim.");
            throw new ForbiddenException(
                "Inactive employees cannot reject claims.");
        }

        // 5. Ambil Employee yang sedang login.
        // ID berasal dari JWT, bukan request frontend.
        var approver = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == command.LoggedInApproverId,
                ct);

        if (approver == null)
        if (claim.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot reject or request revision for your own claim.");

        // Karyawan tidak boleh memproses Claim miliknya sendiri.
        if (requiredRole == "Manager")
        {
            throw new NotFoundException(
                "Approver employee was not found.");
        }
            var isManagerOfSubmitter = await _context.Employees
                .AnyAsync(
                    e => e.Id == claim.EmployeeId &&
                         e.ManagerId == approver.Id,
                    ct);

        if (!approver.IsActive)
        {
            throw new ForbiddenException(
                "Inactive employees cannot reject claims.");
        }

        // Karyawan tidak boleh memproses Claim miliknya sendiri.
        if (claim.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot reject or request revision for your own claim.");
            if (!isManagerOfSubmitter)
            {
                throw new ForbiddenException(
                    "You are not the manager of the employee who submitted this claim.");
            }
        }

        // 6. Jika Manager, pastikan dia atasan langsung pengaju Claim.
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

        // 7. Ubah status berdasarkan keputusan.
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

        await _uow.ClaimApprovals.AddAsync(
            approvalRecord,
            ct);

        // 9. Simpan perubahan.
        await _uow.SaveChangesAsync(ct);

        // 10. Kirim notifikasi email penolakan / revisi ke karyawan
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
