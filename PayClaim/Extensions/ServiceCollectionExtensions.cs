using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.Data.Repositories;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Data.UnitOfWork;
using PayClaim.Features.Auth.Commands.Login;
using PayClaim.Features.Auth.Queries.GetMe;
using PayClaim.Features.BenefitCategories.Commands.CreateBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.DeleteBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.UpdateBenefitCategory;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategories;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategoryById;
using PayClaim.Features.BenefitPlans.Commands;
using PayClaim.Features.BenefitPlans.Queries;
using PayClaim.Features.Benefits.Commands;
using PayClaim.Features.Benefits.Queries;
using PayClaim.Features.Departments.Commands.UpdataDepartment;
using PayClaim.Features.Departments.Queries.GetDepartment;
using PayClaim.Features.Employees.Commands.CreateEmployee;
using PayClaim.Features.Employees.Commands.UpdateEmployee;
using PayClaim.Features.Employees.Queries.GetEmployeeById;
using PayClaim.Features.Employees.Queries.GetEmployees;
using PayClaim.Features.Master.Departments.Commands.CreateDepartment;
using PayClaim.Features.Master.Departments.Commands.DeleteDepartment;
using PayClaim.Features.Master.Departments.Queries.GetDepartmentById;
using PayClaim.Features.Positions.Commands.CreatePosition;
using PayClaim.Features.Positions.Commands.DeletePosition;
using PayClaim.Features.Positions.Commands.UpdatePosition;
using PayClaim.Features.Positions.Queries.GetPositionById;
using PayClaim.Features.Positions.Queries.GetPositions;

namespace PayClaim.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPayClaimServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Database
        services.AddDbContext<PayClaimDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specific Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBenefitPlanRepository, BenefitPlanRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<GetMeHandler>();

        // Department Handlers
        services.AddScoped<CreateDepartmentHandler>();
        services.AddScoped<GetDepartmentsHandler>();
        services.AddScoped<GetDepartmentByIdQueryHandler>();
        services.AddScoped<UpdateDepartmentHandler>();
        services.AddScoped<DeleteDepartmentHandler>();

        // Position Handlers
        services.AddScoped<CreatePositionHandler>();
        services.AddScoped<GetPositionsHandler>();
        services.AddScoped<GetPositionByIdQueryHandler>();
        services.AddScoped<UpdatePositionHandler>();
        services.AddScoped<DeletePositionHandler>();

        // Benefit Category Handlers
        services.AddScoped<CreateBenefitCategoryHandler>();
        services.AddScoped<GetBenefitCategoriesHandler>();
        services.AddScoped<GetBenefitCategoryByIdQueryHandler>();
        services.AddScoped<UpdateBenefitCategoryHandler>();
        services.AddScoped<DeleteBenefitCategoryHandler>();

        // Benefit Handlers
        services.AddScoped<CreateBenefitHandler>();
        services.AddScoped<GetBenefitsHandler>();
        services.AddScoped<GetBenefitByIdHandler>();
        services.AddScoped<UpdateBenefitHandler>();
        services.AddScoped<DeleteBenefitHandler>();

        // Benefit Plan Handlers
        services.AddScoped<CreateBenefitPlanHandler>();
        services.AddScoped<GetBenefitPlansHandler>();
        services.AddScoped<GetBenefitPlanByIdHandler>();
        services.AddScoped<UpdateBenefitPlanHandler>();
        services.AddScoped<DeleteBenefitPlanHandler>();

        // Employee Handlers
        services.AddScoped<GetEmployeesHandler>();
        services.AddScoped<GetEmployeeByIdHandler>();
        services.AddScoped<CreateEmployeeHandler>();
        services.AddScoped<UpdateEmployeeHandler>();

        return services;
    }
}