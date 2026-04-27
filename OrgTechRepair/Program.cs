using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OrgTechRepair.Components;
using OrgTechRepair.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var portFromEnv = Environment.GetEnvironmentVariable("PORT");
var effectivePort = int.TryParse(portFromEnv, out var parsedPort) ? parsedPort : 5121;
// На Render порт приходит через переменную PORT. Локально используем 5121.
builder.WebHost.UseUrls($"http://0.0.0.0:{effectivePort}");
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add API Controllers
builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = 10 * 1024 * 1024; });

// Antiforgery protection is handled by middleware (app.UseAntiforgery())

// Add Entity Framework (PostgreSQL with automatic SQLite fallback)
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                              ?? "Host=localhost;Port=5432;Database=orgtechrepairdb;Username=postgres;Password=postgres";
var sqliteFallbackConnectionString = builder.Configuration.GetConnectionString("SqliteFallback")
                                   ?? "Data Source=orgtechrepair.db";

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
    usePostgres = false;
    Console.WriteLine($"[DB] PostgreSQL недоступен, используем SQLite fallback. Причина: {ex.Message}");
}

if (usePostgres)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(postgresConnectionString));

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(postgresConnectionString),
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
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/AccessDenied";
});

// Add authentication state provider for Blazor Server
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider>();

// Add Email Sender (для разработки - выводит в логи, в продакшене замените на реальную реализацию)
builder.Services.AddScoped<OrgTechRepair.Services.IEmailSender, OrgTechRepair.Services.DevelopmentEmailSender>();
builder.Services.AddScoped<OrgTechRepair.Services.IOrderPdfService, OrgTechRepair.Services.OrderPdfService>();

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

app.UseAuthentication();
app.UseAuthorization();

// Map API Controllers
app.MapControllers();

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
