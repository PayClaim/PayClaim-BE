using Microsoft.EntityFrameworkCore;
using PayClaim.Entities;

namespace PayClaim.Data;

public class PayClaimDbContext : DbContext
{
    public PayClaimDbContext(DbContextOptions<PayClaimDbContext> options)
        : base(options)
    {

    }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<BenefitCategory> BenefitCategories { get; set; }
    public DbSet<Benefit> Benefits { get; set; }
    public DbSet<BenefitPlan> BenefitPlans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PayClaimDbContext).Assembly
        );
    }
}