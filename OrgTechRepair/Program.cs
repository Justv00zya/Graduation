using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OrgTechRepair.Components;
using OrgTechRepair.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Локальные секреты (SMTP и т.д.) — не коммитить; см. appsettings.Local.json.example
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var portFromEnv = Environment.GetEnvironmentVariable("PORT");
var effectivePort = int.TryParse(portFromEnv, out var parsedPort) ? parsedPort : 5121;
// На Render порт приходит через переменную PORT. Локально используем 5121.
builder.WebHost.UseUrls($"http://0.0.0.0:{effectivePort}");
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("OrgTechRepair");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath) && builder.Environment.IsDevelopment())
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
}
else if (string.IsNullOrWhiteSpace(dataProtectionKeysPath) && !builder.Environment.IsDevelopment())
{
    dataProtectionKeysPath = "/var/data/orgtechrepair-dpkeys";
}
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
var dataProtectionPfxPath = builder.Configuration["DataProtection:CertificatePath"];
var dataProtectionPfxPassword = builder.Configuration["DataProtection:CertificatePassword"];
if (!string.IsNullOrWhiteSpace(dataProtectionPfxPath) && File.Exists(dataProtectionPfxPath))
{
    var cert = string.IsNullOrWhiteSpace(dataProtectionPfxPassword)
        ? new X509Certificate2(dataProtectionPfxPath)
        : new X509Certificate2(dataProtectionPfxPath, dataProtectionPfxPassword);
    dataProtectionBuilder.ProtectKeysWithCertificate(cert);
}

// Add API Controllers
builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = 10 * 1024 * 1024; });
builder.Services.AddMemoryCache();

// Antiforgery protection is handled by middleware (app.UseAntiforgery())

// Add Entity Framework (PostgreSQL with automatic SQLite fallback)
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                              ?? "Host=localhost;Port=5432;Database=orgtechrepairdb;Username=postgres;Password=postgres";
var sqliteFallbackConnectionString = builder.Configuration.GetConnectionString("SqliteFallback")
                                   ?? "Data Source=orgtechrepair.db";
var allowSqliteFallback =
    builder.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Database:AllowSqliteFallback");
var maxDbPoolSize = Math.Max(20, builder.Configuration.GetValue<int?>("Database:MaxPoolSize") ?? 120);
var dbCommandTimeoutSec = Math.Clamp(builder.Configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 12, 5, 60);
var postgresBuilder = new NpgsqlConnectionStringBuilder(postgresConnectionString)
{
    MaxPoolSize = maxDbPoolSize,
    MinPoolSize = 5,
    Timeout = 8,
    CommandTimeout = dbCommandTimeoutSec,
    KeepAlive = 30
};
postgresConnectionString = postgresBuilder.ConnectionString;

// Если базы с именем из строки подключения ещё нет — создаём (подключение к служебной БД postgres).
try
{
    PostgreSqlDbBootstrap.EnsureDatabaseExists(postgresConnectionString);
}
catch (Exception ex)
{
    Console.WriteLine($"[DB] Автосоздание базы PostgreSQL пропущено: {ex.Message}");
}

var usePostgres = true;
try
{
    using var testConnection = new NpgsqlConnection(postgresConnectionString);
    testConnection.Open();
    var pgInfo = new NpgsqlConnectionStringBuilder(postgresConnectionString);
    Console.WriteLine($"[DB] PostgreSQL: Host={pgInfo.Host}; Database={pgInfo.Database}; User={pgInfo.Username}");
}
catch (Exception ex)
{
    if (allowSqliteFallback)
    {
        usePostgres = false;
        Console.WriteLine($"[DB] PostgreSQL недоступен, используем SQLite fallback. Причина: {ex.Message}");
    }
    else
    {
        Console.WriteLine($"[DB] PostgreSQL недоступен, fallback в SQLite отключен. Причина: {ex.Message}");
        throw new InvalidOperationException(
            "PostgreSQL недоступен, а SQLite fallback отключен. " +
            "Проверьте строку подключения DefaultConnection и доступ к PostgreSQL. " +
            "Это защищает пользователей от работы с временной/пустой fallback-БД.");
    }
}

if (usePostgres)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(postgresConnectionString, npgsql =>
            npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(8), null)
                .CommandTimeout(dbCommandTimeoutSec)));

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(postgresConnectionString, npgsql =>
            npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(8), null)
                .CommandTimeout(dbCommandTimeoutSec)),
        ServiceLifetime.Scoped);
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteFallbackConnectionString));

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteFallbackConnectionString),
        ServiceLifetime.Scoped);
}

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Максимально упрощаем политику паролей для учебного проекта:
    // допускаются простые пароли длиной от 6 символов (например, 111111 или qwerty1)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Разрешаем в логине русские буквы и пробелы (по умолчанию Identity разрешает только латиницу/цифры)
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ " +
        "абвгдеёжзийклмнопрстуфхцчшщьыъэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЬЫЪЭЮЯ";
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = static (context, _) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "30";
        return ValueTask.CompletedTask;
    };

    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 180,
                Window = TimeSpan.FromSeconds(30),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add authentication state provider for Blazor Server
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider>();

// Почта: Brevo API (HTTPS) при блокировке SMTP на хостинге; иначе SMTP; иначе вывод в логи.
// Если API-ключ Brevo задан, автоматически выбираем API-провайдер даже при забытом флаге Enabled.
var brevoApiEnabled = builder.Configuration.GetValue<bool?>("Email:Brevo:Enabled") ?? false;
var brevoApiKey = builder.Configuration["Email:Brevo:ApiKey"];
var useBrevoApi = brevoApiEnabled || !string.IsNullOrWhiteSpace(brevoApiKey);
var smtpUser = builder.Configuration["Email:Smtp:Username"];
var smtpPass = builder.Configuration["Email:Smtp:Password"];
var smtpFrom = builder.Configuration["Email:Smtp:FromEmail"];
var smtpEnabledExplicit = builder.Configuration.GetValue<bool?>("Email:Smtp:Enabled") ?? false;
var smtpConfigured = !string.IsNullOrWhiteSpace(smtpUser) &&
                     !string.IsNullOrWhiteSpace(smtpPass) &&
                     !string.IsNullOrWhiteSpace(smtpFrom);
var smtpEnabled = smtpEnabledExplicit || smtpConfigured;

builder.Services.AddScoped<OrgTechRepair.Services.SmtpEmailSender>();
builder.Services.AddScoped<OrgTechRepair.Services.DevelopmentEmailSender>();

if (useBrevoApi)
{
    builder.Services.AddHttpClient<OrgTechRepair.Services.BrevoTransactionalEmailSender>((sp, client) =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var baseUrl = (cfg["Email:Brevo:BaseUrl"] ?? "https://api.brevo.com").TrimEnd('/');
        client.BaseAddress = new Uri($"{baseUrl}/v3/");
        var timeoutSec = Math.Clamp(cfg.GetValue<int?>("Email:Brevo:TimeoutSeconds") ?? 60, 10, 300);
        client.Timeout = TimeSpan.FromSeconds(timeoutSec);
    });
    builder.Services.AddScoped<OrgTechRepair.Services.IEmailSender, OrgTechRepair.Services.BrevoTransactionalEmailSender>();
}
else if (smtpEnabled)
{
    builder.Services.AddScoped<OrgTechRepair.Services.IEmailSender, OrgTechRepair.Services.SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<OrgTechRepair.Services.IEmailSender, OrgTechRepair.Services.DevelopmentEmailSender>();
}
builder.Services.AddScoped<OrgTechRepair.Services.IOrderPdfService, OrgTechRepair.Services.OrderPdfService>();
builder.Services.AddHttpClient<OrgTechRepair.Services.ICaptchaVerifier, OrgTechRepair.Services.TurnstileCaptchaVerifier>();

// Хранилище логов для просмотра и экспорта в CSV (только для администратора)
var logStore = new OrgTechRepair.Services.InMemoryLogStore(5000);
builder.Services.AddSingleton<OrgTechRepair.Services.ILogStore>(logStore);
builder.Logging.AddProvider(logStore);

// Резервное копирование БД
builder.Services.AddScoped<OrgTechRepair.Services.IDatabaseBackupService, OrgTechRepair.Services.DatabaseBackupService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OrgTechRepair";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OrgTechRepair";

// Configure JWT Authentication for API (Identity already adds Cookie authentication for Blazor)
builder.Services.AddAuthentication(options =>
{
    // Identity уже установил Cookie как схему по умолчанию
    // Добавляем JWT для API
})
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    
    // Настройка для работы JWT в API контроллерах
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/api"))
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
            }
            return Task.CompletedTask;
        }
    };
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrgTechRepair API",
        Version = "v1",
        Description = "REST API для информационной системы ВузяПринт"
    });

    // Добавляем JWT авторизацию в Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var wwwroot = app.Environment.WebRootPath;
if (!string.IsNullOrEmpty(wwwroot))
    Directory.CreateDirectory(Path.Combine(wwwroot, "uploads", "products"));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER")))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Map API Controllers
app.MapControllers().RequireRateLimiting("api");

// Map Blazor Components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Configure Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrgTechRepair API v1");
    });
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        // Добавить столбцы UserId и Email в Clients, если БД создана до их появления
        ApplyClientColumnsMigration.Apply(context);
        ApplyProductImageColumnMigration.Apply(context);
        ApplyDateColumnsMigration.Apply(context);
        ApplyPartSupplyRequestsTableMigration.Apply(context);

        // Seed initial data
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var seedLogger = services.GetRequiredService<ILogger<Program>>();
        seedLogger.LogError(ex, "An error occurred while seeding the database.");
    }
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var emailMode = useBrevoApi ? "Brevo API" : smtpEnabled ? "SMTP" : "Development (код 2FA → лог и файл OrgTechRepair-2FA-last.txt на рабочем столе)";
logger.LogInformation("Режим отправки почты: {EmailMode}", emailMode);
if (!useBrevoApi && !smtpEnabled)
{
    logger.LogWarning(
        "SMTP/Brevo не настроены. Создайте OrgTechRepair/appsettings.Local.json по образцу appsettings.Local.json.example");
}
logger.LogInformation("Запуск сервера на http://0.0.0.0:{Port}", effectivePort);

try
{
    app.Run();
}
catch (System.IO.IOException ex) when (ex.Message.Contains("address already in use") || ex.Message.Contains("address is already in use"))
{
    logger.LogError(ex, "Порт уже занят. Остановите другие экземпляры приложения или измените порт в launchSettings.json");
    Console.WriteLine("\n===========================================");
    Console.WriteLine($"ОШИБКА: Порт {effectivePort} уже занят!");
    Console.WriteLine("Остановите другой процесс, который уже слушает этот порт.");
    Console.WriteLine("===========================================\n");
    throw; // Пробрасываем исключение дальше, чтобы приложение не запустилось с ошибкой
}
