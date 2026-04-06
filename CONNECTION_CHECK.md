# Отчет о проверке подключения к БД и API

## Проверка подключения к базе данных

### Настройки подключения

**Строка подключения:**
```
Server=(localdb)\mssqllocaldb;Database=OrgTechRepairDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**Тип БД:** SQL Server LocalDB

**Провайдер:** Entity Framework Core 8.0

### Конфигурация

✅ **DbContext зарегистрирован:**
- `AddDbContext<ApplicationDbContext>` - для обычного использования
- `AddDbContextFactory<ApplicationDbContext>` - для создания контекстов в компонентах Blazor

✅ **Автоматическое создание БД:**
- Используется `context.Database.EnsureCreated()` при запуске
- Автоматическая инициализация данных через `SeedData.Initialize()`

### Таблицы в базе данных

Приложение использует следующие таблицы:

1. **Identity таблицы (ASP.NET Core Identity):**
   - AspNetUsers
   - AspNetRoles
   - AspNetUserRoles
   - AspNetUserClaims
   - AspNetRoleClaims
   - AspNetUserLogins
   - AspNetUserTokens

2. **Бизнес-таблицы:**
   - Employees (Сотрудники)
   - Positions (Должности)
   - Clients (Клиенты)
   - Suppliers (Поставщики)
   - Products (Товары)
   - Parts (Запчасти и тонер)
   - Orders (Заявки)
   - OrderParts (Позиции запчастей в заявках)
   - WorkTypes (Виды работ)
   - Works (Работы)
   - Sales (Продажи)
   - SaleItems (Позиции продаж)

## Проверка API

### Статус: ❌ REST API не реализован

**Примечание:** Это Blazor Server приложение, которое использует:
- **Blazor Server** для взаимодействия с пользователем
- **SignalR** для двусторонней связи (встроен в Blazor Server)
- **Прямой доступ к БД** через Entity Framework в компонентах

### Архитектура приложения

Приложение использует **Blazor Server**, а не REST API:
- Компоненты Blazor напрямую обращаются к базе данных через `DbContextFactory`
- Нет контроллеров API (`ApiController`)
- Нет эндпоинтов API (`MapControllers`, `MapApi`)
- Взаимодействие происходит через SignalR соединения

### Если нужен REST API

Для добавления REST API можно:
1. Добавить контроллеры API
2. Настроить `MapControllers()` в `Program.cs`
3. Создать отдельные эндпоинты для внешних клиентов

## Рекомендации по проверке

### Для проверки подключения к БД:

1. **Запустите приложение** и откройте страницу `/db-test` (создана для диагностики)
2. **Проверьте логи** при запуске на наличие ошибок подключения
3. **Убедитесь, что SQL Server LocalDB установлен:**
   ```cmd
   sqllocaldb info
   ```

### Для проверки работы приложения:

1. Запустите приложение: `dotnet run`
2. Откройте браузер: `https://localhost:7281`
3. Войдите с учетными данными: `admin` / `111111`
4. Проверьте работу страниц с данными (заявки, товары, клиенты)

## Возможные проблемы

### Проблема: Не удается подключиться к БД

**Решение:**
1. Убедитесь, что SQL Server LocalDB установлен
2. Проверьте, что LocalDB запущен: `sqllocaldb start mssqllocaldb`
3. Проверьте строку подключения в `appsettings.json`

### Проблема: База данных не создается

**Решение:**
1. Проверьте права доступа к LocalDB
2. Убедитесь, что нет других процессов, использующих БД
3. Проверьте логи приложения на ошибки
