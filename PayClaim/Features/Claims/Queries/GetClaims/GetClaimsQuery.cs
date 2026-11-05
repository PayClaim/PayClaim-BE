using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Claim;
using PayClaim.DTOs.Common;
using PayClaim.Common.Exceptions;

namespace PayClaim.Features.Claims.Queries.GetClaims;

public record GetClaimsQuery(
    ClaimFilterQuery Filter,
    Guid? RequesterEmployeeId,
    IReadOnlyCollection<string> RequesterRoles,
    bool IsMyClaims = false);

public class GetClaimsHandler
{
    private readonly PayClaimDbContext _context;

    public GetClaimsHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<ClaimListDto>> Handle(
    GetClaimsQuery query,
    CancellationToken ct = default)
    {
        var filter = query.Filter;

        var dbQuery = _context.Claims
            .AsNoTracking()
            .AsQueryable();

        var isAdmin = query.RequesterRoles
            .Contains("Admin", StringComparer.OrdinalIgnoreCase);

        var isFinance = query.RequesterRoles
            .Contains("Finance", StringComparer.OrdinalIgnoreCase);

        var isManager = query.RequesterRoles
            .Contains("Manager", StringComparer.OrdinalIgnoreCase);

        // 1. Tentukan data yang boleh diakses
        if (query.IsMyClaims)
        {
            if (!query.RequesterEmployeeId.HasValue)
            {
                throw new ForbiddenException(
                    "Employee identity is required to access your claims.");
            }

            // Abaikan EmployeeId dari query parameter.
            // Gunakan EmployeeId dari JWT.
            filter.EmployeeId = query.RequesterEmployeeId.Value;
        }
        else if (isAdmin || isFinance)
        {
            // Admin dan Finance dapat melihat seluruh Claim.
            // Filter tambahan tetap boleh digunakan.
        }
        else if (isManager)
        {
            if (!query.RequesterEmployeeId.HasValue)
            {
                throw new ForbiddenException(
                    "Employee identity is required for Manager access.");
            }

            var managerId = query.RequesterEmployeeId.Value;

            // Manager dapat melihat Claim sendiri dan Claim bawahannya.
            dbQuery = dbQuery.Where(c =>
                c.EmployeeId == managerId ||
                c.Employee.ManagerId == managerId);
        }
        else
        {
            throw new ForbiddenException(
                "You are not allowed to access all claims.");
        }

        // 2. Filter berdasarkan jenis Claim
        if (filter.ClaimType.HasValue)
        {
            dbQuery = dbQuery.Where(
                c => c.ClaimType == filter.ClaimType.Value);
        }

        // 3. Filter berdasarkan status Claim
        if (filter.Status.HasValue)
        {
            dbQuery = dbQuery.Where(
                c => c.Status == filter.Status.Value);
        }

        // 4. Filter berdasarkan EmployeeId
        if (filter.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(
                c => c.EmployeeId == filter.EmployeeId.Value);
        }

        // 5. Filter rentang tanggal transaksi
        if (filter.StartDate.HasValue)
        {
            dbQuery = dbQuery.Where(
                c => c.TransactionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            dbQuery = dbQuery.Where(
                c => c.TransactionDate <= filter.EndDate.Value);
        }

        // 6. Hitung total data
        var totalCount = await dbQuery.CountAsync(ct);

        // 7. Pagination dan proyeksi DTO
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
                BenefitName = c.EmployeeBenefit != null
                    ? c.EmployeeBenefit.Benefit.Name
                    : null,
                TripProposalDescription = c.TripProposal != null
                    ? c.TripProposal.Description
                    : null,
                FilePath = c.FilePath
            })
            .ToListAsync(ct);

        return new PagedResponse<ClaimListDto>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }
}
