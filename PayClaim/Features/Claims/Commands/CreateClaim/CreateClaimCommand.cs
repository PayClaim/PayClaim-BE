using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Claim;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Commands.CreateClaim;

//public record CreateClaimCommand(CreateClaimDto Request, Guid? SubmitterEmployeeId = null);
public record CreateClaimCommand( CreateClaimDto Request, Guid SubmitterEmployeeId);

public class CreateClaimHandler(IUnitOfWork uow, PayClaimDbContext context, IFileStorageService fileStorage)
{

    public async Task<ClaimDetailDto> Handle(CreateClaimCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        // 1. Ambil Employee berdasarkan EmployeeId dari JWT
        var employee = await context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(
                e => e.Id == command.SubmitterEmployeeId,
                ct);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
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

                // Pastikan tanggal transaksi masuk periode Benefit
                if (request.TransactionDate < empBenefit.StartDate ||
                    request.TransactionDate > empBenefit.EndDate)
                {
                    throw new BusinessRuleException(
                        $"Transaction date must be within the active benefit quota period ({empBenefit.StartDate:yyyy-MM-dd} to {empBenefit.EndDate:yyyy-MM-dd}).");
                }

                // Pastikan saldo mencukupi
                if (empBenefit.Balance < request.Amount)
                {
                    throw new BusinessRuleException(
                        $"Insufficient benefit quota balance. Current balance is Rp {empBenefit.Balance:N0}, but claim amount requested is Rp {request.Amount:N0}.");
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
                var isParticipant = trip.Participants
                    .Any(p => p.EmployeeId == employee.Id);

                if (!isParticipant)
                {
                    throw new BusinessRuleException(
                        "This employee is not registered as a participant in the specified business trip proposal.");
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
            SubmittedAt = DateTime.UtcNow,
            Status = ClaimStatus.Submitted,
            FilePath = filePath,

            // Selalu menggunakan EmployeeId dari JWT
            EmployeeId = employee.Id,

            EmployeeBenefitId = request.EmployeeBenefitId,
            TripProposalId = request.TripProposalId
        };

        await uow.Claims.AddAsync(claim, ct);

        await uow.SaveChangesAsync(ct);

        // 5. Buat Response
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

