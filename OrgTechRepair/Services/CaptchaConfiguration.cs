using Microsoft.Extensions.Caching.Memory;

namespace OrgTechRepair.Services;

/// <summary>Единые правила отображения и проверки CAPTCHA (веб, API, Blazor).</summary>
public static class CaptchaConfiguration
{
    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool?>("Security:Captcha:Enabled") ?? true;

    public static bool UseExternalRequested(IConfiguration configuration) =>
        configuration.GetValue<bool?>("Security:Captcha:UseExternal") ?? true;

    public static bool AllowLocalFallback(IConfiguration configuration) =>
        configuration.GetValue<bool?>("Security:Captcha:AllowLocalFallback") ?? true;

    public static bool HasConfiguredExternalKeys(IConfiguration configuration)
    {
        var siteKey = configuration["Security:Captcha:SiteKey"];
        var secretKey = configuration["Security:Captcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(siteKey) || string.IsNullOrWhiteSpace(secretKey))
            return false;
        if (siteKey.Contains("PASTE", StringComparison.OrdinalIgnoreCase) ||
            secretKey.Contains("PASTE", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    /// <summary>Turnstile только при включённой внешней капче и реальных ключах.</summary>
    public static bool UseExternalCaptcha(IConfiguration configuration) =>
        IsEnabled(configuration) &&
        UseExternalRequested(configuration) &&
        HasConfiguredExternalKeys(configuration);

    public static void IssueLocalChallenge(IMemoryCache cache, out string captchaId, out string captchaQuestion)
    {
        var left = Random.Shared.Next(1, 10);
        var right = Random.Shared.Next(1, 10);
        captchaId = Guid.NewGuid().ToString("N");
        captchaQuestion = $"{left} + {right} = ?";
        cache.Set($"captcha:{captchaId}", (left + right).ToString(), TimeSpan.FromMinutes(3));
    }
}
