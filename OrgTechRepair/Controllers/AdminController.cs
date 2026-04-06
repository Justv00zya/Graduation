using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTechRepair.Services;
using System.Globalization;
using System.Text;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application", Roles = "Administrator")]
public class AdminController : ControllerBase
{
    private readonly ILogStore _logStore;
    private readonly IDatabaseBackupService _backupService;

    public AdminController(ILogStore logStore, IDatabaseBackupService backupService)
    {
        _logStore = logStore;
        _backupService = backupService;
    }

    /// <summary>Получить последние записи лога (JSON).</summary>
    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int? maxCount = 1000)
    {
        var entries = _logStore.GetEntries(maxCount);
        return Ok(entries.Select(e => new
        {
            e.Timestamp,
            e.Level,
            e.Category,
            e.Message,
            e.Exception
        }));
    }

    /// <summary>Скачать логи в формате CSV.</summary>
    [HttpGet("logs/csv")]
    public IActionResult DownloadLogsCsv([FromQuery] int? maxCount = 10000)
    {
        var entries = _logStore.GetEntries(maxCount);
        var csv = BuildLogsCsv(entries);
        var fileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>Создать и скачать резервную копию БД.</summary>
    [HttpGet("backup")]
    public async Task<IActionResult> BackupDatabase(CancellationToken cancellationToken)
    {
        var result = await _backupService.CreateBackupAsync(cancellationToken);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return File(result.Content!, result.ContentType ?? "application/octet-stream", result.FileName);
    }

    private static string BuildLogsCsv(IReadOnlyList<LogEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp;Level;Category;Message;Exception");
        foreach (var e in entries)
        {
            var ts = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var level = CsvEscape(e.Level);
            var category = CsvEscape(e.Category);
            var message = CsvEscape(e.Message);
            var ex = CsvEscape(e.Exception ?? "");
            sb.AppendLine($"{ts};{level};{category};{message};{ex}");
        }
        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
