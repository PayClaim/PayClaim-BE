namespace PayClaim.Entities;

public class Employee
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }


    // Foreign Keys
    public string PositionId { get; set; } = string.Empty;

    public string DepartmentId { get; set; } = string.Empty;

    public Guid? ManagerId { get; set; }

    // Navigation Properties
    public Position Position { get; set; } = null!;

    public Department Department { get; set; } = null!;

    public Employee? Manager { get; set; }
    public ICollection<ProposalParticipant> ProposalParticipants { get; set; }
    = new List<ProposalParticipant>();
}