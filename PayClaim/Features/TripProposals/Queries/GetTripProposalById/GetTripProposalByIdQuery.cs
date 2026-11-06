using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Queries.GetTripProposalById;

public record GetTripProposalByIdQuery(
    Guid Id,
    Guid? EmployeeId,
    string[] Roles);

public class GetTripProposalByIdHandler(PayClaimDbContext context)
{
    public async Task<TripProposalDetailDto> Handle(
        GetTripProposalByIdQuery query,
        CancellationToken ct = default)
    {
        var roles = query.Roles ?? Array.Empty<string>();

        bool isAdmin = roles.Contains(
            "Admin", StringComparer.OrdinalIgnoreCase);

        bool isManager = roles.Contains(
            "Manager", StringComparer.OrdinalIgnoreCase);

        bool isFinance = roles.Contains(
            "Finance", StringComparer.OrdinalIgnoreCase);

        bool isFinanceManager = roles.Contains(
            "Finance Manager", StringComparer.OrdinalIgnoreCase);

        if (!isAdmin &&
            (!query.EmployeeId.HasValue ||
             query.EmployeeId.Value == Guid.Empty))
        {
            throw new ForbiddenException(
                "Employee identity is required.");
        }

        var employeeId = query.EmployeeId ?? Guid.Empty;

        var accessInfo = await context.TripProposals
            .AsNoTracking()
            .Where(tp => tp.Id == query.Id)
            .Select(tp => new
            {
                tp.Status,

                // PIC juga merupakan participant.
                IsParticipant = tp.Participants.Any(p =>
                    p.EmployeeId == employeeId),

                // Manager langsung dari PIC.
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
            accessInfo.Status == TripProposalStatus.Submitted &&
            accessInfo.PicManagerId == employeeId;

        bool isFinanceReviewer =
            isFinance &&
            accessInfo.Status == TripProposalStatus.UnderReview;

        bool isFinanceManagerReviewer =
            isFinanceManager &&
            accessInfo.Status == TripProposalStatus.FinanceManagerReview;

        if (!isAdmin &&
            !accessInfo.IsParticipant &&
            !isDirectManager &&
            !isFinanceReviewer &&
            !isFinanceManagerReviewer)
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
                TotalAmount = tp.TotalAmount,
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