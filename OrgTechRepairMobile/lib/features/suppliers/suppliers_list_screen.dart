import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class SuppliersListScreen extends StatefulWidget {
  const SuppliersListScreen({super.key});

  @override
  State<SuppliersListScreen> createState() => _SuppliersListScreenState();
}

class _SuppliersListScreenState extends State<SuppliersListScreen> {
  List<dynamic> _list = [];
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final list = await context.read<AuthProvider>().api.getSuppliers();
      if (mounted) setState(() { _list = list; _loading = false; });
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
    final canEdit = context.watch<AuthProvider>().isManagerOrDirectorOrAdmin;
    return buildAppScaffold(
      context,
      title: 'Поставщики',
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
                        Text('Проверьте адрес сервера на экране входа', style: Theme.of(context).textTheme.bodySmall),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(8),
                    itemCount: _list.length,
                    itemBuilder: (_, i) {
                      final s = _list[i] as Map<String, dynamic>;
                      final id = s['id'] as int?;
                      final name = s['name']?.toString() ?? '';
                      final phone = s['phone']?.toString() ?? '';
                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          title: Text(name),
                          subtitle: Text(phone),
                          onTap: () => canEdit ? Navigator.pushNamed(context, '/suppliers/edit', arguments: id) : null,
                          trailing: canEdit ? IconButton(icon: const Icon(Icons.edit), onPressed: () => Navigator.pushNamed(context, '/suppliers/edit', arguments: id)) : null,
                        ),
                      );
                    },
                  ),
                ),
      actions: [
        if (canEdit) IconButton(icon: const Icon(Icons.add), onPressed: () => Navigator.pushNamed(context, '/suppliers/create').then((_) => _load())),
      ],
    );
  }
}
