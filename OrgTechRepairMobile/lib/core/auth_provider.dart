import 'package:flutter/foundation.dart';

import 'api_client.dart';
import 'auth_storage.dart';
import 'server_url_storage.dart';

class AuthProvider extends ChangeNotifier {
  final ApiClient _api = ApiClient();
  String? _token;
  String? _username;
  List<String> _roles = [];
  bool _loaded = false;
  String _serverUrl = '';

  AuthStorage get authStorage => _api.authStorage;
  ApiClient get api => _api;
  String? get token => _token;
  String? get username => _username;
  List<String> get roles => _roles;
  String get serverUrl => _serverUrl;
  bool get isAuthenticated => _token != null && _token!.isNotEmpty;
  bool get loaded => _loaded;

  bool hasRole(String role) => _roles.any((r) => r.toLowerCase() == role.toLowerCase());
  bool get isManagerOrDirectorOrAdmin =>
      hasRole('Manager') || hasRole('OfficeManager') || hasRole('Director') || hasRole('Administrator');
  bool get isServiceEngineer => hasRole('ServiceEngineer');
  bool get isEngineerOrDirectorOrAdmin =>
      hasRole('Engineer') || hasRole('WarehouseKeeper') || hasRole('Director') || hasRole('Administrator');
  bool get isAccountantOrDirectorOrAdmin =>
      hasRole('Accountant') || hasRole('Cashier') || hasRole('Director') || hasRole('Administrator');
  bool get isAdministrator => hasRole('Administrator');
  bool get isClient => hasRole('Client');

  Future<void> loadFromStorage() async {
    _token = await _api.authStorage.getToken();
    _username = await _api.authStorage.getUsername();
    _roles = await _api.authStorage.getRoles();
    _serverUrl = await getSavedServerUrl();
    _api.setBaseUrl(_serverUrl);
    _loaded = true;
    notifyListeners();
  }

  Future<void> setServerUrl(String url) async {
    await saveServerUrl(url);
    _serverUrl = await getSavedServerUrl();
    _api.setBaseUrl(_serverUrl);
    notifyListeners();
  }

  Future<void> login(String username, String password) async {
    final data = await _api.login(username, password);
    // Бэкенд ASP.NET Core по умолчанию отдаёт camelCase (token, username, roles)
    final token = (data['Token'] ?? data['token']) as String?;
    if (token == null || token.isEmpty) throw Exception('Нет токена в ответе');
    final rolesList = (data['Roles'] ?? data['roles']) as List<dynamic>?;
    final roles = rolesList?.map((e) => e.toString()).toList() ?? [];
    final usernameStr = (data['Username'] ?? data['username'])?.toString();
    await _api.authStorage.saveToken(
      token,
      username: usernameStr,
      roles: roles,
    );
    _token = token;
    _username = usernameStr;
    _roles = roles;
    notifyListeners();
  }

  Future<void> logout() async {
    await _api.authStorage.clear();
    _token = null;
    _username = null;
    _roles = [];
    notifyListeners();
  }
}
