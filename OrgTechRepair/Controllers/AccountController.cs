using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IAntiforgery antiforgery,
        ILogger<AccountController> logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _antiforgery = antiforgery;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string loginOrEmail, string password, bool rememberMe, string? returnUrl = null)
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

        var user = await _userManager.FindByNameAsync(loginOrEmail);
        if (user == null && loginOrEmail.Contains('@', StringComparison.Ordinal))
        {
            user = await _userManager.FindByEmailAsync(loginOrEmail);
        }

        if (user == null)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Пользователь с таким логином или email не найден")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            password,
            rememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return Redirect(returnUrl ?? "/");
        }
        else if (result.IsLockedOut)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Учетная запись заблокирована")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }
        else if (result.RequiresTwoFactor)
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Требуется двухфакторная аутентификация")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }
        else
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный пароль")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }
    }

    /// <summary>Регистрация клиента: запись в БД (AspNetUsers + роль + карточка Client) и вход в ту же сессию, что и /Account/Login.</summary>
    [HttpPost("Register")]
    public async Task<IActionResult> RegisterClient(string username, string email, string password, string confirmPassword)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return Redirect($"/Register?error={Uri.EscapeDataString("Неверный запрос. Обновите страницу и попробуйте снова.")}");
        }

        username = (username ?? "").Trim();
        email = (email ?? "").Trim();
        password ??= string.Empty;
        confirmPassword ??= string.Empty;

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

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await ClientProfileProvisioner.GetOrCreateForUserAsync(db, user.Id, user.UserName, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка создания карточки клиента для {UserId}", user.Id);
            return Redirect($"/Register?error={Uri.EscapeDataString("Учётная запись создана, но не удалось создать профиль клиента. Войдите позже или обратитесь в офис.")}");
        }

        var signIn = await _signInManager.PasswordSignInAsync(user.UserName!, password, isPersistent: false, lockoutOnFailure: false);
        if (!signIn.Succeeded)
        {
            _logger.LogWarning("После регистрации не удалось выполнить вход для {UserName}", user.UserName);
            return Redirect($"/Login?error={Uri.EscapeDataString("Регистрация выполнена. Войдите с указанным логином и паролем.")}");
        }

        return Redirect("/");
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
        return Redirect("/Login");
    }
}
