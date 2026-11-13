using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.Repositories;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Data.UnitOfWork;

namespace PayClaim.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPayClaimServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Database
        services.AddDbContextPool<PayClaimDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Caching
        services.AddMemoryCache();

        // HTTP Context Accessor & Current User
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // MediatR (auto-registers all IRequestHandler implementations in this assembly)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specific Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBenefitPlanRepository, BenefitPlanRepository>();
        services.AddScoped<ITripProposalRepository, TripProposalRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.Configure<SmtpSettings>(config.GetSection("Smtp"));
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        return services;
    }
}
