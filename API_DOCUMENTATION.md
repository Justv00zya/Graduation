# REST API Документация

## Базовый URL

```
https://localhost:7281/api
или
http://localhost:5121/api
```

## Аутентификация

API использует JWT (JSON Web Token) для аутентификации.

### Получение токена

**POST** `/api/auth/login`

**Request Body:**
```json
{
  "username": "admin",
  "password": "111111"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "email": "admin@printservice.ru",
  "roles": ["Administrator"]
}
```

### Использование токена

Добавьте заголовок в каждый запрос:
```
Authorization: Bearer {token}
```

## Эндпоинты API

### 1. Заявки (Orders)

#### GET /api/orders
Получить список всех заявок

**Требуемые роли:** Любая авторизованная

**Response:**
```json
[
  {
    "id": 1,
    "orderNumber": "ORD-001",
    "clientId": 1,
    "clientName": "Иванов Иван Иванович",
    "equipmentModel": "HP LaserJet Pro",
    "conditionDescription": "Не печатает",
    "complaintDescription": "Принтер не включается",
    "employeeId": 1,
    "employeeName": "Петров Петр",
    "cost": 1500.00,
    "orderDate": "2024-01-15T10:00:00",
    "completionDate": null,
    "status": "В работе"
  }
]
```

#### GET /api/orders/{id}
Получить заявку по ID

**Требуемые роли:** Любая авторизованная

#### POST /api/orders
Создать новую заявку

**Требуемые роли:** Manager, Administrator

**Request Body:**
```json
{
  "orderNumber": "ORD-002",
  "clientId": 1,
  "equipmentModel": "Canon PIXMA",
  "conditionDescription": "Застряла бумага",
  "complaintDescription": "Принтер зажевывает бумагу",
  "employeeId": 1,
  "cost": 800.00,
  "orderDate": "2024-01-20T14:30:00",
  "status": "Принят"
}
```

#### PUT /api/orders/{id}
Обновить заявку

**Требуемые роли:** Manager, Engineer, Administrator

#### DELETE /api/orders/{id}
Удалить заявку

**Требуемые роли:** Administrator

---

### 2. Товары (Products)

#### GET /api/products
Получить список всех товаров

**Требуемые роли:** Любая авторизованная

#### GET /api/products/{id}
Получить товар по ID

**Требуемые роли:** Любая авторизованная

#### POST /api/products
Создать новый товар

**Требуемые роли:** Manager, Administrator

**Request Body:**
```json
{
  "code": "PRD-001",
  "name": "Принтер HP LaserJet",
  "model": "HP 1020",
  "supplierId": 1,
  "price": 15000.00,
  "quantity": 5
}
```

#### PUT /api/products/{id}
Обновить товар

**Требуемые роли:** Manager, Administrator

#### DELETE /api/products/{id}
Удалить товар

**Требуемые роли:** Manager, Administrator

---

### 3. Клиенты (Clients)

#### GET /api/clients
Получить список всех клиентов

**Требуемые роли:** Любая авторизованная

#### GET /api/clients/{id}
Получить клиента по ID

**Требуемые роли:** Любая авторизованная

#### POST /api/clients
Создать нового клиента

**Требуемые роли:** Manager, Administrator

**Request Body:**
```json
{
  "fullName": "Сидоров Сидор Сидорович",
  "address": "г. Москва, ул. Ленина, д. 10",
  "phone": "89161234567"
}
```

#### PUT /api/clients/{id}
Обновить клиента

**Требуемые роли:** Manager, Administrator

#### DELETE /api/clients/{id}
Удалить клиента

**Требуемые роли:** Manager, Administrator

---

### 4. Сотрудники (Employees)

#### GET /api/employees
Получить список всех сотрудников

**Требуемые роли:** Accountant, Director, Administrator

#### GET /api/employees/{id}
Получить сотрудника по ID

**Требуемые роли:** Accountant, Director, Administrator

---

### 5. Продажи (Sales)

#### GET /api/sales
Получить список всех продаж

**Требуемые роли:** Manager, Accountant, Director, Administrator

#### GET /api/sales/{id}
Получить продажу по ID (с деталями)

**Требуемые роли:** Manager, Accountant, Director, Administrator

---

### 6. Запчасти (Parts)

#### GET /api/parts
Получить список всех запчастей и тонера

**Требуемые роли:** Engineer, Director, Administrator

#### GET /api/parts/{id}
Получить запчасть по ID

**Требуемые роли:** Engineer, Director, Administrator

---

### 7. Аутентификация (Auth)

#### POST /api/auth/login
Вход в систему и получение JWT токена

**Требуемые роли:** Не требуется (AllowAnonymous)

#### POST /api/auth/register
Регистрация нового пользователя

**Требуемые роли:** Administrator

**Request Body:**
```json
{
  "username": "newuser",
  "email": "user@example.com",
  "password": "Password123",
  "role": "Manager"
}
```

## Swagger UI

В режиме разработки доступна интерактивная документация Swagger:

```
https://localhost:7281/swagger
```

Swagger UI позволяет:
- Просматривать все эндпоинты API
- Тестировать API прямо в браузере
- Авторизоваться через JWT токен
- Видеть схемы запросов и ответов

## Коды ответов

- `200 OK` - Успешный запрос
- `201 Created` - Ресурс успешно создан
- `204 No Content` - Успешное обновление/удаление
- `400 Bad Request` - Неверный запрос
- `401 Unauthorized` - Требуется аутентификация
- `403 Forbidden` - Недостаточно прав доступа
- `404 Not Found` - Ресурс не найден
- `500 Internal Server Error` - Ошибка сервера

## Примеры использования

### cURL

```bash
# Получение токена
curl -X POST https://localhost:7281/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"111111"}'

# Получение списка заявок
curl -X GET https://localhost:7281/api/orders \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### JavaScript (Fetch)

```javascript
// Получение токена
const loginResponse = await fetch('https://localhost:7281/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    username: 'admin',
    password: '111111'
  })
});

const { token } = await loginResponse.json();

// Использование токена
const ordersResponse = await fetch('https://localhost:7281/api/orders', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const orders = await ordersResponse.json();
```

## Разграничение доступа по ролям

- **Administrator** - Полный доступ ко всем эндпоинтам
- **Manager** - Доступ к заявкам, товарам, клиентам, продажам
- **Engineer** - Доступ к заявкам и запчастям
- **Accountant** - Доступ к продажам и сотрудникам
- **Director** - Доступ ко всем данным (кроме администрирования)
