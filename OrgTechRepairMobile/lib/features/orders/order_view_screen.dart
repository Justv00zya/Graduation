
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
  bool _assigning = false;

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

  bool _isServiceEngineer(Map<String, dynamic> employee) {
    final position = employee['positionName']?.toString().toLowerCase() ?? '';
    final first = employee['firstName']?.toString().toLowerCase() ?? '';
    final last = employee['lastName']?.toString().toLowerCase() ?? '';
    final full = '$last $first $position';
    return full.contains('service') || full.contains('сервис');
  }

  Future<void> _assignServiceEngineer() async {
    final order = _order;
    if (order == null) return;

    setState(() => _assigning = true);
    try {
      final api = context.read<AuthProvider>().api;
      final employeesRaw = await api.getEmployees();
      final engineers = employeesRaw
          .whereType<Map>()
          .map((e) => Map<String, dynamic>.from(e))
          .where(_isServiceEngineer)
          .toList();
      if (!mounted) return;

      if (engineers.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Сервисные инженеры не найдены.')),
        );
        return;
      }

      int? selectedId = order['employeeId'] as int?;
      final selected = await showDialog<int?>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Назначить сервисного инженера'),
          content: StatefulBuilder(
            builder: (ctx, setLocalState) => DropdownButtonFormField<int>(
              isExpanded: true,
              value: selectedId,
              decoration: const InputDecoration(labelText: 'Сервисный инженер'),
              items: engineers.map((e) {
                final id = e['id'] as int;
                final name = '${e['lastName']} ${e['firstName']}';
                return DropdownMenuItem<int>(value: id, child: Text(name));
              }).toList(),
              onChanged: (v) => setLocalState(() => selectedId = v),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx, null),
              child: const Text('Отмена'),
            ),
            FilledButton(
              onPressed: selectedId == null ? null : () => Navigator.pop(ctx, selectedId),
              child: const Text('Назначить'),
            ),
          ],
        ),
      );

      if (!mounted || selected == null) return;

      final dto = <String, dynamic>{
        'orderNumber': order['orderNumber'],
        'clientId': order['clientId'],
        'equipmentModel': order['equipmentModel'],
        'conditionDescription': order['conditionDescription'],
        'complaintDescription': order['complaintDescription'],
        'employeeId': selected,
        'cost': order['cost'],
        'orderDate': order['orderDate'],
        'completionDate': order['completionDate'],
        'status': order['status'],
      };
      await api.updateOrder(widget.orderId, dto);
      await _load();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Сервисный инженер назначен')),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(apiErrorMessage(e))),
      );
    } finally {
      if (mounted) setState(() => _assigning = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final canEdit = auth.isManagerOrDirectorOrAdmin || auth.isServiceEngineer;
    final canAssignEngineer =
        auth.hasRole('Manager') ||
        auth.hasRole('OfficeManager') ||
        auth.hasRole('Director') ||
        auth.hasRole('Administrator');
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
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    FilledButton(
                      onPressed: () => Navigator.pushNamed(context, '/orders/edit', arguments: widget.orderId).then((_) => _load()),
                      child: const Text('Редактировать'),
                    ),
                    if (canAssignEngineer)
                      FilledButton.tonal(
                        onPressed: _assigning ? null : _assignServiceEngineer,
                        child: _assigning
                            ? const SizedBox(
                                height: 18,
                                width: 18,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Text('Назначить инженера'),
                      ),
                  ],
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
