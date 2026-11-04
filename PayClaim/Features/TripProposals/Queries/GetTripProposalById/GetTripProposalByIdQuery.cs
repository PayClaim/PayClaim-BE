using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.TripProposal;

namespace PayClaim.Features.TripProposals.Queries.GetTripProposalById;

public record GetTripProposalByIdQuery(Guid Id);

public class GetTripProposalByIdHandler
{
    private readonly PayClaimDbContext _context;

    public GetTripProposalByIdHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<TripProposalDetailDto> Handle(GetTripProposalByIdQuery query, CancellationToken ct = default)
    {
        var proposal = await _context.TripProposals
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
                Participants = tp.Participants.Select(p => new ParticipantItemDto
                {
                    EmployeeId = p.EmployeeId,
                    EmployeeName = p.Employee.FullName,
                    EmployeeNumber = p.Employee.Number,
                    IsPic = p.IsPic,
                    Amount = p.Amount
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (proposal == null)
        {
            throw new NotFoundException($"Trip proposal with ID '{query.Id}' was not found.");
        }

        // Ambil riwayat persetujuan terkait
        var approvalHistory = await _context.TripApprovals
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
