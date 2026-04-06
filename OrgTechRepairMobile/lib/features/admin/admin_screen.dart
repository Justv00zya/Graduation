import 'package:flutter/material.dart';

import '../../main.dart';

/// Администрирование: логи, бэкап БД. На бэкенде эндпоинты api/Admin/* принимают JWT или cookie для роли Administrator.
/// В приложении экран пока не вызывает API — при необходимости можно добавить вызовы через ApiClient.
class AdminScreen extends StatelessWidget {
  const AdminScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return buildAppScaffold(
      context,
      title: 'Администрирование',
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Раздел администрирования (логи, резервное копирование). На сервере доступен по JWT или cookie для администратора. Здесь можно подключить скачивание логов и бэкапа через API.',
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}
