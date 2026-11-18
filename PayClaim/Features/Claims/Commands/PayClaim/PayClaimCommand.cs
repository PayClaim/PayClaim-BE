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

namespace PayClaim.Features.Claims.Commands.PayClaim;

public record PayClaimCommand(Guid ClaimId, string? Notes = null, Guid? DisburserId = null) : IRequest<ClaimApprovalHistoryDto>;

public class PayClaimHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<PayClaimCommand, ClaimApprovalHistoryDto>
{
    public async Task<ClaimApprovalHistoryDto> Handle(PayClaimCommand command, CancellationToken ct = default)
    {
        var claim = await uow.Claims.GetByIdAsync(new object[] { command.ClaimId }, ct);
        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{command.ClaimId}' was not found.");
        }

        if (claim.Status != ClaimStatus.Approved)
        {
            throw new BusinessRuleException(
                $"Only claims with 'Approved' status can be disbursed / marked as Paid. Current claim status is '{claim.Status}'.");
        }

        Employee? disburser = null;
        var disburserId = command.DisburserId ?? currentUser.EmployeeId;
        if (disburserId.HasValue)
        {
            disburser = await uow.Employees.GetByIdAsync(new object[] { disburserId.Value }, ct);
        }

        disburser ??= await context.Employees
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Position.Title.Contains("Finance"), ct)
            ?? await context.Employees.FirstOrDefaultAsync(ct);

        if (disburser == null)
        {
            throw new NotFoundException("No eligible finance employee found to record disbursement.");
        }

        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = disburser.Id,
            ApproverName = disburser.FullName,
            Decision = ApprovalDecision.Approved,
            Notes = command.Notes ?? "Dana reimburse berhasil dicairkan dan dimasukkan ke dalam payroll selanjutnya.",
            ApprovalLevel = 3, 
            DecidedAt = DateTime.UtcNow.AddHours(7)
        };

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var updatedClaimRows = await context.Claims
            .Where(c => c.Id == claim.Id && c.Status == ClaimStatus.Approved)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.Status, ClaimStatus.Paid),
                ct);

        if (updatedClaimRows == 0)
        {
            throw new ConflictException(
                "Claim has already been disbursed or its status was modified concurrently.");
        }

        claim.Status = ClaimStatus.Paid;
        if (context.Entry(claim).State != EntityState.Detached)
        {
            context.Entry(claim).Property(c => c.Status).IsModified = false;
        }

        await uow.ClaimApprovals.AddAsync(approvalRecord, ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // 7. Kirim notifikasi email pencairan dana ke karyawan (payroll selanjutnya)
        var claimant = await uow.Employees.GetByIdAsync(new object[] { claim.EmployeeId }, ct);
        if (claimant != null && !string.IsNullOrWhiteSpace(claimant.Email))
        {
            await emailService.SendClaimStatusEmailAsync(
                claimant.Email,
                claimant.FullName,
                claim.Description,
                claim.Amount,
                claim.Status.ToString(),
                disburser.FullName,
                approvalRecord.Notes,
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
