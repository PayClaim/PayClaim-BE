using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
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
    Guid? LoggedInApproverId = null,
    IReadOnlyCollection<string>? ApproverRoles = null) : IRequest<ClaimApprovalHistoryDto>;

public class RejectClaimHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<RejectClaimCommand, ClaimApprovalHistoryDto>
{
    public async Task<ClaimApprovalHistoryDto> Handle(
        RejectClaimCommand command,
        CancellationToken ct = default)
    {
        var approverId = command.LoggedInApproverId ?? currentUser.RequiredEmployeeId;
        var approverRoles = command.ApproverRoles ?? currentUser.Roles;

        // 1. Ambil Claim.
        var claim = await uow.Claims.GetByIdAsync(
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
        bool isAdmin = approverRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        bool isFinanceManager = approverRoles.Contains("Finance Manager", StringComparer.OrdinalIgnoreCase);

        bool hasRequiredRole = isAdmin || (currentLevel == 1
            ? (approverRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase) || isFinanceManager)
            : (approverRoles.Contains("Finance", StringComparer.OrdinalIgnoreCase) || isFinanceManager));

        if (!hasRequiredRole)
        {
            throw new ForbiddenException(
                $"Only {requiredRole} (or Admin/Finance Manager) can reject or request revision for this claim.");
        }

        // 5. Ambil Employee yang sedang login berdasarkan JWT.
        var approver = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == approverId,
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

        // 7. Jika Level 1 (Manager), pastikan dia atasan langsung pengaju Claim atau Admin.
        if (currentLevel == 1 && !isAdmin)
        {
            var claimant = await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == claim.EmployeeId, ct);

            if (claimant == null)
            {
                throw new NotFoundException("Claimant employee was not found.");
            }

            if (!claimant.ManagerId.HasValue)
            {
                throw new ForbiddenException(
                    "This employee does not have a direct manager. Only an Admin can reject or request revision for this claim.");
            }

            if (claimant.ManagerId.Value != approver.Id)
            {
                throw new ForbiddenException(
                    "You are not the manager of the employee who submitted this claim.");
            }
        }

        // 8. Ubah status Claim berdasarkan keputusan.
        claim.Status = decision == ApprovalDecision.NeedsRevision
            ? ClaimStatus.NeedsRevision
            : ClaimStatus.Rejected;

        uow.Claims.Update(claim);

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

        await uow.ClaimApprovals.AddAsync(
            approvalRecord,
            ct);

        // 10. Simpan perubahan ke database.
        await uow.SaveChangesAsync(ct);

        // 11. Kirim notifikasi email penolakan / revisi ke karyawan.
        var claimantEmp = await uow.Employees.GetByIdAsync(
            new object[] { claim.EmployeeId },
            ct);

        if (claimantEmp != null && !string.IsNullOrWhiteSpace(claimantEmp.Email))
        {
            await emailService.SendClaimStatusEmailAsync(
                claimantEmp.Email,
                claimantEmp.FullName,
                claim.Description,
                claim.Amount,
                claim.Status.ToString(),
                approver.FullName,
                command.Request.Notes,
                claim.Id,
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
