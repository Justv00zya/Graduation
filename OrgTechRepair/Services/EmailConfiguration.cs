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
        var key = configuration["Email:Brevo:ApiKey"];
        return !string.IsNullOrWhiteSpace(key);
    }

    /// <summary>xsmtpsib — ключ SMTP-реле; для API нужен xkeysib.</summary>
    public static bool IsBrevoSmtpKeyInsteadOfApi(IConfiguration configuration)
    {
        var key = configuration["Email:Brevo:ApiKey"]?.Trim();
        return !string.IsNullOrWhiteSpace(key) &&
               key.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsResendConfigured(IConfiguration configuration)
    {
        var key = configuration["Email:Resend:ApiKey"];
        return !string.IsNullOrWhiteSpace(key);
    }

    public static bool IsHttpsEmailApiConfigured(IConfiguration configuration) =>
        IsResendConfigured(configuration) || IsBrevoConfigured(configuration);
}
