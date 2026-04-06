namespace OrgTechRepair.Services;

/// <summary>
/// Запись лога для просмотра и экспорта.
/// </summary>
public record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Category,
    string Message,
    string? Exception
);

/// <summary>
/// Хранилище последних записей лога (кольцевой буфер в памяти).
/// </summary>
public interface ILogStore
{
    IReadOnlyList<LogEntry> GetEntries(int? maxCount = null);
}
