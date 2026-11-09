using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.BenefitPlans.Commands;

public record CreateBenefitPlanCommand(UpsertBenefitPlanDto Request);

public class CreateBenefitPlanHandler(IUnitOfWork uow, PayClaimDbContext db)
{
    public async Task Handle(CreateBenefitPlanCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var position = await uow.Positions.GetByIdAsync(
            new object[] { request.PositionId }, ct);

        if (position == null)
            throw new NotFoundException("Position not found.");

        var benefit = await uow.Benefits.GetByIdAsync(
            new object[] { request.BenefitId }, ct);

        if (benefit == null)
            throw new NotFoundException("Benefit not found.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly startDate;
        DateOnly endDate;

        if (benefit.PeriodType == BenefitPeriodType.Monthly)
        {
            startDate = new DateOnly(today.Year, today.Month, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);
        }
        else // Yearly & OneTime
        {
            startDate = new DateOnly(today.Year, 1, 1);
            endDate = new DateOnly(today.Year, 12, 31);
        }

        // Cek apakah Admin memilih "ALL" (Semua Departemen)
        bool isAllDepartments = string.Equals(request.DepartmentId, "ALL", StringComparison.OrdinalIgnoreCase);

        List<Department> targetDepartments;
        if (isAllDepartments)
        {
            targetDepartments = await db.Departments.ToListAsync(ct);
        }
        else
        {
            var department = await uow.Departments.GetByIdAsync(
                new object[] { request.DepartmentId }, ct);

            if (department == null)
                throw new NotFoundException("Department not found.");

            targetDepartments = new List<Department> { department };
        }

        foreach (var dept in targetDepartments)
        {
            var existingPlan = await uow.BenefitPlans.GetByIdAsync(
                new object[]
                {
                    dept.Id,
                    request.BenefitId,
                    request.PositionId
                }, ct);

            if (existingPlan != null)
            {
                if (!isAllDepartments)
                {
                    throw new ConflictException($"Benefit plan for department {dept.Name} already exists.");
                }
                existingPlan.LimitAmount = request.LimitAmount;
                uow.BenefitPlans.Update(existingPlan);
            }
            else
            {
                var benefitPlan = new BenefitPlan
                {
                    DepartmentId = dept.Id,
                    BenefitId = request.BenefitId,
                    PositionId = request.PositionId,
                    LimitAmount = request.LimitAmount
                };
                await uow.BenefitPlans.AddAsync(benefitPlan, ct);
            }

            // Opsi 1: Auto-alokasi ke seluruh karyawan lama yang aktif pada departemen & jabatan ini
            var matchingEmployees = await db.Employees
                .Where(e => e.DepartmentId == dept.Id && e.PositionId == request.PositionId && e.IsActive)
                .ToListAsync(ct);

            foreach (var emp in matchingEmployees)
            {
                bool alreadyHasBenefit = await db.EmployeeBenefits
                    .AnyAsync(eb => eb.EmployeeId == emp.Id && eb.BenefitId == request.BenefitId, ct);

                if (!alreadyHasBenefit)
                {
                    var empBenefit = new EmployeeBenefit
                    {
                        Id = Guid.CreateVersion7(),
                        EmployeeId = emp.Id,
                        BenefitId = request.BenefitId,
                        Balance = request.LimitAmount,
                        StartDate = startDate,
                        EndDate = endDate
                    };
                    await db.EmployeeBenefits.AddAsync(empBenefit, ct);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
