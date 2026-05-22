using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OrgTechRepair.Data;
using OrgTechRepair.Services;

namespace OrgTechRepair.Controllers;

[Route("Account")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AccountController> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IMemoryCache _cache;
    private readonly IEmailSender _emailSender;
    private readonly bool _captchaEnabled;
    private readonly bool _twoFactorEnabled;
    private readonly int _twoFactorCodeTtlMinutes;
    private readonly int _twoFactorResendCooldownSeconds;
    private readonly bool _externalCaptchaEnabled;
    private readonly bool _allowLocalCaptchaFallback;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly IConfiguration _configuration;
    private readonly bool _smtpConfigured;
    private readonly bool _brevoConfigured;
    private readonly bool _resendConfigured;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IAntiforgery antiforgery,
        ILogger<AccountController> logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ICaptchaVerifier captchaVerifier,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _antiforgery = antiforgery;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _emailSender = emailSender;
        _captchaEnabled = CaptchaConfiguration.IsEnabled(configuration);
        _twoFactorEnabled = configuration.GetValue<bool?>("Security:TwoFactor:Enabled") ?? true;
        _twoFactorCodeTtlMinutes = Math.Clamp(configuration.GetValue<int?>("Security:TwoFactor:CodeTtlMinutes") ?? 10, 1, 30);
        _twoFactorResendCooldownSeconds = Math.Clamp(configuration.GetValue<int?>("Security:TwoFactor:ResendCooldownSeconds") ?? 30, 5, 300);
        _externalCaptchaEnabled = CaptchaConfiguration.UseExternalCaptcha(configuration);
        _allowLocalCaptchaFallback = CaptchaConfiguration.AllowLocalFallback(configuration);
        _captchaVerifier = captchaVerifier;
        _configuration = configuration;
        _smtpConfigured = EmailConfiguration.IsSmtpConfigured(configuration);
        _brevoConfigured = EmailConfiguration.IsBrevoConfigured(configuration);
        _resendConfigured = EmailConfiguration.IsResendConfigured(configuration);
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(
        string loginOrEmail,
        string password,
        bool rememberMe,
        string? captchaId,
        string? captchaAnswer,
        string? captchaToken,
        [FromForm(Name = "cf-turnstile-response")] string? turnstileResponse,
        string? returnUrl = null)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный запрос. Пожалуйста, попробуйте снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        loginOrEmail = (loginOrEmail ?? "").Trim();
        password ??= string.Empty;

        if (string.IsNullOrWhiteSpace(loginOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Укажите логин/email и пароль")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }
        
        var effectiveCaptchaToken = string.IsNullOrWhiteSpace(captchaToken) ? turnstileResponse : captchaToken;
        if (_captchaEnabled && !await IsCaptchaValidAsync(captchaId, captchaAnswer, effectiveCaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверная капча")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var user = await _userManager.FindByNameAsync(loginOrEmail);
        if (user == null && loginOrEmail.Contains('@', StringComparison.Ordinal))
        {
            user = await _userManager.FindByEmailAsync(loginOrEmail);
        }

        if (user == null)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Пользователь с таким логином или email не найден")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if ((result.Succeeded || result.RequiresTwoFactor) && _twoFactorEnabled)
        {
            var deliveryEmail = TwoFactorEmailRouting.ResolveDeliveryEmail(_configuration, user);
            if (string.IsNullOrWhiteSpace(deliveryEmail))
                return Redirect($"/Login?error={Uri.EscapeDataString("Для 2FA у пользователя не указан email")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");

            var code = Random.Shared.Next(100000, 1000000).ToString();
            var challengeId = Guid.NewGuid().ToString("N");
            _cache.Set(
                $"web2fa:{challengeId}",
                new PendingWebTwoFactor
                {
                    UserId = user.Id,
                    Code = code,
                    RememberMe = rememberMe,
                    ReturnUrl = returnUrl
                },
                TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));
            _cache.Set(
                $"web2fa:resend:{challengeId}",
                DateTimeOffset.UtcNow.AddSeconds(_twoFactorResendCooldownSeconds),
                TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));

            var emailSent = await SendWebTwoFactorEmailOrLogAsync(deliveryEmail, code, user.UserName);

            var twoFactorUrl =
                $"/Login?twoFactor=1&challengeId={Uri.EscapeDataString(challengeId)}" +
                $"&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}" +
                $"&emailSent={(emailSent ? "1" : "0")}";
            twoFactorUrl += emailSent
                ? "&message=" + Uri.EscapeDataString("Код подтверждения отправлен на ваш e-mail.")
                : "&message=" + Uri.EscapeDataString(BuildTwoFactorEmailFailureMessage());
            return Redirect(twoFactorUrl);
        }

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, rememberMe);
            return Redirect(returnUrl ?? "/");
        }
        else if (result.IsLockedOut)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Учетная запись заблокирована")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }
        else
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный пароль")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }
    }

    [HttpPost("VerifyTwoFactorWeb")]
    public async Task<IActionResult> VerifyTwoFactorWeb(string challengeId, string code, string? returnUrl = null)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный запрос. Пожалуйста, попробуйте снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Введите код подтверждения")}&twoFactor=1&challengeId={Uri.EscapeDataString(challengeId ?? "")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (!_cache.TryGetValue<PendingWebTwoFactor>($"web2fa:{challengeId}", out var pending) || pending == null)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Код истек. Войдите снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (!string.Equals(pending.Code, code.Trim(), StringComparison.Ordinal))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный код подтверждения")}&twoFactor=1&challengeId={Uri.EscapeDataString(challengeId)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var user = await _userManager.FindByIdAsync(pending.UserId);
        if (user == null)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Пользователь не найден")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        _cache.Remove($"web2fa:{challengeId}");
        await _signInManager.SignInAsync(user, pending.RememberMe);
        var target = pending.ReturnUrl ?? returnUrl ?? "/";
        if (!Url.IsLocalUrl(target))
            target = "/";
        return Redirect(target);
    }

    [HttpPost("ResendTwoFactorWeb")]
    public async Task<IActionResult> ResendTwoFactorWeb(string challengeId, string? returnUrl = null)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный запрос. Пожалуйста, попробуйте снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (string.IsNullOrWhiteSpace(challengeId))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Код истек. Войдите снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (!_cache.TryGetValue<PendingWebTwoFactor>($"web2fa:{challengeId}", out var pending) || pending == null)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Код истек. Войдите снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (_cache.TryGetValue<DateTimeOffset>($"web2fa:resend:{challengeId}", out var nextResendAt))
        {
            var waitSeconds = (int)Math.Ceiling((nextResendAt - DateTimeOffset.UtcNow).TotalSeconds);
            if (waitSeconds > 0)
            {
                return Redirect($"/Login?error={Uri.EscapeDataString($"Повторная отправка доступна через {waitSeconds} сек.")}&twoFactor=1&challengeId={Uri.EscapeDataString(challengeId)}&returnUrl={Uri.EscapeDataString(returnUrl ?? pending.ReturnUrl ?? "")}");
            }
        }

        var user = await _userManager.FindByIdAsync(pending.UserId);
        if (user == null)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Пользователь не найден.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var deliveryEmail = TwoFactorEmailRouting.ResolveDeliveryEmail(_configuration, user);
        if (string.IsNullOrWhiteSpace(deliveryEmail))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Пользователь не найден или email не указан.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var newCode = Random.Shared.Next(100000, 1000000).ToString();
        pending.Code = newCode;
        _cache.Set($"web2fa:{challengeId}", pending, TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));
        _cache.Set(
            $"web2fa:resend:{challengeId}",
            DateTimeOffset.UtcNow.AddSeconds(_twoFactorResendCooldownSeconds),
            TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));

        var resent = await SendWebTwoFactorEmailOrLogAsync(deliveryEmail, newCode, user.UserName, isResend: true);
        var resendMsg = resent
            ? "Код отправлен повторно."
            : BuildTwoFactorEmailFailureMessage();

        return Redirect(
            $"/Login?twoFactor=1&challengeId={Uri.EscapeDataString(challengeId)}" +
            $"&returnUrl={Uri.EscapeDataString(returnUrl ?? pending.ReturnUrl ?? "")}" +
            $"&emailSent={(resent ? "1" : "0")}" +
            $"&message={Uri.EscapeDataString(resendMsg)}");
    }

    /// <summary>Регистрация клиента: запись в БД (AspNetUsers + роль + карточка Client) и вход в ту же сессию, что и /Account/Login.</summary>
    [HttpPost("Register")]
    public async Task<IActionResult> RegisterClient(
        string username,
        string email,
        string password,
        string confirmPassword,
        string? captchaId,
        string? captchaAnswer,
        string? captchaToken,
        [FromForm(Name = "cf-turnstile-response")] string? turnstileResponse)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return Redirect($"/Register?error={Uri.EscapeDataString("Неверный запрос. Обновите страницу и попробуйте снова.")}");
        }

        try
        {
            username = (username ?? "").Trim();
            email = (email ?? "").Trim();
            password ??= string.Empty;
            confirmPassword ??= string.Empty;

            var effectiveCaptchaToken = string.IsNullOrWhiteSpace(captchaToken) ? turnstileResponse : captchaToken;
            if (_captchaEnabled && !await IsCaptchaValidAsync(captchaId, captchaAnswer, effectiveCaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted))
                return Redirect($"/Register?error={Uri.EscapeDataString("Неверная капча")}");

            if (password != confirmPassword)
                return Redirect($"/Register?error={Uri.EscapeDataString("Пароли не совпадают")}");

            if (username.Length < 3 || username.Length > 50)
                return Redirect($"/Register?error={Uri.EscapeDataString("Логин должен быть от 3 до 50 символов")}");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
                return Redirect($"/Register?error={Uri.EscapeDataString("Укажите корректный email")}");

            if (password.Length < 6)
                return Redirect($"/Register?error={Uri.EscapeDataString("Пароль должен быть не короче 6 символов")}");

            if (await _userManager.FindByNameAsync(username) != null)
                return Redirect($"/Register?error={Uri.EscapeDataString("Пользователь с таким логином уже существует")}");

            if (await _userManager.FindByEmailAsync(email) != null)
                return Redirect($"/Register?error={Uri.EscapeDataString("Пользователь с таким email уже существует")}");

            var user = new IdentityUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var msg = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Redirect($"/Register?error={Uri.EscapeDataString(msg)}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Client");
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Не назначена роль Client пользователю {UserId}: {Errors}",
                    user.Id, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                return Redirect($"/Register?error={Uri.EscapeDataString("Ошибка назначения роли. Обратитесь к администратору.")}");
            }

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await ClientProfileProvisioner.GetOrCreateForUserAsync(db, user.Id, user.UserName, user.Email);

            // После регистрации переводим на страницу входа: это стабильнее и совместимо с 2FA-потоком.
            return Redirect($"/Login?message={Uri.EscapeDataString("Регистрация выполнена. Войдите с указанным логином и паролем.")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка регистрации клиента для {UserName}", username);
            return Redirect($"/Register?error={Uri.EscapeDataString("Произошла внутренняя ошибка регистрации. Попробуйте снова через минуту.")}");
        }

    }

    [HttpGet("Logout")]
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при выходе");
        }
        return Redirect("/Login?message=" + Uri.EscapeDataString("Вы вышли из системы."));
    }

    private async Task<bool> IsCaptchaValidAsync(
        string? captchaId,
        string? captchaAnswer,
        string? captchaToken,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        if (_externalCaptchaEnabled && !string.IsNullOrWhiteSpace(captchaToken))
        {
            var externalOk = await _captchaVerifier.VerifyAsync(captchaToken, remoteIp, cancellationToken);
            if (externalOk) return true;
            if (!_allowLocalCaptchaFallback) return false;
        }

        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(captchaAnswer))
            return false;

        if (!_cache.TryGetValue<string>($"captcha:{captchaId}", out var expected) || string.IsNullOrWhiteSpace(expected))
            return false;

        _cache.Remove($"captcha:{captchaId}");
        return string.Equals(expected.Trim(), captchaAnswer.Trim(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Отправка 2FA только в рамках запроса: scoped HttpClient нельзя использовать из Task.Run после dispose scope.
    /// </summary>
    private async Task<bool> SendWebTwoFactorEmailOrLogAsync(string email, string code, string? userName, bool isResend = false)
    {
        try
        {
            var sent = await _emailSender.SendTwoFactorCodeAsync(email, code);
            if (!sent)
            {
                _logger.LogWarning("Не удалось отправить WEB 2FA код пользователю {UserName}. Резервный код: {Code}", userName, code);
                WriteTwoFactorCodeToDesktop(email, code);
            }
            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки WEB 2FA кода пользователю {UserName}. Резервный код: {Code}", userName, code);
            WriteTwoFactorCodeToDesktop(email, code);
            return false;
        }
    }

    private string BuildTwoFactorEmailFailureMessage()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER")))
        {
            if (_resendConfigured || _brevoConfigured)
            {
                return "Письмо не отправлено. Проверьте API-ключ почты в Environment на Render и логи сервиса.";
            }

            return "На Render обычный SMTP (Gmail/Яндекс) заблокирован. " +
                   "Зарегистрируйтесь на resend.com → API Keys → добавьте Email__Resend__ApiKey на Render → Redeploy.";
        }

        if (_resendConfigured || _brevoConfigured)
        {
            return "Письмо не отправлено (ошибка API почты). Код сохранён в OrgTechRepair-2FA-last.txt на рабочем столе.";
        }

        if (_smtpConfigured)
        {
            return "Письмо не отправлено (ошибка SMTP). " +
                   "Код сохранён в OrgTechRepair-2FA-last.txt на рабочем столе ПК, где запущен сайт.";
        }

        return "Почта не настроена: создайте appsettings.Local.json (см. example) и перезапустите сайт. " +
               "Код: OrgTechRepair-2FA-last.txt на рабочем столе.";
    }

    private static void WriteTwoFactorCodeToDesktop(string email, string code)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrWhiteSpace(desktop))
                return;
            var path = Path.Combine(desktop, "OrgTechRepair-2FA-last.txt");
            System.IO.File.WriteAllText(
                path,
                $"Время (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\r\nКому: {email}\r\nКод 2FA: {code}\r\n");
        }
        catch
        {
            // ignore
        }
    }

    private sealed class PendingWebTwoFactor
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
