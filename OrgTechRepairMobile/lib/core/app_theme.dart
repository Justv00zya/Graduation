import 'package:flutter/material.dart';

/// Общая визуальная система приложения (Material 3).
abstract final class AppTheme {
  /// Неоновый акцент для киберпанк-темы.
  static const Color _seed = Color(0xFF2AE9FF);

  static ThemeData light() {
    final cs = ColorScheme.fromSeed(seedColor: _seed, brightness: Brightness.light);
    return _base(cs);
  }

  static ThemeData dark() {
    final cs = ColorScheme.fromSeed(seedColor: _seed, brightness: Brightness.dark);
    return _base(cs);
  }

  static ThemeData _base(ColorScheme cs) {
    final baseText = ThemeData(colorScheme: cs, useMaterial3: true).textTheme;
    return ThemeData(
      useMaterial3: true,
      colorScheme: cs,
      visualDensity: VisualDensity.adaptivePlatformDensity,
      textTheme: baseText.copyWith(
        headlineSmall: baseText.headlineSmall?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.4),
        titleMedium: baseText.titleMedium?.copyWith(fontWeight: FontWeight.w600),
        titleSmall: baseText.titleSmall?.copyWith(fontWeight: FontWeight.w600),
      ),
      navigationDrawerTheme: NavigationDrawerThemeData(
        indicatorColor: cs.secondaryContainer,
        backgroundColor: cs.surface,
      ),
      segmentedButtonTheme: SegmentedButtonThemeData(
        style: ButtonStyle(
          visualDensity: VisualDensity.compact,
          side: WidgetStatePropertyAll(BorderSide(color: cs.outline.withValues(alpha: 0.5))),
        ),
      ),
      appBarTheme: AppBarTheme(
        centerTitle: false,
        elevation: 0,
        scrolledUnderElevation: 2,
        backgroundColor: cs.brightness == Brightness.dark ? const Color(0xFF0D1224) : cs.surface,
        foregroundColor: cs.onSurface,
        surfaceTintColor: cs.surfaceTint,
        titleTextStyle: TextStyle(
          color: cs.onSurface,
          fontSize: 20,
          fontWeight: FontWeight.w800,
          letterSpacing: 1.2,
          shadows: const [
            Shadow(color: Color(0x992AE9FF), blurRadius: 10),
            Shadow(color: Color(0x66FF3FD0), blurRadius: 18),
          ],
        ),
      ),
      cardTheme: CardThemeData(
        clipBehavior: Clip.antiAlias,
        elevation: 0,
        color: cs.brightness == Brightness.dark ? const Color(0xFF111A33) : cs.surfaceContainerLow,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        margin: EdgeInsets.zero,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8))),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: cs.brightness == Brightness.dark
            ? const Color(0xFF172344)
            : cs.surfaceContainerHighest.withValues(alpha: 0.55),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: cs.outline.withValues(alpha: 0.45)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: cs.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: cs.error),
        ),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      ),
      listTileTheme: ListTileThemeData(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      ),
      dividerTheme: DividerThemeData(color: cs.outlineVariant, space: 1),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: cs.primary),
      scaffoldBackgroundColor: cs.brightness == Brightness.dark ? const Color(0xFF070B16) : null,
    );
  }
}
