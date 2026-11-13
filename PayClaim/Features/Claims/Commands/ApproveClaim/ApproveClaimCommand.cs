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

namespace PayClaim.Features.Claims.Commands.ApproveClaim;

public record ApproveClaimCommand(
    Guid ClaimId,
    ClaimApprovalActionDto Request,
    Guid? LoggedInApproverId = null,
    IReadOnlyCollection<string>? ApproverRoles = null) : IRequest<ClaimApprovalHistoryDto>;

public class ApproveClaimHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<ApproveClaimCommand, ClaimApprovalHistoryDto>
{
    public async Task<ClaimApprovalHistoryDto> Handle(
        ApproveClaimCommand command,
        CancellationToken ct = default)
    {
        var approverId = command.LoggedInApproverId ?? currentUser.RequiredEmployeeId;
        var approverRoles = command.ApproverRoles ?? currentUser.Roles;

        // 1. Ambil Claim
        var claim = await uow.Claims.GetByIdAsync(
            new object[] { command.ClaimId },
            ct);

        if (claim == null)
        {
            throw new NotFoundException(
                $"Claim with ID '{command.ClaimId}' was not found.");
        }

        // 2. Tentukan tahap approval berdasarkan status Claim
        byte currentLevel;
        ClaimStatus nextStatus;
        string requiredRole;

        if (claim.Status == ClaimStatus.Submitted)
        {
            currentLevel = 1;
            nextStatus = ClaimStatus.UnderReview;
            requiredRole = "Manager";
        }
        else if (claim.Status == ClaimStatus.UnderReview)
        {
            currentLevel = 2;
            nextStatus = ClaimStatus.Approved;
            requiredRole = "Finance";
        }
        else
        {
            throw new BusinessRuleException(
                $"Cannot approve claim with status '{claim.Status}'. Only Submitted or UnderReview claims can be approved.");
        }

        // 3. Periksa apakah approver memiliki role yang sesuai
        bool isAdmin = approverRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        bool isFinanceManager = approverRoles.Contains("Finance Manager", StringComparer.OrdinalIgnoreCase);
        bool isFinance = approverRoles.Contains("Finance", StringComparer.OrdinalIgnoreCase);
        bool isManager = approverRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase);

        // Role tertinggi pengaju menentukan siapa yang boleh approve di level 2.
        // Employee/Manager -> Finance atau Finance Manager
        // Finance         -> Finance Manager (tidak boleh rekan Finance sendiri)
        // Finance Manager -> SuperAdmin
        var claimantRoles = await context.GetRoleNamesAsync(
            claim.EmployeeId,
            ct);
        var claimantHighestRole =
            ApprovalRouting.GetHighestBusinessRole(claimantRoles);

        bool hasRequiredRole;
        if (currentLevel == 1)
        {
            hasRequiredRole = isAdmin || isManager || isFinanceManager;
        }
        else
        {
            hasRequiredRole = claimantHighestRole switch
            {
                ApprovalRouting.FinanceManager => isAdmin,
                ApprovalRouting.Finance => isFinanceManager || isAdmin,
                _ => isFinance || isFinanceManager || isAdmin
            };
        }

        if (!hasRequiredRole)
        {
            throw new ForbiddenException(
                $"Only {requiredRole} (or a higher role) can approve this claim at the current stage.");
        }

        // 4. Ambil Employee yang sedang melakukan approval
        var approver = await context.Employees
            .FirstOrDefaultAsync(
                e => e.Id == approverId,
                ct);

        if (approver == null)
        {
            throw new NotFoundException(
                "Approver employee was not found.");
        }

        if (claim.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot approve your own claim.");
        }

        if (!approver.IsActive)
        {
            throw new ForbiddenException(
                "Inactive employees cannot approve claims.");
        }

        // 5. Jika Level 1 (Manager), pastikan dia Manager dari pengaju Claim atau Admin
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
                    "This employee does not have a direct manager. Only an Admin can approve this claim.");
            }

            if (claimant.ManagerId.Value != approver.Id)
            {
                throw new ForbiddenException(
                    "You are not the manager of the employee who submitted this claim.");
            }
        }

        // 6. Jika Finance menyetujui Benefit Claim, potong saldo Benefit
        if (currentLevel == 2 &&
            claim.ClaimType == ClaimType.Benefit)
        {
            if (!claim.EmployeeBenefitId.HasValue)
            {
                throw new BusinessRuleException(
                    "Benefit claim does not have an associated EmployeeBenefit quota.");
            }

            var empBenefit = await context.EmployeeBenefits
                .FirstOrDefaultAsync(
                    eb => eb.Id == claim.EmployeeBenefitId.Value,
                    ct);

            if (empBenefit == null)
            {
                throw new NotFoundException(
                    $"Employee benefit quota with ID '{claim.EmployeeBenefitId.Value}' was not found.");
            }

            if (empBenefit.Balance < claim.Amount)
            {
                throw new BusinessRuleException(
                    $"Cannot approve claim: Insufficient benefit quota balance. Available balance is Rp {empBenefit.Balance:N0}, but claim amount requested is Rp {claim.Amount:N0}.");
            }

            var updatedRows = await context.EmployeeBenefits
                .Where(eb => eb.Id == claim.EmployeeBenefitId.Value && eb.Balance >= claim.Amount)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(eb => eb.Balance, eb => eb.Balance - claim.Amount),
                    ct);

            if (updatedRows == 0)
            {
                throw new BusinessRuleException(
                    $"Cannot approve claim: Insufficient benefit quota balance due to concurrent update.");
            }
        }

        // 7. Update status Claim
        claim.Status = nextStatus;

        uow.Claims.Update(claim);

        // 8. Simpan riwayat approval
        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = ApprovalDecision.Approved,
            Notes = command.Request.Notes,
            ApprovalLevel = currentLevel,
            DecidedAt = DateTime.UtcNow.AddHours(7)
        };

        await uow.ClaimApprovals.AddAsync(
            approvalRecord,
            ct);

        // 9. Simpan seluruh perubahan
        await uow.SaveChangesAsync(ct);

        // 10. Ambil data pengaju klaim (claimant) untuk kirim email notifikasi
        var claimantEmp = await context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == claim.EmployeeId, ct);

        // 11. Notifikasi ke bawahan/pengaju klaim mengenai perubahan status
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

        // 12. Jika disetujui Manajer (Level 1 / status UnderReview), kirim notifikasi ke Tim Finance
        if (currentLevel == 1)
        {
            var financeUsers = await context.Accounts
                .Where(a => a.IsActive && a.AccountRoles.Any(ar => ar.Role.Name == "Finance" || ar.Role.Name == "Finance Manager"))
                .Select(a => new { a.Employee.Email, a.Employee.FullName })
                .Where(x => !string.IsNullOrEmpty(x.Email))
                .Distinct()
                .ToListAsync(ct);

            await Task.WhenAll(financeUsers.Select(fin =>
                emailService.SendClaimApprovedToFinanceEmailAsync(
                    fin.Email,
                    fin.FullName,
                    claimantEmp?.FullName ?? "Karyawan",
                    claimantEmp?.Department?.Name ?? "-",
                    claim.ClaimType.ToString(),
                    claim.Description,
                    claim.Amount,
                    approver.FullName,
                    command.Request.Notes,
                    claim.Id,
                    ct)));
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
