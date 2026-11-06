namespace PayClaim.Common.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);

    Task SendWelcomeEmployeeEmailAsync(
        string toEmail,
        string fullName,
        string employeeNumber,
        string username,
        string tempPassword,
        string departmentName,
        string positionTitle,
        CancellationToken ct = default);

    Task SendClaimStatusEmailAsync(
        string toEmail,
        string fullName,
        string claimDescription,
        decimal amount,
        string newStatus,
        string? approverName,
        string? notes,
        CancellationToken ct = default);

    Task SendTripProposalStatusEmailAsync(
        string toEmail,
        string picName,
        string destination,
        string newStatus,
        string? approverName,
        string? notes,
        CancellationToken ct = default);
}
