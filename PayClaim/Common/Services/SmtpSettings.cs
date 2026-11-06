namespace PayClaim.Common.Services;

public class SmtpSettings
{
    public string Host { get; set; } = "sandbox.smtp.mailtrap.io";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string SenderEmail { get; set; } = "no-reply@payclaim.perusahaan.co.id";
    public string SenderName { get; set; } = "PayClaim System";
    public string FrontendUrl { get; set; } = "http://localhost:3000";
    public bool EnableSsl { get; set; } = true;
}
