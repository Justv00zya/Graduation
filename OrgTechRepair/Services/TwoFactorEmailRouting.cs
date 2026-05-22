using Microsoft.AspNetCore.Identity;

namespace OrgTechRepair.Services;

public static class TwoFactorEmailRouting
{
    private static readonly HashSet<string> SeededDemoUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "demo_manager",
        "demo_client",
        "demo_service",
        "demo_office",
        "demo_cashier",
        "demo_warehouse",
        "demo_director"
    };

    /// <summary>
    /// Куда отправить код 2FA: для demo_* — общий ящик из конфига, иначе email пользователя.
    /// </summary>
    public static string ResolveDeliveryEmail(IConfiguration configuration, IdentityUser user)
    {
        var sharedEnabled = configuration.GetValue<bool>("SeedData:EnableSharedTwoFactorEmail");
        var sharedEmail = configuration["SeedData:SharedTwoFactorEmail"];

        if (sharedEnabled &&
            !string.IsNullOrWhiteSpace(sharedEmail) &&
            !string.IsNullOrWhiteSpace(user.UserName) &&
            SeededDemoUsernames.Contains(user.UserName))
        {
            return sharedEmail;
        }

        return user.Email ?? string.Empty;
    }
}
