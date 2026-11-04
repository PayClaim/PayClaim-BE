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

        return services;
    }
}
