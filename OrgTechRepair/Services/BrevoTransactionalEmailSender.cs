using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrgTechRepair.Services;

/// <summary>
/// Отправка через Brevo Transactional API по HTTPS (порт 443).
/// На части хостингов исходящий SMTP (587/465) заблокирован — API остаётся доступным.
/// </summary>
public sealed class BrevoTransactionalEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoTransactionalEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BrevoTransactionalEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoTransactionalEmailSender> logger)
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
        var apiKey = _configuration["Email:Brevo:ApiKey"];
        var fromEmail = _configuration["Email:Brevo:FromEmail"] ?? _configuration["Email:Smtp:FromEmail"];
        var fromName = _configuration["Email:Brevo:FromName"] ?? _configuration["Email:Smtp:FromName"] ?? "OrgTechRepair";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("Brevo transactional email не настроен: нужны Email:Brevo:ApiKey и адрес отправителя (Email:Brevo:FromEmail или Email:Smtp:FromEmail).");
            return false;
        }

        var htmlBody = "<p>"
            + WebUtility.HtmlEncode(body).Replace("\r\n", "<br/>", StringComparison.Ordinal).Replace("\n", "<br/>", StringComparison.Ordinal)
            + "</p>";

        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new { name = fromName, email = fromEmail },
            ["to"] = new[] { new { email = toEmail } },
            ["subject"] = subject,
            ["textContent"] = body,
            ["htmlContent"] = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", apiKey.Trim());
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        try
        {
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return true;

            var err = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogError(
                "Brevo transactional: ответ {Status}: {Body}",
                (int)response.StatusCode,
                err.Length > 2000 ? err[..2000] + "…" : err);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo transactional: не удалось отправить письмо на {Email}", toEmail);
            return false;
        }
    }
}
