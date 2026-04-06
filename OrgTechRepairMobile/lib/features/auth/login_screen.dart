import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/app_config.dart';
import '../../core/auth_provider.dart';
import '../../core/server_url_storage.dart';
import '../../widgets/auth_ui.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  final _serverUrlController = TextEditingController();
  bool _loading = false;
  bool _savingUrl = false;
  bool _checkingConnection = false;
  String? _error;
  String? _urlSavedMessage;
  /// null — ещё читаем настройки; true/false — развернуть блок «Адрес сервера».
  bool? _serverSectionExpanded;

  @override
  void initState() {
    super.initState();
    _serverUrlController.addListener(_onServerUrlTextChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) => _initServerSection());
  }

  void _onServerUrlTextChanged() {
    if (mounted) setState(() {});
  }

  Future<void> _initServerSection() async {
    if (!mounted) return;
    final auth = context.read<AuthProvider>();
    if (!auth.loaded) await auth.loadFromStorage();
    final explicit = await hasExplicitServerUrlPreference();
    var url = auth.serverUrl.isNotEmpty ? auth.serverUrl : await getSavedServerUrl();
    if (!mounted) return;
    setState(() {
      _serverUrlController.text = url;
      // После сохранения адреса или входа блок по умолчанию свёрнут — меньше прокрутки.
      _serverSectionExpanded = !explicit;
    });
  }

  @override
  void dispose() {
    _serverUrlController.removeListener(_onServerUrlTextChanged);
    _usernameController.dispose();
    _passwordController.dispose();
    _serverUrlController.dispose();
    super.dispose();
  }

  Future<void> _saveServerUrl() async {
    final url = _serverUrlController.text.trim();
    if (url.isEmpty) return;
    setState(() { _savingUrl = true; _urlSavedMessage = null; });
    try {
      await context.read<AuthProvider>().setServerUrl(url);
      if (!mounted) return;
      setState(() { _urlSavedMessage = 'Адрес сохранён'; _savingUrl = false; });
    } catch (e) {
      if (mounted) setState(() { _urlSavedMessage = null; _savingUrl = false; });
    }
  }

  Future<void> _checkConnection() async {
    String url = _serverUrlController.text.trim();
    if (url.isEmpty) {
      url = context.read<AuthProvider>().serverUrl;
      if (url.isEmpty) url = kApiBaseUrl;
    }
    if (url.isEmpty) return;
    setState(() { _checkingConnection = true; _urlSavedMessage = null; });
    try {
      final auth = context.read<AuthProvider>();
      await auth.setServerUrl(url);
      await auth.api.checkConnection();
      if (!mounted) return;
      setState(() { _checkingConnection = false; _urlSavedMessage = 'Сервер доступен'; });
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

  Future<void> _submit() async {
    _error = null;
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final auth = context.read<AuthProvider>();
      final serverUrl = _serverUrlController.text.trim();
      if (serverUrl.isNotEmpty) await auth.setServerUrl(serverUrl);
      await auth.login(_usernameController.text.trim(), _passwordController.text);
      if (!mounted) return;
      Navigator.pushReplacementNamed(context, '/');
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      appBar: AppBar(title: const Text('Вход')),
      body: AuthDecoratedBody(
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 420),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const AppBrandHeader(subtitle: 'Вход в систему'),
                    const SizedBox(height: 24),
                    AuthFormCard(
                      child: Form(
                        key: _formKey,
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            TextFormField(
                              controller: _usernameController,
                              decoration: const InputDecoration(labelText: 'Логин или Email'),
                              textInputAction: TextInputAction.next,
                              validator: (v) => (v == null || v.trim().isEmpty) ? 'Введите логин или email' : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _passwordController,
                              decoration: const InputDecoration(labelText: 'Пароль'),
                              obscureText: true,
                              validator: (v) => (v == null || v.isEmpty) ? 'Введите пароль' : null,
                              onFieldSubmitted: (_) => _submit(),
                            ),
                            if (_error != null) ...[
                              const SizedBox(height: 16),
                              AuthErrorBanner(message: _error!),
                            ],
                            const SizedBox(height: 24),
                            FilledButton(
                              onPressed: _loading ? null : _submit,
                              child: _loading
                                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                                  : const Text('Войти'),
                            ),
                            const SizedBox(height: 16),
                            TextButton(
                              onPressed: () => Navigator.pushNamed(context, '/forgot-password'),
                              child: const Text('Забыли пароль?'),
                            ),
                            TextButton(
                              onPressed: () => Navigator.pushNamed(context, '/register'),
                              child: const Text('Нет аккаунта? Зарегистрироваться'),
                            ),
                            const SizedBox(height: 16),
                            if (_serverSectionExpanded == null)
                              const Padding(
                                padding: EdgeInsets.symmetric(vertical: 12),
                                child: Center(
                                  child: SizedBox(
                                    width: 24,
                                    height: 24,
                                    child: CircularProgressIndicator(strokeWidth: 2),
                                  ),
                                ),
                              )
                            else
                              ExpansionTile(
                                key: ValueKey(_serverSectionExpanded),
                                initiallyExpanded: _serverSectionExpanded!,
                                title: Text('Адрес сервера', style: Theme.of(context).textTheme.titleSmall),
                                subtitle: Text(
                                  _serverUrlController.text.isEmpty ? 'Не задан — разверните и укажите' : _serverUrlController.text,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant),
                                ),
                                shape: const Border(),
                                collapsedShape: const Border(),
                                children: [
                                  Padding(
                                    padding: const EdgeInsets.fromLTRB(0, 0, 0, 8),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.stretch,
                                      children: [
                                        Text(
                                          'Телефон и ПК в одной Wi‑Fi, порт 5121, например: 192.168.1.2:5121',
                                          style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.onSurfaceVariant),
                                        ),
                                        const SizedBox(height: 8),
                                        TextFormField(
                                          controller: _serverUrlController,
                                          decoration: const InputDecoration(
                                            labelText: 'http://IP:5121',
                                            hintText: '192.168.1.2:5121',
                                          ),
                                          keyboardType: TextInputType.url,
                                        ),
                                        const SizedBox(height: 8),
                                        Row(
                                          children: [
                                            Expanded(
                                              child: FilledButton.tonal(
                                                onPressed: _checkingConnection ? null : _checkConnection,
                                                child: _checkingConnection
                                                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                                                    : const Text('Проверить соединение'),
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
                                            padding: const EdgeInsets.only(top: 8),
                                            child: Text(
                                              _urlSavedMessage!,
                                              style: TextStyle(color: Theme.of(context).colorScheme.primary, fontWeight: FontWeight.w500),
                                            ),
                                          ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
