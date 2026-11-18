using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.EmployeeBenefit;
using PayClaim.Entities;

namespace PayClaim.Features.EmployeeBenefits.Queries.GetMyBenefits;

public record GetMyBenefitsQuery(Guid? EmployeeId = null) : IRequest<List<MyBenefitSummaryDto>>;

public class GetMyBenefitsHandler(PayClaimDbContext db, ICurrentUser currentUser) : IRequestHandler<GetMyBenefitsQuery, List<MyBenefitSummaryDto>>
{
    public async Task<List<MyBenefitSummaryDto>> Handle(
        GetMyBenefitsQuery query,
        CancellationToken ct)
    {
        var targetEmployeeId = query.EmployeeId ?? currentUser.RequiredEmployeeId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == targetEmployeeId)
            .Select(e => new { e.DepartmentId, e.PositionId })
            .FirstOrDefaultAsync(ct);

        if (employee != null)
        {
            var plans = await db.BenefitPlans
                .AsNoTracking()
                .Include(bp => bp.Benefit)
                    .ThenInclude(b => b.Category)
                .Where(bp => (bp.DepartmentId == employee.DepartmentId || bp.DepartmentId == "ALL") &&
                             bp.PositionId == employee.PositionId)
                .OrderBy(bp => bp.DepartmentId == "ALL" ? 1 : 0)
                .ToListAsync(ct);

            var existingBenefits = await db.EmployeeBenefits
                .Include(eb => eb.Benefit)
                    .ThenInclude(b => b.Category)
                .Where(eb => eb.EmployeeId == targetEmployeeId)
                .ToListAsync(ct);

            bool hasChanges = false;
            var processedBenefitIds = new HashSet<Guid>();

            foreach (var plan in plans)
            {
                if (!processedBenefitIds.Add(plan.BenefitId))
                {
                    continue;
                }

                var existing = existingBenefits.FirstOrDefault(eb => eb.BenefitId == plan.BenefitId);
                if (existing == null)
                {
                    // Auto-enroll kuota baru untuk periode berjalan saat ini
                    var (startDate, endDate) = BenefitPeriodHelper.GetCurrentPeriod(plan.Benefit.PeriodType, today);
                    var newBenefit = new EmployeeBenefit
                    {
                        Id = Guid.CreateVersion7(),
                        EmployeeId = targetEmployeeId,
                        BenefitId = plan.BenefitId,
                        Balance = plan.LimitAmount,
                        StartDate = startDate,
                        EndDate = endDate,
                        Benefit = plan.Benefit
                    };

                    await db.EmployeeBenefits.AddAsync(newBenefit, ct);
                    existingBenefits.Add(newBenefit);
                    hasChanges = true;
                }
                else
                {
                    // Periksa dan perbarui jika periode sebelumnya sudah kadaluarsa (renewal)
                    if (BenefitPeriodHelper.TryRenewIfExpired(existing, plan.Benefit.PeriodType, plan.LimitAmount, today))
                    {
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    // Request paralel baru saja menyisipkan (EmployeeId, BenefitId) yang sama
                    db.ChangeTracker.Clear();
                    existingBenefits = await db.EmployeeBenefits
                        .AsNoTracking()
                        .Include(eb => eb.Benefit)
                            .ThenInclude(b => b.Category)
                        .Where(eb => eb.EmployeeId == targetEmployeeId)
                        .ToListAsync(ct);
                }
            }

            return existingBenefits
                .OrderBy(eb => eb.Benefit.Name)
                .Select(eb => new MyBenefitSummaryDto
                {
                    EmployeeBenefitId = eb.Id,
                    BenefitId = eb.BenefitId,
                    BenefitName = eb.Benefit.Name,
                    CategoryName = eb.Benefit.Category?.Name ?? string.Empty,
                    PeriodType = eb.Benefit.PeriodType,
                    Balance = eb.Balance,
                    StartDate = eb.StartDate,
                    EndDate = eb.EndDate,
                    IsActive = eb.StartDate <= today && eb.EndDate >= today
                })
                .ToList();
        }

        return await db.EmployeeBenefits
            .AsNoTracking()
            .Where(eb => eb.EmployeeId == targetEmployeeId)
            .OrderBy(eb => eb.Benefit.Name)
            .Select(eb => new MyBenefitSummaryDto
            {
                EmployeeBenefitId = eb.Id,
                BenefitId = eb.BenefitId,
                BenefitName = eb.Benefit.Name,
                CategoryName = eb.Benefit.Category.Name,
                PeriodType = eb.Benefit.PeriodType,
                Balance = eb.Balance,
                StartDate = eb.StartDate,
                EndDate = eb.EndDate,
                IsActive = eb.StartDate <= today && eb.EndDate >= today
            })
            .ToListAsync(ct);
    }
}
