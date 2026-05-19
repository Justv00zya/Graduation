using Microsoft.Extensions.Logging;

namespace OrgTechRepair.Services;

/// <summary>
/// Резервный отправитель: пишет письма в лог и на рабочий стол (когда SMTP/Brevo не настроены).
/// </summary>
public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
    {
        LogDevEmail(email, "Восстановление пароля", $"Ссылка: {resetLink}");
        return Task.FromResult(false);
    }

    public Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        LogDevEmail(email, "Подтверждение email", $"Ссылка: {confirmationLink}");
        return Task.FromResult(false);
    }

    public Task<bool> SendTwoFactorCodeAsync(string email, string code)
    {
        LogDevEmail(email, "Код подтверждения входа", $"Код: {code}");
        WriteDesktopHint(email, code);
        return Task.FromResult(false);
    }

    private void LogDevEmail(string to, string subject, string body)
    {
        _logger.LogWarning(
            "Почта НЕ отправлена (SMTP/Brevo не настроены). To={Email}, Subject={Subject}, Body={Body}",
            to,
            subject,
            body);
    }

    private void WriteDesktopHint(string email, string code)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrWhiteSpace(desktop))
                return;

            var path = Path.Combine(desktop, "OrgTechRepair-2FA-last.txt");
            var text =
                $"Время (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\r\n" +
                $"Кому: {email}\r\n" +
                $"Код 2FA: {code}\r\n\r\n" +
                "Чтобы код приходил на почту, заполните Email:Smtp в appsettings.Local.json " +
                "(см. appsettings.Local.json.example) и перезапустите сайт.\r\n";
            File.WriteAllText(path, text);
            _logger.LogWarning("Код 2FA записан на рабочий стол: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось записать код 2FA на рабочий стол");
        }
    }
}
