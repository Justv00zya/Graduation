# Скрипты базы данных для OrgTechRepair

Этот документ описывает SQL скрипты для создания и заполнения базы данных приложения OrgTechRepair.

## Файлы скриптов

### 1. `database_full_setup.sql` (РЕКОМЕНДУЕТСЯ)
Полный скрипт развертывания базы данных "все в одном". Включает:
- Создание базы данных `OrgTechRepairDb`
- Создание всех таблиц
- Создание внешних ключей и индексов
- Заполнение начальными данными (должности и типы работ)

**Использование:**
```sql
-- Запустите скрипт в SQL Server Management Studio или через sqlcmd
sqlcmd -S (localdb)\mssqllocaldb -i database_full_setup.sql
```

### 2. `database_script.sql`
Основной скрипт создания структуры базы данных. Включает:
- Создание базы данных `OrgTechRepairDb`
- Создание всех таблиц (Positions, Employees, Clients, Suppliers, Products, Parts, Orders, OrderParts, WorkTypes, Works, Sales, SaleItems)
- Создание внешних ключей и ограничений
- Создание индексов для оптимизации производительности
- Вставка начальных данных (должности и типы работ)

**Использование:**
```sql
-- Запустите скрипт в SQL Server Management Studio или через sqlcmd
sqlcmd -S (localdb)\mssqllocaldb -i database_script.sql
```

### 2. `database_seed_data.sql`
Скрипт заполнения базы данных тестовыми данными:
- Поставщики
- Клиенты
- Сотрудники
- Товары
- Запчасти
- Заявки с работами и запчастями
- Продажи

**Использование:**
```sql
-- Запустите после создания структуры БД
sqlcmd -S (localdb)\mssqllocaldb -d OrgTechRepairDb -i database_seed_data.sql
```

### 3. `database_identity_tables.sql`
Скрипт создания таблиц ASP.NET Core Identity (опционально):
- AspNetUsers
- AspNetRoles
- AspNetUserRoles
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetRoleClaims

**Примечание:** Эти таблицы обычно создаются автоматически Entity Framework при первом запуске приложения. Этот скрипт нужен только если вы хотите создать их вручную.

## Порядок выполнения скриптов

### Вариант 1: Автоматическое создание через Entity Framework (рекомендуется)
1. Запустите приложение - Entity Framework автоматически создаст все таблицы через `EnsureCreated()` или миграции
2. Начальные данные (роли, пользователь admin, должности, типы работ) будут добавлены автоматически через `SeedData.Initialize()`

### Вариант 2: Ручное создание через SQL скрипты

**Быстрый способ (рекомендуется):**
1. Выполните `database_full_setup.sql` - создаст всю структуру и начальные данные
2. Выполните `database_seed_data.sql` для добавления тестовых данных (опционально)
3. При первом запуске приложения будут созданы только роли и пользователь admin (если их еще нет)

**Пошаговый способ:**
1. Выполните `database_script.sql` для создания структуры БД
2. Выполните `database_identity_tables.sql` для создания таблиц Identity (или дождитесь автоматического создания)
3. Выполните `database_seed_data.sql` для добавления тестовых данных
4. При первом запуске приложения будут созданы только роли и пользователь admin (если их еще нет)

## Структура базы данных

### Основные таблицы:
- **Positions** - Должности сотрудников
- **Employees** - Сотрудники
- **Clients** - Клиенты
- **Suppliers** - Поставщики
- **Products** - Товары
- **Parts** - Запчасти
- **Orders** - Заявки на ремонт
- **OrderParts** - Запчасти в заявках
- **WorkTypes** - Типы работ
- **Works** - Работы в заявках
- **Sales** - Продажи
- **SaleItems** - Позиции продажи

### Таблицы Identity:
- **AspNetUsers** - Пользователи системы
- **AspNetRoles** - Роли пользователей
- **AspNetUserRoles** - Связь пользователей и ролей
- **AspNetUserClaims** - Утверждения пользователей
- **AspNetUserLogins** - Внешние логины
- **AspNetUserTokens** - Токены пользователей
- **AspNetRoleClaims** - Утверждения ролей

## Учетные данные по умолчанию

После выполнения скриптов и запуска приложения будет создан пользователь-администратор:
- **Логин:** `admin`
- **Пароль:** `111111`
- **Роль:** Administrator

## Роли системы

1. **Administrator** - Полный доступ ко всем функциям
2. **Manager** - Управление заявками, товарами, клиентами
3. **Engineer** - Просмотр и редактирование заявок, запчастей
4. **Accountant** - Просмотр сотрудников, продаж
5. **Director** - Просмотр всех данных и отчетов

## Примечания

- Все скрипты используют проверку существования объектов перед созданием (`IF NOT EXISTS`)
- Скрипты можно запускать многократно без ошибок
- Для очистки данных перед повторным заполнением раскомментируйте секции `DELETE` в скриптах
- Индексы создаются автоматически для улучшения производительности запросов
- Внешние ключи настроены с правильными правилами удаления (CASCADE, SET NULL, RESTRICT)

## Подключение к базе данных

Строка подключения по умолчанию:
```
Server=(localdb)\mssqllocaldb;Database=OrgTechRepairDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

Или через `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OrgTechRepairDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```
