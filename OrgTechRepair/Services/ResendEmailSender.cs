using System.Net.Http.Json;
using System.Text.Json;

namespace OrgTechRepair.Services;

/// <summary>
/// Отправка через Resend API (HTTPS, порт 443). Проще Brevo: один ключ re_...
/// На Render free SMTP заблокирован — Resend работает.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ResendEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var subject = "Восстановление пароля";
        var body = $"Для восстановления пароля перейдите по ссылке: {resetLink}";
        return SendAsync(email, subject, body);
    }

    public Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        var subject = "Подтверждение email адреса";
        var body = $"Для подтверждения email перейдите по ссылке: {confirmationLink}";
        return SendAsync(email, subject, body);
    }

    public Task<bool> SendTwoFactorCodeAsync(string email, string code)
    {
        var subject = "Код подтверждения входа";
        var body = $"Ваш код подтверждения: {code}";
        return SendAsync(email, subject, body);
    }

    private async Task<bool> SendAsync(string toEmail, string subject, string body)
    {
        var apiKey = _configuration["Email:Resend:ApiKey"];
        var fromEmail = _configuration["Email:Resend:FromEmail"]
                        ?? _configuration["Email:Smtp:FromEmail"]
                        ?? "onboarding@resend.dev";
        var fromName = _configuration["Email:Resend:FromName"]
                       ?? _configuration["Email:Smtp:FromName"]
                       ?? "ВузяПринт";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Resend не настроен: нужен Email:Resend:ApiKey.");
            return false;
        }

        var from = fromEmail.Contains('@', StringComparison.Ordinal)
            ? $"{fromName} <{fromEmail}>"
            : fromEmail;

        var payload = new Dictionary<string, object?>
        {
            ["from"] = from,
            ["to"] = new[] { toEmail },
            ["subject"] = subject,
            ["text"] = body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey.Trim()}");
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        try
        {
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return true;

            var err = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogError(
                "Resend: ответ {Status}: {Body}",
                (int)response.StatusCode,
                err.Length > 2000 ? err[..2000] + "…" : err);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend: не удалось отправить письмо на {Email}", toEmail);
            return false;
        }
    }
}
