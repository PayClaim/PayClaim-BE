using PayClaim.Enums;

namespace PayClaim.Common.Services;

// Hirarki role yang dipakai untuk menentukan approver berikutnya.
// Employee &lt; Manager &lt; Finance &lt; Finance Manager &lt; Admin (SuperAdmin).
public static class ApprovalRouting
{
    public const string SuperAdmin = "Admin";
    public const string Employee = "Employee";
    public const string Manager = "Manager";
    public const string Finance = "Finance";
    public const string FinanceManager = "Finance Manager";

    private static readonly string[] Ladder =
    {
        Employee,
        Manager,
        Finance,
        FinanceManager
    };

    public static bool IsSuperAdmin(IEnumerable<string> roles) =>
        roles.Any(r => string.Equals(r, SuperAdmin, StringComparison.OrdinalIgnoreCase));

    // Role bisnis tertinggi yang dimiliki seorang karyawan.
    // Role Admin tidak dihitung karena berada di luar rantai approval.
    public static string GetHighestBusinessRole(IEnumerable<string> roles)
    {
        var normalized = roles
            .Where(r => !string.Equals(r, SuperAdmin, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = Ladder.Length - 1; i >= 0; i--)
        {
            if (normalized.Contains(Ladder[i]))
            {
                return Ladder[i];
            }
        }

        return Employee;
    }

    // Approver berikutnya = tepat satu tingkat di atas role pengaju.
    // Finance Manager diteruskan ke SuperAdmin karena tidak ada level di atasnya.
    public static string GetNextApproverRole(string highestRole) =>
        highestRole switch
        {
            Employee => Manager,
            Manager => Finance,
            Finance => FinanceManager,
            FinanceManager => SuperAdmin,
            _ => Manager
        };

    /// Status awal claim: tahap Manager memakai Submitted, tahap Finance ke atas
    /// memakai UnderReview (level 2).
    public static ClaimStatus GetInitialClaimStatus(string approverRole) =>
        IsManagerStage(approverRole)
            ? ClaimStatus.Submitted
            : ClaimStatus.UnderReview;

    // Status awal trip proposal mengikuti stage approver yang dituju.
    public static TripProposalStatus GetInitialTripStatus(string approverRole) =>
        approverRole switch
        {
            Finance => TripProposalStatus.UnderReview,
            FinanceManager or SuperAdmin => TripProposalStatus.FinanceManagerReview,
            _ => TripProposalStatus.Submitted
        };

    public static bool IsManagerStage(string approverRole) =>
        string.Equals(approverRole, Manager, StringComparison.OrdinalIgnoreCase);

    public static bool IsFinanceStage(string approverRole) =>
        string.Equals(approverRole, Finance, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(approverRole, FinanceManager, StringComparison.OrdinalIgnoreCase);

    public static bool IsFinanceManagerStage(string approverRole) =>
        string.Equals(approverRole, FinanceManager, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(approverRole, SuperAdmin, StringComparison.OrdinalIgnoreCase);
}
