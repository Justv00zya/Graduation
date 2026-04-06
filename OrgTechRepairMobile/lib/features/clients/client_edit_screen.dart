import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class ClientEditScreen extends StatefulWidget {
  final int? clientId;

  const ClientEditScreen({super.key, this.clientId});

  @override
  State<ClientEditScreen> createState() => _ClientEditScreenState();
}

class _ClientEditScreenState extends State<ClientEditScreen> {
  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  final _phoneController = TextEditingController();
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.clientId != null) _load(); else setState(() => _loading = false);
  }

  Future<void> _load() async {
    try {
      final c = await context.read<AuthProvider>().api.getClient(widget.clientId!);
      _nameController.text = c['fullName']?.toString() ?? '';
      _addressController.text = c['address']?.toString() ?? '';
      _phoneController.text = c['phone']?.toString() ?? '';
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
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _error = null);
    try {
      final api = context.read<AuthProvider>().api;
      final dto = {'fullName': _nameController.text.trim(), 'address': _addressController.text.trim().isEmpty ? null : _addressController.text.trim(), 'phone': _phoneController.text.trim().isEmpty ? null : _phoneController.text.trim()};
      if (widget.clientId != null) await api.updateClient(widget.clientId!, dto); else await api.createClient(dto);
      if (!mounted) return;
      Navigator.pop(context);
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return buildAppScaffold(context, title: widget.clientId == null ? 'Новый клиент' : 'Редактирование', body: const Center(child: CircularProgressIndicator()));
    return buildAppScaffold(
      context,
      title: widget.clientId == null ? 'Новый клиент' : 'Редактирование клиента',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextField(controller: _nameController, decoration: const InputDecoration(labelText: 'ФИО / Название *')),
            const SizedBox(height: 12),
            TextField(controller: _addressController, decoration: const InputDecoration(labelText: 'Адрес')),
            const SizedBox(height: 12),
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
