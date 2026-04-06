import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class ClientsListScreen extends StatefulWidget {
  const ClientsListScreen({super.key});

  @override
  State<ClientsListScreen> createState() => _ClientsListScreenState();
}

class _ClientsListScreenState extends State<ClientsListScreen> {
  List<dynamic> _list = [];
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final list = await context.read<AuthProvider>().api.getClients();
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
      title: 'Клиенты',
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
                      final c = _list[i] as Map<String, dynamic>;
                      final id = c['id'] as int?;
                      final name = c['fullName']?.toString() ?? '';
                      final phone = c['phone']?.toString() ?? '';
                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          title: Text(name),
                          subtitle: Text(phone),
                          onTap: () => Navigator.pushNamed(context, '/clients/edit', arguments: id),
                          trailing: canEdit ? IconButton(icon: const Icon(Icons.edit), onPressed: () => Navigator.pushNamed(context, '/clients/edit', arguments: id)) : null,
                        ),
                      );
                    },
                  ),
                ),
      actions: [
        if (canEdit) IconButton(icon: const Icon(Icons.add), onPressed: () => Navigator.pushNamed(context, '/clients/create').then((_) => _load())),
      ],
    );
  }
}
