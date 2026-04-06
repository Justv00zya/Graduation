import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class ClientCabinetScreen extends StatefulWidget {
  const ClientCabinetScreen({super.key});

  @override
  State<ClientCabinetScreen> createState() => _ClientCabinetScreenState();
}

class _ClientCabinetScreenState extends State<ClientCabinetScreen> {
  Map<String, dynamic>? _profile;
  List<dynamic> _orders = [];
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final api = context.read<AuthProvider>().api;
      final profile = await api.getClientCabinetProfile();
      final orders = await api.getClientCabinetOrders();
      if (mounted) setState(() {
        _profile = profile;
        _orders = orders is List ? orders : [];
        _loading = false;
      }); 
    } catch (e) {
      if (mounted) setState(() {
        _error = apiErrorMessage(e);
        _loading = false;
      });
    }
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return buildAppScaffold(
      context,
      title: 'Личный кабинет',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(_error!, textAlign: TextAlign.center, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                        const SizedBox(height: 12),
                        TextButton(onPressed: _load, child: const Text('Повторить')),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (_profile != null) _buildProfileCard(context, _profile!),
                        const SizedBox(height: 20),
                        const Text('Мои заявки', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 8),
                        if (_orders.isEmpty)
                          const Card(
                            child: Padding(
                              padding: EdgeInsets.all(16),
                              child: Text('У вас пока нет заявок. Оставить заявку можно на главной странице.'),
                            ),
                          )
                        else
                          ..._orders.map((o) => _orderCard(context, o)),
                      ],
                    ),
                  ),
                ),
    );
  }

  Widget _buildProfileCard(BuildContext context, Map<String, dynamic> p) {
    final fullName = (p['fullName'] ?? p['FullName'])?.toString() ?? '—';
    final email = (p['email'] ?? p['Email'])?.toString() ?? '—';
    final phone = (p['phone'] ?? p['Phone'])?.toString() ?? '—';
    final address = (p['address'] ?? p['Address'])?.toString() ?? '—';
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Мои данные', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            _row('ФИО / Название', fullName),
            _row('Email', email),
            _row('Телефон', phone),
            _row('Адрес', address),
          ],
        ),
      ),
    );
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 130, child: Text('$label:', style: const TextStyle(fontWeight: FontWeight.w500))),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }

  Widget _orderCard(BuildContext context, dynamic o) {
    final map = o is Map ? o : null;
    final id = map?['id'] ?? map?['Id'];
    final number = (map?['orderNumber'] ?? map?['OrderNumber'])?.toString() ?? '—';
    final date = (map?['orderDate'] ?? map?['OrderDate'])?.toString();
    final status = (map?['status'] ?? map?['Status'])?.toString() ?? '—';
    final cost = map?['cost'] ?? map?['Cost'];
    final equipment = (map?['equipmentModel'] ?? map?['EquipmentModel'])?.toString() ?? '—';
    String dateStr = '—';
    if (date != null && date.length >= 10) dateStr = date.substring(0, 10);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        title: Text(number),
        subtitle: Text('$equipment · $dateStr · $status'),
        trailing: cost != null ? Text('${cost} ₽', style: const TextStyle(fontWeight: FontWeight.w500)) : null,
        onTap: () => Navigator.pushNamed(context, '/client-cabinet/order', arguments: id).then((_) => _load()),
      ),
    );
  }
}
