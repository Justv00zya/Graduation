import 'package:flutter/material.dart';

/// Фон с мягким градиентом для экранов входа / регистрации.
class AuthDecoratedBody extends StatelessWidget {
  const AuthDecoratedBody({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return DecoratedBox(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          stops: const [0, 0.35, 1],
          colors: [
            cs.primaryContainer.withValues(alpha: 0.45),
            cs.tertiaryContainer.withValues(alpha: 0.2),
            cs.surface,
          ],
        ),
      ),
      child: child,
    );
  }
}

/// Логотип и подзаголовок под бренд «ВузяПринт».
class AppBrandHeader extends StatelessWidget {
  const AppBrandHeader({super.key, required this.subtitle});

  final String subtitle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cs = theme.colorScheme;
    return Column(
      children: [
        DecoratedBox(
          decoration: BoxDecoration(
            color: cs.primary.withValues(alpha: 0.12),
            shape: BoxShape.circle,
          ),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Icon(Icons.print_rounded, size: 44, color: cs.primary),
          ),
        ),
        const SizedBox(height: 14),
        Text(
          'ВузяПринт',
          textAlign: TextAlign.center,
          style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.5),
        ),
        const SizedBox(height: 4),
        Text(
          subtitle,
          textAlign: TextAlign.center,
          style: theme.textTheme.bodyLarge?.copyWith(color: cs.onSurfaceVariant, fontWeight: FontWeight.w500),
        ),
      ],
    );
  }
}

/// Карточка с формой на экранах авторизации.
class AuthFormCard extends StatelessWidget {
  const AuthFormCard({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(color: cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: child,
      ),
    );
  }
}

/// Блок ошибки в стиле приложения.
class AuthErrorBanner extends StatelessWidget {
  const AuthErrorBanner({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: cs.errorContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.error_outline_rounded, size: 22, color: cs.onErrorContainer),
            const SizedBox(width: 10),
            Expanded(
              child: Text(message, style: TextStyle(color: cs.onErrorContainer)),
            ),
          ],
        ),
      ),
    );
  }
}

/// Экран успеха (регистрация, письмо отправлено и т.д.).
class AuthSuccessPanel extends StatelessWidget {
  const AuthSuccessPanel({
    super.key,
    required this.icon,
    required this.message,
    required this.buttonLabel,
    required this.onPressed,
  });

  final IconData icon;
  final String message;
  final String buttonLabel;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 400),
          child: AuthFormCard(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                DecoratedBox(
                  decoration: BoxDecoration(
                    color: cs.primaryContainer,
                    shape: BoxShape.circle,
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: Icon(icon, size: 48, color: cs.onPrimaryContainer),
                  ),
                ),
                const SizedBox(height: 20),
                Text(message, textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyLarge),
                const SizedBox(height: 24),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(onPressed: onPressed, child: Text(buttonLabel)),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
