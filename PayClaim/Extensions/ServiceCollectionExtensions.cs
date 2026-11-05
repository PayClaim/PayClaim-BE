using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
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
using PayClaim.Features.Claims.Commands.ApproveClaim;
using PayClaim.Features.Claims.Commands.CreateClaim;
using PayClaim.Features.Claims.Commands.RejectClaim;
using PayClaim.Features.Claims.Queries.GetClaimById;
using PayClaim.Features.Claims.Queries.GetClaims;
using PayClaim.Features.BenefitPlans.Commands;
using PayClaim.Features.BenefitPlans.Queries;
using PayClaim.Features.Benefits.Commands;
using PayClaim.Features.Benefits.Queries;
using PayClaim.Features.Departments.Commands.UpdataDepartment;
using PayClaim.Features.Departments.Queries.GetDepartment;
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
using PayClaim.Features.TripProposals.Commands.ApproveTripProposal;
using PayClaim.Features.TripProposals.Commands.CreateTripProposal;
using PayClaim.Features.TripProposals.Queries.GetTripProposalById;
using PayClaim.Features.TripProposals.Queries.GetTripProposals;

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
        services.AddScoped<ITripProposalRepository, TripProposalRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        // Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<GetMeHandler>();

        // Trip Proposal Handlers
        services.AddScoped<CreateTripProposalHandler>();
        services.AddScoped<GetTripProposalsHandler>();
        services.AddScoped<GetTripProposalByIdHandler>();
        services.AddScoped<ApproveTripProposalHandler>();

        //handler
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

        // Claim Handlers
        services.AddScoped<CreateClaimHandler>();
        services.AddScoped<GetClaimsHandler>();
        services.AddScoped<GetClaimByIdHandler>();
        services.AddScoped<ApproveClaimHandler>();
        services.AddScoped<RejectClaimHandler>();

        return services;
    }
}