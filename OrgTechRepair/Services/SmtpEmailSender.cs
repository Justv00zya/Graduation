using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OrgTechRepair.Services;

/// <summary>
/// SMTP через MailKit: <see cref="System.Net.Mail.SmtpClient"/> на Linux/с Brevo иногда шлёт MAIL до AUTH (5.7.0 Please authenticate first).
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
        var timeoutSeconds = _configuration.GetValue<int?>("Email:Smtp:TimeoutSeconds") ?? 15;

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

        try
        {
            using var client = new SmtpClient();

            client.Timeout = Math.Max(5, timeoutSeconds) * 1000;

            await client.ConnectAsync(host, port, ResolveSecureSocketOptions(port, enableSsl));
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email send failed to {Email}", toEmail);
            return false;
        }
    }

    private static SecureSocketOptions ResolveSecureSocketOptions(int port, bool enableSsl)
    {
        // 465 — implicit TLS; иначе при enableSsl используем STARTTLS (587/2525 для Brevo и др.)
        if (!enableSsl)
            return SecureSocketOptions.None;

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}
