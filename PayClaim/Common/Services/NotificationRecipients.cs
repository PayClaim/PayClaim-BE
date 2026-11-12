using Microsoft.EntityFrameworkCore;
using PayClaim.Data;

namespace PayClaim.Common.Services;

/// <summary>
/// Helper untuk mengambil daftar penerima email notifikasi berdasarkan role.
/// </summary>
public static class NotificationRecipients
{
    public sealed record Recipient(string Email, string Name);

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
            .Select(a => new Recipient(a.Employee.Email!, a.Employee.FullName))
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
                e.Manager!.Email!,
                e.Manager!.FullName))
            .FirstOrDefaultAsync(ct);
    }
}
