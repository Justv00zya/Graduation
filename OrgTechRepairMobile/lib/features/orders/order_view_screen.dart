import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class OrderViewScreen extends StatefulWidget {
  final int orderId;

  const OrderViewScreen({super.key, required this.orderId});

  @override
  State<OrderViewScreen> createState() => _OrderViewScreenState();
}

class _OrderViewScreenState extends State<OrderViewScreen> {
  Map<String, dynamic>? _order;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final o = await context.read<AuthProvider>().api.getOrder(widget.orderId);
      if (mounted) setState(() { _order = o; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = apiErrorMessage(e); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final canEdit = context.watch<AuthProvider>().isManagerOrDirectorOrAdmin || context.watch<AuthProvider>().isServiceEngineer;
    if (_loading) return buildAppScaffold(context, title: 'Заявка', body: const Center(child: CircularProgressIndicator()));
    if (_error != null || _order == null) return buildAppScaffold(context, title: 'Заявка', body: Center(child: Text(_error ?? 'Не найдено')));
    final o = _order!;
    return buildAppScaffold(
      context,
      title: o['orderNumber']?.toString() ?? 'Заявка',
      body: RefreshIndicator(
        onRefresh: _load,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _row('Номер', o['orderNumber']?.toString()),
              _row('Клиент', o['clientName']?.toString()),
              _row('Модель', o['equipmentModel']?.toString()),
              _row('Состояние', o['conditionDescription']?.toString()),
              _row('Неисправность', o['complaintDescription']?.toString()),
              _row('Исполнитель', o['employeeName']?.toString()),
              _row('Стоимость', o['cost']?.toString()),
              _row('Дата', o['orderDate']?.toString()),
              _row('Выполнено', o['completionDate']?.toString()),
              _row('Статус', o['status']?.toString()),
              if (canEdit) ...[
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: () => Navigator.pushNamed(context, '/orders/edit', arguments: widget.orderId).then((_) => _load()),
                  child: const Text('Редактировать'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _row(String label, String? value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 120, child: Text('$label:', style: const TextStyle(fontWeight: FontWeight.bold))),
          Expanded(child: Text(value ?? '—')),
        ],
      ),
    );
  }
}
