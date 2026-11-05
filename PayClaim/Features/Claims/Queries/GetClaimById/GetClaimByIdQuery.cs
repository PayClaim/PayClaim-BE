using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Claim;

namespace PayClaim.Features.Claims.Queries.GetClaimById;

public record GetClaimByIdQuery(Guid Id);

public class GetClaimByIdHandler
{
    private readonly PayClaimDbContext _context;

    public GetClaimByIdHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<ClaimDetailDto> Handle(GetClaimByIdQuery query, CancellationToken ct = default)
    {
        var claim = await _context.Claims
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new ClaimDetailDto
            {
                Id = c.Id,
                ClaimType = c.ClaimType,
                Description = c.Description,
                TransactionDate = c.TransactionDate,
                Amount = c.Amount,
                SubmittedAt = c.SubmittedAt,
                Status = c.Status,
                FilePath = c.FilePath,
                EmployeeId = c.EmployeeId,
                EmployeeName = c.Employee.FullName,
                EmployeeNumber = c.Employee.Number,
                DepartmentName = c.Employee.Department.Name,
                EmployeeBenefitId = c.EmployeeBenefitId,
                BenefitName = c.EmployeeBenefit != null ? c.EmployeeBenefit.Benefit.Name : null,
                TripProposalId = c.TripProposalId
            })
            .FirstOrDefaultAsync(ct);

        if (claim == null)
        {
            throw new NotFoundException($"Claim with ID '{query.Id}' was not found.");
        }

        // Ambil riwayat keputusan approval terkait klaim ini
        var approvalHistory = await _context.ClaimApprovals
            .AsNoTracking()
            .Where(ca => ca.ClaimId == query.Id)
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

        claim.ApprovalHistory = approvalHistory;
        return claim;
    }
}
