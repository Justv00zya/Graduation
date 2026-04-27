import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

/// Запрос деталей со склада (сервисный инженер / инженер).
class PartSupplyRequestScreen extends StatefulWidget {
  const PartSupplyRequestScreen({super.key, this.initialPartId});

  final int? initialPartId;

  @override
  State<PartSupplyRequestScreen> createState() => _PartSupplyRequestScreenState();
}

class _PartSupplyRequestScreenState extends State<PartSupplyRequestScreen> {
  List<dynamic> _parts = [];
  List<dynamic> _orders = [];
  List<dynamic> _my = [];
  bool _loading = true;
  String? _error;
  int? _partId;
  int? _orderId;
  final _qty = TextEditingController(text: '1');
  final _comment = TextEditingController();
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _partId = widget.initialPartId;
  }

  @override
  void dispose() {
    _qty.dispose();
    _comment.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final api = context.read<AuthProvider>().api;
      final parts = await api.getParts();
      final orders = await api.getOrders();
      final my = await api.getMyPartSupplyRequests();
      if (!mounted) return;
      int? partId = _partId;
      if (partId != null && !parts.any((p) => (p as Map)['id'] == partId)) {
        partId = null;
      }
      partId ??= parts.isNotEmpty ? (parts.first as Map)['id'] as int? : null;
      setState(() {
        _parts = parts;
        _orders = orders;
        _my = my;
        _partId = partId;
        _loading = false;
      });
    } catch (e) {
      if (mounted) setState(() {
        _error = apiErrorMessage(e);
        _loading = false;
      });
    }
  }

  Future<void> _submit() async {
    final partId = _partId;
    if (partId == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Выберите запчасть')));
      return;
    }
    final q = int.tryParse(_qty.text.trim());
    if (q == null || q < 1) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Укажите количество')));
      return;
    }
    setState(() => _submitting = true);
    try {
      await context.read<AuthProvider>().api.createPartSupplyRequest(
            partId: partId,
            quantity: q,
            orderId: _orderId,
            comment: _comment.text.isEmpty ? null : _comment.text,
          );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Заявка отправлена кладовщику')));
      _comment.clear();
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(apiErrorMessage(e))));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return buildAppScaffold(
      context,
      title: 'Заявка на склад',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(_error!, textAlign: TextAlign.center),
                        const SizedBox(height: 12),
                        TextButton(onPressed: _load, child: const Text('Повторить')),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      if (_parts.isEmpty)
                        const Padding(
                          padding: EdgeInsets.only(bottom: 24),
                          child: Text('Нет позиций в справочнике запчастей.'),
                        ),
                      Text('Новая заявка', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 8),
                      if (_parts.isNotEmpty)
                        DropdownButtonFormField<int>(
                        value: _partId,
                        decoration: const InputDecoration(labelText: 'Запчасть', border: OutlineInputBorder()),
                        items: _parts.map((p) {
                          final m = p as Map<String, dynamic>;
                          final id = m['id'];
                          if (id is! int) {
                            return null;
                          }
                          final name = m['name']?.toString() ?? '';
                          final code = m['code']?.toString() ?? '';
                          final stock = m['quantity'];
                          return DropdownMenuItem<int>(
                            value: id,
                            child: Text('$code · $name (склад: $stock)', overflow: TextOverflow.ellipsis),
                          );
                        }).whereType<DropdownMenuItem<int>>().toList(),
                        onChanged: (v) => setState(() => _partId = v),
                        ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _qty,
                        decoration: const InputDecoration(labelText: 'Количество', border: OutlineInputBorder()),
                        keyboardType: TextInputType.number,
                      ),
                      const SizedBox(height: 12),
                      DropdownButtonFormField<int?>(
                        value: _orderId,
                        decoration: const InputDecoration(
                          labelText: 'Заявка на ремонт (необязательно)',
                          border: OutlineInputBorder(),
                        ),
                        items: [
                          const DropdownMenuItem<int?>(value: null, child: Text('— не привязывать —')),
                          for (final o in _orders)
                            if ((o as Map)['id'] is int)
                              DropdownMenuItem<int?>(
                                value: (o as Map)['id'] as int,
                                child: Text(
                                  '№ ${(o as Map)['orderNumber'] ?? ''} · ${(o as Map)['equipmentModel'] ?? ''}',
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                        ],
                        onChanged: (v) => setState(() => _orderId = v),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _comment,
                        decoration: const InputDecoration(
                          labelText: 'Комментарий для кладовщика',
                          border: OutlineInputBorder(),
                        ),
                      ),
                      const SizedBox(height: 16),
                      FilledButton(
                        onPressed: _submitting ? null : _submit,
                        child: _submitting
                            ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Text('Отправить'),
                      ),
                      const SizedBox(height: 28),
                      Text('Мои заявки', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 8),
                      if (_my.isEmpty)
                        const Text('Пока нет заявок.', style: TextStyle(color: Colors.grey))
                      else
                        ..._my.map((raw) {
                          final r = raw as Map<String, dynamic>;
                          final status = r['status']?.toString() ?? '';
                          final label = _statusRu(status);
                          final partName = r['partName']?.toString() ?? '';
                          final code = r['partCode']?.toString() ?? '';
                          final qty = r['quantity'];
                          final ord = r['orderNumber']?.toString();
                          final wh = r['warehouseComment']?.toString();
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              title: Text('$code $partName × $qty'),
                              subtitle: Text('$label${ord != null && ord.isNotEmpty ? ' · ремонт № $ord' : ''}'
                                  '${wh != null && wh.isNotEmpty ? '\nОтвет склада: $wh' : ''}'),
                              isThreeLine: wh != null && wh.isNotEmpty,
                            ),
                          );
                        }),
                    ],
                  ),
                ),
    );
  }

  static String _statusRu(String s) => switch (s) {
        'Pending' => 'Ожидает',
        'Completed' => 'Выдано',
        'Rejected' => 'Отклонено',
        _ => s,
      };
}
