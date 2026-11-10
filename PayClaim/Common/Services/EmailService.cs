using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PayClaim.Common.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogWarning("SMTP host not configured. Skipped sending email to {ToEmail}", toEmail);
            return;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(toName) ? toEmail : toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var client = new SmtpClient();
            var secureOption = _settings.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, linkedCts.Token);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? "", linkedCts.Token);
            }

            await client.SendAsync(message, linkedCts.Token);
            await client.DisconnectAsync(true, linkedCts.Token);

            _logger.LogInformation("Email sent successfully to {ToEmail} - Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending email to {ToEmail}. Workflow will continue safely.", toEmail);
        }
    }

    public async Task SendWelcomeEmployeeEmailAsync(
        string toEmail,
        string fullName,
        string employeeNumber,
        string username,
        string tempPassword,
        string departmentName,
        string positionTitle,
        CancellationToken ct = default)
    {
        var subject = $"[PayClaim] Selamat Bergabung di Perusahaan - Informasi Akun Karyawan";
        var changePasswordUrl = $"{_settings.FrontendUrl.TrimEnd('/')}/change-password";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #334155;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='padding: 40px 10px;'>
        <tr>
            <td align='center'>
                <table width='600' border='0' cellspacing='0' cellpadding='0' style='max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); border: 1px solid #e2e8f0;'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding: 32px 40px; text-align: left;'>
                            <div style='font-size: 22px; font-weight: 700; color: #ffffff; letter-spacing: -0.5px;'>
                                PayClaim System
                            </div>
                            <div style='font-size: 13px; color: #94a3b8; margin-top: 4px;'>
                                HR &amp; Finance Management Portal
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 36px 40px;'>
                            <h2 style='margin: 0 0 16px 0; font-size: 20px; font-weight: 700; color: #0f172a;'>
                                Selamat Datang, {fullName}!
                            </h2>
                            <p style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #475569;'>
                                Akun Anda pada portal <strong>PayClaim</strong> telah berhasil didaftarkan oleh HR Administrator. Anda sekarang dapat mengakses layanan penugasan tunjangan benefit kesehatan dan klaim reimbursement perusahaan.
                            </p>

                            <!-- Credentials Box -->
                            <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px; margin-bottom: 24px;'>
                                <div style='font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: #64748b; margin-bottom: 16px;'>
                                    Informasi Akun Karyawan
                                </div>
                                <table width='100%' style='font-size: 14px; border-collapse: collapse; line-height: 1.5;'>
                                    <tr>
                                        <td style='padding: 12px 20px 12px 0; color: #64748b; width: 45%; vertical-align: top;'>Nomor Karyawan (NIP)</td>
                                        <td style='padding: 12px 0; font-weight: 600; color: #0f172a; vertical-align: top;'>{employeeNumber}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 20px 12px 0; color: #64748b; vertical-align: top;'>Departemen / Posisi</td>
                                        <td style='padding: 12px 0; font-weight: 600; color: #0f172a; vertical-align: top;'>{departmentName} / {positionTitle}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 20px 12px 0; color: #64748b; vertical-align: top;'>Username</td>
                                        <td style='padding: 12px 0; font-weight: 700; color: #2563eb; font-family: monospace; font-size: 15px; vertical-align: top;'>{username}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 20px 12px 0; color: #64748b; vertical-align: top;'>Password Sementara</td>
                                        <td style='padding: 12px 0; font-weight: 700; color: #dc2626; font-family: monospace; font-size: 15px; vertical-align: top;'>{tempPassword}</td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Security Notice -->
                            <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 14px 16px; border-radius: 4px; margin-bottom: 28px;'>
                                <p style='margin: 0; font-size: 13px; color: #92400e; line-height: 1.5;'>
                                    <strong>Penting:</strong> Demi keamanan data Anda, sistem mewajibkan Anda untuk segera mengubah password sementara ini setelah login pertama kali.
                                </p>
                            </div>

                            <!-- CTA Button -->
                            <div style='text-align: center; margin-bottom: 20px;'>
                                <a href='{changePasswordUrl}' target='_blank' style='display: inline-block; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 32px; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2);'>
                                    Ganti Password Akun Anda &rarr;
                                </a>
                            </div>

                            <p style='text-align: center; margin: 0; font-size: 12px; color: #94a3b8;'>
                                Atau kunjungi: <a href='{_settings.FrontendUrl}' style='color: #2563eb; text-decoration: underline;'>{_settings.FrontendUrl}</a>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 40px; text-align: center;'>
                            <p style='margin: 0; font-size: 12px; color: #94a3b8;'>
                                &copy; 2026 PayClaim - HR &amp; Finance Management Portal. Seluruh hak cipta dilindungi undang-undang.
                            </p>
                            <p style='margin: 4px 0 0 0; font-size: 11px; color: #cbd5e1;'>
                                Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await SendEmailAsync(toEmail, fullName, subject, html, ct);
    }

    public async Task SendClaimStatusEmailAsync(
        string toEmail,
        string fullName,
        string claimDescription,
        decimal amount,
        string newStatus,
        string? approverName,
        string? notes,
        Guid? claimId = null,
        CancellationToken ct = default)
    {
        var subject = newStatus switch
        {
            "Paid" => "[PayClaim] Pencairan Reimbursement: Klaim Masuk Payroll Selanjutnya",
            "UnderReview" => "[PayClaim] Pengajuan Klaim Disetujui Manajer (Under Review)",
            "Approved" => "[PayClaim] Pengajuan Klaim Disetujui oleh Finance",
            "Rejected" => "[PayClaim] Pengajuan Klaim Ditolak",
            "NeedsRevision" => "[PayClaim] Pengajuan Klaim Memerlukan Perbaikan / Revisi",
            _ => $"[PayClaim] Pembaruan Status Pengajuan Klaim: {newStatus}"
        };

        var badgeColor = newStatus switch
        {
            "Approved" or "Paid" => "#16a34a",
            "Rejected" => "#dc2626",
            "UnderReview" => "#2563eb",
            _ => "#d97706"
        };

        var statusDisplay = newStatus switch
        {
            "Paid" => "Paid (Masuk Payroll)",
            "UnderReview" => "Under Review (Menunggu Verifikasi Finance)",
            "Approved" => "Approved (Disetujui Finance)",
            "Rejected" => "Ditolak (Rejected)",
            "NeedsRevision" => "Perlu Revisi (Needs Revision)",
            _ => newStatus
        };

        var statusExplanation = newStatus switch
        {
            "Paid" => $"Pengajuan reimbursement Anda sebesar <strong>Rp {amount:N0}</strong> telah disetujui untuk dicairkan oleh Tim Finance (<strong>{approverName ?? "Finance"}</strong>) dan <strong>telah dimasukkan ke dalam payroll periode berikutnya</strong>.",
            "UnderReview" => $"Pengajuan reimbursement Anda telah <strong>disetujui oleh Manajer ({approverName ?? "Manajer"})</strong> dan saat ini <strong>telah diteruskan ke Tim Finance</strong> untuk verifikasi akhir dan pencairan.",
            "Approved" => $"Pengajuan reimbursement Anda telah <strong>disetujui secara resmi oleh Tim Finance ({approverName ?? "Finance"})</strong> dan siap untuk proses pencairan dana/payroll.",
            "Rejected" => $"Pengajuan reimbursement Anda telah <strong>ditolak</strong> oleh <strong>{approverName ?? "Approver"}</strong>.",
            "NeedsRevision" => $"Pengajuan reimbursement Anda <strong>memerlukan revisi/perbaikan dokumen</strong> dari Anda sesuai catatan approver.",
            _ => $"Status pengajuan reimbursement klaim Anda telah diperbarui oleh <strong>{approverName ?? "Sistem"}</strong> menjadi <strong>{newStatus}</strong>."
        };

        var claimUrl = claimId.HasValue
            ? $"{_settings.FrontendUrl.TrimEnd('/')}/claims/{claimId.Value}"
            : $"{_settings.FrontendUrl.TrimEnd('/')}/claims";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color:#334155;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='padding: 40px 10px;'>
        <tr>
            <td align='center'>
                <table width='600' border='0' cellspacing='0' cellpadding='0' style='max-width:600px; width:100%; background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding:28px 36px; color:#ffffff;'>
                            <div style='font-size:20px; font-weight:700;'>PayClaim - Notifikasi Status Klaim</div>
                            <div style='font-size:13px; color:#94a3b8; margin-top:4px;'>HR &amp; Finance Management Portal</div>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:36px;'>
                            <h3 style='margin:0 0 12px 0; color:#0f172a;'>Halo, {fullName}</h3>
                            <p style='margin:0 0 20px 0; font-size:15px; line-height:1.6; color:#475569;'>
                                {statusExplanation}
                            </p>
                            
                            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; padding:20px; margin:24px 0;'>
                                <div style='font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.5px; color:#64748b; margin-bottom:12px;'>
                                    Rincian Pengajuan Klaim
                                </div>
                                <table width='100%' style='font-size:14px; border-collapse:collapse; line-height:1.6;'>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b; width:35%;'>Deskripsi</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{claimDescription}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Nominal Reimburse</td>
                                        <td style='padding:6px 0; font-weight:700; color:#0f172a; font-size:15px;'>Rp {amount:N0}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Status</td>
                                        <td style='padding:6px 0;'>
                                            <span style='background-color:{badgeColor}; color:#ffffff; padding:4px 12px; border-radius:12px; font-size:12px; font-weight:bold; display:inline-block;'>
                                                {statusDisplay}
                                            </span>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Diverifikasi Oleh</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{approverName ?? "Sistem"}</td>
                                    </tr>
                                    {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b; vertical-align:top;'>Catatan</td>
                                        <td style='padding:6px 0; color:#334155;'><em>&ldquo;{notes}&rdquo;</em></td>
                                    </tr>")}
                                </table>
                            </div>

                            <div style='text-align: center; margin: 28px 0 16px 0;'>
                                <a href='{claimUrl}' target='_blank' style='display: inline-block; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 14px; font-weight: 600; padding: 12px 28px; border-radius: 8px;'>
                                    Lihat Rincian Klaim di Portal &rarr;
                                </a>
                            </div>

                            <p style='font-size:13px; color:#64748b; text-align:center; margin:0;'>
                                Anda dapat memantau riwayat lengkap klaim Anda di portal PayClaim.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 36px; text-align: center;'>
                            <p style='margin: 0; font-size: 12px; color: #94a3b8;'>
                                &copy; 2026 PayClaim - HR &amp; Finance Management Portal.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await SendEmailAsync(toEmail, fullName, subject, html, ct);
    }

    public async Task SendClaimSubmittedToManagerEmailAsync(
        string toEmail,
        string managerName,
        string employeeName,
        string employeeNumber,
        string departmentName,
        string claimType,
        string claimDescription,
        decimal amount,
        DateTime submissionDate,
        Guid claimId,
        CancellationToken ct = default)
    {
        var subject = $"[PayClaim] Pengajuan Klaim Baru dari {employeeName} - Rp {amount:N0}";
        var claimUrl = $"{_settings.FrontendUrl.TrimEnd('/')}/claims/{claimId}";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color:#334155;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='padding: 40px 10px;'>
        <tr>
            <td align='center'>
                <table width='600' border='0' cellspacing='0' cellpadding='0' style='max-width:600px; width:100%; background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding:28px 36px; color:#ffffff;'>
                            <div style='font-size:20px; font-weight:700;'>PayClaim - Notifikasi Approval Manajer</div>
                            <div style='font-size:13px; color:#94a3b8; margin-top:4px;'>Persetujuan Reimbursement Bawahan</div>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:36px;'>
                            <h3 style='margin:0 0 12px 0; color:#0f172a;'>Halo, {managerName}</h3>
                            <p style='margin:0 0 20px 0; font-size:15px; line-height:1.6; color:#475569;'>
                                Karyawan bawahan Anda, <strong>{employeeName}</strong>, baru saja mengajukan klaim reimbursement baru yang memerlukan persetujuan dan verifikasi dari Anda sebagai atasan langsung.
                            </p>
                            
                            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; padding:20px; margin:24px 0;'>
                                <div style='font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.5px; color:#64748b; margin-bottom:12px;'>
                                    Rincian Pengajuan Klaim
                                </div>
                                <table width='100%' style='font-size:14px; border-collapse:collapse; line-height:1.6;'>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b; width:35%;'>Nama Karyawan</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{employeeName} ({employeeNumber})</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Departemen</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{departmentName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Jenis Klaim</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{claimType}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Deskripsi</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{claimDescription}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Nominal Klaim</td>
                                        <td style='padding:6px 0; font-weight:700; color:#059669; font-size:15px;'>Rp {amount:N0}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Tanggal Pengajuan</td>
                                        <td style='padding:6px 0; color:#0f172a;'>{submissionDate:dd MMMM yyyy HH:mm}</td>
                                    </tr>
                                </table>
                            </div>

                            <div style='text-align: center; margin: 28px 0 16px 0;'>
                                <a href='{claimUrl}' target='_blank' style='display: inline-block; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 32px; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2);'>
                                    Tinjau &amp; Berikan Persetujuan &rarr;
                                </a>
                            </div>

                            <p style='font-size:13px; color:#64748b; text-align:center; margin:0;'>
                                Mohon segera ditinjau agar pengajuan dapat diteruskan ke Tim Finance.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 36px; text-align: center;'>
                            <p style='margin: 0; font-size: 12px; color: #94a3b8;'>
                                &copy; 2026 PayClaim - HR &amp; Finance Management Portal.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await SendEmailAsync(toEmail, managerName, subject, html, ct);
    }

    public async Task SendClaimApprovedToFinanceEmailAsync(
        string toEmail,
        string financeName,
        string employeeName,
        string departmentName,
        string claimType,
        string claimDescription,
        decimal amount,
        string managerName,
        string? notes,
        Guid claimId,
        CancellationToken ct = default)
    {
        var subject = $"[PayClaim] Klaim Baru Masuk Antrean Finance: {employeeName} (Disetujui Manajer)";
        var claimUrl = $"{_settings.FrontendUrl.TrimEnd('/')}/claims/{claimId}";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color:#334155;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='padding: 40px 10px;'>
        <tr>
            <td align='center'>
                <table width='600' border='0' cellspacing='0' cellpadding='0' style='max-width:600px; width:100%; background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding:28px 36px; color:#ffffff;'>
                            <div style='font-size:20px; font-weight:700;'>PayClaim - Notifikasi Antrean Finance</div>
                            <div style='font-size:13px; color:#94a3b8; margin-top:4px;'>Klaim Disetujui Manajer &amp; Masuk ke Sistem Finance</div>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:36px;'>
                            <h3 style='margin:0 0 12px 0; color:#0f172a;'>Halo, {financeName}</h3>
                            <p style='margin:0 0 20px 0; font-size:15px; line-height:1.6; color:#475569;'>
                                Pengajuan klaim reimbursement dari <strong>{employeeName}</strong> telah <strong>disetujui oleh Manajer ({managerName})</strong> dan kini telah masuk ke antrean verifikasi Tim Finance.
                            </p>
                            
                            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; padding:20px; margin:24px 0;'>
                                <div style='font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.5px; color:#64748b; margin-bottom:12px;'>
                                    Rincian Pengajuan Klaim
                                </div>
                                <table width='100%' style='font-size:14px; border-collapse:collapse; line-height:1.6;'>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b; width:35%;'>Nama Pengaju</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{employeeName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Departemen</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{departmentName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Jenis Klaim</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{claimType}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Deskripsi</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{claimDescription}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Nominal Klaim</td>
                                        <td style='padding:6px 0; font-weight:700; color:#059669; font-size:15px;'>Rp {amount:N0}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b;'>Disetujui Oleh Manajer</td>
                                        <td style='padding:6px 0; font-weight:600; color:#0f172a;'>{managerName}</td>
                                    </tr>
                                    {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
                                    <tr>
                                        <td style='padding:6px 0; color:#64748b; vertical-align:top;'>Catatan Manajer</td>
                                        <td style='padding:6px 0; color:#334155;'><em>&ldquo;{notes}&rdquo;</em></td>
                                    </tr>")}
                                </table>
                            </div>

                            <div style='text-align: center; margin: 28px 0 16px 0;'>
                                <a href='{claimUrl}' target='_blank' style='display: inline-block; background-color: #059669; color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 32px; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(5, 150, 105, 0.2);'>
                                    Buka &amp; Proses Klaim di Portal &rarr;
                                </a>
                            </div>

                            <p style='font-size:13px; color:#64748b; text-align:center; margin:0;'>
                                Silakan verifikasi bukti transaksi dan lakukan persetujuan atau pembayaran melalui portal.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 36px; text-align: center;'>
                            <p style='margin: 0; font-size: 12px; color: #94a3b8;'>
                                &copy; 2026 PayClaim - HR &amp; Finance Management Portal.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await SendEmailAsync(toEmail, financeName, subject, html, ct);
    }

    public async Task SendTripProposalStatusEmailAsync(
        string toEmail,
        string picName,
        string destination,
        string newStatus,
        string? approverName,
        string? notes,
        CancellationToken ct = default)
    {
        var subject = $"[PayClaim] Pembaruan Status Proposal SPD ({destination}): {newStatus}";
        var badgeColor = newStatus switch
        {
            "Approved" or "Completed" => "#16a34a",
            "Rejected" => "#dc2626",
            "UnderReview" => "#2563eb",
            _ => "#d97706"
        };

        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family: sans-serif; color:#334155;'>
    <table width='100%' style='padding: 40px 10px;'>
        <tr>
            <td align='center'>
                <table width='600' style='max-width:600px; background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e2e8f0;'>
                    <tr>
                        <td style='background:#0f172a; padding:24px 32px; color:#ffffff;'>
                            <div style='font-size:20px; font-weight:700;'>PayClaim - Perjalanan Dinas (SPD)</div>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:32px;'>
                            <h3 style='margin-top:0;'>Halo {picName},</h3>
                            <p>Proposal perjalanan dinas ke <strong>{destination}</strong> telah ditinjau oleh <strong>{approverName ?? "Sistem"}</strong>:</p>
                            
                            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; padding:16px; margin:20px 0;'>
                                <p style='margin:6px 0;'><strong>Destinasi:</strong> {destination}</p>
                                <p style='margin:6px 0;'>
                                    <strong>Status:</strong> 
                                    <span style='background-color:{badgeColor}; color:#ffffff; padding:4px 10px; border-radius:12px; font-size:12px; font-weight:bold;'>{newStatus}</span>
                                </p>
                                {(string.IsNullOrWhiteSpace(notes) ? "" : $"<p style='margin:6px 0;'><strong>Catatan Approver:</strong> <em>\"{notes}\"</em></p>")}
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await SendEmailAsync(toEmail, picName, subject, html, ct);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string fullName,
        string username,
        string newPassword,
        CancellationToken ct = default)
    {
        var subject = "[PayClaim] Pembaruan Kata Sandi Akun Anda";
        var loginUrl = $"{_settings.FrontendUrl.TrimEnd('/')}/login";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #334155;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='padding: 40px 10px;'>
        <tr>
            <td align='center'>
                <table width='600' border='0' cellspacing='0' cellpadding='0' style='max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); border: 1px solid #e2e8f0;'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding: 32px 40px; text-align: left;'>
                            <div style='font-size: 22px; font-weight: 700; color: #ffffff; letter-spacing: -0.5px;'>
                                PayClaim System
                            </div>
                            <div style='font-size: 13px; color: #94a3b8; margin-top: 4px;'>
                                HR &amp; Finance Management Portal
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 36px 40px;'>
                            <h2 style='margin: 0 0 16px 0; font-size: 20px; font-weight: 700; color: #0f172a;'>
                                Halo, {fullName}
                            </h2>
                            <p style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #475569;'>
                                Kata sandi akun <strong>PayClaim</strong> Anda telah berhasil diatur ulang oleh Administrator. Berikut adalah kredensial baru Anda untuk masuk ke sistem:
                            </p>

                            <!-- Credentials Box -->
                            <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px; margin-bottom: 24px;'>
                                <div style='font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: #64748b; margin-bottom: 16px;'>
                                    Kredensial Baru Akun
                                </div>
                                <table width='100%' style='font-size: 14px; border-collapse: collapse; line-height: 1.5;'>
                                    <tr>
                                        <td style='padding: 10px 20px 10px 0; color: #64748b; width: 45%; vertical-align: top;'>Username</td>
                                        <td style='padding: 10px 0; font-weight: 700; color: #2563eb; font-family: monospace; font-size: 15px; vertical-align: top;'>{username}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 20px 10px 0; color: #64748b; vertical-align: top;'>Kata Sandi Baru</td>
                                        <td style='padding: 10px 0; font-weight: 700; color: #dc2626; font-family: monospace; font-size: 15px; vertical-align: top;'>{newPassword}</td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Security Notice -->
                            <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 14px 16px; border-radius: 4px; margin-bottom: 28px;'>
                                <p style='margin: 0; font-size: 13px; color: #92400e; line-height: 1.5;'>
                                    <strong>Penting:</strong> Demi keamanan akun Anda, silakan segera masuk ke portal dan lakukan perubahan kata sandi ini melalui menu profil/pengaturan akun.
                                </p>
                            </div>

                            <!-- CTA Button -->
                            <div style='text-align: center; margin-bottom: 20px;'>
                                <a href='{loginUrl}' target='_blank' style='display: inline-block; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 32px; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2);'>
                                    Masuk ke Portal PayClaim &rarr;
                                </a>
                            </div>

                            <p style='text-align: center; margin: 0; font-size: 12px; color: #94a3b8;'>
                                Atau kunjungi: <a href='{_settings.FrontendUrl}' style='color: #2563eb; text-decoration: underline;'>{_settings.FrontendUrl}</a>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 40px; text-align: center;'>
                            <p style='margin: 0; font-size: 12px; color: #94a3b8;'>
                                &copy; 2026 PayClaim - HR &amp; Finance Management Portal. Seluruh hak cipta dilindungi undang-undang.
                            </p>
                            <p style='margin: 4px 0 0 0; font-size: 11px; color: #cbd5e1;'>
                                Email ini dikirim secara otomatis oleh sistem. Mohon tidak membalas email ini.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await SendEmailAsync(toEmail, fullName, subject, html, ct);
    }
}
