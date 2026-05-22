namespace OrgTechRepair.Services;

/// <summary>
/// appsettings.Local.json в bin\Debug часто устаревает (пустой Password).
/// Подгружаем все кандидаты; последний найденный перекрывает предыдущие — приоритет у корня проекта.
/// </summary>
public static class LocalConfigurationExtensions
{
    public static void AddOrgTechRepairLocalSettings(this ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        var candidates = new List<string>();

        void AddCandidate(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var full = Path.GetFullPath(path);
            if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase))
                candidates.Add(full);
        }

        AddCandidate(Path.Combine(environment.ContentRootPath, "appsettings.Local.json"));
        AddCandidate(Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json"));
        AddCandidate(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json"));

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                AddCandidate(Path.Combine(dir.FullName, "appsettings.Local.json"));
                break;
            }

            dir = dir.Parent;
        }

        foreach (var path in candidates.Where(File.Exists))
        {
            configuration.AddJsonFile(path, optional: true, reloadOnChange: environment.IsDevelopment());
        }
    }
}
