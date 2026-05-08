using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OrgTechRepair.Services;

/// <summary>
/// SMTP через MailKit: надёжный порядок STARTTLS + AUTH для Brevo и др.
/// На части хостингов outbound 587/2525 «висит» — есть fallback на 465 (implicit TLS).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
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
        var host = _configuration["Email:Smtp:Host"];
        var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var fromEmail = _configuration["Email:Smtp:FromEmail"];
        var fromName = _configuration["Email:Smtp:FromName"] ?? "OrgTechRepair";
        var enableSsl = _configuration.GetValue<bool?>("Email:Smtp:EnableSsl") ?? true;
        // Подключение к внешнему SMTP через облако часто занимает больше 15 с — минимум 60 с по умолчанию
        var timeoutSeconds = Math.Clamp(_configuration.GetValue<int?>("Email:Smtp:TimeoutSeconds") ?? 60, 30, 300);

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("SMTP email is not configured completely. Host/Username/Password/FromEmail required.");
            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        var timeoutMs = timeoutSeconds * 1000;

        try
        {
            await TrySendOnceAsync(host, message, username, password, port, ResolveSecureSocketOptions(port, enableSsl), timeoutMs);

            return true;
        }
        catch (Exception ex) when (IsLikelyOutboundOrConnectTimeout(ex) && port != 465 && enableSsl && (port == 587 || port == 2525))
        {
            _logger.LogWarning(
                ex,
                "SMTP соединение с {Host}:{Port} не удалось вовремя, повтор через порт 465 (implicit SSL).",
                host,
                port);

            try
            {
                await TrySendOnceAsync(host, message, username, password, 465, SecureSocketOptions.SslOnConnect, timeoutMs);
                return true;
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "SMTP email send failed to {Email} (включая fallback 465)", toEmail);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email send failed to {Email}", toEmail);
            return false;
        }
    }

    private static async Task TrySendOnceAsync(
        string host,
        MimeMessage message,
        string username,
        string password,
        int port,
        SecureSocketOptions options,
        int timeoutMs)
    {
        using var client = new SmtpClient();
        client.Timeout = timeoutMs;

        await client.ConnectAsync(host, port, options);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static bool IsLikelyOutboundOrConnectTimeout(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is TimeoutException)
                return true;

            var msg = e.Message ?? "";
            if (msg.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                return true;
            if (msg.Contains("The operation has timed out", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static SecureSocketOptions ResolveSecureSocketOptions(int port, bool enableSsl)
    {
        if (!enableSsl)
            return SecureSocketOptions.None;

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}
