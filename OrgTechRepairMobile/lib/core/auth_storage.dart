import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthStorage {
  static const _keyToken = 'jwt_token';
  static const _keyUsername = 'username';
  static const _keyRoles = 'roles';
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  Future<void> saveToken(String token, {String? username, List<String>? roles}) async {
    await _storage.write(key: _keyToken, value: token);
    if (username != null) await _storage.write(key: _keyUsername, value: username);
    if (roles != null) await _storage.write(key: _keyRoles, value: roles.join(','));
  }

  Future<String?> getToken() => _storage.read(key: _keyToken);
  Future<String?> getUsername() => _storage.read(key: _keyUsername);
  Future<List<String>> getRoles() async {
    final s = await _storage.read(key: _keyRoles);
    if (s == null || s.isEmpty) return [];
    return s.split(',').map((e) => e.trim()).where((e) => e.isNotEmpty).toList();
  }

  Future<void> clear() async {
    await _storage.delete(key: _keyToken);
    await _storage.delete(key: _keyUsername);
    await _storage.delete(key: _keyRoles);
  }
}
