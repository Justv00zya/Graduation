
import 'package:dio/dio.dart';

import 'app_config.dart';
import 'auth_storage.dart';

class ApiClient {
  late final Dio _dio;
  final AuthStorage _authStorage = AuthStorage();
  String? _baseUrl;

  ApiClient([String? baseUrl]) {
    _baseUrl = baseUrl ?? kApiBaseUrl;
    _dio = Dio(BaseOptions(
      baseUrl: _baseUrl!,
      connectTimeout: const Duration(seconds: 120),
      receiveTimeout: const Duration(seconds: 120),
      sendTimeout: const Duration(seconds: 120),
      headers: {'Content-Type': 'application/json', 'Accept': 'application/json'},
    ));

    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        if (options.data is FormData) {
          options.headers.remove(Headers.contentTypeHeader);
        }
        final token = await _authStorage.getToken();
        if (token != null && token.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (e, handler) {
        if (e.response?.statusCode == 401) {
          // Можно вызвать logout через провайдер
        }
        return handler.next(e);
      },
    ));
  }

  void setBaseUrl(String url) {
    _baseUrl = url;
    _dio.options.baseUrl = url;
  }

  /// Проверка соединения с сервером (GET без авторизации). Бросает при ошибке.
  Future<void> checkConnection() async {
    await _dio.get('/api/Health');
  }

  // ——— Auth ———
  Future<Map<String, dynamic>> login(String username, String password) async {
    final r = await _dio.post('/api/Auth/login', data: {'Username': username, 'Password': password});
    return r.data as Map<String, dynamic>;
  }

  Future<void> registerPublic({
    required String username,
    required String email,
    required String password,
    required String confirmPassword,
  }) async {
    await _dio.post('/api/Auth/register-public', data: {
      'Username': username,
      'Email': email,
      'Password': password,
      'ConfirmPassword': confirmPassword,
      'UserType': 'Client',
    });
  }

  /// Returns response body; may contain token and email for in-app reset.
  Future<Map<String, dynamic>> forgotPassword(String email) async {
    final r = await _dio.post('/api/Auth/forgot-password', data: {'Email': email});
    return r.data is Map<String, dynamic> ? r.data as Map<String, dynamic> : <String, dynamic>{};
  }

  Future<void> resetPassword({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  }) async {
    await _dio.post('/api/Auth/reset-password', data: {
      'Email': email,
      'Token': token,
      'NewPassword': newPassword,
      'ConfirmPassword': confirmPassword,
    });
  }

  // ——— Quick request (no auth) ———
  Future<Map<String, dynamic>> quickRequest({
    required String clientName,
    required String phone,
    required String equipmentModel,
    required String complaintDescription,
  }) async {
    final r = await _dio.post('/api/Orders/quick-request', data: {
      'ClientName': clientName,
      'Phone': phone,
      'EquipmentModel': equipmentModel,
      'ComplaintDescription': complaintDescription,
    });
    return r.data as Map<String, dynamic>;
  }

  // ——— Clients ———
  Future<List<dynamic>> getClients() async {
    final r = await _dio.get('/api/Clients');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getClient(int id) async {
    final r = await _dio.get('/api/Clients/$id');
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createClient(Map<String, dynamic> dto) async {
    final r = await _dio.post('/api/Clients', data: dto);
    return r.data as Map<String, dynamic>;
  }

  Future<void> updateClient(int id, Map<String, dynamic> dto) async {
    await _dio.put('/api/Clients/$id', data: dto);
  }

  Future<void> deleteClient(int id) async {
    await _dio.delete('/api/Clients/$id');
  }

  // ——— Orders ———
  Future<List<dynamic>> getOrders() async {
    final r = await _dio.get('/api/Orders');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getOrder(int id) async {
    final r = await _dio.get('/api/Orders/$id');
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createOrder(Map<String, dynamic> dto) async {
    final r = await _dio.post('/api/Orders', data: dto);
    return r.data as Map<String, dynamic>;
  }

  Future<void> updateOrder(int id, Map<String, dynamic> dto) async {
    await _dio.put('/api/Orders/$id', data: dto);
  }

  Future<void> deleteOrder(int id) async {
    await _dio.delete('/api/Orders/$id');
  }

  // ——— Products ———
  Future<List<dynamic>> getProducts() async {
    final r = await _dio.get('/api/Products');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getProduct(int id) async {
    final r = await _dio.get('/api/Products/$id');
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createProduct(Map<String, dynamic> dto) async {
    final r = await _dio.post('/api/Products', data: dto);
    return r.data as Map<String, dynamic>;
  }

  Future<void> updateProduct(int id, Map<String, dynamic> dto) async {
    await _dio.put('/api/Products/$id', data: dto);
  }

  Future<void> deleteProduct(int id) async {
    await _dio.delete('/api/Products/$id');
  }

  /// Полный URL файла изображения (сервер отдаёт путь вида `/uploads/products/...`).
  String? productImageAbsoluteUrl(String? imageUrl) {
    if (imageUrl == null || imageUrl.isEmpty) return null;
    if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) return imageUrl;
    final base = _dio.options.baseUrl.replaceAll(RegExp(r'/+$'), '');
    final path = imageUrl.startsWith('/') ? imageUrl : '/$imageUrl';
    return '$base$path';
  }

  Future<Map<String, dynamic>> uploadProductImage(int id, String filePath) async {
    final normalized = filePath.replaceAll('\\', '/');
    final name = normalized.split('/').last;
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(filePath, filename: name.isEmpty ? 'image.jpg' : name),
    });
    final r = await _dio.post<Map<String, dynamic>>(
      '/api/Products/$id/image',
      data: formData,
    );
    return Map<String, dynamic>.from(r.data ?? {});
  }

  Future<void> deleteProductImage(int id) async {
    await _dio.delete('/api/Products/$id/image');
  }

  // ——— Suppliers ———
  Future<List<dynamic>> getSuppliers() async {
    final r = await _dio.get('/api/Suppliers');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getSupplier(int id) async {
    final r = await _dio.get('/api/Suppliers/$id');
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createSupplier(Map<String, dynamic> dto) async {
    final r = await _dio.post('/api/Suppliers', data: dto);
    return r.data as Map<String, dynamic>;
  }

  Future<void> updateSupplier(int id, Map<String, dynamic> dto) async {
    await _dio.put('/api/Suppliers/$id', data: dto);
  }

  Future<void> deleteSupplier(int id) async {
    await _dio.delete('/api/Suppliers/$id');
  }

  // ——— Sales ———
  Future<List<dynamic>> getSales() async {
    final r = await _dio.get('/api/Sales');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getSale(int id) async {
    final r = await _dio.get('/api/Sales/$id');
    return r.data as Map<String, dynamic>;
  }

  // ——— Employees ———
  Future<List<dynamic>> getEmployees() async {
    final r = await _dio.get('/api/Employees');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getEmployee(int id) async {
    final r = await _dio.get('/api/Employees/$id');
    return r.data as Map<String, dynamic>;
  }

  // ——— Личный кабинет клиента (роль Client) ———
  Future<Map<String, dynamic>> getClientCabinetProfile() async {
    final r = await _dio.get('/api/ClientCabinet/profile');
    return r.data as Map<String, dynamic>;
  }

  Future<void> updateClientCabinetProfile(Map<String, dynamic> dto) async {
    await _dio.put('/api/ClientCabinet/profile', data: dto);
  }

  Future<List<dynamic>> getClientCabinetOrders() async {
    final r = await _dio.get('/api/ClientCabinet/orders');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getClientCabinetOrder(int id) async {
    final r = await _dio.get('/api/ClientCabinet/orders/$id');
    return r.data as Map<String, dynamic>;
  }

  // ——— Reports ———
  Future<List<dynamic>> getReportsSales({required DateTime dateFrom, required DateTime dateTo}) async {
    final from = '${dateFrom.year}-${dateFrom.month.toString().padLeft(2, '0')}-${dateFrom.day.toString().padLeft(2, '0')}';
    final to = '${dateTo.year}-${dateTo.month.toString().padLeft(2, '0')}-${dateTo.day.toString().padLeft(2, '0')}';
    final r = await _dio.get('/api/Reports/sales', queryParameters: {'dateFrom': from, 'dateTo': to});
    return r.data as List<dynamic>;
  }

  Future<List<dynamic>> getReportsOrders({required DateTime dateFrom, required DateTime dateTo}) async {
    final from = '${dateFrom.year}-${dateFrom.month.toString().padLeft(2, '0')}-${dateFrom.day.toString().padLeft(2, '0')}';
    final to = '${dateTo.year}-${dateTo.month.toString().padLeft(2, '0')}-${dateTo.day.toString().padLeft(2, '0')}';
    final r = await _dio.get('/api/Reports/orders', queryParameters: {'dateFrom': from, 'dateTo': to});
    return r.data as List<dynamic>;
  }

  // ——— Parts ———
  Future<List<dynamic>> getParts() async {
    final r = await _dio.get('/api/Parts');
    return r.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getPart(int id) async {
    final r = await _dio.get('/api/Parts/$id');
    return r.data as Map<String, dynamic>;
  }

  // ——— Part supply requests (инженер → кладовщик) ———
  Future<List<dynamic>> getPartSupplyRequestsQueue({String? status}) async {
    final r = await _dio.get(
      '/api/PartSupplyRequests',
      queryParameters: status != null ? {'status': status} : null,
    );
    return r.data as List<dynamic>;
  }

  Future<List<dynamic>> getMyPartSupplyRequests() async {
    final r = await _dio.get('/api/PartSupplyRequests/my');
    return r.data as List<dynamic>;
  }

  Future<void> createPartSupplyRequest({
    required int partId,
    required int quantity,
    int? orderId,
    String? comment,
  }) async {
    await _dio.post('/api/PartSupplyRequests', data: {
      'partId': partId,
      'quantity': quantity,
      if (orderId != null) 'orderId': orderId,
      if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
    });
  }

  Future<void> completePartSupplyRequest(int id, {String? comment}) async {
    await _dio.post('/api/PartSupplyRequests/$id/complete', data: {
      if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
    });
  }

  Future<void> rejectPartSupplyRequest(int id, {String? comment}) async {
    await _dio.post('/api/PartSupplyRequests/$id/reject', data: {
      if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
    });
  }

  AuthStorage get authStorage => _authStorage;
}

String apiErrorMessage(dynamic e) {
  if (e is DioException) {
    final data = e.response?.data;
    if (data is Map) {
      final msg = data['message'] ?? data['Message'];
      if (msg != null) return msg.toString();
      if (data['errors'] != null) {
        final list = data['errors'] is List ? data['errors'] as List : [data['errors']];
        return list.join(' ');
      }
    }
    if (e.response?.statusCode == 401) return 'Неверный логин или пароль';
    final msg = (e.message ?? e.toString()).toLowerCase();
    if (msg.contains('no route to host') || msg.contains('network is unreachable')) {
      return 'Нет доступа к серверу. Проверьте интернет или адрес в «Адрес сервера» (для ПК в Wi‑Fi — http://IP:5121, для Render — https://ваш-сервис.onrender.com) и нажмите «Сохранить».';
    }
    if (msg.contains('connection refused') || msg.contains('connection reset')) {
      return 'Сервер недоступен. Убедитесь, что адрес верный: для облака — https с доменом хостинга; для ПК в сети — http://IP:5121 и запущенный бэкенд.';
    }
    if (msg.contains('timeout') || msg.contains('timed out')) {
      return 'Таймаут соединения. Проверьте адрес сервера и доступность сайта (облако может «просыпаться» до минуты на бесплатном плане).';
    }
    return e.message ?? 'Ошибка сети';
  }
  return e.toString();
}
