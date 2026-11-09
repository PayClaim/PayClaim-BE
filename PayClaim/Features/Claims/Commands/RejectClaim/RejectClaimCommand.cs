
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
        // 1. Ambil Claim berdasarkan ID.
        var claim = await _uow.Claims.GetByIdAsync(
            new object[] { command.ClaimId },
            ct);

        if (claim == null)
        {
            throw new NotFoundException(
                $"Claim with ID '{command.ClaimId}' was not found.");
        }

        // 2. Tentukan level approval dan role yang diperlukan.
        byte currentLevel;
        string requiredRole;

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

        // 3. Pastikan keputusan adalah Rejected atau NeedsRevision.
        var decision = command.Request.Decision switch
        {
            ApprovalDecision.Rejected =>
                ApprovalDecision.Rejected,

            ApprovalDecision.NeedsRevision =>
                ApprovalDecision.NeedsRevision,

            _ => throw new BusinessRuleException(
                "Invalid rejection decision.")
        };

        // 4. Pastikan approver memiliki role yang sesuai.
        var hasRequiredRole = command.ApproverRoles.Contains(
            requiredRole,
            StringComparer.OrdinalIgnoreCase);

        if (!hasRequiredRole)
        {
            throw new ForbiddenException(
                $"Only {requiredRole} can reject or request revision for this claim.");
        }

        // 5. Ambil Employee yang sedang login berdasarkan JWT.
        var approver = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == command.LoggedInApproverId,
                ct);

        if (approver == null)
        {
            throw new NotFoundException(
                "Approver employee was not found.");
        }

        if (!approver.IsActive)
        {
            throw new ForbiddenException(
                "Inactive employees cannot reject claims.");
        }

        // 6. Approver tidak boleh memproses Claim miliknya sendiri.
        if (claim.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot reject or request revision for your own claim.");
        }

        // 7. Jika Manager, pastikan dia atasan langsung pengaju Claim.
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

        // 8. Ubah status Claim berdasarkan keputusan.
        claim.Status = decision == ApprovalDecision.NeedsRevision
            ? ClaimStatus.NeedsRevision
            : ClaimStatus.Rejected;

        _uow.Claims.Update(claim);

        // 9. Buat riwayat approval.
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

        await _uow.ClaimApprovals.AddAsync(
            approvalRecord,
            ct);

        // 10. Simpan perubahan ke database.
        await _uow.SaveChangesAsync(ct);

        // 11. Kirim email pemberitahuan kepada pengaju Claim.
        var claimant = await _uow.Employees.GetByIdAsync(
            new object[] { claim.EmployeeId },
            ct);

        if (claimant != null &&
            !string.IsNullOrWhiteSpace(claimant.Email))
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

        // 12. Kembalikan riwayat approval.
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
