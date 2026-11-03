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
    public DbSet<EmployeeBenefit> EmployeeBenefits { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimApproval> ClaimApprovals { get; set; }
    public DbSet<TripProposal> TripProposals { get; set; }
    public DbSet<TripApproval> TripApprovals { get; set; }
    public DbSet<ProposalParticipant> ProposalParticipants { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<AccountRole> AccountRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PayClaimDbContext).Assembly
        );
    }
}