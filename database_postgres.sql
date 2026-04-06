-- =============================================
-- Скрипт создания базы данных orgtechrepairdb для PostgreSQL
-- Структура предметной области (без Identity-таблиц)
-- =============================================

-- Создать базу данных (выполнить в postgres, если нужно):
-- CREATE DATABASE orgtechrepairdb WITH ENCODING 'UTF8';

-- Далее выполняйте этот скрипт уже в базе orgtechrepairdb

-- =============================================
-- Создание таблиц
-- =============================================

-- Positions (Должности)
CREATE TABLE IF NOT EXISTS positions (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    salary      NUMERIC(18,2) NOT NULL
);

-- Employees (Сотрудники)
CREATE TABLE IF NOT EXISTS employees (
    id              SERIAL PRIMARY KEY,
    tabnumber       VARCHAR(50) NOT NULL,
    positionid      INT NOT NULL,
    firstname       VARCHAR(100) NOT NULL,
    lastname        VARCHAR(100) NOT NULL,
    middlename      VARCHAR(100),
    dateofbirth     TIMESTAMP NOT NULL,
    inn             VARCHAR(20),
    address         VARCHAR(500),
    hiredate        TIMESTAMP NOT NULL,
    CONSTRAINT fk_employees_positions
        FOREIGN KEY (positionid) REFERENCES positions(id)
        ON DELETE RESTRICT
);

-- Clients (Клиенты)
CREATE TABLE IF NOT EXISTS clients (
    id          SERIAL PRIMARY KEY,
    fullname    VARCHAR(200) NOT NULL,
    address     VARCHAR(500),
    phone       VARCHAR(50)
);

-- Suppliers (Поставщики)
CREATE TABLE IF NOT EXISTS suppliers (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    address         VARCHAR(500),
    inn             VARCHAR(20),
    accountnumber   VARCHAR(50),
    phone           VARCHAR(50)
);

-- Products (Товары)
CREATE TABLE IF NOT EXISTS products (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) NOT NULL,
    supplierid  INT NOT NULL,
    name        VARCHAR(200) NOT NULL,
    model       VARCHAR(100),
    price       NUMERIC(18,2) NOT NULL,
    quantity    INT NOT NULL DEFAULT 0,
    CONSTRAINT fk_products_suppliers
        FOREIGN KEY (supplierid) REFERENCES suppliers(id)
        ON DELETE RESTRICT
);

-- Parts (Запчасти)
CREATE TABLE IF NOT EXISTS parts (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) NOT NULL,
    supplierid  INT NOT NULL,
    name        VARCHAR(200) NOT NULL,
    price       NUMERIC(18,2) NOT NULL,
    quantity    INT NOT NULL DEFAULT 0,
    CONSTRAINT fk_parts_suppliers
        FOREIGN KEY (supplierid) REFERENCES suppliers(id)
        ON DELETE RESTRICT
);

-- Orders (Заявки)
CREATE TABLE IF NOT EXISTS orders (
    id                   SERIAL PRIMARY KEY,
    ordernumber          VARCHAR(50) NOT NULL,
    clientid             INT NOT NULL,
    equipmentmodel       VARCHAR(200) NOT NULL,
    conditiondescription TEXT,
    complaintdescription TEXT,
    employeeid           INT,
    cost                 NUMERIC(18,2),
    orderdate            TIMESTAMP NOT NULL,
    completiondate       TIMESTAMP,
    status               VARCHAR(50) NOT NULL DEFAULT 'Принят',
    CONSTRAINT fk_orders_clients
        FOREIGN KEY (clientid) REFERENCES clients(id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_orders_employees
        FOREIGN KEY (employeeid) REFERENCES employees(id)
        ON DELETE SET NULL
);

-- OrderParts (Запчасти в заявке)
CREATE TABLE IF NOT EXISTS orderparts (
    id          SERIAL PRIMARY KEY,
    orderid     INT NOT NULL,
    partid      INT NOT NULL,
    quantity    INT NOT NULL DEFAULT 1,
    CONSTRAINT fk_orderparts_orders
        FOREIGN KEY (orderid) REFERENCES orders(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_orderparts_parts
        FOREIGN KEY (partid) REFERENCES parts(id)
        ON DELETE RESTRICT
);

-- WorkTypes (Типы работ)
CREATE TABLE IF NOT EXISTS worktypes (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    price       NUMERIC(18,2) NOT NULL
);

-- Works (Работы по заявке)
CREATE TABLE IF NOT EXISTS works (
    id              SERIAL PRIMARY KEY,
    orderid         INT NOT NULL,
    worktypeid      INT NOT NULL,
    sequencenumber  INT NOT NULL,
    CONSTRAINT fk_works_orders
        FOREIGN KEY (orderid) REFERENCES orders(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_works_worktypes
        FOREIGN KEY (worktypeid) REFERENCES worktypes(id)
        ON DELETE RESTRICT
);

-- Sales (Продажи)
CREATE TABLE IF NOT EXISTS sales (
    id          SERIAL PRIMARY KEY,
    salenumber  VARCHAR(50) NOT NULL,
    clientid    INT,
    saledate    TIMESTAMP NOT NULL,
    totalamount NUMERIC(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT fk_sales_clients
        FOREIGN KEY (clientid) REFERENCES clients(id)
        ON DELETE SET NULL
);

-- SaleItems (Позиции продажи)
CREATE TABLE IF NOT EXISTS saleitems (
    id          SERIAL PRIMARY KEY,
    saleid      INT NOT NULL,
    productid   INT NOT NULL,
    quantity    INT NOT NULL DEFAULT 1,
    unitprice   NUMERIC(18,2) NOT NULL,
    totalprice  NUMERIC(18,2) NOT NULL,
    CONSTRAINT fk_saleitems_sales
        FOREIGN KEY (saleid) REFERENCES sales(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_saleitems_products
        FOREIGN KEY (productid) REFERENCES products(id)
        ON DELETE RESTRICT
);

-- =============================================
-- Индексы
-- =============================================

CREATE UNIQUE INDEX IF NOT EXISTS ix_orders_ordernumber ON orders(ordernumber);
CREATE INDEX IF NOT EXISTS ix_orders_clientid ON orders(clientid);
CREATE INDEX IF NOT EXISTS ix_orders_employeeid ON orders(employeeid);

CREATE INDEX IF NOT EXISTS ix_products_supplierid ON products(supplierid);
CREATE INDEX IF NOT EXISTS ix_products_code ON products(code);

CREATE INDEX IF NOT EXISTS ix_parts_supplierid ON parts(supplierid);
CREATE INDEX IF NOT EXISTS ix_parts_code ON parts(code);

CREATE INDEX IF NOT EXISTS ix_employees_positionid ON employees(positionid);

CREATE INDEX IF NOT EXISTS ix_orderparts_orderid ON orderparts(orderid);
CREATE INDEX IF NOT EXISTS ix_orderparts_partid ON orderparts(partid);

CREATE INDEX IF NOT EXISTS ix_works_orderid ON works(orderid);
CREATE INDEX IF NOT EXISTS ix_works_worktypeid ON works(worktypeid);

CREATE INDEX IF NOT EXISTS ix_saleitems_saleid ON saleitems(saleid);
CREATE INDEX IF NOT EXISTS ix_saleitems_productid ON saleitems(productid);

-- =============================================
-- Базовый сидинг (должности и типы работ)
-- =============================================

INSERT INTO positions (name, salary)
SELECT 'Менеджер', 35000.00
WHERE NOT EXISTS (SELECT 1 FROM positions WHERE name = 'Менеджер');

INSERT INTO positions (name, salary)
SELECT 'Бухгалтер', 40000.00
WHERE NOT EXISTS (SELECT 1 FROM positions WHERE name = 'Бухгалтер');

INSERT INTO positions (name, salary)
SELECT 'Инженер', 45000.00
WHERE NOT EXISTS (SELECT 1 FROM positions WHERE name = 'Инженер');

INSERT INTO positions (name, salary)
SELECT 'Директор', 80000.00
WHERE NOT EXISTS (SELECT 1 FROM positions WHERE name = 'Директор');

INSERT INTO worktypes (name, price)
SELECT 'Диагностика', 500.00
WHERE NOT EXISTS (SELECT 1 FROM worktypes WHERE name = 'Диагностика');

INSERT INTO worktypes (name, price)
SELECT 'Ремонт', 1500.00
WHERE NOT EXISTS (SELECT 1 FROM worktypes WHERE name = 'Ремонт');

INSERT INTO worktypes (name, price)
SELECT 'Заправка картриджа', 800.00
WHERE NOT EXISTS (SELECT 1 FROM worktypes WHERE name = 'Заправка картриджа');

INSERT INTO worktypes (name, price)
SELECT 'Чистка', 600.00
WHERE NOT EXISTS (SELECT 1 FROM worktypes WHERE name = 'Чистка');

-- =============================================
-- Примечание по Identity
-- =============================================
-- Таблицы ASP.NET Core Identity (пользователи, роли и т.п.)
-- будут созданы автоматически Entity Framework Core
-- при первом запуске приложения (EnsureCreated/миграции).

