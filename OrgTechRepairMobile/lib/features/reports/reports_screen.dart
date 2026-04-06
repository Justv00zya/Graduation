import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  DateTime _salesFrom = DateTime.now().subtract(const Duration(days: 30));
  DateTime _salesTo = DateTime.now();
  DateTime _ordersFrom = DateTime.now().subtract(const Duration(days: 30));
  DateTime _ordersTo = DateTime.now();

  List<dynamic> _salesReport = [];
  List<dynamic> _ordersReport = [];
  bool _salesLoading = false;
  bool _ordersLoading = false;
  String? _salesError;
  String? _ordersError;
  bool _salesLoaded = false;
  bool _ordersLoaded = false;

  Future<void> _loadSalesReport() async {
    setState(() { _salesLoading = true; _salesError = null; });
    try {
      final list = await context.read<AuthProvider>().api.getReportsSales(
            dateFrom: _salesFrom,
            dateTo: _salesTo,
          );
      if (mounted) setState(() {
        _salesReport = list;
        _salesLoading = false;
        _salesLoaded = true;
      });
    } catch (e) {
      if (mounted) setState(() {
        _salesError = apiErrorMessage(e);
        _salesLoading = false;
        _salesLoaded = true;
      });
    }
  }

  Future<void> _loadOrdersReport() async {
    setState(() { _ordersLoading = true; _ordersError = null; });
    try {
      final list = await context.read<AuthProvider>().api.getReportsOrders(
            dateFrom: _ordersFrom,
            dateTo: _ordersTo,
          );
      if (mounted) setState(() {
        _ordersReport = list;
        _ordersLoading = false;
        _ordersLoaded = true;
      });
    } catch (e) {
      if (mounted) setState(() {
        _ordersError = apiErrorMessage(e);
        _ordersLoading = false;
        _ordersLoaded = true;
      });
    }
  }

  String _formatDate(dynamic d) {
    if (d == null) return '—';
    if (d is String) return d.length >= 10 ? d.substring(0, 10) : d;
    return d.toString();
  }

  @override
  Widget build(BuildContext context) {
    return buildAppScaffold(
      context,
      title: 'Отчёты',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Отчёт по продажам
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Отчёт по продажам', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: _DateField(
                            label: 'С',
                            value: _salesFrom,
                            onTap: () async {
                              final d = await showDatePicker(
                                context: context,
                                initialDate: _salesFrom,
                                firstDate: DateTime(2020),
                                lastDate: DateTime.now(),
                              );
                              if (d != null && mounted) setState(() => _salesFrom = d);
                            },
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: _DateField(
                            label: 'По',
                            value: _salesTo,
                            onTap: () async {
                              final d = await showDatePicker(
                                context: context,
                                initialDate: _salesTo,
                                firstDate: DateTime(2020),
                                lastDate: DateTime.now(),
                              );
                              if (d != null && mounted) setState(() => _salesTo = d);
                            },
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: _salesLoading ? null : _loadSalesReport,
                        child: _salesLoading
                            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Text('Сформировать отчёт'),
                      ),
                    ),
                    if (_salesLoaded) ...[
                      const SizedBox(height: 12),
                      if (_salesError != null)
                        Text(_salesError!, style: TextStyle(color: Theme.of(context).colorScheme.error))
                      else
                        _SalesTable(items: _salesReport, formatDate: _formatDate),
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            // Отчёт по заявкам
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Отчёт по заявкам', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: _DateField(
                            label: 'С',
                            value: _ordersFrom,
                            onTap: () async {
                              final d = await showDatePicker(
                                context: context,
                                initialDate: _ordersFrom,
                                firstDate: DateTime(2020),
                                lastDate: DateTime.now(),
                              );
                              if (d != null && mounted) setState(() => _ordersFrom = d);
                            },
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: _DateField(
                            label: 'По',
                            value: _ordersTo,
                            onTap: () async {
                              final d = await showDatePicker(
                                context: context,
                                initialDate: _ordersTo,
                                firstDate: DateTime(2020),
                                lastDate: DateTime.now(),
                              );
                              if (d != null && mounted) setState(() => _ordersTo = d);
                            },
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: _ordersLoading ? null : _loadOrdersReport,
                        child: _ordersLoading
                            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Text('Сформировать отчёт'),
                      ),
                    ),
                    if (_ordersLoaded) ...[
                      const SizedBox(height: 12),
                      if (_ordersError != null)
                        Text(_ordersError!, style: TextStyle(color: Theme.of(context).colorScheme.error))
                      else
                        _OrdersTable(items: _ordersReport, formatDate: _formatDate),
                    ],
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DateField extends StatelessWidget {
  final String label;
  final DateTime value;
  final VoidCallback onTap;

  const _DateField({required this.label, required this.value, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final s = '${value.day.toString().padLeft(2, '0')}.${value.month.toString().padLeft(2, '0')}.${value.year}';
    return InkWell(
      onTap: onTap,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          border: const OutlineInputBorder(),
          contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        ),
        child: Text(s),
      ),
    );
  }
}

class _SalesTable extends StatelessWidget {
  final List<dynamic> items;
  final String Function(dynamic) formatDate;

  const _SalesTable({required this.items, required this.formatDate});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return const Text('Нет данных за выбранный период');
    }
    num total = 0;
    for (final s in items) {
      final m = s is Map ? (s['totalAmount'] ?? s['TotalAmount']) : null;
      if (m != null && m is num) total += m;
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Table(
          columnWidths: const {0: FlexColumnWidth(1.5), 1: FlexColumnWidth(1), 2: FlexColumnWidth(2), 3: FlexColumnWidth(1.2)},
          children: [
            TableRow(
              children: [
                _cell('Номер', bold: true),
                _cell('Дата', bold: true),
                _cell('Клиент', bold: true),
                _cell('Сумма', bold: true),
              ],
            ),
            ...items.map((s) {
              final m = s is Map ? s : null;
              final amount = m != null ? (m['totalAmount'] ?? m['TotalAmount']) : null;
              if (amount != null && amount is num) {}
              return TableRow(
                children: [
                  _cell(m?['saleNumber'] ?? m?['SaleNumber'] ?? '—'),
                  _cell(formatDate(m?['saleDate'] ?? m?['SaleDate'])),
                  _cell(m?['clientName'] ?? m?['ClientName'] ?? '—'),
                  _cell(amount != null ? '$amount' : '—'),
                ],
              );
            }),
            TableRow(
              children: [
                _cell('Итого:', bold: true),
                _cell(''),
                _cell(''),
                _cell(total.toStringAsFixed(2), bold: true),
              ],
            ),
          ],
        ),
      ],
    );
  }

  Widget _cell(dynamic text, {bool bold = false}) {
    return TableCell(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
        child: Text(
          text?.toString() ?? '—',
          style: TextStyle(fontWeight: bold ? FontWeight.bold : null, fontSize: 12),
        ),
      ),
    );
  }
}

class _OrdersTable extends StatelessWidget {
  final List<dynamic> items;
  final String Function(dynamic) formatDate;

  const _OrdersTable({required this.items, required this.formatDate});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return const Text('Нет данных за выбранный период');
    }
    num totalCost = 0;
    for (final o in items) {
      final c = o is Map ? (o['cost'] ?? o['Cost']) : null;
      if (c != null && c is num) totalCost += c;
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Table(
          columnWidths: const {0: FlexColumnWidth(1.5), 1: FlexColumnWidth(1), 2: FlexColumnWidth(1.5), 3: FlexColumnWidth(1), 4: FlexColumnWidth(1)},
          children: [
            TableRow(
              children: [
                _cell('Номер', bold: true),
                _cell('Дата', bold: true),
                _cell('Клиент', bold: true),
                _cell('Статус', bold: true),
                _cell('Стоимость', bold: true),
              ],
            ),
            ...items.map((o) {
              final m = o is Map ? o : null;
              return TableRow(
                children: [
                  _cell(m?['orderNumber'] ?? m?['OrderNumber'] ?? '—'),
                  _cell(formatDate(m?['orderDate'] ?? m?['OrderDate'])),
                  _cell(m?['clientName'] ?? m?['ClientName'] ?? '—'),
                  _cell(m?['status'] ?? m?['Status'] ?? '—'),
                  _cell(m?['cost'] != null || m?['Cost'] != null ? '${m?['cost'] ?? m?['Cost']}' : '—'),
                ],
              );
            }),
            TableRow(
              children: [
                _cell('Итого:', bold: true),
                _cell(''),
                _cell(''),
                _cell(''),
                _cell(totalCost.toStringAsFixed(2), bold: true),
              ],
            ),
          ],
        ),
      ],
    );
  }

  Widget _cell(dynamic text, {bool bold = false}) {
    return TableCell(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
        child: Text(
          text?.toString() ?? '—',
          style: TextStyle(fontWeight: bold ? FontWeight.bold : null, fontSize: 12),
        ),
      ),
    );
  }
}
