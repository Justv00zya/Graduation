namespace OrgTechRepair.Services;

public static class EmailConfiguration
{
    public static bool IsSmtpConfigured(IConfiguration configuration)
    {
        var user = configuration["Email:Smtp:Username"];
        var pass = configuration["Email:Smtp:Password"];
        var from = configuration["Email:Smtp:FromEmail"];
        var host = configuration["Email:Smtp:Host"];
        return !string.IsNullOrWhiteSpace(host) &&
               !string.IsNullOrWhiteSpace(user) &&
               !string.IsNullOrWhiteSpace(pass) &&
               !string.IsNullOrWhiteSpace(from) &&
               !pass.Contains("your-app-password", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBrevoConfigured(IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("Email:Brevo:Enabled") ?? false;
        var key = configuration["Email:Brevo:ApiKey"];
        return enabled || !string.IsNullOrWhiteSpace(key);
    }
}
