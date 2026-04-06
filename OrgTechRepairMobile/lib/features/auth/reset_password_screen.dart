import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/auth_provider.dart';
import '../../widgets/auth_ui.dart';

class ResetPasswordScreen extends StatefulWidget {
  final String email;
  final String token;

  const ResetPasswordScreen({super.key, required this.email, required this.token});

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _emailDisplayController;
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _loading = false;
  String? _error;
  bool _success = false;

  @override
  void initState() {
    super.initState();
    _emailDisplayController = TextEditingController(text: widget.email);
  }

  @override
  void dispose() {
    _emailDisplayController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    _error = null;
    if (widget.token.isEmpty) {
      setState(() => _error = 'Неверная ссылка. Запросите новую.');
      return;
    }
    if (!_formKey.currentState!.validate()) return;
    final p = _passwordController.text;
    final c = _confirmController.text;
    if (p != c) {
      setState(() => _error = 'Пароли не совпадают');
      return;
    }
    setState(() => _loading = true);
    try {
      await context.read<AuthProvider>().api.resetPassword(
            email: widget.email,
            token: widget.token,
            newPassword: p,
            confirmPassword: c,
          );
      if (!mounted) return;
      setState(() {
        _success = true;
        _loading = false;
      });
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted && !_success) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_success) {
      return Scaffold(
        appBar: AppBar(title: const Text('Сброс пароля')),
        body: AuthDecoratedBody(
          child: SafeArea(
            child: AuthSuccessPanel(
              icon: Icons.verified_rounded,
              message: 'Пароль успешно изменён. Войдите с новым паролем.',
              buttonLabel: 'Перейти к входу',
              onPressed: () => Navigator.pushNamedAndRemoveUntil(context, '/login', (r) => false),
            ),
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Новый пароль')),
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
                    const AppBrandHeader(subtitle: 'Задайте новый пароль'),
                    const SizedBox(height: 24),
                    AuthFormCard(
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            TextFormField(
                              controller: _emailDisplayController,
                              readOnly: true,
                              decoration: const InputDecoration(
                                labelText: 'Email',
                                prefixIcon: Icon(Icons.mail_outline_rounded),
                              ),
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _passwordController,
                              decoration: const InputDecoration(
                                labelText: 'Новый пароль *',
                                prefixIcon: Icon(Icons.lock_outline_rounded),
                              ),
                              obscureText: true,
                              textInputAction: TextInputAction.next,
                              validator: (v) => (v == null || v.length < 6) ? 'Минимум 6 символов' : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _confirmController,
                              decoration: const InputDecoration(
                                labelText: 'Подтверждение пароля *',
                                prefixIcon: Icon(Icons.lock_reset_rounded),
                              ),
                              obscureText: true,
                              onFieldSubmitted: (_) => _submit(),
                              validator: (v) => (v == null || v.isEmpty) ? 'Обязательно' : null,
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
                                  : const Text('Сохранить пароль'),
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
