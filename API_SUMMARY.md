# REST API - Краткое описание

## ✅ Реализованные эндпоинты

### Аутентификация
- `POST /api/auth/login` - Получение JWT токена
- `POST /api/auth/register` - Регистрация пользователя (только для администраторов)

### Заявки
- `GET /api/orders` - Список заявок
- `GET /api/orders/{id}` - Заявка по ID
- `POST /api/orders` - Создание заявки (Manager, Administrator)
- `PUT /api/orders/{id}` - Обновление заявки (Manager, Engineer, Administrator)
- `DELETE /api/orders/{id}` - Удаление заявки (Administrator)

### Товары
- `GET /api/products` - Список товаров
- `GET /api/products/{id}` - Товар по ID
- `POST /api/products` - Создание товара (Manager, Administrator)
- `PUT /api/products/{id}` - Обновление товара (Manager, Administrator)
- `DELETE /api/products/{id}` - Удаление товара (Manager, Administrator)

### Клиенты
- `GET /api/clients` - Список клиентов
- `GET /api/clients/{id}` - Клиент по ID
- `POST /api/clients` - Создание клиента (Manager, Administrator)
- `PUT /api/clients/{id}` - Обновление клиента (Manager, Administrator)
- `DELETE /api/clients/{id}` - Удаление клиента (Manager, Administrator)

### Сотрудники
- `GET /api/employees` - Список сотрудников (Accountant, Director, Administrator)
- `GET /api/employees/{id}` - Сотрудник по ID (Accountant, Director, Administrator)

### Продажи
- `GET /api/sales` - Список продаж (Manager, Accountant, Director, Administrator)
- `GET /api/sales/{id}` - Продажа по ID с деталями (Manager, Accountant, Director, Administrator)

### Запчасти
- `GET /api/parts` - Список запчастей (Engineer, Director, Administrator)
- `GET /api/parts/{id}` - Запчасть по ID (Engineer, Director, Administrator)

## 🔐 Аутентификация

API использует JWT токены. Получите токен через `/api/auth/login` и используйте его в заголовке:
```
Authorization: Bearer {token}
```

## 📚 Документация

Swagger UI доступен по адресу: `https://localhost:7281/swagger` (в режиме разработки)

Полная документация в файле `API_DOCUMENTATION.md`
