using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Constants;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Queries.GetTripProposalById;

public record GetTripProposalByIdQuery(
    Guid Id,
    Guid? EmployeeId = null,
    string[]? Roles = null
) : IRequest<TripProposalDetailDto>;

public class GetTripProposalByIdHandler(
    PayClaimDbContext context,
    ICurrentUser currentUser
) : IRequestHandler<GetTripProposalByIdQuery, TripProposalDetailDto>
{
    public async Task<TripProposalDetailDto> Handle(
        GetTripProposalByIdQuery query,
        CancellationToken ct = default)
    {
        var roles = query.Roles ?? currentUser.Roles;
        var employeeId = query.EmployeeId ?? currentUser.EmployeeId;

        bool isAdmin = roles.Contains(
            "Admin",
            StringComparer.OrdinalIgnoreCase);

        bool isManager = roles.Contains(
            "Manager",
            StringComparer.OrdinalIgnoreCase);

        bool isFinance = roles.Contains(
            "Finance",
            StringComparer.OrdinalIgnoreCase);

        bool isFinanceManager = roles.Contains(
            "Finance Manager",
            StringComparer.OrdinalIgnoreCase);

        if (!isAdmin &&
            (!employeeId.HasValue ||
             employeeId.Value == Guid.Empty))
        {
            throw new ForbiddenException(
                "Employee identity is required.");
        }

        var currentEmpId = employeeId ?? Guid.Empty;

        var accessInfo = await context.TripProposals
            .AsNoTracking()
            .Where(tp => tp.Id == query.Id)
            .Select(tp => new
            {
                tp.Status,

                // Semua peserta boleh melihat proposal, bukan PIC saja.
                IsParticipant = tp.Participants.Any(p =>
                    p.EmployeeId == currentEmpId),

                PicManagerId = tp.Participants
                    .Where(p => p.IsPic)
                    .Select(p => p.Employee.ManagerId)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (accessInfo == null)
        {
            throw new NotFoundException(
                $"Trip proposal with ID '{query.Id}' was not found.");
        }

        bool isDirectManager =
            isManager &&
            accessInfo.PicManagerId == currentEmpId;

        bool hasApproved = await context.TripApprovals
            .AsNoTracking()
            .AnyAsync(
                ta =>
                    ta.TripProposalId == query.Id &&
                    ta.ApproverId == currentEmpId,
                ct);

        bool isFinanceEligible =
            (isFinance || isFinanceManager) &&
            (
                accessInfo.Status == TripProposalStatus.UnderReview ||
                accessInfo.Status == TripProposalStatus.FinanceManagerReview ||
                accessInfo.Status == TripProposalStatus.Approved ||
                accessInfo.Status == TripProposalStatus.Completed ||
                await context.TripApprovals
                    .AsNoTracking()
                    .AnyAsync(
                        ta =>
                            ta.TripProposalId == query.Id &&
                            ta.ApprovalLevel >= 2,
                        ct)
            );

        bool hasAccess =
            isAdmin ||
            accessInfo.IsParticipant ||
            isDirectManager ||
            hasApproved ||
            isFinanceEligible;

        if (!hasAccess)
        {
            throw new ForbiddenException(
                "You are not allowed to access this trip proposal.");
        }

        var proposal = await context.TripProposals
            .AsNoTracking()
            .Where(tp => tp.Id == query.Id)
            .Select(tp => new TripProposalDetailDto
            {
                Id = tp.Id,
                Destination = tp.Destination,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,

                // Kolom TotalAmount Entity masih menyimpan biaya akomodasi.
                AccommodationAmount = tp.TotalAmount,

                // Total response = akomodasi + limit seluruh peserta.
                TotalAmount = tp.TotalAmount +
                        (tp.Participants.Sum(p => (decimal?)p.Amount) ?? 0m),

                FinanceLimit = TripProposalApprovalLimit.FinanceLimit,

                SubmittedAt = tp.SubmittedAt,
                Description = tp.Description,
                Status = tp.Status,
                FilePath = tp.FilePath,

                Participants = tp.Participants
                    .Select(p => new ParticipantItemDto
                    {
                        EmployeeId = p.EmployeeId,
                        EmployeeName = p.Employee.FullName,
                        EmployeeNumber = p.Employee.Number,
                        IsPic = p.IsPic,
                        Amount = p.Amount
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (proposal == null)
        {
            throw new NotFoundException(
                $"Trip proposal with ID '{query.Id}' was not found.");
        }

        var approvalHistory = await context.TripApprovals
            .AsNoTracking()
            .Where(ta => ta.TripProposalId == query.Id)
            .OrderBy(ta => ta.ApprovalLevel)
            .ThenBy(ta => ta.DecidedAt)
            .Select(ta => new TripApprovalHistoryDto
            {
                Id = ta.Id,
                ApproverId = ta.ApproverId,
                ApproverName = ta.ApproverName,
                Decision = ta.Decision,
                Notes = ta.Notes,
                ApprovalLevel = ta.ApprovalLevel,
                DecidedAt = ta.DecidedAt
            })
            .ToListAsync(ct);

        proposal.ApprovalHistory = approvalHistory;

        return proposal;
    }
}