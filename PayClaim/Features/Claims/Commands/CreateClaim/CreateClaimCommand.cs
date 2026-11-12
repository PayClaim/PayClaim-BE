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

namespace PayClaim.Features.Claims.Commands.CreateClaim;

public record CreateClaimCommand(
    CreateClaimDto Request,
    Guid? SubmitterEmployeeId = null,
    IReadOnlyCollection<string>? Roles = null) : IRequest<ClaimDetailDto>;

public class CreateClaimHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IFileStorageService fileStorage,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<CreateClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> Handle(CreateClaimCommand command, CancellationToken ct = default)
    {
        var submitterId = command.SubmitterEmployeeId ?? currentUser.RequiredEmployeeId;
        var roles = command.Roles ?? currentUser.Roles;

        // 0. Cegah role Admin membuat claim reimbursement
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                "Akun Administrator tidak diperbolehkan mengajukan klaim reimbursement.");
        }

        var request = command.Request;

        // 1. Ambil Employee beserta Manager dan Departemen berdasarkan EmployeeId dari JWT
        var employee = await context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(
                e => e.Id == submitterId,
                ct);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
        }

        // Cegah posisi Director membuat claim reimbursement
        if (string.Equals(employee.PositionId, "DIR", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                "Posisi Director tidak diperbolehkan mengajukan klaim reimbursement.");
        }

        if (!employee.IsActive)
        {
            throw new BusinessRuleException(
                "Inactive employees cannot submit claims.");
        }

        string? benefitName = null;

        // 2. Validasi berdasarkan jenis Claim
        switch (request.ClaimType)
        {
            case ClaimType.Benefit:

                if (!request.EmployeeBenefitId.HasValue)
                {
                    throw new BusinessRuleException(
                        "EmployeeBenefitId is required for Benefit claims.");
                }

                var empBenefit = await context.EmployeeBenefits
                    .Include(eb => eb.Benefit)
                    .FirstOrDefaultAsync(
                        eb => eb.Id == request.EmployeeBenefitId.Value,
                        ct);

                if (empBenefit == null)
                {
                    throw new NotFoundException(
                        $"Employee benefit quota with ID '{request.EmployeeBenefitId.Value}' was not found.");
                }

                // Pastikan alokasi Benefit memang milik pengaju
                if (empBenefit.EmployeeId != employee.Id)
                {
                    throw new BusinessRuleException(
                        "The selected benefit quota does not belong to this employee.");
                }

                // Cek dan lakukan auto-renewal jika periode benefit sudah kadaluarsa
                var plan = await context.BenefitPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(bp =>
                        bp.DepartmentId == employee.DepartmentId &&
                        bp.PositionId == employee.PositionId &&
                        bp.BenefitId == empBenefit.BenefitId, ct);

                if (plan != null && BenefitPeriodHelper.TryRenewIfExpired(empBenefit, empBenefit.Benefit.PeriodType, plan.LimitAmount))
                {
                    uow.EmployeeBenefits.Update(empBenefit);
                    await uow.SaveChangesAsync(ct);
                }

                // Pastikan tanggal transaksi masuk periode Benefit
                if (request.TransactionDate < empBenefit.StartDate ||
                    request.TransactionDate > empBenefit.EndDate)
                {
                    throw new BusinessRuleException(
                        $"Transaction date must be within the active benefit quota period ({empBenefit.StartDate:yyyy-MM-dd} to {empBenefit.EndDate:yyyy-MM-dd}).");
                }

                // Hitung klaim benefit yang sedang berjalan (Submitted / UnderReview / Approved)
                var pendingBenefitClaims = await context.Claims
                    .Where(c => c.EmployeeBenefitId == empBenefit.Id &&
                                c.EmployeeId == employee.Id &&
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

                request.TripProposalId = null;

                break;

            case ClaimType.BusinessTripReimbursement:

                if (!request.TripProposalId.HasValue)
                {
                    throw new BusinessRuleException(
                        "TripProposalId is required for Business Trip Reimbursement claims.");
                }

                var trip = await context.TripProposals
                    .Include(tp => tp.Participants)
                    .FirstOrDefaultAsync(
                        tp => tp.Id == request.TripProposalId.Value,
                        ct);

                if (trip == null)
                {
                    throw new NotFoundException(
                        $"Business trip proposal with ID '{request.TripProposalId.Value}' was not found.");
                }

                if (trip.Status != TripProposalStatus.Approved &&
                    trip.Status != TripProposalStatus.Completed)
                {
                    throw new BusinessRuleException(
                        $"Cannot submit reimbursement for a trip proposal that is in '{trip.Status}' status. The trip proposal must be 'Approved' first.");
                }

                // Pastikan pengaju merupakan peserta perjalanan
                var participant = trip.Participants
                    .FirstOrDefault(p => p.EmployeeId == employee.Id);

                if (participant == null)
                {
                    throw new BusinessRuleException(
                        "This employee is not registered as a participant in the specified business trip proposal.");
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
                                c.EmployeeId == employee.Id &&
                                c.Status != ClaimStatus.Rejected)
                    .SumAsync(c => c.Amount, ct);

                if (existingClaimedAmount + request.Amount > participant.Amount)
                {
                    var remainingBudget = Math.Max(0, participant.Amount - existingClaimedAmount);
                    throw new BusinessRuleException(
                        $"Claim amount (Rp {request.Amount:N0}) exceeds remaining trip budget for this participant (Rp {remainingBudget:N0} out of allocated Rp {participant.Amount:N0}).");
                }

                request.EmployeeBenefitId = null;

                break;

            case ClaimType.OperationalReimbursement:

                request.EmployeeBenefitId = null;
                request.TripProposalId = null;

                break;

            default:

                throw new BusinessRuleException(
                    "Invalid or unsupported claim type.");
        }

        // 3. Simpan file bukti pembayaran jika tersedia
        var filePath = string.Empty;

        if (request.ReceiptFile != null &&
            request.ReceiptFile.Length > 0)
        {
            filePath = await fileStorage.SaveFileAsync(
                request.ReceiptFile,
                "receipts",
                ct);
        }

        // 4. Buat Entity Claim
        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimType = request.ClaimType,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            Amount = request.Amount,
            SubmittedAt = DateTime.UtcNow.AddHours(7),
            Status = ClaimStatus.Submitted,
            FilePath = filePath,

            // Selalu menggunakan EmployeeId dari JWT
            EmployeeId = employee.Id,

            EmployeeBenefitId = request.EmployeeBenefitId,
            TripProposalId = request.TripProposalId
        };

        await uow.Claims.AddAsync(claim, ct);

        await uow.SaveChangesAsync(ct);

        // 5. Kirim notifikasi email ke Atasan Langsung (Manager) jika ada
        if (employee.Manager != null && !string.IsNullOrWhiteSpace(employee.Manager.Email))
        {
            await emailService.SendClaimSubmittedToManagerEmailAsync(
                employee.Manager.Email,
                employee.Manager.FullName,
                employee.FullName,
                employee.Number,
                employee.Department?.Name ?? "-",
                claim.ClaimType.ToString(),
                claim.Description,
                claim.Amount,
                claim.SubmittedAt,
                claim.Id,
                ct);
        }

        // 6. Buat Response
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

            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            EmployeeNumber = employee.Number,
            DepartmentName = employee.Department?.Name,

            EmployeeBenefitId = claim.EmployeeBenefitId,
            BenefitName = benefitName,

            TripProposalId = claim.TripProposalId,

            ApprovalHistory = new List<ClaimApprovalHistoryDto>()
        };
    }
}
