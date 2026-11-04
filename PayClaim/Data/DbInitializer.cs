using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(PayClaimDbContext context)
    {
        // 1. Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Id = Guid.Parse("00000001-0000-4000-8000-000000000001"), Name = "Admin", Description = "HR & System Administrator" },
                new() { Id = Guid.Parse("00000002-0000-4000-8000-000000000002"), Name = "Employee", Description = "General Employee" },
                new() { Id = Guid.Parse("00000003-0000-4000-8000-000000000003"), Name = "Manager", Description = "Department / Direct Manager" },
                new() { Id = Guid.Parse("00000004-0000-4000-8000-000000000004"), Name = "Finance", Description = "Finance Officer" },
                new() { Id = Guid.Parse("00000005-0000-4000-8000-000000000005"), Name = "Finance Manager", Description = "Finance Department Head" }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // 2. Seed Departments
        if (!await context.Departments.AnyAsync())
        {
            var hq = new Department { Id = "DEPT-HQ", Name = "Headquarter", ParentDepartmentId = null };
            await context.Departments.AddAsync(hq);
            await context.SaveChangesAsync();

            var subDepts = new List<Department>
            {
                new() { Id = "DEPT-ENG", Name = "Engineering", ParentDepartmentId = "DEPT-HQ" },
                new() { Id = "DEPT-FIN", Name = "Finance & Accounting", ParentDepartmentId = "DEPT-HQ" },
                new() { Id = "DEPT-HR", Name = "Human Resources", ParentDepartmentId = "DEPT-HQ" }
            };
            await context.Departments.AddRangeAsync(subDepts);
            await context.SaveChangesAsync();
        }

        // 3. Seed Positions
        if (!await context.Positions.AnyAsync())
        {
            var positions = new List<Position>
            {
                new() { Id = "POS-DIR", Title = "Director" },
                new() { Id = "POS-MGR-ENG", Title = "Engineering Manager" },
                new() { Id = "POS-MGR-FIN", Title = "Finance Manager" },
                new() { Id = "POS-ENG-L5", Title = "Staff Software Engineer" },
                new() { Id = "POS-ENG-L3", Title = "Junior Software Engineer" },
                new() { Id = "POS-FIN-L3", Title = "Finance Specialist" }
            };
            await context.Positions.AddRangeAsync(positions);
            await context.SaveChangesAsync();
        }

        // 4. Seed Employees & Accounts
        if (!await context.Employees.AnyAsync())
        {
            var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            // Admin
            var adminEmpId = Guid.Parse("10100000-88a1-4b11-9c01-000000000000");
            var adminEmp = new Employee
            {
                Id = adminEmpId,
                Number = "EMP-2026-0001",
                FullName = "Rina Wijaya",
                Email = "admin@perusahaan.co.id",
                IsActive = true,
                DepartmentId = "DEPT-HR",
                PositionId = "POS-DIR",
                ManagerId = null
            };
            var adminAcc = new Account
            {
                Id = Guid.Parse("10100000-1111-4a22-8b01-000000000000"),
                Username = "admin",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                EmployeeId = adminEmpId
            };
            var adminRole = new AccountRole
            {
                AccountId = adminAcc.Id,
                RoleId = Guid.Parse("00000001-0000-4000-8000-000000000001"), // Admin
                AssignedAt = DateTime.UtcNow
            };

            // Manager
            var managerEmpId = Guid.Parse("20200000-88a1-4b11-9c01-000000000099");
            var managerEmp = new Employee
            {
                Id = managerEmpId,
                Number = "EMP-2026-0099",
                FullName = "Andi Pratama",
                Email = "andi.pratama@perusahaan.co.id",
                IsActive = true,
                DepartmentId = "DEPT-ENG",
                PositionId = "POS-MGR-ENG",
                ManagerId = null
            };
            var managerAcc = new Account
            {
                Id = Guid.Parse("20200000-1111-4a22-8b01-000000000099"),
                Username = "andi.pratama",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                EmployeeId = managerEmpId
            };
            var managerRoles = new List<AccountRole>
            {
                new() { AccountId = managerAcc.Id, RoleId = Guid.Parse("00000003-0000-4000-8000-000000000003"), AssignedAt = DateTime.UtcNow }, // Manager
                new() { AccountId = managerAcc.Id, RoleId = Guid.Parse("00000002-0000-4000-8000-000000000002"), AssignedAt = DateTime.UtcNow }  // Employee
            };

            // Employee: Budi Santoso (Main Test Account)
            var budiEmpId = Guid.Parse("10100000-88a1-4b11-9c01-000000000001");
            var budiEmp = new Employee
            {
                Id = budiEmpId,
                Number = "EMP-2026-0142",
                FullName = "Budi Santoso",
                Email = "budi.santoso@perusahaan.co.id",
                IsActive = true,
                DepartmentId = "DEPT-ENG",
                PositionId = "POS-ENG-L5",
                ManagerId = managerEmpId
            };
            var budiAcc = new Account
            {
                Id = Guid.Parse("10100000-1111-4a22-8b01-000000000001"),
                Username = "budi.santoso",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                EmployeeId = budiEmpId
            };
            var budiRole = new AccountRole
            {
                AccountId = budiAcc.Id,
                RoleId = Guid.Parse("00000002-0000-4000-8000-000000000002"), // Employee
                AssignedAt = DateTime.UtcNow
            };

            // Finance Manager: Dewi Lestari
            var financeEmpId = Guid.Parse("30300000-88a1-4b11-9c01-000000000088");
            var financeEmp = new Employee
            {
                Id = financeEmpId,
                Number = "EMP-2026-0088",
                FullName = "Dewi Lestari",
                Email = "dewi.lestari@perusahaan.co.id",
                IsActive = true,
                DepartmentId = "DEPT-FIN",
                PositionId = "POS-MGR-FIN",
                ManagerId = null
            };
            var financeAcc = new Account
            {
                Id = Guid.Parse("30300000-1111-4a22-8b01-000000000088"),
                Username = "dewi.lestari",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                EmployeeId = financeEmpId
            };
            var financeRoles = new List<AccountRole>
            {
                new() { AccountId = financeAcc.Id, RoleId = Guid.Parse("00000005-0000-4000-8000-000000000005"), AssignedAt = DateTime.UtcNow }, // Finance Manager
                new() { AccountId = financeAcc.Id, RoleId = Guid.Parse("00000004-0000-4000-8000-000000000004"), AssignedAt = DateTime.UtcNow }, // Finance
                new() { AccountId = financeAcc.Id, RoleId = Guid.Parse("00000002-0000-4000-8000-000000000002"), AssignedAt = DateTime.UtcNow }  // Employee
            };

            await context.Employees.AddRangeAsync(adminEmp, managerEmp, budiEmp, financeEmp);
            await context.Accounts.AddRangeAsync(adminAcc, managerAcc, budiAcc, financeAcc);
            await context.AccountRoles.AddRangeAsync(adminRole, budiRole);
            await context.AccountRoles.AddRangeAsync(managerRoles);
            await context.AccountRoles.AddRangeAsync(financeRoles);
            await context.SaveChangesAsync();
        }

        // 5. Seed Benefit Categories
        if (!await context.BenefitCategories.AnyAsync())
        {
            var categories = new List<BenefitCategory>
            {
                new() { Name = "Kesehatan & Medis" },
                new() { Name = "Kesejahteraan & Kebugaran" },
                new() { Name = "Operasional & Perjalanan Dinas" }
            };
            await context.BenefitCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 6. Seed Benefits
        if (!await context.Benefits.AnyAsync())
        {
            var medicalCategory = await context.BenefitCategories.FirstAsync(c => c.Name.Contains("Kesehatan"));
            var wellnessCategory = await context.BenefitCategories.FirstAsync(c => c.Name.Contains("Kesejahteraan"));

            var benefits = new List<Benefit>
            {
                new()
                {
                    Id = Guid.Parse("b1010000-2222-4a11-8a01-000000000001"),
                    Name = "Tunjangan Rawat Jalan & Medis",
                    Description = "Plafon reimbursement rawat jalan, resep dokter, dan obat-obatan.",
                    PeriodType = BenefitPeriodType.Yearly,
                    CategoryId = medicalCategory.Id
                },
                new()
                {
                    Id = Guid.Parse("b1010000-2222-4a11-8a01-000000000002"),
                    Name = "Tunjangan Kacamata & Optik",
                    Description = "Reimbursement lensa dan frame kacamata resep dokter.",
                    PeriodType = BenefitPeriodType.Yearly,
                    CategoryId = medicalCategory.Id
                },
                new()
                {
                    Id = Guid.Parse("b1010000-2222-4a11-8a01-000000000003"),
                    Name = "Tunjangan Kebugaran & Gym",
                    Description = "Membership pusat kebugaran bulanan.",
                    PeriodType = BenefitPeriodType.Monthly,
                    CategoryId = wellnessCategory.Id
                }
            };
            await context.Benefits.AddRangeAsync(benefits);
            await context.SaveChangesAsync();
        }

        // 7. Seed Benefit Plans (Limit Plafon per Dept & Posisi)
        if (!await context.BenefitPlans.AnyAsync())
        {
            var benefit1 = Guid.Parse("b1010000-2222-4a11-8a01-000000000001");
            var benefit2 = Guid.Parse("b1010000-2222-4a11-8a01-000000000002");
            var benefit3 = Guid.Parse("b1010000-2222-4a11-8a01-000000000003");

            var plans = new List<BenefitPlan>
            {
                new() { DepartmentId = "DEPT-ENG", PositionId = "POS-ENG-L5", BenefitId = benefit1, LimitAmount = 10000000.00m },
                new() { DepartmentId = "DEPT-ENG", PositionId = "POS-ENG-L5", BenefitId = benefit2, LimitAmount = 2000000.00m },
                new() { DepartmentId = "DEPT-ENG", PositionId = "POS-ENG-L5", BenefitId = benefit3, LimitAmount = 500000.00m }
            };
            await context.BenefitPlans.AddRangeAsync(plans);
            await context.SaveChangesAsync();
        }

        // 8. Seed Employee Benefit Allocation (Untuk Budi Santoso)
        if (!await context.EmployeeBenefits.AnyAsync())
        {
            var budiEmpId = Guid.Parse("10100000-88a1-4b11-9c01-000000000001");
            var benefit1 = Guid.Parse("b1010000-2222-4a11-8a01-000000000001");
            var benefit2 = Guid.Parse("b1010000-2222-4a11-8a01-000000000002");

            var allocations = new List<EmployeeBenefit>
            {
                new()
                {
                    Id = Guid.Parse("0b010000-5555-4c44-9c01-000000000001"),
                    EmployeeId = budiEmpId,
                    BenefitId = benefit1,
                    Balance = 8500000.00m,
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 12, 31)
                },
                new()
                {
                    Id = Guid.Parse("0b010000-5555-4c44-9c01-000000000002"),
                    EmployeeId = budiEmpId,
                    BenefitId = benefit2,
                    Balance = 1800000.00m,
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 12, 31)
                }
            };
            await context.EmployeeBenefits.AddRangeAsync(allocations);
            await context.SaveChangesAsync();
        }
    }
}
