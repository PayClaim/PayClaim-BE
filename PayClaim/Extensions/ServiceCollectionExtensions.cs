using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.Data.Repositories;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Data.UnitOfWork;
using PayClaim.Features.Departments.Commands.UpdataDepartment;
using PayClaim.Features.Departments.Queries.GetDepartment;
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
    public static IServiceCollection AddPayClaimServices(this IServiceCollection services, IConfiguration config)
    {
        //db
        services.AddDbContext<PayClaimDbContext>(options => options.UseSqlServer(
            config.GetConnectionString("DefaultConnection")));

        //repo
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        //uosw
        services.AddScoped<IUnitOfWork, UnitOfWork>();

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

        return services;
    }
}
