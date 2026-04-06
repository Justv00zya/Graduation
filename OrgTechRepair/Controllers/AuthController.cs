using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrgTechRepair.Data;
using OrgTechRepair.Models;
using OrgTechRepair.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailSender? _emailSender;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IEmailSender? emailSender = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _contextFactory = contextFactory;
        _emailSender = emailSender;
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
        var user = await _userManager.FindByNameAsync(request.Username ?? "");
        if (user == null && request.Username?.Contains("@") == true)
            user = await _userManager.FindByEmailAsync(request.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
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

        var role = request.UserType == "Client" ? "Client" : "Manager";
        await _userManager.AddToRoleAsync(user, role);

        if (role == "Client")
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Clients.Add(new Client
            {
                UserId = user.Id,
                FullName = request.Username ?? "",
                Email = request.Email
            });
            await context.SaveChangesAsync();
        }

        return Ok(new { message = "Регистрация успешна! Теперь вы можете войти в систему.", userId = user.Id });
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
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
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
        public string UserType { get; set; } = "Team"; // "Team" | "Client"
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
}
