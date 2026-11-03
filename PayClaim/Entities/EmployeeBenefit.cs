namespace PayClaim.Entities;

public class EmployeeBenefit
{
    public Guid Id { get; set; }

    public decimal Balance { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid BenefitId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Benefit Benefit { get; set; } = null!;
}