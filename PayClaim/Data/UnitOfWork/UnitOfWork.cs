using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.UnitOfWork;

public class UnitOfWork(
    PayClaimDbContext context,
    IEmployeeRepository employees,
    IAccountRepository accounts,
    IBenefitPlanRepository benefitPlans,
    IRepository<Department> departments,
    IRepository<Position> positions,
    IRepository<BenefitCategory> benefitCategories,
    IRepository<Benefit> benefits,
    IRepository<AccountRole> accountRoles,
    IRepository<EmployeeBenefit> employeeBenefits,
    ITripProposalRepository tripProposals,
    IRepository<TripApproval> tripApprovals,
    IRepository<ProposalParticipant> proposalParticipants
) : IUnitOfWork
{
    public IEmployeeRepository Employees { get; } = employees;
    public IAccountRepository Accounts { get; } = accounts;
    public IBenefitPlanRepository BenefitPlans { get; } = benefitPlans;

    public IRepository<Department> Departments { get; } = departments;
    public IRepository<Position> Positions { get; } = positions;
    public IRepository<BenefitCategory> BenefitCategories { get; } = benefitCategories;
    public IRepository<Benefit> Benefits { get; } = benefits;
    public IRepository<AccountRole> AccountRoles { get; } = accountRoles;
    public IRepository<EmployeeBenefit> EmployeeBenefits { get; } = employeeBenefits;
    public ITripProposalRepository TripProposals { get; } = tripProposals;
    public IRepository<TripApproval> TripApprovals { get; } = tripApprovals;
    public IRepository<ProposalParticipant> ProposalParticipants { get; } = proposalParticipants;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
