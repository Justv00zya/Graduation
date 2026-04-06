using Microsoft.EntityFrameworkCore;

namespace OrgTechRepair.Data;

/// <summary>
/// Добавляет столбцы UserId и Email в таблицу Clients, если их ещё нет
/// (для БД, созданных до появления этих полей в модели).
/// </summary>
public static class ApplyClientColumnsMigration
{
    public static void Apply(ApplicationDbContext context)
    {
        var connTypeName = context.Database.GetDbConnection().GetType().Name;

        if (connTypeName == "SqliteConnection")
        {
            try
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Clients ADD COLUMN UserId TEXT;");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1 && (ex.Message?.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase) == true))
            {
                // столбец уже есть
            }

            try
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Clients ADD COLUMN Email TEXT;");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1 && (ex.Message?.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase) == true))
            {
                // столбец уже есть
            }
        }
        else if (connTypeName == "NpgsqlConnection")
        {
            context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Clients"" ADD COLUMN IF NOT EXISTS ""UserId"" TEXT;");
            context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Clients"" ADD COLUMN IF NOT EXISTS ""Email"" TEXT;");
        }
    }
}
