using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
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
    Guid LoggedInApproverId,
    IReadOnlyCollection<string> ApproverRoles);

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

    public async Task<ClaimApprovalHistoryDto> Handle(
    ApproveClaimCommand command,
    CancellationToken ct = default)
    {
        // 1. Ambil Claim
        var claim = await _uow.Claims.GetByIdAsync(
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
        var hasRequiredRole = command.ApproverRoles
            .Contains(requiredRole, StringComparer.OrdinalIgnoreCase);

        if (!hasRequiredRole)
        {
            throw new ForbiddenException(
                $"Only {requiredRole} can approve this claim at the current stage.");
        }

        // 4. Ambil Employee yang sedang melakukan approval
        var approver = await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Id == command.LoggedInApproverId,
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

        // 5. Jika Manager, pastikan dia Manager dari pengaju Claim
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

        // 6. Jika Finance menyetujui Benefit Claim, potong saldo Benefit
        if (requiredRole == "Finance" &&
            claim.ClaimType == ClaimType.Benefit)
        {
            if (!claim.EmployeeBenefitId.HasValue)
            {
                throw new BusinessRuleException(
                    "Benefit claim does not have an associated EmployeeBenefit quota.");
            }

            var empBenefit = await _context.EmployeeBenefits
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

            empBenefit.Balance -= claim.Amount;

            _uow.EmployeeBenefits.Update(empBenefit);
        }

        // 7. Update status Claim
        claim.Status = nextStatus;

        _uow.Claims.Update(claim);

        // 8. Simpan riwayat approval
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

        await _uow.ClaimApprovals.AddAsync(
            approvalRecord,
            ct);

        // 9. Simpan seluruh perubahan
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
