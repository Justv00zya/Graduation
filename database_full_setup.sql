-- =============================================
-- Полный скрипт развертывания базы данных OrgTechRepairDb
-- Включает создание структуры и заполнение начальными данными
-- =============================================

USE master;
GO

-- Удаление существующей базы данных (если нужно пересоздать)
-- Раскомментируйте следующую строку, если нужно пересоздать БД:
-- DROP DATABASE IF EXISTS OrgTechRepairDb;
GO

-- Создание базы данных
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'OrgTechRepairDb')
BEGIN
    CREATE DATABASE OrgTechRepairDb;
END
GO

USE OrgTechRepairDb;
GO

PRINT 'База данных OrgTechRepairDb создана.';
PRINT 'Выполняется создание таблиц...';
GO

-- =============================================
-- Создание таблиц
-- =============================================

-- Таблица: Positions (Должности)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Positions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Positions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [Salary] DECIMAL(18,2) NOT NULL
    );
    PRINT 'Таблица Positions создана.';
END
GO

-- Таблица: Employees (Сотрудники)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Employees] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TabNumber] NVARCHAR(50) NOT NULL,
        [PositionId] INT NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [MiddleName] NVARCHAR(100) NULL,
        [DateOfBirth] DATETIME2 NOT NULL,
        [INN] NVARCHAR(20) NULL,
        [Address] NVARCHAR(500) NULL,
        [HireDate] DATETIME2 NOT NULL,
        CONSTRAINT [FK_Employees_Positions] FOREIGN KEY ([PositionId]) 
            REFERENCES [dbo].[Positions]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Таблица Employees создана.';
END
GO

-- Таблица: Clients (Клиенты)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Clients]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Clients] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FullName] NVARCHAR(200) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [Phone] NVARCHAR(50) NULL
    );
    PRINT 'Таблица Clients создана.';
END
GO

-- Таблица: Suppliers (Поставщики)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Suppliers] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [INN] NVARCHAR(20) NULL,
        [AccountNumber] NVARCHAR(50) NULL,
        [Phone] NVARCHAR(50) NULL
    );
    PRINT 'Таблица Suppliers создана.';
END
GO

-- Таблица: Products (Товары)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] NVARCHAR(50) NOT NULL,
        [SupplierId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Model] NVARCHAR(100) NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 0,
        CONSTRAINT [FK_Products_Suppliers] FOREIGN KEY ([SupplierId]) 
            REFERENCES [dbo].[Suppliers]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Таблица Products создана.';
END
GO

-- Таблица: Parts (Запчасти)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Parts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Parts] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] NVARCHAR(50) NOT NULL,
        [SupplierId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 0,
        CONSTRAINT [FK_Parts_Suppliers] FOREIGN KEY ([SupplierId]) 
            REFERENCES [dbo].[Suppliers]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Таблица Parts создана.';
END
GO

-- Таблица: Orders (Заявки)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderNumber] NVARCHAR(50) NOT NULL,
        [ClientId] INT NOT NULL,
        [EquipmentModel] NVARCHAR(200) NOT NULL,
        [ConditionDescription] NVARCHAR(MAX) NULL,
        [ComplaintDescription] NVARCHAR(MAX) NULL,
        [EmployeeId] INT NULL,
        [Cost] DECIMAL(18,2) NULL,
        [OrderDate] DATETIME2 NOT NULL,
        [CompletionDate] DATETIME2 NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT N'Принят',
        CONSTRAINT [FK_Orders_Clients] FOREIGN KEY ([ClientId]) 
            REFERENCES [dbo].[Clients]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Employees] FOREIGN KEY ([EmployeeId]) 
            REFERENCES [dbo].[Employees]([Id]) ON DELETE SET NULL
    );
    PRINT 'Таблица Orders создана.';
END
GO

-- Таблица: OrderParts (Запчасти в заявке)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderParts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OrderParts] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [PartId] INT NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_OrderParts_Orders] FOREIGN KEY ([OrderId]) 
            REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderParts_Parts] FOREIGN KEY ([PartId]) 
            REFERENCES [dbo].[Parts]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Таблица OrderParts создана.';
END
GO

-- Таблица: WorkTypes (Типы работ)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WorkTypes] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [Price] DECIMAL(18,2) NOT NULL
    );
    PRINT 'Таблица WorkTypes создана.';
END
GO

-- Таблица: Works (Работы)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Works]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Works] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [WorkTypeId] INT NOT NULL,
        [SequenceNumber] INT NOT NULL,
        CONSTRAINT [FK_Works_Orders] FOREIGN KEY ([OrderId]) 
            REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Works_WorkTypes] FOREIGN KEY ([WorkTypeId]) 
            REFERENCES [dbo].[WorkTypes]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Таблица Works создана.';
END
GO

-- Таблица: Sales (Продажи)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Sales]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Sales] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SaleNumber] NVARCHAR(50) NOT NULL,
        [ClientId] INT NULL,
        [SaleDate] DATETIME2 NOT NULL,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT [FK_Sales_Clients] FOREIGN KEY ([ClientId]) 
            REFERENCES [dbo].[Clients]([Id]) ON DELETE SET NULL
    );
    PRINT 'Таблица Sales создана.';
END
GO

-- Таблица: SaleItems (Позиции продажи)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SaleItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SaleItems] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SaleId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        [TotalPrice] DECIMAL(18,2) NOT NULL,
        CONSTRAINT [FK_SaleItems_Sales] FOREIGN KEY ([SaleId]) 
            REFERENCES [dbo].[Sales]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SaleItems_Products] FOREIGN KEY ([ProductId]) 
            REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Таблица SaleItems создана.';
END
GO

-- =============================================
-- Создание индексов
-- =============================================

PRINT 'Создание индексов...';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_ClientId' AND object_id = OBJECT_ID('dbo.Orders'))
    CREATE INDEX [IX_Orders_ClientId] ON [dbo].[Orders]([ClientId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_EmployeeId' AND object_id = OBJECT_ID('dbo.Orders'))
    CREATE INDEX [IX_Orders_EmployeeId] ON [dbo].[Orders]([EmployeeId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_OrderNumber' AND object_id = OBJECT_ID('dbo.Orders'))
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [dbo].[Orders]([OrderNumber]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_SupplierId' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_SupplierId] ON [dbo].[Products]([SupplierId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Code' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_Code] ON [dbo].[Products]([Code]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Parts_SupplierId' AND object_id = OBJECT_ID('dbo.Parts'))
    CREATE INDEX [IX_Parts_SupplierId] ON [dbo].[Parts]([SupplierId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Parts_Code' AND object_id = OBJECT_ID('dbo.Parts'))
    CREATE INDEX [IX_Parts_Code] ON [dbo].[Parts]([Code]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Employees_PositionId' AND object_id = OBJECT_ID('dbo.Employees'))
    CREATE INDEX [IX_Employees_PositionId] ON [dbo].[Employees]([PositionId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderParts_OrderId' AND object_id = OBJECT_ID('dbo.OrderParts'))
    CREATE INDEX [IX_OrderParts_OrderId] ON [dbo].[OrderParts]([OrderId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderParts_PartId' AND object_id = OBJECT_ID('dbo.OrderParts'))
    CREATE INDEX [IX_OrderParts_PartId] ON [dbo].[OrderParts]([PartId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Works_OrderId' AND object_id = OBJECT_ID('dbo.Works'))
    CREATE INDEX [IX_Works_OrderId] ON [dbo].[Works]([OrderId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Works_WorkTypeId' AND object_id = OBJECT_ID('dbo.Works'))
    CREATE INDEX [IX_Works_WorkTypeId] ON [dbo].[Works]([WorkTypeId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_SaleId' AND object_id = OBJECT_ID('dbo.SaleItems'))
    CREATE INDEX [IX_SaleItems_SaleId] ON [dbo].[SaleItems]([SaleId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_ProductId' AND object_id = OBJECT_ID('dbo.SaleItems'))
    CREATE INDEX [IX_SaleItems_ProductId] ON [dbo].[SaleItems]([ProductId]);
GO

PRINT 'Индексы созданы.';
GO

-- =============================================
-- Заполнение начальными данными
-- =============================================

PRINT 'Заполнение начальными данными...';
GO

-- Должности
IF NOT EXISTS (SELECT * FROM [dbo].[Positions] WHERE [Name] = N'Менеджер')
    INSERT INTO [dbo].[Positions] ([Name], [Salary]) VALUES (N'Менеджер', 35000.00);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Positions] WHERE [Name] = N'Бухгалтер')
    INSERT INTO [dbo].[Positions] ([Name], [Salary]) VALUES (N'Бухгалтер', 40000.00);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Positions] WHERE [Name] = N'Инженер')
    INSERT INTO [dbo].[Positions] ([Name], [Salary]) VALUES (N'Инженер', 45000.00);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Positions] WHERE [Name] = N'Директор')
    INSERT INTO [dbo].[Positions] ([Name], [Salary]) VALUES (N'Директор', 80000.00);
GO

-- Типы работ
IF NOT EXISTS (SELECT * FROM [dbo].[WorkTypes] WHERE [Name] = N'Диагностика')
    INSERT INTO [dbo].[WorkTypes] ([Name], [Price]) VALUES (N'Диагностика', 500.00);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[WorkTypes] WHERE [Name] = N'Ремонт')
    INSERT INTO [dbo].[WorkTypes] ([Name], [Price]) VALUES (N'Ремонт', 1500.00);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[WorkTypes] WHERE [Name] = N'Заправка картриджа')
    INSERT INTO [dbo].[WorkTypes] ([Name], [Price]) VALUES (N'Заправка картриджа', 800.00);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[WorkTypes] WHERE [Name] = N'Чистка')
    INSERT INTO [dbo].[WorkTypes] ([Name], [Price]) VALUES (N'Чистка', 600.00);
GO

PRINT 'Начальные данные добавлены.';
PRINT '';
PRINT '========================================';
PRINT 'База данных успешно создана!';
PRINT '========================================';
PRINT '';
PRINT 'Примечания:';
PRINT '1. Таблицы Identity (AspNetUsers, AspNetRoles и т.д.) будут созданы автоматически при первом запуске приложения.';
PRINT '2. Роли и пользователь admin будут созданы автоматически через SeedData при первом запуске.';
PRINT '3. Для добавления тестовых данных выполните скрипт database_seed_data.sql';
PRINT '';
GO
