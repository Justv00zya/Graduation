import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class OrderEditScreen extends StatefulWidget {
  final int? orderId;

  const OrderEditScreen({super.key, this.orderId});

  @override
  State<OrderEditScreen> createState() => _OrderEditScreenState();
}

class _OrderEditScreenState extends State<OrderEditScreen> {
  final _numberController = TextEditingController();
  final _modelController = TextEditingController();
  final _conditionController = TextEditingController();
  final _complaintController = TextEditingController();
  final _costController = TextEditingController();

  int? _clientId;
  int? _employeeId;
  List<dynamic> _clients = [];
  List<dynamic> _employees = [];
  String _status = 'Принят';
  bool _loading = true;
  String? _error;

  bool _isServiceEngineer(Map<String, dynamic> employee) {
    final position = employee['positionName']?.toString().toLowerCase() ?? '';
    final first = employee['firstName']?.toString().toLowerCase() ?? '';
    final last = employee['lastName']?.toString().toLowerCase() ?? '';
    final full = '$last $first $position';
    return full.contains('service') || full.contains('сервис');
  }

  @override
  void initState() {
    super.initState();
    if (widget.orderId == null) {
      _numberController.text = 'ORD-${DateTime.now().millisecondsSinceEpoch}';
    }
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _loading = true);
    try {
      final api = context.read<AuthProvider>().api;
      _clients = await api.getClients();
      try { _employees = await api.getEmployees(); } catch (_) {}
      if (widget.orderId != null) {
        final o = await api.getOrder(widget.orderId!);
        _numberController.text = o['orderNumber']?.toString() ?? '';
        _clientId = o['clientId'] as int?;
        _modelController.text = o['equipmentModel']?.toString() ?? '';
        _conditionController.text = o['conditionDescription']?.toString() ?? '';
        _complaintController.text = o['complaintDescription']?.toString() ?? '';
        _employeeId = o['employeeId'] as int?;
        _costController.text = o['cost']?.toString() ?? '';
        _status = o['status']?.toString() ?? 'Принят';
      }
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _numberController.dispose();
    _modelController.dispose();
    _conditionController.dispose();
    _complaintController.dispose();
    _costController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_clientId == null) { setState(() => _error = 'Выберите клиента'); return; }
    setState(() => _error = null);
    try {
      final api = context.read<AuthProvider>().api;
      final dto = {
        'orderNumber': _numberController.text.trim(),
        'clientId': _clientId,
        'equipmentModel': _modelController.text.trim(),
        'conditionDescription': _conditionController.text.trim().isEmpty ? null : _conditionController.text.trim(),
        'complaintDescription': _complaintController.text.trim().isEmpty ? null : _complaintController.text.trim(),
        'employeeId': _employeeId,
        'cost': double.tryParse(_costController.text.replaceAll(',', '.')),
        'orderDate': DateTime.now().toIso8601String(),
        'status': _status,
      };
      if (widget.orderId != null) {
        dto['completionDate'] = null;
        await api.updateOrder(widget.orderId!, dto);
      } else {
        await api.createOrder(dto);
      }
      if (!mounted) return;
      Navigator.pop(context);
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return buildAppScaffold(context, title: widget.orderId == null ? 'Новая заявка' : 'Редактирование заявки', body: const Center(child: CircularProgressIndicator()));
    final engineerEmployees = _employees
        .whereType<Map>()
        .map((e) => Map<String, dynamic>.from(e))
        .where(_isServiceEngineer)
        .toList();
    final dropdownEmployees = engineerEmployees.isNotEmpty ? engineerEmployees : _employees;
    return buildAppScaffold(
      context,
      title: widget.orderId == null ? 'Новая заявка' : 'Редактирование заявки',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextField(controller: _numberController, decoration: const InputDecoration(labelText: 'Номер заказа')),
            const SizedBox(height: 12),
            DropdownButtonFormField<int?>(
              value: _clientId,
              decoration: const InputDecoration(labelText: 'Клиент *'),
              items: [
                const DropdownMenuItem<int?>(value: null, child: Text('— Выберите клиента')),
                ..._clients.map<DropdownMenuItem<int?>>((c) {
                  final id = c['id'] as int;
                  final name = c['fullName']?.toString() ?? 'id=$id';
                  return DropdownMenuItem(value: id, child: Text(name));
                }),
              ],
              onChanged: (v) => setState(() => _clientId = v),
            ),
            const SizedBox(height: 12),
            TextField(controller: _modelController, decoration: const InputDecoration(labelText: 'Модель оборудования')),
            TextField(controller: _conditionController, decoration: const InputDecoration(labelText: 'Состояние'), maxLines: 2),
            TextField(controller: _complaintController, decoration: const InputDecoration(labelText: 'Описание неисправности'), maxLines: 2),
            const SizedBox(height: 12),
            DropdownButtonFormField<int?>(
              value: _employeeId,
              decoration: const InputDecoration(labelText: 'Сервисный инженер'),
              items: [const DropdownMenuItem<int?>(value: null, child: Text('—'))] +
                  dropdownEmployees.map<DropdownMenuItem<int?>>((e) {
                    final id = e['id'] as int;
                    final name = '${e['lastName']} ${e['firstName']}';
                    return DropdownMenuItem(value: id, child: Text(name));
                  }).toList(),
              onChanged: (v) => setState(() => _employeeId = v),
            ),
            TextField(controller: _costController, decoration: const InputDecoration(labelText: 'Стоимость'), keyboardType: TextInputType.number),
            DropdownButtonFormField<String>(
              value: _status,
              decoration: const InputDecoration(labelText: 'Статус'),
              items: ['Принят', 'В работе', 'Выполнен', 'Отменен'].map((s) => DropdownMenuItem(value: s, child: Text(s))).toList(),
              onChanged: (v) => setState(() => _status = v ?? 'Принят'),
            ),
            if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
            const SizedBox(height: 16),
            FilledButton(onPressed: _save, child: const Text('Сохранить')),
          ],
        ),
      ),
    );
  }
}
