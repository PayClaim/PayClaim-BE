using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Claim;
using PayClaim.DTOs.Common;

namespace PayClaim.Features.Claims.Queries.GetClaims;

public record GetClaimsQuery(ClaimFilterQuery Filter);

public class GetClaimsHandler
{
    private readonly PayClaimDbContext _context;

    public GetClaimsHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<ClaimListDto>> Handle(GetClaimsQuery query, CancellationToken ct = default)
    {
        var filter = query.Filter;
        var dbQuery = _context.Claims.AsNoTracking().AsQueryable();

        // 1. Filter Berdasarkan Jenis Klaim
        if (filter.ClaimType.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.ClaimType == filter.ClaimType.Value);
        }

        // 2. Filter Berdasarkan Status Klaim
        if (filter.Status.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Status == filter.Status.Value);
        }

        // 3. Filter Berdasarkan Karyawan Pengaju
        if (filter.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.EmployeeId == filter.EmployeeId.Value);
        }

        // 4. Filter Rentang Tanggal Transaksi
        if (filter.StartDate.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.TransactionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.TransactionDate <= filter.EndDate.Value);
        }

        // 5. Hitung Total Data Sesuai Filter
        var totalCount = await dbQuery.CountAsync(ct);

        // 6. Pagination & Proyeksi Langsung ke DTO
        var items = await dbQuery
            .OrderByDescending(c => c.SubmittedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new ClaimListDto
            {
                Id = c.Id,
                ClaimType = c.ClaimType,
                Description = c.Description,
                TransactionDate = c.TransactionDate,
                Amount = c.Amount,
                SubmittedAt = c.SubmittedAt,
                Status = c.Status,
                EmployeeId = c.EmployeeId,
                EmployeeName = c.Employee.FullName,
                BenefitName = c.EmployeeBenefit != null ? c.EmployeeBenefit.Benefit.Name : null,
                TripProposalDescription = c.TripProposal != null ? c.TripProposal.Description : null,
                FilePath = c.FilePath
            })
            .ToListAsync(ct);

        return new PagedResponse<ClaimListDto>(items, filter.PageNumber, filter.PageSize, totalCount);
    }
}
