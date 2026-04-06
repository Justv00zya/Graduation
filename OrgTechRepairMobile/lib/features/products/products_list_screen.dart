import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';
import '../../widgets/product_card_image.dart';

class ProductsListScreen extends StatefulWidget {
  const ProductsListScreen({super.key});

  @override
  State<ProductsListScreen> createState() => _ProductsListScreenState();
}

class _ProductsListScreenState extends State<ProductsListScreen> {
  List<dynamic> _list = [];
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final list = await context.read<AuthProvider>().api.getProducts();
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
      title: 'Товары',
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
                      final api = context.read<AuthProvider>().api;
                      final id = p['id'] as int?;
                      final name = p['name']?.toString() ?? '';
                      final code = p['code']?.toString() ?? '';
                      final price = p['price'];
                      final img = api.productImageAbsoluteUrl(p['imageUrl']?.toString());
                      return Card(
                        margin: const EdgeInsets.only(bottom: 10),
                        child: InkWell(
                          onTap: canEdit && id != null ? () => Navigator.pushNamed(context, '/products/edit', arguments: id).then((_) => _load()) : null,
                          borderRadius: BorderRadius.circular(16),
                          child: Padding(
                            padding: const EdgeInsets.all(12),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.center,
                              children: [
                                ProductCardImage(imageUrl: img, width: 80, height: 80),
                                const SizedBox(width: 14),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(name, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
                                      const SizedBox(height: 4),
                                      Text('$code · ${price != null ? price.toString() : ""} ₽', style: Theme.of(context).textTheme.bodySmall),
                                    ],
                                  ),
                                ),
                                if (canEdit && id != null)
                                  IconButton(icon: const Icon(Icons.edit_outlined), onPressed: () => Navigator.pushNamed(context, '/products/edit', arguments: id).then((_) => _load())),
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
      actions: [
        if (canEdit) IconButton(icon: const Icon(Icons.add), onPressed: () => Navigator.pushNamed(context, '/products/create').then((_) => _load())),
      ],
    );
  }
}
