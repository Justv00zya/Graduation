-- =============================================
-- Скрипт заполнения базы данных тестовыми данными
-- OrgTechRepairDb
-- =============================================

USE OrgTechRepairDb;
GO

-- =============================================
-- Очистка существующих данных (опционально)
-- =============================================
-- Раскомментируйте, если нужно очистить данные перед заполнением:
/*
DELETE FROM SaleItems;
DELETE FROM Sales;
DELETE FROM Works;
DELETE FROM OrderParts;
DELETE FROM Orders;
DELETE FROM Parts;
DELETE FROM Products;
DELETE FROM Employees;
DELETE FROM Clients;
DELETE FROM Suppliers;
*/

-- =============================================
-- Заполнение тестовыми данными
-- =============================================

-- Поставщики
IF NOT EXISTS (SELECT * FROM [dbo].[Suppliers] WHERE [Name] = N'ООО "ТехноСнаб"')
    INSERT INTO [dbo].[Suppliers] ([Name], [Address], [INN], [Phone]) 
    VALUES (N'ООО "ТехноСнаб"', N'г. Москва, ул. Промышленная, д. 10', N'7701234567', N'+7 (495) 123-45-67');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Suppliers] WHERE [Name] = N'ИП Иванов И.И.')
    INSERT INTO [dbo].[Suppliers] ([Name], [Address], [INN], [Phone]) 
    VALUES (N'ИП Иванов И.И.', N'г. Москва, ул. Торговая, д. 5', N'500123456789', N'+7 (495) 234-56-78');
GO

-- Клиенты
IF NOT EXISTS (SELECT * FROM [dbo].[Clients] WHERE [FullName] = N'Петров Петр Петрович')
    INSERT INTO [dbo].[Clients] ([FullName], [Address], [Phone]) 
    VALUES (N'Петров Петр Петрович', N'г. Москва, ул. Ленина, д. 1, кв. 10', N'+7 (495) 111-22-33');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Clients] WHERE [FullName] = N'ООО "Бизнес-Сервис"')
    INSERT INTO [dbo].[Clients] ([FullName], [Address], [Phone]) 
    VALUES (N'ООО "Бизнес-Сервис"', N'г. Москва, ул. Деловая, д. 20', N'+7 (495) 222-33-44');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Clients] WHERE [FullName] = N'Сидоров Сидор Сидорович')
    INSERT INTO [dbo].[Clients] ([FullName], [Address], [Phone]) 
    VALUES (N'Сидоров Сидор Сидорович', N'г. Москва, ул. Центральная, д. 15, кв. 5', N'+7 (495) 333-44-55');
GO

-- Сотрудники
DECLARE @ManagerPositionId INT = (SELECT [Id] FROM [dbo].[Positions] WHERE [Name] = N'Менеджер');
DECLARE @EngineerPositionId INT = (SELECT [Id] FROM [dbo].[Positions] WHERE [Name] = N'Инженер');
DECLARE @AccountantPositionId INT = (SELECT [Id] FROM [dbo].[Positions] WHERE [Name] = N'Бухгалтер');
DECLARE @DirectorPositionId INT = (SELECT [Id] FROM [dbo].[Positions] WHERE [Name] = N'Директор');

IF NOT EXISTS (SELECT * FROM [dbo].[Employees] WHERE [TabNumber] = N'001')
    INSERT INTO [dbo].[Employees] ([TabNumber], [PositionId], [FirstName], [LastName], [MiddleName], [DateOfBirth], [INN], [Address], [HireDate]) 
    VALUES (N'001', @DirectorPositionId, N'Иван', N'Иванов', N'Иванович', '1980-01-15', N'123456789012', N'г. Москва, ул. Главная, д. 1', '2020-01-01');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Employees] WHERE [TabNumber] = N'002')
    INSERT INTO [dbo].[Employees] ([TabNumber], [PositionId], [FirstName], [LastName], [MiddleName], [DateOfBirth], [INN], [Address], [HireDate]) 
    VALUES (N'002', @ManagerPositionId, N'Мария', N'Петрова', N'Сергеевна', '1985-05-20', N'234567890123', N'г. Москва, ул. Вторая, д. 2', '2021-03-15');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Employees] WHERE [TabNumber] = N'003')
    INSERT INTO [dbo].[Employees] ([TabNumber], [PositionId], [FirstName], [LastName], [MiddleName], [DateOfBirth], [INN], [Address], [HireDate]) 
    VALUES (N'003', @EngineerPositionId, N'Алексей', N'Сидоров', N'Александрович', '1990-08-10', N'345678901234', N'г. Москва, ул. Третья, д. 3', '2022-06-01');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Employees] WHERE [TabNumber] = N'004')
    INSERT INTO [dbo].[Employees] ([TabNumber], [PositionId], [FirstName], [LastName], [MiddleName], [DateOfBirth], [INN], [Address], [HireDate]) 
    VALUES (N'004', @AccountantPositionId, N'Елена', N'Козлова', N'Викторовна', '1988-12-25', N'456789012345', N'г. Москва, ул. Четвертая, д. 4', '2021-09-10');
GO

-- Товары
DECLARE @Supplier1Id INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'ООО "ТехноСнаб"');
DECLARE @Supplier2Id INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'ИП Иванов И.И.');

IF NOT EXISTS (SELECT * FROM [dbo].[Products] WHERE [Code] = N'PRD-001')
    INSERT INTO [dbo].[Products] ([Code], [SupplierId], [Name], [Model], [Price], [Quantity]) 
    VALUES (N'PRD-001', @Supplier1Id, N'Принтер HP LaserJet Pro', N'HP LaserJet Pro M404dn', 25000.00, 5);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Products] WHERE [Code] = N'PRD-002')
    INSERT INTO [dbo].[Products] ([Code], [SupplierId], [Name], [Model], [Price], [Quantity]) 
    VALUES (N'PRD-002', @Supplier1Id, N'МФУ Canon PIXMA', N'Canon PIXMA TR8620', 18000.00, 3);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Products] WHERE [Code] = N'PRD-003')
    INSERT INTO [dbo].[Products] ([Code], [SupplierId], [Name], [Model], [Price], [Quantity]) 
    VALUES (N'PRD-003', @Supplier2Id, N'Сканер Epson', N'Epson Perfection V39', 8000.00, 8);
GO

-- Запчасти
IF NOT EXISTS (SELECT * FROM [dbo].[Parts] WHERE [Code] = N'PART-001')
    INSERT INTO [dbo].[Parts] ([Code], [SupplierId], [Name], [Price], [Quantity]) 
    VALUES (N'PART-001', @Supplier1Id, N'Картридж HP 85A', 2500.00, 20);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Parts] WHERE [Code] = N'PART-002')
    INSERT INTO [dbo].[Parts] ([Code], [SupplierId], [Name], [Price], [Quantity]) 
    VALUES (N'PART-002', @Supplier1Id, N'Картридж Canon PG-540', 1800.00, 15);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Parts] WHERE [Code] = N'PART-003')
    INSERT INTO [dbo].[Parts] ([Code], [SupplierId], [Name], [Price], [Quantity]) 
    VALUES (N'PART-003', @Supplier2Id, N'Ролик захвата бумаги', 500.00, 30);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Parts] WHERE [Code] = N'PART-004')
    INSERT INTO [dbo].[Parts] ([Code], [SupplierId], [Name], [Price], [Quantity]) 
    VALUES (N'PART-004', @Supplier2Id, N'Лампа сканера', 1200.00, 10);
GO

-- Заявки
DECLARE @Client1Id INT = (SELECT [Id] FROM [dbo].[Clients] WHERE [FullName] = N'Петров Петр Петрович');
DECLARE @Client2Id INT = (SELECT [Id] FROM [dbo].[Clients] WHERE [FullName] = N'ООО "Бизнес-Сервис"');
DECLARE @Employee2Id INT = (SELECT [Id] FROM [dbo].[Employees] WHERE [TabNumber] = N'002');
DECLARE @Employee3Id INT = (SELECT [Id] FROM [dbo].[Employees] WHERE [TabNumber] = N'003');

IF NOT EXISTS (SELECT * FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2024-001')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [ClientId], [EquipmentModel], [ConditionDescription], [ComplaintDescription], [EmployeeId], [Cost], [OrderDate], [CompletionDate], [Status]) 
    VALUES (N'ORD-2024-001', @Client1Id, N'HP LaserJet Pro M404dn', N'Принтер не печатает', N'Необходима диагностика и ремонт', @Employee2Id, 2000.00, '2024-01-15', '2024-01-20', N'Выполнен');
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2024-002')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [ClientId], [EquipmentModel], [ConditionDescription], [ComplaintDescription], [EmployeeId], [Cost], [OrderDate], [Status]) 
    VALUES (N'ORD-2024-002', @Client2Id, N'Canon PIXMA TR8620', N'Заправка картриджа', N'Требуется заправка черного картриджа', @Employee3Id, NULL, '2024-01-20', N'В работе');
GO

-- Запчасти в заявках
DECLARE @Order1Id INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2024-001');
DECLARE @Part1Id INT = (SELECT [Id] FROM [dbo].[Parts] WHERE [Code] = N'PART-001');

IF NOT EXISTS (SELECT * FROM [dbo].[OrderParts] WHERE [OrderId] = @Order1Id AND [PartId] = @Part1Id)
    INSERT INTO [dbo].[OrderParts] ([OrderId], [PartId], [Quantity]) 
    VALUES (@Order1Id, @Part1Id, 1);
GO

-- Работы в заявках
DECLARE @WorkTypeDiagnosticsId INT = (SELECT [Id] FROM [dbo].[WorkTypes] WHERE [Name] = N'Диагностика');
DECLARE @WorkTypeRepairId INT = (SELECT [Id] FROM [dbo].[WorkTypes] WHERE [Name] = N'Ремонт');
DECLARE @WorkTypeRefillId INT = (SELECT [Id] FROM [dbo].[WorkTypes] WHERE [Name] = N'Заправка картриджа');

IF NOT EXISTS (SELECT * FROM [dbo].[Works] WHERE [OrderId] = @Order1Id AND [WorkTypeId] = @WorkTypeDiagnosticsId)
    INSERT INTO [dbo].[Works] ([OrderId], [WorkTypeId], [SequenceNumber]) 
    VALUES (@Order1Id, @WorkTypeDiagnosticsId, 1);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Works] WHERE [OrderId] = @Order1Id AND [WorkTypeId] = @WorkTypeRepairId)
    INSERT INTO [dbo].[Works] ([OrderId], [WorkTypeId], [SequenceNumber]) 
    VALUES (@Order1Id, @WorkTypeRepairId, 2);
GO

DECLARE @Order2Id INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2024-002');
DECLARE @Part2Id INT = (SELECT [Id] FROM [dbo].[Parts] WHERE [Code] = N'PART-002');

IF NOT EXISTS (SELECT * FROM [dbo].[OrderParts] WHERE [OrderId] = @Order2Id AND [PartId] = @Part2Id)
    INSERT INTO [dbo].[OrderParts] ([OrderId], [PartId], [Quantity]) 
    VALUES (@Order2Id, @Part2Id, 1);
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Works] WHERE [OrderId] = @Order2Id AND [WorkTypeId] = @WorkTypeRefillId)
    INSERT INTO [dbo].[Works] ([OrderId], [WorkTypeId], [SequenceNumber]) 
    VALUES (@Order2Id, @WorkTypeRefillId, 1);
GO

-- Продажи
IF NOT EXISTS (SELECT * FROM [dbo].[Sales] WHERE [SaleNumber] = N'SALE-2024-001')
    INSERT INTO [dbo].[Sales] ([SaleNumber], [ClientId], [SaleDate], [TotalAmount]) 
    VALUES (N'SALE-2024-001', @Client1Id, '2024-01-10', 25000.00);
GO

-- Позиции продажи
DECLARE @Sale1Id INT = (SELECT [Id] FROM [dbo].[Sales] WHERE [SaleNumber] = N'SALE-2024-001');
DECLARE @Product1Id INT = (SELECT [Id] FROM [dbo].[Products] WHERE [Code] = N'PRD-001');

IF NOT EXISTS (SELECT * FROM [dbo].[SaleItems] WHERE [SaleId] = @Sale1Id AND [ProductId] = @Product1Id)
    INSERT INTO [dbo].[SaleItems] ([SaleId], [ProductId], [Quantity], [UnitPrice], [TotalPrice]) 
    VALUES (@Sale1Id, @Product1Id, 1, 25000.00, 25000.00);
GO

PRINT 'Тестовые данные успешно добавлены в базу данных!';
GO
