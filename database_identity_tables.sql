-- =============================================
-- Скрипт создания таблиц ASP.NET Core Identity
-- Эти таблицы обычно создаются автоматически через Entity Framework,
-- но можно создать их вручную для полного контроля
-- =============================================

USE OrgTechRepairDb;
GO

-- =============================================
-- Таблицы Identity (если не созданы автоматически)
-- =============================================

-- Таблица: AspNetRoles (Роли)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(256) NULL,
        [NormalizedName] NVARCHAR(256) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL
    );
    
    CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
END
GO

-- Таблица: AspNetUsers (Пользователи)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserName] NVARCHAR(256) NULL,
        [NormalizedUserName] NVARCHAR(256) NULL,
        [Email] NVARCHAR(256) NULL,
        [NormalizedEmail] NVARCHAR(256) NULL,
        [EmailConfirmed] BIT NOT NULL DEFAULT 0,
        [PasswordHash] NVARCHAR(MAX) NULL,
        [SecurityStamp] NVARCHAR(MAX) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,
        [PhoneNumber] NVARCHAR(MAX) NULL,
        [PhoneNumberConfirmed] BIT NOT NULL DEFAULT 0,
        [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
        [LockoutEnd] DATETIMEOFFSET NULL,
        [LockoutEnabled] BIT NOT NULL DEFAULT 0,
        [AccessFailedCount] INT NOT NULL DEFAULT 0
    );
    
    CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers]([NormalizedEmail]);
    CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
END
GO

-- Таблица: AspNetUserRoles (Связь пользователей и ролей)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) 
            REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]([RoleId]);
END
GO

-- Таблица: AspNetUserClaims (Утверждения пользователей)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [ClaimType] NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]([UserId]);
END
GO

-- Таблица: AspNetUserLogins (Внешние логины)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins] (
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [ProviderKey] NVARCHAR(450) NOT NULL,
        [ProviderDisplayName] NVARCHAR(MAX) NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]([UserId]);
END
GO

-- Таблица: AspNetUserTokens (Токены пользователей)
-- Используется для хранения токенов восстановления пароля, подтверждения email и других токенов
-- Токены генерируются через UserManager.GeneratePasswordResetTokenAsync() и другие методы
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens] (
        [UserId] NVARCHAR(450) NOT NULL,
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [Name] NVARCHAR(450) NOT NULL,
        [Value] NVARCHAR(MAX) NULL,
        PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
END
GO

-- Таблица: AspNetRoleClaims (Утверждения ролей)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleId] NVARCHAR(450) NOT NULL,
        [ClaimType] NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) 
            REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]([RoleId]);
END
GO

-- =============================================
-- Вставка ролей (если не созданы через SeedData)
-- =============================================

-- Роли создаются через SeedData.cs, но можно добавить вручную:
-- Примечание: Id должны быть GUID или другие уникальные значения
-- В реальности они генерируются автоматически через RoleManager

PRINT 'Таблицы Identity успешно созданы!';
PRINT 'Роли и пользователи будут созданы автоматически при первом запуске приложения через SeedData.';
PRINT '';
PRINT 'Примечание: Таблица AspNetUserTokens поддерживает:';
PRINT '  - Токены восстановления пароля (PasswordResetToken)';
PRINT '  - Токены подтверждения email (EmailConfirmationToken)';
PRINT '  - Другие токены безопасности';
PRINT 'Токены генерируются автоматически через UserManager при использовании функций';
PRINT 'восстановления пароля и регистрации пользователей.';
GO
