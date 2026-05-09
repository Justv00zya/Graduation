using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using OrgTechRepair.Data;
using OrgTechRepair.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailSender? _emailSender;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly bool _twoFactorEnabled;
    private readonly bool _captchaEnabled;
    private readonly bool _externalCaptchaEnabled;
    private readonly bool _allowLocalCaptchaFallback;
    private readonly bool _totpFallbackEnabled;
    private readonly int _twoFactorCodeTtlMinutes;
    private readonly int _twoFactorResendCooldownSeconds;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMemoryCache cache,
        ICaptchaVerifier captchaVerifier,
        IEmailSender? emailSender = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _contextFactory = contextFactory;
        _cache = cache;
        _captchaVerifier = captchaVerifier;
        _emailSender = emailSender;
        _twoFactorEnabled = _configuration.GetValue<bool?>("Security:TwoFactor:Enabled") ?? true;
        _captchaEnabled = _configuration.GetValue<bool?>("Security:Captcha:Enabled") ?? true;
        _externalCaptchaEnabled = _configuration.GetValue<bool?>("Security:Captcha:UseExternal") ?? true;
        _allowLocalCaptchaFallback = _configuration.GetValue<bool?>("Security:Captcha:AllowLocalFallback") ?? true;
        _totpFallbackEnabled = _configuration.GetValue<bool?>("Security:TwoFactor:EnableAuthenticatorFallback") ?? true;
        _twoFactorCodeTtlMinutes = Math.Clamp(_configuration.GetValue<int?>("Security:TwoFactor:CodeTtlMinutes") ?? 10, 1, 30);
        _twoFactorResendCooldownSeconds = Math.Clamp(_configuration.GetValue<int?>("Security:TwoFactor:ResendCooldownSeconds") ?? 30, 5, 300);
    }

    [HttpGet("captcha-config")]
    [AllowAnonymous]
    public ActionResult<CaptchaConfigResponse> GetCaptchaConfig()
    {
        var siteKey = _configuration["Security:Captcha:SiteKey"];
        var useExternal = _captchaEnabled && _externalCaptchaEnabled && !string.IsNullOrWhiteSpace(siteKey);
        return Ok(new CaptchaConfigResponse
        {
            Enabled = _captchaEnabled,
            Mode = useExternal ? "external" : "local",
            SiteKey = useExternal ? siteKey : null
        });
    }

    [HttpGet("captcha")]
    [AllowAnonymous]
    public ActionResult<CaptchaChallengeResponse> GetCaptcha()
    {
        var left = Random.Shared.Next(1, 10);
        var right = Random.Shared.Next(1, 10);
        var captchaId = Guid.NewGuid().ToString("N");
        var expected = (left + right).ToString();
        _cache.Set($"captcha:{captchaId}", expected, TimeSpan.FromMinutes(3));
        return Ok(new CaptchaChallengeResponse
        {
            CaptchaId = captchaId,
            Question = $"{left} + {right} = ?",
            ExpiresInSeconds = 180
        });
    }

    /// <summary>Текущий пользователь по JWT (любая авторизованная роль).</summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<LoginResponse>> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new LoginResponse
        {
            Token = string.Empty,
            Username = user.UserName,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (_captchaEnabled && !await IsCaptchaValidAsync(request, HttpContext.RequestAborted))
        {
            return BadRequest(new { message = "Неверная капча. Попробуйте снова." });
        }

        var user = await _userManager.FindByNameAsync(request.Username ?? "");
        if (user == null && request.Username?.Contains("@") == true)
            user = await _userManager.FindByEmailAsync(request.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        if (_twoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = "Для 2FA у пользователя не указан email." });

            var code = Random.Shared.Next(100000, 1000000).ToString();
            var challengeId = Guid.NewGuid().ToString("N");
            _cache.Set($"2fa:{challengeId}",
                new PendingTwoFactorLogin
                {
                    UserId = user.Id,
                    Code = code
                },
                TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));

            await SendApiTwoFactorEmailOrLogAsync(user.Email, code, user.UserName);

            var methods = new List<string> { "email" };
            var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (_totpFallbackEnabled && !string.IsNullOrWhiteSpace(authenticatorKey))
            {
                methods.Add("totp");
                methods.Add("recovery");
            }

            return Ok(new LoginResponse
            {
                RequiresTwoFactor = true,
                TwoFactorChallengeId = challengeId,
                Username = user.UserName,
                Email = user.Email,
                Roles = new List<string>(),
                TwoFactorMethods = methods
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.UserName!,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Неверный запрос 2FA." });

        if (!_cache.TryGetValue<PendingTwoFactorLogin>($"2fa:{request.ChallengeId}", out var pending) || pending == null)
            return BadRequest(new { message = "Код 2FA истёк или недействителен." });

        var user = await _userManager.FindByIdAsync(pending.UserId);
        if (user == null)
            return Unauthorized(new { message = "Пользователь не найден." });

        var method = (request.Method ?? "email").Trim().ToLowerInvariant();
        var code = request.Code.Trim().Replace(" ", string.Empty);

        var valid = method switch
        {
            "email" => string.Equals(pending.Code, code, StringComparison.Ordinal),
            "totp" => _totpFallbackEnabled &&
                      await _userManager.VerifyTwoFactorTokenAsync(
                          user,
                          _userManager.Options.Tokens.AuthenticatorTokenProvider,
                          code),
            "recovery" => _totpFallbackEnabled &&
                          (await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded,
            _ => false
        };

        if (!valid)
            return Unauthorized(new { message = "Неверный код подтверждения." });

        _cache.Remove($"2fa:{request.ChallengeId}");

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.UserName!,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }

    [HttpPost("resend-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult> ResendTwoFactor([FromBody] ResendTwoFactorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId))
            return BadRequest(new { message = "Неверный запрос 2FA." });

        if (!_cache.TryGetValue<PendingTwoFactorLogin>($"2fa:{request.ChallengeId}", out var pending) || pending == null)
            return BadRequest(new { message = "Код 2FA истёк или недействителен." });

        if (_cache.TryGetValue<DateTimeOffset>($"2fa:resend:{request.ChallengeId}", out var nextResendAt))
        {
            var waitSeconds = (int)Math.Ceiling((nextResendAt - DateTimeOffset.UtcNow).TotalSeconds);
            if (waitSeconds > 0)
            {
                return BadRequest(new { message = $"Повторная отправка доступна через {waitSeconds} сек.", resendAfterSeconds = waitSeconds });
            }
        }

        var user = await _userManager.FindByIdAsync(pending.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return BadRequest(new { message = "Пользователь не найден или email не указан." });

        var newCode = Random.Shared.Next(100000, 1000000).ToString();
        pending.Code = newCode;
        _cache.Set($"2fa:{request.ChallengeId}", pending, TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));
        _cache.Set(
            $"2fa:resend:{request.ChallengeId}",
            DateTimeOffset.UtcNow.AddSeconds(_twoFactorResendCooldownSeconds),
            TimeSpan.FromMinutes(_twoFactorCodeTtlMinutes));

        await SendApiTwoFactorEmailOrLogAsync(user.Email, newCode, user.UserName, isResend: true);

        return Ok(new { message = "Код подтверждения отправлен повторно.", resendAfterSeconds = _twoFactorResendCooldownSeconds });
    }

    [HttpPost("2fa/setup-authenticator")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AuthenticatorSetupResponse>> SetupAuthenticator()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { message = "Не удалось получить ключ аутентификатора." });

        var issuer = Uri.EscapeDataString(_configuration["Jwt:Issuer"] ?? "OrgTechRepair");
        var userLabel = Uri.EscapeDataString(user.Email ?? user.UserName ?? "user");
        var encodedKey = key.Replace(" ", string.Empty);
        var uri = $"otpauth://totp/{issuer}:{userLabel}?secret={encodedKey}&issuer={issuer}&digits=6";

        return Ok(new AuthenticatorSetupResponse
        {
            SharedKey = encodedKey,
            OtpauthUri = uri
        });
    }

    [HttpPost("2fa/enable-authenticator")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<EnableAuthenticatorResponse>> EnableAuthenticator([FromBody] EnableAuthenticatorRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        var code = (request.Code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
            return BadRequest(new { message = "Неверный код из приложения-аутентификатора." });

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        return Ok(new EnableAuthenticatorResponse
        {
            RecoveryCodes = codes?.ToList() ?? new List<string>()
        });
    }

    // POST: api/auth/register (только для администратора; доступ по JWT или cookie)
    [HttpPost("register")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application", Roles = "Administrator")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new IdentityUser
        {
            UserName = request.Username,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            await _userManager.AddToRoleAsync(user, request.Role);
        }

        return Ok(new { message = "Пользователь успешно создан", userId = user.Id });
    }

    // POST: api/auth/register-public (для мобильного приложения и регистрации без прав админа)
    [HttpPost("register-public")]
    [AllowAnonymous]
    public async Task<ActionResult> RegisterPublic([FromBody] RegisterPublicRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Пароли не совпадают" });

        if (await _userManager.FindByNameAsync(request.Username ?? "") != null)
            return BadRequest(new { message = "Пользователь с таким логином уже существует" });

        if (await _userManager.FindByEmailAsync(request.Email ?? "") != null)
            return BadRequest(new { message = "Пользователь с таким email уже существует" });

        var user = new IdentityUser
        {
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password!);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // Публичная регистрация — только клиенты; сотрудников создаёт администратор.
        var requested = (request.UserType ?? "Client").Trim();
        if (!string.Equals(requested, "Client", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Регистрация сотрудников доступна только администратору. Зарегистрируйтесь как клиент или обратитесь в офис." });

        await _userManager.AddToRoleAsync(user, "Client");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await ClientProfileProvisioner.GetOrCreateForUserAsync(
            context,
            user.Id,
            user.UserName ?? request.Username,
            user.Email);

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.UserName!,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }

    // POST: api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        string? resetToken = null;
        string? emailUsed = null;
        var user = await _userManager.FindByEmailAsync(request.Email ?? "");
        if (user != null)
        {
            resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            emailUsed = user.Email;
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var resetLink = $"{baseUrl}/ResetPassword?email={Uri.EscapeDataString(user.Email ?? "")}&token={Uri.EscapeDataString(resetToken)}";
            if (_emailSender != null)
                await _emailSender.SendPasswordResetEmailAsync(user.Email!, resetLink);
        }
        return Ok(new { message = "Если аккаунт с указанным email существует, на него отправлена ссылка для восстановления пароля.", token = resetToken, email = emailUsed });
    }

    // POST: api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "Пароли не совпадают" });

        var user = await _userManager.FindByEmailAsync(request.Email ?? "");
        if (user == null)
            return BadRequest(new { message = "Неверная ссылка для сброса пароля или ссылка устарела." });

        var result = await _userManager.ResetPasswordAsync(user, request.Token ?? "", request.NewPassword!);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Пароль успешно изменен." });
    }

    private string GenerateJwtToken(IdentityUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "OrgTechRepair",
            audience: _configuration["Jwt:Audience"] ?? "OrgTechRepair",
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? CaptchaId { get; set; }
        public string? CaptchaAnswer { get; set; }
        public string? CaptchaToken { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool RequiresTwoFactor { get; set; }
        public string? TwoFactorChallengeId { get; set; }
        public List<string> TwoFactorMethods { get; set; } = new();
    }

    public class CaptchaChallengeResponse
    {
        public string CaptchaId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }

    public class CaptchaConfigResponse
    {
        public bool Enabled { get; set; }
        public string Mode { get; set; } = "local";
        public string? SiteKey { get; set; }
    }

    public class VerifyTwoFactorRequest
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Method { get; set; }
    }

    public class ResendTwoFactorRequest
    {
        public string ChallengeId { get; set; } = string.Empty;
    }

    public class AuthenticatorSetupResponse
    {
        public string SharedKey { get; set; } = string.Empty;
        public string OtpauthUri { get; set; } = string.Empty;
    }

    public class EnableAuthenticatorRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public class EnableAuthenticatorResponse
    {
        public List<string> RecoveryCodes { get; set; } = new();
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class RegisterPublicRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        /// <summary>Должно быть только Client (совместимость со старыми клиентами). Иное отклоняется.</summary>
        public string UserType { get; set; } = "Client";
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    private async Task<bool> IsCaptchaValidAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (_externalCaptchaEnabled && !string.IsNullOrWhiteSpace(request.CaptchaToken))
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var externalOk = await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken);
            if (externalOk) return true;
            if (!_allowLocalCaptchaFallback) return false;
        }

        var captchaId = request.CaptchaId;
        var captchaAnswer = request.CaptchaAnswer;
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(captchaAnswer))
            return false;

        if (!_cache.TryGetValue<string>($"captcha:{captchaId}", out var expected) || string.IsNullOrWhiteSpace(expected))
            return false;

        _cache.Remove($"captcha:{captchaId}");
        return string.Equals(expected.Trim(), captchaAnswer.Trim(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Отправка 2FA в том же запросе: иначе scoped HttpClient из IEmailSender оказывается disposed после Task.Run.
    /// </summary>
    private async Task SendApiTwoFactorEmailOrLogAsync(string email, string code, string? userName, bool isResend = false)
    {
        if (_emailSender == null)
        {
            _logger.LogWarning(
                isResend ? "2FA код (повтор) для {UserName}: {Code}" : "2FA код для {UserName}: {Code}",
                userName,
                code);
            return;
        }

        try
        {
            var sent = await _emailSender.SendTwoFactorCodeAsync(email, code);
            if (!sent)
                _logger.LogWarning("Не удалось отправить 2FA код пользователю {UserName}. Резервный код: {Code}", userName, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки 2FA кода пользователю {UserName}. Резервный код: {Code}", userName, code);
        }
    }

    private sealed class PendingTwoFactorLogin
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
