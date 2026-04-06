import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/app_config.dart';
import '../../core/auth_provider.dart';
import '../../main.dart';

/// Смена адреса API без выхода из аккаунта (меню → Сервер API).
class ServerConnectionScreen extends StatefulWidget {
  const ServerConnectionScreen({super.key});

  @override
  State<ServerConnectionScreen> createState() => _ServerConnectionScreenState();
}

class _ServerConnectionScreenState extends State<ServerConnectionScreen> {
  final _serverUrlController = TextEditingController();
  bool _savingUrl = false;
  bool _checkingConnection = false;
  String? _urlSavedMessage;
  bool _seededFromAuth = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_seededFromAuth) return;
    _seededFromAuth = true;
    final url = context.read<AuthProvider>().serverUrl;
    if (url.isNotEmpty) _serverUrlController.text = url;
  }

  @override
  void dispose() {
    _serverUrlController.dispose();
    super.dispose();
  }

  Future<void> _saveServerUrl() async {
    final url = _serverUrlController.text.trim();
    if (url.isEmpty) return;
    setState(() {
      _savingUrl = true;
      _urlSavedMessage = null;
    });
    try {
      await context.read<AuthProvider>().setServerUrl(url);
      if (!mounted) return;
      setState(() {
        _urlSavedMessage = 'Адрес сохранён';
        _savingUrl = false;
      });
    } catch (_) {
      if (mounted) setState(() {
        _urlSavedMessage = null;
        _savingUrl = false;
      });
    }
  }

  Future<void> _checkConnection() async {
    String url = _serverUrlController.text.trim();
    if (url.isEmpty) {
      url = context.read<AuthProvider>().serverUrl;
      if (url.isEmpty) url = kApiBaseUrl;
    }
    if (url.isEmpty) return;
    setState(() {
      _checkingConnection = true;
      _urlSavedMessage = null;
    });
    try {
      final auth = context.read<AuthProvider>();
      await auth.setServerUrl(url);
      await auth.api.checkConnection();
      if (!mounted) return;
      setState(() {
        _checkingConnection = false;
        _urlSavedMessage = 'Сервер доступен';
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _checkingConnection = false;
        _urlSavedMessage = null;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(apiErrorMessage(e)),
            backgroundColor: Theme.of(context).colorScheme.error,
            duration: const Duration(seconds: 5),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return buildAppScaffold(
      context,
      title: 'Сервер API',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Телефон и ПК должны быть в одной сети Wi‑Fi (или укажите публичный URL, если сервер в интернете). Порт по умолчанию: 5121.',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _serverUrlController,
                  decoration: const InputDecoration(
                    labelText: 'http://IP:5121',
                    hintText: '192.168.1.2:5121',
                  ),
                  keyboardType: TextInputType.url,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: FilledButton.tonal(
                        onPressed: _checkingConnection ? null : _checkConnection,
                        child: _checkingConnection
                            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Text('Проверить'),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: FilledButton.tonal(
                        onPressed: _savingUrl ? null : _saveServerUrl,
                        child: _savingUrl
                            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Text('Сохранить'),
                      ),
                    ),
                  ],
                ),
                if (_urlSavedMessage != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(
                      _urlSavedMessage!,
                      style: TextStyle(color: cs.primary, fontWeight: FontWeight.w500),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
