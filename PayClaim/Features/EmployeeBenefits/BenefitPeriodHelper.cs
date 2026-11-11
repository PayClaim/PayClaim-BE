using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.EmployeeBenefits;

public static class BenefitPeriodHelper
{
    public static (DateOnly StartDate, DateOnly EndDate) GetCurrentPeriod(
        BenefitPeriodType periodType,
        DateOnly? referenceDate = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);

        return periodType switch
        {
            BenefitPeriodType.Monthly => (
                new DateOnly(today.Year, today.Month, 1),
                new DateOnly(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)
            ),
            _ => ( // Yearly & OneTime
                new DateOnly(today.Year, 1, 1),
                new DateOnly(today.Year, 12, 31)
            )
        };
    }

    public static bool TryRenewIfExpired(
        EmployeeBenefit empBenefit,
        BenefitPeriodType periodType,
        decimal planLimitAmount,
        DateOnly? referenceDate = null)
    {
        if (periodType == BenefitPeriodType.OneTime)
        {
            return false;
        }

        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);

        // Jika periode belum berakhir, tidak perlu renewal
        if (empBenefit.EndDate >= today)
        {
            return false;
        }

        // Lakukan renewal ke periode berjalan saat ini
        var (startDate, endDate) = GetCurrentPeriod(periodType, today);
        empBenefit.StartDate = startDate;
        empBenefit.EndDate = endDate;
        empBenefit.Balance = planLimitAmount;

        return true;
    }
}
