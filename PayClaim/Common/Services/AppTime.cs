namespace PayClaim.Common.Services;

/// <summary>
/// Helper waktu terpusat zona WIB (+07:00) agar konsisten lintas zona waktu OS/container.
/// </summary>
public static class AppTime
{
    public static DateTime WibNow => DateTime.UtcNow.AddHours(7);

    public static DateOnly WibToday => DateOnly.FromDateTime(WibNow);
}
