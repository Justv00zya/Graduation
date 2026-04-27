import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

/// Очередь заявок на выдачу со склада (кладовщик, руководство).
class WarehousePartSupplyScreen extends StatefulWidget {
  const WarehousePartSupplyScreen({super.key});

  @override
  State<WarehousePartSupplyScreen> createState() => _WarehousePartSupplyScreenState();
}

class _WarehousePartSupplyScreenState extends State<WarehousePartSupplyScreen> {
  List<dynamic> _list = [];
  bool _loading = true;
  bool _pendingOnly = true;
  String? _error;

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final api = context.read<AuthProvider>().api;
      final list = await api.getPartSupplyRequestsQueue(
        status: _pendingOnly ? 'Pending' : 'all',
      );
      if (mounted) setState(() {
        _list = list;
        _loading = false;
      });
    } catch (e) {
      if (mounted) setState(() {
        _error = apiErrorMessage(e);
        _loading = false;
      });
    }
  }

  Future<void> _complete(int id) async {
    try {
      await context.read<AuthProvider>().api.completePartSupplyRequest(id);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Выдача оформлена')));
        await _load();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(apiErrorMessage(e))));
      }
    }
  }

  Future<void> _reject(int id) async {
    try {
      await context.read<AuthProvider>().api.rejectPartSupplyRequest(id);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Заявка отклонена')));
        await _load();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(apiErrorMessage(e))));
      }
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
      title: 'Выдача со склада',
      actions: [
        IconButton(
          tooltip: _pendingOnly ? 'Показать все' : 'Только ожидающие',
          icon: Icon(_pendingOnly ? Icons.filter_list : Icons.filter_list_off),
          onPressed: () {
            setState(() => _pendingOnly = !_pendingOnly);
            _load();
          },
        ),
      ],
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
                  child: _list.isEmpty
                      ? ListView(
                          children: const [
                            SizedBox(height: 120),
                            Center(child: Text('Нет заявок')),
                          ],
                        )
                      : ListView.builder(
                          padding: const EdgeInsets.all(8),
                          itemCount: _list.length,
                          itemBuilder: (_, i) {
                            final r = _list[i] as Map<String, dynamic>;
                            final id = r['id'] as int?;
                            final status = r['status']?.toString() ?? '';
                            final pending = status == 'Pending';
                            final part = '${r['partCode'] ?? ''} ${r['partName'] ?? ''}'.trim();
                            final qty = r['quantity'];
                            final stock = r['stockQty'];
                            final who = r['requestedByUserName']?.toString() ?? '';
                            final ord = r['orderNumber']?.toString();
                            final comment = r['comment']?.toString();
                            return Card(
                              margin: const EdgeInsets.only(bottom: 8),
                              child: Padding(
                                padding: const EdgeInsets.all(12),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(part, style: Theme.of(context).textTheme.titleSmall),
                                    const SizedBox(height: 4),
                                    Text('Запрошено: $qty · на складе: $stock'),
                                    Text('Инженер: $who'),
                                    if (ord != null && ord.isNotEmpty) Text('Ремонт: № $ord'),
                                    if (comment != null && comment.isNotEmpty) Text('Комментарий: $comment'),
                                    const SizedBox(height: 8),
                                    if (pending && id != null)
                                      Row(
                                        children: [
                                          FilledButton(
                                            onPressed: () => _complete(id),
                                            child: const Text('Выдать'),
                                          ),
                                          const SizedBox(width: 8),
                                          OutlinedButton(
                                            onPressed: () => _reject(id),
                                            child: const Text('Отклонить'),
                                          ),
                                        ],
                                      )
                                    else
                                      Text(
                                        _statusRu(status),
                                        style: TextStyle(color: Theme.of(context).colorScheme.primary),
                                      ),
                                  ],
                                ),
                              ),
                            );
                          },
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
