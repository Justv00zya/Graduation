namespace OrgTechRepair.Services;

/// <summary>
/// Результат создания резервной копии БД.
/// </summary>
public class BackupResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public byte[]? Content { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IDatabaseBackupService
{
    Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default);
}
