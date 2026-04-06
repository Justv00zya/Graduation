using System.Collections.Concurrent;

namespace OrgTechRepair.Services;

/// <summary>
/// Хранит последние N записей лога в памяти для просмотра и экспорта в CSV.
/// </summary>
public sealed class InMemoryLogStore : ILogStore, ILoggerProvider
{
    private const int DefaultCapacity = 5000;
    private readonly FixedSizeQueue<LogEntry> _entries;

    public InMemoryLogStore(int capacity = DefaultCapacity)
    {
        _entries = new FixedSizeQueue<LogEntry>(capacity);
    }

    public IReadOnlyList<LogEntry> GetEntries(int? maxCount = null)
    {
        var list = _entries.ToList();
        if (maxCount.HasValue && list.Count > maxCount.Value)
            list = list.TakeLast(maxCount.Value).ToList();
        return list;
    }

    public void Add(LogEntry entry) => _entries.Enqueue(entry);

    public ILogger CreateLogger(string categoryName) => new StoreLogger(categoryName, this);

    public void Dispose() { }

    private sealed class StoreLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly InMemoryLogStore _store;

        public StoreLogger(string categoryName, InMemoryLogStore store)
        {
            _categoryName = categoryName;
            _store = store;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var exceptionStr = exception?.ToString();
            _store.Add(new LogEntry(
                DateTimeOffset.Now,
                logLevel.ToString(),
                _categoryName,
                message ?? "",
                exceptionStr
            ));
        }
    }

    private sealed class FixedSizeQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();
        private readonly int _capacity;
        private readonly object _lock = new();

        public FixedSizeQueue(int capacity) => _capacity = capacity;

        public void Enqueue(T item)
        {
            lock (_lock)
            {
                _queue.Enqueue(item);
                while (_queue.Count > _capacity && _queue.TryDequeue(out _)) { }
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                return _queue.ToList();
            }
        }
    }
}
