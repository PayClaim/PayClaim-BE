using Microsoft.EntityFrameworkCore;
using PayClaim.Data;

namespace PayClaim.Common.Services;

/// <summary>
/// Helper untuk mengambil daftar penerima email notifikasi berdasarkan role.
/// </summary>
public static class NotificationRecipients
{
    public sealed record Recipient(
        Guid EmployeeId,
        string Email,
        string Name);

    /// <summary>
    /// Semua akun aktif yang memiliki salah satu role yang diminta
    /// (mis. "Finance", "Finance Manager", "Manager").
    /// </summary>
    public static async Task<List<Recipient>> GetActiveByRolesAsync(
        this PayClaimDbContext db,
        CancellationToken ct,
        params string[] roleNames)
    {
        if (roleNames.Length == 0)
        {
            return new List<Recipient>();
        }

        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive &&
                        a.Employee != null &&
                        a.Employee.Email != null &&
                        a.Employee.Email != "" &&
                        a.AccountRoles.Any(ar => roleNames.Contains(ar.Role.Name)))
            .Select(a => new Recipient(
                a.EmployeeId,
                a.Employee.Email!,
                a.Employee.FullName))
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Nama-nama role aktif milik seorang karyawan.
    /// </summary>
    public static async Task<List<string>> GetRoleNamesAsync(
        this PayClaimDbContext db,
        Guid employeeId,
        CancellationToken ct)
    {
        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive && a.EmployeeId == employeeId)
            .SelectMany(a => a.AccountRoles)
            .Select(ar => ar.Role.Name)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Apakah employee tersebut memiliki role tertentu.
    /// </summary>
    public static async Task<bool> HasRoleAsync(
        this PayClaimDbContext db,
        Guid employeeId,
        string roleName,
        CancellationToken ct)
    {
        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive &&
                        a.EmployeeId == employeeId &&
                        a.AccountRoles.Any(ar => ar.Role.Name == roleName))
            .AnyAsync(ct);
    }

    /// <summary>
    /// Atasan langsung (Manager) dari employee, bila ada dan aktif.
    /// </summary>
    public static async Task<Recipient?> GetDirectManagerAsync(
        this PayClaimDbContext db,
        Guid employeeId,
        CancellationToken ct)
    {
        return await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId &&
                        e.Manager != null &&
                        e.Manager.IsActive &&
                        e.Manager.Email != null &&
                        e.Manager.Email != "")
            .Select(e => new Recipient(
                e.Manager!.Id,
                e.Manager!.Email!,
                e.Manager!.FullName))
            .FirstOrDefaultAsync(ct);
    }

    // Penerima untuk satu role approval tertentu. Pengaju selalu dikecualikan.
    public static async Task<List<Recipient>> GetApproversForRoleAsync(
        this PayClaimDbContext db,
        string roleName,
        Guid submitterEmployeeId,
        CancellationToken ct)
    {
        if (ApprovalRouting.IsManagerStage(roleName))
        {
            var manager = await db.GetDirectManagerAsync(
                submitterEmployeeId,
                ct);

            return manager == null || manager.EmployeeId == submitterEmployeeId
                ? new List<Recipient>()
                : new List<Recipient> { manager };
        }

        var recipients = await db.GetActiveByRolesAsync(ct, roleName);

        return recipients
            .Where(r => r.EmployeeId != submitterEmployeeId)
            .ToList();
    }

    /// Resolusi approver berikutnya mulai dari role yang diharapkan.
    /// Bila tidak ada penerima (mis. satu-satunya Finance adalah pengaju),
    /// otomatis naik ke level berikutnya sampai SuperAdmin.
    public static async Task<(string Role, List<Recipient> Recipients)>
        ResolveNextApproversAsync(
            this PayClaimDbContext db,
            string startRole,
            Guid submitterEmployeeId,
            CancellationToken ct)
    {
        var role = startRole;

        while (true)
        {
            var recipients = await db.GetApproversForRoleAsync(
                role,
                submitterEmployeeId,
                ct);

            if (recipients.Count > 0)
            {
                return (role, recipients);
            }

            if (string.Equals(
                    role,
                    ApprovalRouting.SuperAdmin,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (role, recipients);
            }

            role = ApprovalRouting.GetNextApproverRole(role);
        }
    }
}
