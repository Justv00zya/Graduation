using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OrgTechRepair.Data;

/// <summary>Добавляет столбец ImageUrl в Products для существующих БД.</summary>
public static class ApplyProductImageColumnMigration
{
    public static void Apply(ApplicationDbContext context)
    {
        var connTypeName = context.Database.GetDbConnection().GetType().Name;

        if (connTypeName == "SqliteConnection")
        {
            try
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN ImageUrl TEXT;");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && (ex.Message?.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase) == true))
            {
            }
        }
        else if (connTypeName == "NpgsqlConnection")
        {
            context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" TEXT;");
        }
    }
}
