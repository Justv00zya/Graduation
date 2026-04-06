using Npgsql;

namespace OrgTechRepair.Data;

/// <summary>Подготовка базы PostgreSQL перед запуском приложения.</summary>
public static class PostgreSqlDbBootstrap
{
    /// <summary>Создаёт базу данных, если её ещё нет (подключение к служебной БД postgres).</summary>
    public static void EnsureDatabaseExists(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;
        if (string.IsNullOrWhiteSpace(dbName))
            return;

        builder.Database = "postgres";
        using var conn = new NpgsqlConnection(builder.ConnectionString);
        conn.Open();

        using (var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", conn))
        {
            check.Parameters.AddWithValue("n", dbName);
            if (check.ExecuteScalar() != null)
                return;
        }

        var safe = dbName.Replace("\"", "\"\"");
        using (var create = new NpgsqlCommand($"CREATE DATABASE \"{safe}\"", conn))
        {
            create.ExecuteNonQuery();
        }

        Console.WriteLine($"[DB] Создана база данных PostgreSQL «{dbName}».");
    }
}
