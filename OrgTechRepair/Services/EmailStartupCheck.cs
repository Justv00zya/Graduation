using MailKit.Net.Smtp;
using MailKit.Security;

namespace OrgTechRepair.Services;

/// <summary>
/// При старте проверяет подключение к SMTP (без отправки письма) и пишет результат в лог.
/// </summary>
public sealed class EmailStartupCheck : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailStartupCheck> _logger;

    public EmailStartupCheck(IConfiguration configuration, ILogger<EmailStartupCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => VerifyAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (EmailConfiguration.IsResendConfigured(_configuration))
        {
            _logger.LogInformation(
                "Почта: Resend API (ключ задан). Отправка 2FA через HTTPS — подходит для Render.");
            return;
        }

        if (EmailConfiguration.IsBrevoConfigured(_configuration))
        {
            _logger.LogInformation(
                "Почта: Brevo API (ключ задан). Отправка 2FA через HTTPS, не через SMTP.");
            return;
        }

        if (!EmailConfiguration.IsSmtpConfigured(_configuration))
        {
            _logger.LogWarning(
                "Почта: SMTP не настроен. Создайте appsettings.Local.json (локально) или переменные Email__Smtp__* на Render.");
            return;
        }

        var host = _configuration["Email:Smtp:Host"]!;
        var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
        var username = _configuration["Email:Smtp:Username"]!;
        var password = _configuration["Email:Smtp:Password"]!;
        var enableSsl = _configuration.GetValue<bool?>("Email:Smtp:EnableSsl") ?? true;
        var timeoutMs = Math.Clamp(_configuration.GetValue<int?>("Email:Smtp:TimeoutSeconds") ?? 60, 30, 300) * 1000;

        try
        {
            await TryConnectAsync(host, port, username, password, enableSsl, timeoutMs, cancellationToken);
            _logger.LogInformation("Почта: SMTP {Host}:{Port} — подключение и авторизация успешны.", host, port);
        }
        catch (Exception ex) when (port != 465 && enableSsl && (port == 587 || port == 2525))
        {
            try
            {
                await TryConnectAsync(host, 465, username, password, true, timeoutMs, cancellationToken);
                _logger.LogInformation("Почта: SMTP {Host}:465 — подключение и авторизация успешны (fallback).", host);
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Почта: SMTP недоступен ({Host}). Ошибка 587: {Message}", host, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Почта: SMTP недоступен ({Host}:{Port}).", host, port);
        }
    }

    private static async Task TryConnectAsync(
        string host,
        int port,
        string username,
        string password,
        bool enableSsl,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient { Timeout = timeoutMs };
        var options = !enableSsl
            ? SecureSocketOptions.None
            : port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

        await client.ConnectAsync(host, port, options, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
