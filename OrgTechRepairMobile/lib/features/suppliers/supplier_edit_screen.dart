import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class SupplierEditScreen extends StatefulWidget {
  final int? supplierId;

  const SupplierEditScreen({super.key, this.supplierId});

  @override
  State<SupplierEditScreen> createState() => _SupplierEditScreenState();
}

class _SupplierEditScreenState extends State<SupplierEditScreen> {
  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  final _innController = TextEditingController();
  final _accountController = TextEditingController();
  final _phoneController = TextEditingController();
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.supplierId != null) _load(); else setState(() => _loading = false);
  }

  Future<void> _load() async {
    try {
      final s = await context.read<AuthProvider>().api.getSupplier(widget.supplierId!);
      _nameController.text = s['name']?.toString() ?? '';
      _addressController.text = s['address']?.toString() ?? '';
      _innController.text = s['inn']?.toString() ?? '';
      _accountController.text = s['accountNumber']?.toString() ?? '';
      _phoneController.text = s['phone']?.toString() ?? '';
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _addressController.dispose();
    _innController.dispose();
    _accountController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _error = null);
    try {
      final api = context.read<AuthProvider>().api;
      final dto = {
        'name': _nameController.text.trim(),
        'address': _addressController.text.trim().isEmpty ? null : _addressController.text.trim(),
        'inn': _innController.text.trim().isEmpty ? null : _innController.text.trim(),
        'accountNumber': _accountController.text.trim().isEmpty ? null : _accountController.text.trim(),
        'phone': _phoneController.text.trim().isEmpty ? null : _phoneController.text.trim(),
      };
      if (widget.supplierId != null) await api.updateSupplier(widget.supplierId!, dto);
      else await api.createSupplier(dto);
      if (!mounted) return;
      Navigator.pop(context);
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return buildAppScaffold(context, title: widget.supplierId == null ? 'Новый поставщик' : 'Редактирование', body: const Center(child: CircularProgressIndicator()));
    return buildAppScaffold(
      context,
      title: widget.supplierId == null ? 'Новый поставщик' : 'Редактирование поставщика',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextField(controller: _nameController, decoration: const InputDecoration(labelText: 'Название *')),
            const SizedBox(height: 12),
            TextField(controller: _addressController, decoration: const InputDecoration(labelText: 'Адрес')),
            TextField(controller: _innController, decoration: const InputDecoration(labelText: 'ИНН')),
            TextField(controller: _accountController, decoration: const InputDecoration(labelText: 'Расчётный счёт')),
            TextField(controller: _phoneController, decoration: const InputDecoration(labelText: 'Телефон'), keyboardType: TextInputType.phone),
            if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
            const SizedBox(height: 16),
            FilledButton(onPressed: _save, child: const Text('Сохранить')),
          ],
        ),
      ),
    );
  }
}
