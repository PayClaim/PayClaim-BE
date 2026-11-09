using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Claim;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Commands.PayClaim;

public record PayClaimCommand(Guid ClaimId, string? Notes = null, Guid? DisburserId = null);

public class PayClaimHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;
    private readonly IEmailService _emailService;

    public PayClaimHandler(IUnitOfWork uow, PayClaimDbContext context, IEmailService emailService)
    {
        _uow = uow;
        _context = context;
        _emailService = emailService;
    }

    public async Task<ClaimApprovalHistoryDto> Handle(PayClaimCommand command, CancellationToken ct = default)
    {
        var claim = await _uow.Claims.GetByIdAsync(new object[] { command.ClaimId }, ct);
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
        if (command.DisburserId.HasValue)
        {
            disburser = await _uow.Employees.GetByIdAsync(new object[] { command.DisburserId.Value }, ct);
        }

        disburser ??= await _context.Employees
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Position.Title.Contains("Finance"), ct)
            ?? await _context.Employees.FirstOrDefaultAsync(ct);

        if (disburser == null)
        {
            throw new NotFoundException("No eligible finance employee found to record disbursement.");
        }

        claim.Status = ClaimStatus.Paid;
        _uow.Claims.Update(claim);

        var approvalRecord = new ClaimApproval
        {
            ClaimId = claim.Id,
            ApproverId = disburser.Id,
            ApproverName = disburser.FullName,
            Decision = ApprovalDecision.Approved,
            Notes = command.Notes ?? "Dana reimburse berhasil dicairkan ke payroll / ditransfer.",
            ApprovalLevel = 3, 
            DecidedAt = DateTime.UtcNow
        };

        await _uow.ClaimApprovals.AddAsync(approvalRecord, ct);
        await _uow.SaveChangesAsync(ct);

        // 7. Kirim notifikasi email pencairan dana ke karyawan
        var claimant = await _uow.Employees.GetByIdAsync(new object[] { claim.EmployeeId }, ct);
        if (claimant != null && !string.IsNullOrWhiteSpace(claimant.Email))
        {
            await _emailService.SendClaimStatusEmailAsync(
                claimant.Email,
                claimant.FullName,
                claim.Description,
                claim.Amount,
                claim.Status.ToString(),
                disburser.FullName,
                approvalRecord.Notes,
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
