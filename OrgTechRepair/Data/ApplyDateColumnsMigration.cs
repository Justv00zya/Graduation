using Microsoft.EntityFrameworkCore;

namespace OrgTechRepair.Data;

/// <summary>
/// Приводит существующие столбцы дат к типу date (для PostgreSQL),
/// чтобы не ловить ошибку DateTimeKind=Unspecified при timestamptz.
/// </summary>
public static class ApplyDateColumnsMigration
{
    public static void Apply(ApplicationDbContext context)
    {
        var connTypeName = context.Database.GetDbConnection().GetType().Name;
        if (connTypeName != "NpgsqlConnection")
            return;

        context.Database.ExecuteSqlRaw(
            @"ALTER TABLE ""Employees"" ALTER COLUMN ""DateOfBirth"" TYPE date USING ""DateOfBirth""::date;");
        context.Database.ExecuteSqlRaw(
            @"ALTER TABLE ""Employees"" ALTER COLUMN ""HireDate"" TYPE date USING ""HireDate""::date;");
        // В заявках сохраняем и дату, и время (для «Создана N минут назад» в клиенте).
        context.Database.ExecuteSqlRaw(
            @"ALTER TABLE ""Orders"" ALTER COLUMN ""OrderDate"" TYPE timestamp without time zone USING ""OrderDate""::timestamp;");
        context.Database.ExecuteSqlRaw(
            @"ALTER TABLE ""Orders"" ALTER COLUMN ""CompletionDate"" TYPE timestamp without time zone USING ""CompletionDate""::timestamp;");
        context.Database.ExecuteSqlRaw(
            @"ALTER TABLE ""Sales"" ALTER COLUMN ""SaleDate"" TYPE date USING ""SaleDate""::date;");
    }
}

