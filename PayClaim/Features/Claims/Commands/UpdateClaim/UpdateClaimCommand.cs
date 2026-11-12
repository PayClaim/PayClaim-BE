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
using PayClaim.Features.EmployeeBenefits;

namespace PayClaim.Features.Claims.Commands.UpdateClaim;

public record UpdateClaimCommand(Guid ClaimId, CreateClaimDto Request, Guid? SubmitterEmployeeId = null) : IRequest<ClaimDetailDto>;

public class UpdateClaimHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IFileStorageService fileStorage,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<UpdateClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> Handle(UpdateClaimCommand command, CancellationToken ct = default)
    {
        var submitterId = command.SubmitterEmployeeId ?? currentUser.EmployeeId;

        // 1. Ambil data klaim yang akan diubah
        var claim = await context.Claims
            .Include(c => c.Employee)
                .ThenInclude(e => e.Department)
            .Include(c => c.EmployeeBenefit)
                .ThenInclude(eb => eb!.Benefit)
            .Include(c => c.TripProposal)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, ct);

        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{command.ClaimId}' was not found.");
        }

        // 2. Validasi status: Hanya klaim berstatus NeedsRevision atau Submitted yang boleh diubah/diajukan ulang
        if (claim.Status != ClaimStatus.NeedsRevision && claim.Status != ClaimStatus.Submitted)
        {
            throw new BusinessRuleException(
                $"Cannot update claim with status '{claim.Status}'. Only claims in 'NeedsRevision' or 'Submitted' can be modified.");
        }

        // 3. Validasi kepemilikan (jika ada SubmitterEmployeeId dari token login)
        if (submitterId.HasValue && claim.EmployeeId != submitterId.Value)
        {
            throw new BusinessRuleException("You are not authorized to modify a claim submitted by another employee.");
        }

        var request = command.Request;
        string? benefitName = null;

        // 4. Validasi aturan bisnis berdasarkan ClaimType
        switch (request.ClaimType)
        {
            case ClaimType.Benefit:
                var benefitId = request.EmployeeBenefitId ?? claim.EmployeeBenefitId;
                if (!benefitId.HasValue)
                {
                    throw new BusinessRuleException("EmployeeBenefitId is required for Benefit claims.");
                }

                var empBenefit = await context.EmployeeBenefits
                    .Include(eb => eb.Benefit)
                    .FirstOrDefaultAsync(eb => eb.Id == benefitId.Value, ct);

                if (empBenefit == null)
                {
                    throw new NotFoundException($"Employee benefit quota with ID '{benefitId.Value}' was not found.");
                }

                if (empBenefit.EmployeeId != claim.EmployeeId)
                {
                    throw new BusinessRuleException("The selected benefit quota does not belong to this employee.");
                }

                // Cek dan lakukan auto-renewal jika periode benefit sudah kadaluarsa
                var plan = await context.BenefitPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(bp =>
                        bp.DepartmentId == claim.Employee.DepartmentId &&
                        bp.PositionId == claim.Employee.PositionId &&
                        bp.BenefitId == empBenefit.BenefitId, ct);

                if (plan != null && BenefitPeriodHelper.TryRenewIfExpired(empBenefit, empBenefit.Benefit.PeriodType, plan.LimitAmount))
                {
                    uow.EmployeeBenefits.Update(empBenefit);
                    await uow.SaveChangesAsync(ct);
                }

                if (request.TransactionDate < empBenefit.StartDate || request.TransactionDate > empBenefit.EndDate)
                {
                    throw new BusinessRuleException(
                        $"Transaction date must be within the active benefit quota period ({empBenefit.StartDate:yyyy-MM-dd} to {empBenefit.EndDate:yyyy-MM-dd}).");
                }

                // Hitung klaim benefit lain yang sedang berjalan (kecuali klaim ini sendiri)
                var pendingBenefitClaims = await context.Claims
                    .Where(c => c.EmployeeBenefitId == empBenefit.Id &&
                                c.EmployeeId == claim.EmployeeId &&
                                c.Id != claim.Id &&
                                c.Status != ClaimStatus.Rejected &&
                                c.Status != ClaimStatus.Paid)
                    .SumAsync(c => c.Amount, ct);

                var availableBalance = Math.Max(0, empBenefit.Balance - pendingBenefitClaims);
                if (availableBalance < request.Amount)
                {
                    throw new BusinessRuleException(
                        $"Insufficient benefit quota balance. Current available balance is Rp {availableBalance:N0} (Active balance: Rp {empBenefit.Balance:N0}, Pending claims: Rp {pendingBenefitClaims:N0}), but claim amount requested is Rp {request.Amount:N0}.");
                }

                benefitName = empBenefit.Benefit?.Name;
                claim.EmployeeBenefitId = empBenefit.Id;
                claim.TripProposalId = null;
                break;

            case ClaimType.BusinessTripReimbursement:
                var tripId = request.TripProposalId ?? claim.TripProposalId;
                if (!tripId.HasValue)
                {
                    throw new BusinessRuleException("TripProposalId is required for Business Trip Reimbursement claims.");
                }

                var trip = await context.TripProposals
                    .Include(tp => tp.Participants)
                    .FirstOrDefaultAsync(tp => tp.Id == tripId.Value, ct);

                if (trip == null)
                {
                    throw new NotFoundException($"Business trip proposal with ID '{tripId.Value}' was not found.");
                }

                if (trip.Status != TripProposalStatus.Approved && trip.Status != TripProposalStatus.Completed)
                {
                    throw new BusinessRuleException(
                        $"Cannot submit reimbursement for a trip proposal that is in '{trip.Status}' status. The trip proposal must be 'Approved' or 'Completed' first.");
                }

                var participant = trip.Participants.FirstOrDefault(p => p.EmployeeId == claim.EmployeeId);
                if (participant == null)
                {
                    throw new BusinessRuleException("This employee is not registered as a participant in the specified business trip proposal.");
                }

                // Pastikan tanggal invoice / transaksi berada dalam periode perjalanan dinas
                if (request.TransactionDate < trip.StartDate ||
                    request.TransactionDate > trip.EndDate)
                {
                    throw new BusinessRuleException(
                        $"Tanggal invoice ({request.TransactionDate:yyyy-MM-dd}) harus berada di dalam periode perjalanan dinas ({trip.StartDate:yyyy-MM-dd} s/d {trip.EndDate:yyyy-MM-dd}).");
                }

                var existingClaimedAmount = await context.Claims
                    .Where(c => c.TripProposalId == trip.Id &&
                                c.EmployeeId == claim.EmployeeId &&
                                c.Id != claim.Id &&
                                c.Status != ClaimStatus.Rejected)
                    .SumAsync(c => c.Amount, ct);

                if (existingClaimedAmount + request.Amount > participant.Amount)
                {
                    var remainingBudget = Math.Max(0, participant.Amount - existingClaimedAmount);
                    throw new BusinessRuleException(
                        $"Claim amount (Rp {request.Amount:N0}) exceeds remaining trip budget for this participant (Rp {remainingBudget:N0} out of allocated Rp {participant.Amount:N0}).");
                }

                claim.TripProposalId = trip.Id;
                claim.EmployeeBenefitId = null;
                break;

            case ClaimType.OperationalReimbursement:
                claim.EmployeeBenefitId = null;
                claim.TripProposalId = null;
                break;

            default:
                throw new BusinessRuleException("Invalid or unsupported claim type.");
        }

        // 5. Perbarui berkas kuitansi / nota jika ada file baru yang diunggah
        if (request.ReceiptFile != null && request.ReceiptFile.Length > 0)
        {
            claim.FilePath = await fileStorage.SaveFileAsync(request.ReceiptFile, "receipts", ct);
        }

        // 6. Update properti klaim & kembalikan status ke Submitted (Resubmitted).
        // Jika pengaju adalah Manager, tahap Manager tetap dilewati.
        claim.ClaimType = request.ClaimType;
        claim.Description = request.Description;
        claim.TransactionDate = request.TransactionDate;
        claim.Amount = request.Amount;

        var claimantIsManager = await context.HasRoleAsync(
            claim.EmployeeId, "Manager", ct);

        claim.Status = claimantIsManager
            ? ClaimStatus.UnderReview
            : ClaimStatus.Submitted;
        claim.SubmittedAt = DateTime.UtcNow;

        uow.Claims.Update(claim);
        await uow.SaveChangesAsync(ct);

        // 6b. Notifikasi email ke approver berikutnya setelah revisi.
        if (claimantIsManager)
        {
            var financeUsers = (await context.GetActiveByRolesAsync(ct, "Finance"))
                .Where(f => !string.Equals(
                    f.Email, claim.Employee.Email, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await Task.WhenAll(financeUsers.Select(fin =>
                emailService.SendClaimSubmittedToFinanceEmailAsync(
                    fin.Email,
                    fin.Name,
                    claim.Employee.FullName,
                    claim.Employee.Number,
                    claim.Employee.Department?.Name ?? "-",
                    claim.ClaimType.ToString(),
                    claim.Description,
                    claim.Amount,
                    claim.SubmittedAt,
                    claim.Id,
                    ct)));
        }
        else
        {
            var directManager = await context.GetDirectManagerAsync(
                claim.EmployeeId, ct);

            if (directManager != null)
            {
                await emailService.SendClaimSubmittedToManagerEmailAsync(
                    directManager.Email,
                    directManager.Name,
                    claim.Employee.FullName,
                    claim.Employee.Number,
                    claim.Employee.Department?.Name ?? "-",
                    claim.ClaimType.ToString(),
                    claim.Description,
                    claim.Amount,
                    claim.SubmittedAt,
                    claim.Id,
                    ct);
            }
        }

        // 7. Ambil riwayat approval untuk detail DTO
        var approvalHistory = await context.ClaimApprovals
            .AsNoTracking()
            .Where(ca => ca.ClaimId == claim.Id)
            .OrderBy(ca => ca.ApprovalLevel)
            .ThenBy(ca => ca.DecidedAt)
            .Select(ca => new ClaimApprovalHistoryDto
            {
                Id = ca.Id,
                ApproverId = ca.ApproverId,
                ApproverName = ca.ApproverName,
                Decision = ca.Decision,
                Notes = ca.Notes,
                ApprovalLevel = ca.ApprovalLevel,
                DecidedAt = ca.DecidedAt
            })
            .ToListAsync(ct);

        return new ClaimDetailDto
        {
            Id = claim.Id,
            ClaimType = claim.ClaimType,
            Description = claim.Description,
            TransactionDate = claim.TransactionDate,
            Amount = claim.Amount,
            SubmittedAt = claim.SubmittedAt,
            Status = claim.Status,
            FilePath = claim.FilePath,
            EmployeeId = claim.EmployeeId,
            EmployeeName = claim.Employee.FullName,
            EmployeeNumber = claim.Employee.Number,
            DepartmentName = claim.Employee.Department?.Name,
            EmployeeBenefitId = claim.EmployeeBenefitId,
            BenefitName = benefitName ?? claim.EmployeeBenefit?.Benefit?.Name,
            TripProposalId = claim.TripProposalId,
            ApprovalHistory = approvalHistory
        };
    }
}
