using Microsoft.Extensions.Logging;

namespace OrgTechRepair.Services;

/// <summary>
/// Реализация IEmailSender для разработки (выводит ссылки в консоль/логи)
/// В продакшене замените на реальную реализацию (SMTP, SendGrid, и т.д.)
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
        _logger.LogInformation("=== EMAIL (Development Mode) ===");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Subject: Восстановление пароля");
        _logger.LogInformation("Body: Для восстановления пароля перейдите по ссылке: {ResetLink}", resetLink);
        _logger.LogInformation("================================");
        
        // В продакшене здесь должна быть реальная отправка email
        return Task.FromResult(true);
    }

    public Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        _logger.LogInformation("=== EMAIL (Development Mode) ===");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Subject: Подтверждение email адреса");
        _logger.LogInformation("Body: Для подтверждения email перейдите по ссылке: {ConfirmationLink}", confirmationLink);
        _logger.LogInformation("================================");
        
        // В продакшене здесь должна быть реальная отправка email
        return Task.FromResult(true);
    }
}
