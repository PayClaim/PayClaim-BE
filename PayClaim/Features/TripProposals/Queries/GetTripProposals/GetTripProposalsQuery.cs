using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.TripProposal;

namespace PayClaim.Features.TripProposals.Queries.GetTripProposals;

public record GetTripProposalsQuery(TripProposalFilterQuery Filter);

public class GetTripProposalsHandler
{
    private readonly PayClaimDbContext _context;

    public GetTripProposalsHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<TripProposalListDto>> Handle(GetTripProposalsQuery query, CancellationToken ct = default)
    {
        var filter = query.Filter;
        var dbQuery = _context.TripProposals.AsNoTracking().AsQueryable();

        // 1. Filter Pencarian Tujuan
        if (!string.IsNullOrWhiteSpace(filter.Destination))
        {
            dbQuery = dbQuery.Where(tp => tp.Destination.Contains(filter.Destination));
        }

        // 2. Filter Status Proposal
        if (filter.Status.HasValue)
        {
            dbQuery = dbQuery.Where(tp => tp.Status == filter.Status.Value);
        }

        // 3. Filter Rentang Tanggal Mulai
        if (filter.StartDateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(tp => tp.StartDate >= filter.StartDateFrom.Value);
        }

        if (filter.StartDateTo.HasValue)
        {
            dbQuery = dbQuery.Where(tp => tp.StartDate <= filter.StartDateTo.Value);
        }

        // 4. Filter Karyawan Peserta / PIC
        if (filter.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(tp => tp.Participants.Any(p => p.EmployeeId == filter.EmployeeId.Value));
        }

        // 5. Hitung Total Data Sesuai Filter
        var totalCount = await dbQuery.CountAsync(ct);

        // 6. Paging & Proyeksi Langsung ke DTO
        var items = await dbQuery
            .OrderByDescending(tp => tp.SubmittedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(tp => new TripProposalListDto
            {
                Id = tp.Id,
                Destination = tp.Destination,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                TotalAmount = tp.TotalAmount,
                SubmittedAt = tp.SubmittedAt,
                Status = tp.Status,
                PicName = tp.Participants
                    .Where(p => p.IsPic)
                    .Select(p => p.Employee.FullName)
                    .FirstOrDefault(),
                ParticipantCount = tp.Participants.Count,
                FilePath = tp.FilePath
            })
            .ToListAsync(ct);

        return new PagedResponse<TripProposalListDto>(items, filter.PageNumber, filter.PageSize, totalCount);
    }
}
