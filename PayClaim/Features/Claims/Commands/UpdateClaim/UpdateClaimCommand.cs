using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Claim;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Commands.UpdateClaim;

public record UpdateClaimCommand(Guid ClaimId, CreateClaimDto Request, Guid? SubmitterEmployeeId = null);

public class UpdateClaimHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public UpdateClaimHandler(IUnitOfWork uow, PayClaimDbContext context, IFileStorageService fileStorage)
    {
        _uow = uow;
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<ClaimDetailDto> Handle(UpdateClaimCommand command, CancellationToken ct = default)
    {
        // 1. Ambil data klaim yang akan diubah
        var claim = await _context.Claims
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
        if (command.SubmitterEmployeeId.HasValue && claim.EmployeeId != command.SubmitterEmployeeId.Value)
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

                var empBenefit = await _context.EmployeeBenefits
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

                if (request.TransactionDate < empBenefit.StartDate || request.TransactionDate > empBenefit.EndDate)
                {
                    throw new BusinessRuleException(
                        $"Transaction date must be within the active benefit quota period ({empBenefit.StartDate:yyyy-MM-dd} to {empBenefit.EndDate:yyyy-MM-dd}).");
                }

                if (empBenefit.Balance < request.Amount)
                {
                    throw new BusinessRuleException(
                        $"Insufficient benefit quota balance. Current balance is Rp {empBenefit.Balance:N0}, but claim amount requested is Rp {request.Amount:N0}.");
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

                var trip = await _context.TripProposals
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

                var isParticipant = trip.Participants.Any(p => p.EmployeeId == claim.EmployeeId);
                if (!isParticipant)
                {
                    throw new BusinessRuleException("This employee is not registered as a participant in the specified business trip proposal.");
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
            claim.FilePath = await _fileStorage.SaveFileAsync(request.ReceiptFile, "receipts", ct);
        }

        // 6. Update properti klaim & kembalikan status ke Submitted (Resubmitted)
        claim.ClaimType = request.ClaimType;
        claim.Description = request.Description;
        claim.TransactionDate = request.TransactionDate;
        claim.Amount = request.Amount;
        claim.Status = ClaimStatus.Submitted;
        claim.SubmittedAt = DateTime.UtcNow;

        _uow.Claims.Update(claim);
        await _uow.SaveChangesAsync(ct);

        // 7. Ambil riwayat approval untuk detail DTO
        var approvalHistory = await _context.ClaimApprovals
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
