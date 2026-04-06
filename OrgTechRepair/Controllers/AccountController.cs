using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OrgTechRepair.Controllers;

[Route("Account")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IAntiforgery antiforgery,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string loginOrEmail, string password, bool rememberMe, string? returnUrl = null)
    {
        // Валидация антифоржерного токена
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Неверный запрос. Пожалуйста, попробуйте снова.")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        if (string.IsNullOrWhiteSpace(loginOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return Redirect($"/Login?error={Uri.EscapeDataString("Укажите логин/email и пароль")}&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
        }

        var user = await _userManager.FindByNameAsync(loginOrEmail);
        if (user == null && loginOrEmail.Contains('@'))
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
