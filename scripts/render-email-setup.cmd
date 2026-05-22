@echo off
chcp 65001 >nul
echo ============================================================
echo  Настройка почты OrgTechRepair на Render (для руководителя)
echo ============================================================
echo.
echo Откройте: https://dashboard.render.com
echo Сервис: orgtechrepair-web → Environment → Add Environment Variable
echo.
echo --- Обязательно (Gmail SMTP) ---
echo Email__Smtp__Username     = valpcon2@gmail.com
echo Email__Smtp__FromEmail    = valpcon2@gmail.com
echo Email__Smtp__Password     = ^(16-значный пароль приложения Google^)
echo.
echo --- Куда приходит код 2FA для демо-логинов ---
echo SeedData__SharedTwoFactorEmail = email руководителя ИЛИ ваш valpcon2@gmail.com
echo.
echo SeedData__EnableSharedTwoFactorEmail уже true в render.yaml
echo.
echo --- Если SMTP на Render не работает (часто на free) ---
echo 1. Зарегистрируйтесь на https://app.brevo.com
echo 2. SMTP ^& API → API Keys → создать ключ
echo 3. Senders → подтвердить valpcon2@gmail.com
echo 4. На Render:
echo    Email__Brevo__ApiKey    = xkeysib-...
echo    Email__Brevo__FromEmail = valpcon2@gmail.com
echo.
echo --- Данные для входа руководителя ---
echo URL: ваш https://....onrender.com/Login
echo Логин: demo_director  (директор, полный доступ + отчёты)
echo        demo_manager   (менеджер, заявки и клиенты)
echo Пароль: 111111
echo Капча: ответ на арифметический вопрос на экране
echo Код 2FA: письмо на SeedData__SharedTwoFactorEmail
echo.
echo После сохранения переменных: Manual Deploy → Deploy latest commit
echo ============================================================
pause
