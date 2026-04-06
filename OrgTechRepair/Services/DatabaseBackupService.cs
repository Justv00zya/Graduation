using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OrgTechRepair.Data;

namespace OrgTechRepair.Services;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;

    public DatabaseBackupService(
        IConfiguration configuration,
        ApplicationDbContext db,
        IHostEnvironment env)
    {
        _configuration = configuration;
        _db = db;
        _env = env;
    }

    public async Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        // Важно: бэкапим ту же БД, с которой сейчас работает EF (после переноса ПК часто SQLite в bin,
        // а в конфиге остаётся строка Postgres — старая логика давала «файл не найден» или требовала pg_dump зря).
        var provider = _db.Database.ProviderName ?? "";
        var liveCs = _db.Database.GetConnectionString();

        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            var postgresCs = liveCs ?? _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(postgresCs))
                return new BackupResult { Success = false, ErrorMessage = "Строка подключения PostgreSQL не задана." };
            return await BackupPostgresAsync(postgresCs, cancellationToken);
        }

        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqliteCs = liveCs
                           ?? _configuration.GetConnectionString("SqliteFallback")
                           ?? "Data Source=orgtechrepair.db";
            return BackupSqlite(sqliteCs);
        }

        return new BackupResult
        {
            Success = false,
            ErrorMessage = $"Резервное копирование для провайдера «{provider}» не настроено."
        };
    }

    private BackupResult BackupSqlite(string connectionString)
    {
        try
        {
            var match = Regex.Match(connectionString, @"Data Source\s*=\s*(.+?)(?:;|$)", RegexOptions.IgnoreCase);
            var dataSource = match.Success ? match.Groups[1].Value.Trim().Trim('"') : "orgtechrepair.db";

            string? path = null;
            foreach (var candidate in EnumerateSqliteFileCandidates(dataSource))
            {
                if (File.Exists(candidate))
                {
                    path = candidate;
                    break;
                }
            }

            if (path == null)
            {
                var tried = string.Join("; ", EnumerateSqliteFileCandidates(dataSource).Distinct());
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage =
                        $"Файл SQLite не найден. Искали: {tried}. " +
                        "Укажите полный путь в appsettings.json → ConnectionStrings:SqliteFallback, например " +
                        "\"Data Source=C:\\\\Data\\\\orgtechrepair.db\""
                };
            }

            var bytes = File.ReadAllBytes(path);
            var fileName = $"orgtechrepair_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            return new BackupResult
            {
                Success = true,
                FileName = fileName,
                Content = bytes,
                ContentType = "application/octet-stream"
            };
        }
        catch (Exception ex)
        {
            return new BackupResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private IEnumerable<string> EnumerateSqliteFileCandidates(string dataSource)
    {
        if (Path.IsPathRooted(dataSource))
        {
            yield return dataSource;
            yield break;
        }

        yield return Path.Combine(AppContext.BaseDirectory, dataSource);
        yield return Path.Combine(_env.ContentRootPath, dataSource);
        yield return Path.Combine(Directory.GetCurrentDirectory(), dataSource);
    }

    private async Task<BackupResult> BackupPostgresAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var host = builder.Host ?? "localhost";
            var port = builder.Port;
            var database = builder.Database ?? "orgtechrepairdb";
            var username = builder.Username ?? "postgres";
            var password = builder.Password;

            var pgDump = FindPgDump();
            if (string.IsNullOrEmpty(pgDump))
            {
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage =
                        "pg_dump не найден. Установите PostgreSQL (в комплекте есть pg_dump) и добавьте папку bin в PATH, " +
                        "либо укажите полный путь в appsettings.json → Database:PgDumpPath (например C:\\\\Program Files\\\\PostgreSQL\\\\16\\\\bin\\\\pg_dump.exe)."
                };
            }

            var fileName = $"orgtechrepair_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            var tempFile = Path.Combine(Path.GetTempPath(), fileName);

            var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PGPASSWORD"] = password
            };

            var args = $"--host={host} --port={port} --username={username} --no-password --file=\"{tempFile}\" --format=plain \"{database}\"";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pgDump,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            foreach (var (key, value) in env)
                process.StartInfo.Environment[key] = value ?? "";

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || !File.Exists(tempFile))
            {
                if (File.Exists(tempFile)) try { File.Delete(tempFile); } catch { /* ignore */ }

                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = stderr.Length > 0 ? stderr.Trim() : "Ошибка pg_dump (код выхода не 0)."
                };
            }

            var content = await File.ReadAllBytesAsync(tempFile, cancellationToken);
            try { File.Delete(tempFile); } catch { /* ignore */ }

            return new BackupResult
            {
                Success = true,
                FileName = fileName,
                Content = content,
                ContentType = "application/sql"
            };
        }
        catch (Exception ex)
        {
            return new BackupResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private string? FindPgDump()
    {
        var configured = _configuration["Database:PgDumpPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var p = configured.Trim().Trim('"');
            if (File.Exists(p))
                return p;
        }

        var exe = OperatingSystem.IsWindows() ? "pg_dump.exe" : "pg_dump";
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var full = Path.Combine(dir.Trim(), exe);
                if (File.Exists(full)) return full;
            }
        }

        // На Windows часто pg_dump есть, но bin не добавлен в PATH.
        if (OperatingSystem.IsWindows())
        {
            var candidates = EnumerateCommonWindowsPgDumpPaths(exe);
            foreach (var full in candidates)
            {
                if (File.Exists(full))
                    return full;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCommonWindowsPgDumpPaths(string exe)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            // Пробуем типовые версии PostgreSQL.
            for (var v = 18; v >= 9; v--)
            {
                yield return Path.Combine(root, "PostgreSQL", v.ToString(), "bin", exe);
            }
        }
    }
}
