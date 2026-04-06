import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class ClientOrderViewScreen extends StatefulWidget {
  final int orderId;

  const ClientOrderViewScreen({super.key, required this.orderId});

  @override
  State<ClientOrderViewScreen> createState() => _ClientOrderViewScreenState();
}

class _ClientOrderViewScreenState extends State<ClientOrderViewScreen> {
  Map<String, dynamic>? _order;
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final o = await context.read<AuthProvider>().api.getClientCabinetOrder(widget.orderId);
      if (mounted) setState(() { _order = o; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = apiErrorMessage(e); _loading = false; });
    }
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return buildAppScaffold(context, title: 'Заявка', body: const Center(child: CircularProgressIndicator()));
    if (_error != null || _order == null) {
      return buildAppScaffold(
        context,
        title: 'Заявка',
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(_error ?? 'Заявка не найдена', style: TextStyle(color: Theme.of(context).colorScheme.error)),
              const SizedBox(height: 12),
              TextButton(onPressed: () => Navigator.pop(context), child: const Text('Назад')),
            ],
          ),
        ),
      );
    }
    final o = _order!;
    final number = (o['orderNumber'] ?? o['OrderNumber'])?.toString() ?? 'Заявка';
    return buildAppScaffold(
      context,
      title: number,
      body: RefreshIndicator(
        onRefresh: _load,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _row('Номер', o['orderNumber'] ?? o['OrderNumber']),
              _row('Модель техники', o['equipmentModel'] ?? o['EquipmentModel']),
              _row('Описание проблемы', o['complaintDescription'] ?? o['ComplaintDescription']),
              _row('Состояние', o['conditionDescription'] ?? o['ConditionDescription']),
              _row('Дата заказа', o['orderDate'] ?? o['OrderDate']),
              _row('Статус', o['status'] ?? o['Status']),
              _row('Стоимость', o['cost'] ?? o['Cost']),
              _row('Дата выполнения', o['completionDate'] ?? o['CompletionDate']),
              _row('Исполнитель', o['employeeName'] ?? o['EmployeeName']),
              const SizedBox(height: 24),
              TextButton.icon(
                onPressed: () => Navigator.pop(context),
                icon: const Icon(Icons.arrow_back),
                label: const Text('В личный кабинет'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _row(String label, dynamic value) {
    final s = value?.toString() ?? '—';
    if (s.length > 10 && (s.contains('T') || s.contains('-')) && label.contains('Дата')) {
      final dateStr = s.substring(0, 10);
      return Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(width: 140, child: Text('$label:', style: const TextStyle(fontWeight: FontWeight.bold))),
            Expanded(child: Text(dateStr)),
          ],
        ),
      );
    }
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 140, child: Text('$label:', style: const TextStyle(fontWeight: FontWeight.bold))),
          Expanded(child: Text(s)),
        ],
      ),
    );
  }
}
