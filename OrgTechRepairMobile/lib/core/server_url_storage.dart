import 'package:shared_preferences/shared_preferences.dart';

import 'app_config.dart';

const String _keyServerUrl = 'api_base_url';

/// Порт локального бэкенда по умолчанию (Kestrel в разработке).
const int _defaultLocalApiPort = 5121;

/// Был ли адрес явно сохранён (кнопка «Сохранить» или вход с заполненным полем).
/// Пока ключа нет, на экране входа блок сервера можно показывать развёрнутым.
Future<bool> hasExplicitServerUrlPreference() async {
  final prefs = await SharedPreferences.getInstance();
  return prefs.containsKey(_keyServerUrl);
}

/// Приводит адрес API к виду, подходящему для Dio: без завершающего `/`.
/// Для [https] публичных хостов не подставляет порт 5121 (раньше ломало Render и др.).
/// Для локального [http] без порта подставляет [:5121], как раньше.
String normalizeApiBaseUrl(String input) {
  final trimmed = input.trim();
  if (trimmed.isEmpty) return '';

  final withScheme = _ensureScheme(trimmed);
  final uri = Uri.tryParse(withScheme);
  if (uri == null || uri.host.isEmpty) return withScheme;

  var scheme = uri.scheme;
  final host = uri.host;
  var port = uri.port;

  if (scheme == 'https') {
    // Исправление старых сохранённых URL: https + :5121 на публичном хосте.
    if (port == _defaultLocalApiPort && !_isLocalDevHost(host)) {
      port = 443;
    }
  } else if (scheme == 'http' && _isLocalDevHost(host) && port == 80) {
    port = _defaultLocalApiPort;
  }

  return _formatBaseUrl(scheme: scheme, host: host, port: port);
}

String _ensureScheme(String value) {
  if (value.startsWith('http://') || value.startsWith('https://')) {
    return value;
  }
  final hostPart = value.split('/').first.split(':').first;
  if (_isLocalDevHost(hostPart)) {
    return 'http://$value';
  }
  return 'https://$value';
}

bool _isLocalDevHost(String host) {
  final h = host.toLowerCase();
  if (h.isEmpty) return false;
  if (h == 'localhost' || h == '10.0.2.2' || h == '127.0.0.1') return true;
  final ipv4 = RegExp(r'^(?:\d{1,3}\.){3}\d{1,3}$');
  if (!ipv4.hasMatch(h)) return false;
  try {
    final parts = h.split('.').map(int.parse).toList();
    final a = parts[0], b = parts[1];
    if (a == 10) return true;
    if (a == 192 && b == 168) return true;
    if (a == 172 && b >= 16 && b <= 31) return true;
    if (a == 127) return true;
  } catch (_) {}
  return false;
}

String _formatBaseUrl({
  required String scheme,
  required String host,
  required int port,
}) {
  if (scheme == 'https' && port == 443) return 'https://$host';
  if (scheme == 'http' && port == 80) return 'http://$host';
  return '$scheme://$host:$port';
}

Future<String> getSavedServerUrl() async {
  final prefs = await SharedPreferences.getInstance();
  final raw = prefs.getString(_keyServerUrl) ?? kApiBaseUrl;
  if (raw.trim().isEmpty) return '';
  return normalizeApiBaseUrl(raw);
}

Future<void> saveServerUrl(String url) async {
  final trimmed = url.trim();
  final prefs = await SharedPreferences.getInstance();
  if (trimmed.isEmpty) {
    await prefs.remove(_keyServerUrl);
  } else {
    await prefs.setString(_keyServerUrl, normalizeApiBaseUrl(trimmed));
  }
}
