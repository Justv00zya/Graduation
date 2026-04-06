import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';
import '../../widgets/product_card_image.dart';

class SaleViewScreen extends StatefulWidget {
  final int saleId;

  const SaleViewScreen({super.key, required this.saleId});

  @override
  State<SaleViewScreen> createState() => _SaleViewScreenState();
}

class _SaleViewScreenState extends State<SaleViewScreen> {
  Map<String, dynamic>? _sale;
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
      final s = await context.read<AuthProvider>().api.getSale(widget.saleId);
      if (mounted) setState(() { _sale = s; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = apiErrorMessage(e); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return buildAppScaffold(context, title: 'Продажа', body: const Center(child: CircularProgressIndicator()));
    if (_error != null || _sale == null) return buildAppScaffold(context, title: 'Продажа', body: Center(child: Text(_error ?? 'Не найдено')));
    final s = _sale!;
    final items = s['items'] as List<dynamic>? ?? [];
    final api = context.read<AuthProvider>().api;
    return buildAppScaffold(
      context,
      title: s['saleNumber']?.toString() ?? 'Продажа',
      body: RefreshIndicator(
        onRefresh: _load,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Клиент: ${s['clientName'] ?? "—"}'),
              const SizedBox(height: 8),
              Text('Дата: ${s['saleDate'] ?? "—"}'),
              Text('Сумма: ${s['totalAmount'] ?? "—"} ₽'),
              const SizedBox(height: 16),
              const Text('Позиции:', style: TextStyle(fontWeight: FontWeight.bold)),
              ...items.map((e) {
                final m = e as Map<String, dynamic>;
                final imgUrl = api.productImageAbsoluteUrl(
                  m['productImageUrl']?.toString() ?? m['ProductImageUrl']?.toString(),
                );
                final name = m['productName']?.toString() ?? '';
                return Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: Padding(
                    padding: const EdgeInsets.all(10),
                    child: Row(
                      children: [
                        ProductCardImage(imageUrl: imgUrl, width: 72, height: 72),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(name, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
                              const SizedBox(height: 4),
                              Text('${m['quantity']} × ${m['unitPrice']} = ${m['totalPrice']} ₽', style: Theme.of(context).textTheme.bodySmall),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              }),
            ],
          ),
        ),
      ),
    );
  }
}
