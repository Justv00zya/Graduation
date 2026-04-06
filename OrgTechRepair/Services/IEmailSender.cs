namespace OrgTechRepair.Services;

/// <summary>
/// Интерфейс для отправки email сообщений
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Отправляет email для восстановления пароля
    /// </summary>
    /// <param name="email">Email адрес получателя</param>
    /// <param name="resetLink">Ссылка для сброса пароля</param>
    /// <returns>True если отправка успешна, иначе false</returns>
    Task<bool> SendPasswordResetEmailAsync(string email, string resetLink);

    /// <summary>
    /// Отправляет email для подтверждения регистрации
    /// </summary>
    /// <param name="email">Email адрес получателя</param>
    /// <param name="confirmationLink">Ссылка для подтверждения email</param>
    /// <returns>True если отправка успешна, иначе false</returns>
    Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink);
}
