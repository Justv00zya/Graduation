@echo off
chcp 65001 >nul
echo ============================================================
echo  Почта на Render для ВузяПринт (2FA на graduation-vv8e)
echo ============================================================
echo.
echo Gmail/Яндекс SMTP на Render НЕ РАБОТАЕТ (блокируют порт 587).
echo Нужен API по HTTPS. Проще всего — Resend (3 шага):
echo.
echo --- ШАГ 1: Resend ---
echo 1. Откройте https://resend.com и зарегистрируйтесь
echo    (лучше на v00zyaprint@gmail.com — тогда письма себе бесплатно)
echo 2. Слева: API Keys → Create API Key → скопируйте re_...
echo.
echo --- ШАГ 2: Render ---
echo https://dashboard.render.com → ваш сервис → Environment
echo.
echo   Email__Resend__ApiKey     = re_xxxxxxxx  (ваш ключ)
echo   Email__Resend__FromEmail  = onboarding@resend.dev
echo.
echo SeedData__SharedTwoFactorEmail = v00zyaprint@gmail.com  (уже в render.yaml)
echo.
echo --- ШАГ 3: Deploy ---
echo Manual Deploy → Deploy latest commit
echo В логах должно быть: "Режим отправки почты: Resend API"
echo.
echo --- Вход для руководителя ---
echo URL:    https://graduation-vv8e.onrender.com/Login
echo Логин:  demo_director
echo Пароль: 111111
echo Код 2FA: письмо на v00zyaprint@gmail.com
echo.
echo --- Локально (на вашем ПК) ---
echo SMTP Gmail в appsettings.Local.json — как сейчас, без Resend.
echo ============================================================
pause
