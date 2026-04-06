import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class PartsListScreen extends StatefulWidget {
  const PartsListScreen({super.key});

  @override
  State<PartsListScreen> createState() => _PartsListScreenState();
}

class _PartsListScreenState extends State<PartsListScreen> {
  List<dynamic> _list = [];
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final list = await context.read<AuthProvider>().api.getParts();
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
    return buildAppScaffold(
      context,
      title: 'Запчасти и тонер',
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
                      final p = _list[i] as Map<String, dynamic>;
                      final name = p['name']?.toString() ?? '';
                      final code = p['code']?.toString() ?? '';
                      final supplier = p['supplierName']?.toString() ?? '';
                      final price = p['price'];
                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          title: Text(name),
                          subtitle: Text('$code · $supplier · ${price != null ? price.toString() : ""} ₽'),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
