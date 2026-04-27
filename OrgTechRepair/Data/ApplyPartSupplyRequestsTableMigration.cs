using Microsoft.EntityFrameworkCore;

namespace OrgTechRepair.Data;

/// <summary>
/// Таблица заявок на выдачу запчастей со склада (БД, созданные до появления сущности).
/// </summary>
public static class ApplyPartSupplyRequestsTableMigration
{
    public static void Apply(ApplicationDbContext context)
    {
        var connTypeName = context.Database.GetDbConnection().GetType().Name;

        if (connTypeName == "SqliteConnection")
        {
            context.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "PartSupplyRequests" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_PartSupplyRequests" PRIMARY KEY AUTOINCREMENT,
                    "PartId" INTEGER NOT NULL,
                    "Quantity" INTEGER NOT NULL,
                    "RequestedByUserId" TEXT NOT NULL,
                    "OrderId" INTEGER NULL,
                    "Comment" TEXT NULL,
                    "Status" TEXT NOT NULL DEFAULT 'Pending',
                    "CreatedAt" TEXT NOT NULL,
                    "ProcessedAt" TEXT NULL,
                    "ProcessedByUserId" TEXT NULL,
                    "WarehouseComment" TEXT NULL,
                    CONSTRAINT "FK_PartSupplyRequests_Parts_PartId" FOREIGN KEY ("PartId") REFERENCES "Parts" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_PartSupplyRequests_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE SET NULL
                );
                """);
        }
        else if (connTypeName == "NpgsqlConnection")
        {
            context.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "PartSupplyRequests" (
                    "Id" serial NOT NULL CONSTRAINT "PK_PartSupplyRequests" PRIMARY KEY,
                    "PartId" integer NOT NULL,
                    "Quantity" integer NOT NULL,
                    "RequestedByUserId" text NOT NULL,
                    "OrderId" integer NULL,
                    "Comment" text NULL,
                    "Status" text NOT NULL DEFAULT 'Pending',
                    "CreatedAt" timestamp without time zone NOT NULL,
                    "ProcessedAt" timestamp without time zone NULL,
                    "ProcessedByUserId" text NULL,
                    "WarehouseComment" text NULL,
                    CONSTRAINT "FK_PartSupplyRequests_Parts_PartId" FOREIGN KEY ("PartId") REFERENCES "Parts" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_PartSupplyRequests_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE SET NULL
                );
                """);
        }
    }
}
