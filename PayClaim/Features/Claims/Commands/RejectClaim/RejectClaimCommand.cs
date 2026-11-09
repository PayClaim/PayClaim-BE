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
        bool isAdmin = command.ApproverRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        bool isFinanceManager = command.ApproverRoles.Contains("Finance Manager", StringComparer.OrdinalIgnoreCase);

        bool hasRequiredRole = isAdmin || (currentLevel == 1
            ? (command.ApproverRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase) || isFinanceManager)
            : (command.ApproverRoles.Contains("Finance", StringComparer.OrdinalIgnoreCase) || isFinanceManager));

        if (!hasRequiredRole)
        {
            throw new ForbiddenException(
                $"Only {requiredRole} (or Admin/Finance Manager) can reject or request revision for this claim.");
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

        // 7. Jika Level 1 (Manager), pastikan dia atasan langsung pengaju Claim atau Admin.
        if (currentLevel == 1 && !isAdmin)
        {
            var claimant = await _context.Employees
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

        // 11. Kirim notifikasi email penolakan / revisi ke karyawan.
        var claimantEmp = await _uow.Employees.GetByIdAsync(
            new object[] { claim.EmployeeId },
            ct);

        if (claimantEmp != null && !string.IsNullOrWhiteSpace(claimantEmp.Email))
        {
            await _emailService.SendClaimStatusEmailAsync(
                claimantEmp.Email,
                claimantEmp.FullName,
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
