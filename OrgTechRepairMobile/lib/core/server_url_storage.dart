import 'package:shared_preferences/shared_preferences.dart';

import 'app_config.dart';

const String _keyServerUrl = 'api_base_url';

/// Был ли адрес явно сохранён (кнопка «Сохранить» или вход с заполненным полем).
/// Пока ключа нет, на экране входа блок сервера можно показывать развёрнутым.
Future<bool> hasExplicitServerUrlPreference() async {
  final prefs = await SharedPreferences.getInstance();
  return prefs.containsKey(_keyServerUrl);
}

Future<String> getSavedServerUrl() async {
  final prefs = await SharedPreferences.getInstance();
  String url = prefs.getString(_keyServerUrl) ?? kApiBaseUrl;
  try {
    final uri = Uri.parse(url);
    if (uri.host.isNotEmpty && (uri.port == 80 || uri.port == 443)) {
      url = '${uri.scheme}://${uri.host}:$_defaultApiPort';
    }
  } catch (_) {}
  return url;
}

/// Порт бэкенда по умолчанию.
const int _defaultApiPort = 5121;

Future<void> saveServerUrl(String url) async {
  final trimmed = url.trim();
  final prefs = await SharedPreferences.getInstance();
  if (trimmed.isEmpty) {
    await prefs.remove(_keyServerUrl);
  } else {
    String toSave = trimmed;
    if (!toSave.startsWith('http://') && !toSave.startsWith('https://')) {
      toSave = 'http://$toSave';
    }
    try {
      final uri = Uri.parse(toSave);
      if (uri.host.isNotEmpty && (uri.port == 80 || uri.port == 443)) {
        toSave = '${uri.scheme}://${uri.host}:$_defaultApiPort${uri.path.isEmpty ? '' : uri.path}';
      }
    } catch (_) {}
    await prefs.setString(_keyServerUrl, toSave);
  }
}
