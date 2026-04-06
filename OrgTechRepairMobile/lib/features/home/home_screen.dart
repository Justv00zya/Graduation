import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'dart:async';

import '../../core/api_client.dart';
import '../../core/auth_provider.dart';
import '../../main.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final _formKey = GlobalKey<FormState>();
  final _clientNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _printerModelController = TextEditingController();
  final _problemController = TextEditingController();

  String? _selectedModel;
  String? _selectedProblem;
  bool _showCustomModel = false;
  bool _showCustomProblem = false;
  bool _loading = false;
  String? _success;
  String? _error;
  List<dynamic> _recentOrders = [];
  bool _recentLoading = false;
  String? _recentError;
  String? _recentLoadedFor;
  Timer? _relativeTimeTicker;

  static const _printerModels = [
    'HP LaserJet Pro M404dn', 'HP LaserJet Pro M402dn', 'HP LaserJet Pro M404dw',
    'Canon PIXMA TR8620', 'Canon PIXMA G3010', 'Canon imageRUNNER ADVANCE C5235i',
    'Epson Perfection V39', 'Epson L805', 'Epson WorkForce Pro WF-3720',
    'Brother HL-L2350DW', 'Brother MFC-L2700DW', 'Xerox VersaLink C405', 'Xerox Phaser 6510',
    'Samsung Xpress M2020W', 'Kyocera ECOSYS P5021cdn',
  ];

  static const _problems = [
    'Застряла бумага', 'Не печатает', 'Ошибка картриджа', 'Низкое качество печати',
    'Принтер не определяется компьютером', 'Замятие бумаги', 'Ошибка сканера',
    'Проблемы с подключением по Wi-Fi', 'Заканчивается тонер/чернила', 'Принтер издает странные звуки',
    'Не работает автоподача бумаги', 'Ошибка при двусторонней печати', 'Проблемы с драйвером', 'Принтер не включается',
  ];

  @override
  void dispose() {
    _relativeTimeTicker?.cancel();
    _clientNameController.dispose();
    _phoneController.dispose();
    _printerModelController.dispose();
    _problemController.dispose();
    super.dispose();
  }

  void _ensureRelativeTimeTicker() {
    _relativeTimeTicker ??= Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      if (_recentOrders.isEmpty) return;
      setState(() {});
    });
  }

  Future<void> _submitQuickRequest() async {
    _success = null;
    _error = null;
    if (!_formKey.currentState!.validate()) return;
    String model = _showCustomModel ? _printerModelController.text.trim() : (_selectedModel ?? '');
    String problem = _showCustomProblem ? _problemController.text.trim() : (_selectedProblem ?? '');
    if (model.isEmpty || problem.isEmpty) {
      setState(() => _error = 'Выберите или введите модель и проблему');
      return;
    }
    setState(() => _loading = true);
    try {
      await context.read<AuthProvider>().api.quickRequest(
        clientName: _clientNameController.text.trim(),
        phone: _phoneController.text.trim(),
        equipmentModel: model,
        complaintDescription: problem,
      );
      if (!mounted) return;
      setState(() {
        _success = 'Спасибо! Ваша заявка принята.';
        _clientNameController.clear();
        _phoneController.clear();
        _printerModelController.clear();
        _problemController.clear();
        _selectedModel = null;
        _selectedProblem = null;
        _showCustomModel = false;
        _showCustomProblem = false;
      });
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _loadRecentOrders() async {
    setState(() {
      _recentLoading = true;
      _recentError = null;
    });
    try {
      final list = await context.read<AuthProvider>().api.getOrders();
      if (!mounted) return;
      final sorted = List<dynamic>.from(list)
        ..sort((a, b) {
          final am = a as Map<String, dynamic>;
          final bm = b as Map<String, dynamic>;
          final ad = DateTime.tryParse(am['orderDate']?.toString() ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);
          final bd = DateTime.tryParse(bm['orderDate']?.toString() ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);
          return bd.compareTo(ad);
        });
      setState(() {
        _recentOrders = sorted.take(8).toList();
        _recentLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _recentError = apiErrorMessage(e);
        _recentLoading = false;
      });
    }
  }

  String _relativeOrderTime(String? iso) {
    if (iso == null || iso.isEmpty) return 'Создана недавно';
    final dt = DateTime.tryParse(iso);
    if (dt == null) return 'Создана недавно';
    final diff = DateTime.now().difference(dt);
    if (diff.inMinutes < 1) return 'Создана только что';
    if (diff.inMinutes < 60) return 'Создана ${diff.inMinutes} мин назад';
    if (diff.inHours < 24) return 'Создана ${diff.inHours} ч назад';
    return 'Создана ${diff.inDays} дн назад';
  }

  Widget _buildRecentOrdersCard(AuthProvider auth) {
    final canViewOrders = auth.isManagerOrDirectorOrAdmin || auth.isServiceEngineer;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.schedule_rounded, size: 22, color: Theme.of(context).colorScheme.primary),
                const SizedBox(width: 8),
                Text('Свежие заявки', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
                const Spacer(),
                IconButton(
                  tooltip: 'Обновить',
                  onPressed: _recentLoading || !canViewOrders ? null : _loadRecentOrders,
                  icon: const Icon(Icons.refresh_rounded),
                ),
              ],
            ),
            if (!canViewOrders)
              const Text('Для вашей роли список заявок недоступен.')
            else if (_recentLoading)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 20),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_recentError != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(_recentError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
              )
            else if (_recentOrders.isEmpty)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text('Новых заявок пока нет.'),
              )
            else
              ..._recentOrders.map((o) {
                final m = o as Map<String, dynamic>;
                final id = m['id'] as int?;
                final number = m['orderNumber']?.toString() ?? 'Заявка';
                final client = m['clientName']?.toString() ?? '—';
                final status = m['status']?.toString() ?? '—';
                final orderDate = m['orderDate']?.toString();
                return Card(
                  margin: const EdgeInsets.only(top: 8),
                  child: ListTile(
                    title: Text(number),
                    subtitle: Text('$client · $status\n${_relativeOrderTime(orderDate)}'),
                    isThreeLine: true,
                    trailing: const Icon(Icons.chevron_right_rounded),
                    onTap: id == null ? null : () => Navigator.pushNamed(context, '/orders/view', arguments: id),
                  ),
                );
              }),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final isEmployee = auth.isAuthenticated && !auth.isClient;
    final loadKey = '${auth.username}:${auth.roles.join(",")}';
    if (isEmployee) _ensureRelativeTimeTicker();
    if (isEmployee && _recentLoadedFor != loadKey && !_recentLoading) {
      _recentLoadedFor = loadKey;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _loadRecentOrders();
      });
    }
    return buildAppScaffold(
      context,
      title: 'ВузяПринт',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.print_rounded, size: 28, color: Theme.of(context).colorScheme.primary),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Добро пожаловать в систему управления!',
                            style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Наша платформа создана для бесперебойной автоматизации процессов торговли и ремонта оргтехники, обеспечивая точность, скорость и удобство в ежедневной работе.',
                    ),
                    const SizedBox(height: 8),
                    const Text('Как начать работу? Используйте меню — легко перемещайтесь между разделами.'),
                    const SizedBox(height: 8),
                    const Text('С уважением, команда ВузяПринт', style: TextStyle(fontStyle: FontStyle.italic)),
                    if (auth.isAuthenticated) ...[
                      const Divider(),
                      Text('Вы вошли как: ${auth.username}'),
                      Text('Роли: ${auth.roles.join(", ")}'),
                      if (auth.isClient) ...[
                        const SizedBox(height: 12),
                        FilledButton.tonal(
                          onPressed: () => Navigator.pushNamed(context, '/client-cabinet'),
                          child: const Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(Icons.person, size: 20),
                              SizedBox(width: 8),
                              Text('Личный кабинет'),
                            ],
                          ),
                        ),
                      ],
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            if (!isEmployee)
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(Icons.send_rounded, size: 22, color: Theme.of(context).colorScheme.tertiary),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                'Быстрая заявка на ремонт принтера',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: _clientNameController,
                          decoration: const InputDecoration(labelText: 'ФИО / Название организации *'),
                          validator: (v) => (v == null || v.trim().isEmpty) ? 'Обязательно' : null,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _phoneController,
                          decoration: const InputDecoration(labelText: 'Телефон *'),
                          keyboardType: TextInputType.phone,
                          validator: (v) => (v == null || v.trim().isEmpty) ? 'Обязательно' : null,
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String?>(
                          value: _selectedModel,
                          decoration: const InputDecoration(labelText: 'Модель принтера *'),
                          items: [
                            const DropdownMenuItem<String?>(value: null, child: Text('Выберите модель')),
                            ..._printerModels.map((m) => DropdownMenuItem<String?>(value: m, child: Text(m))),
                            const DropdownMenuItem<String?>(value: 'Другое', child: Text('Другое')),
                          ],
                          onChanged: (v) {
                            setState(() {
                              _selectedModel = v;
                              _showCustomModel = v == 'Другое';
                              if (!_showCustomModel && v != null) _printerModelController.text = v;
                              else if (_showCustomModel) _printerModelController.clear();
                            });
                          },
                        ),
                        if (_showCustomModel) ...[
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _printerModelController,
                            decoration: const InputDecoration(labelText: 'Модель (вручную) *'),
                            validator: (v) => _showCustomModel && (v == null || v.trim().isEmpty) ? 'Обязательно' : null,
                          ),
                        ],
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String?>(
                          value: _selectedProblem,
                          decoration: const InputDecoration(labelText: 'Описание проблемы *'),
                          items: [
                            const DropdownMenuItem<String?>(value: null, child: Text('Выберите проблему')),
                            ..._problems.map((p) => DropdownMenuItem<String?>(value: p, child: Text(p))),
                            const DropdownMenuItem<String?>(value: 'Другое', child: Text('Другое')),
                          ],
                          onChanged: (v) {
                            setState(() {
                              _selectedProblem = v;
                              _showCustomProblem = v == 'Другое';
                              if (!_showCustomProblem && v != null) _problemController.text = v;
                              else if (_showCustomProblem) _problemController.clear();
                            });
                          },
                        ),
                        if (_showCustomProblem) ...[
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _problemController,
                            decoration: const InputDecoration(labelText: 'Неисправность (вручную) *'),
                            maxLines: 2,
                            validator: (v) => _showCustomProblem && (v == null || v.trim().isEmpty) ? 'Обязательно' : null,
                          ),
                        ],
                        const SizedBox(height: 16),
                        SizedBox(
                          width: double.infinity,
                          child: FilledButton(
                            onPressed: _loading ? null : _submitQuickRequest,
                            child: _loading ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Отправить заявку'),
                          ),
                        ),
                        if (_success != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 12),
                            child: DecoratedBox(
                              decoration: BoxDecoration(
                                color: Theme.of(context).colorScheme.primaryContainer,
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Padding(
                                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Icon(Icons.check_circle_rounded, size: 22, color: Theme.of(context).colorScheme.onPrimaryContainer),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Text(
                                        _success!,
                                        style: TextStyle(color: Theme.of(context).colorScheme.onPrimaryContainer),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        if (_error != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 12),
                            child: DecoratedBox(
                              decoration: BoxDecoration(
                                color: Theme.of(context).colorScheme.errorContainer,
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Padding(
                                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Icon(Icons.error_outline_rounded, size: 22, color: Theme.of(context).colorScheme.onErrorContainer),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Text(
                                        _error!,
                                        style: TextStyle(color: Theme.of(context).colorScheme.onErrorContainer),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              )
            else
              _buildRecentOrdersCard(auth),
          ],
        ),
      ),
      actions: [
        if (!auth.isAuthenticated)
          TextButton(
            onPressed: () => Navigator.pushNamed(context, '/login'),
            child: const Text('Вход'),
          )
        else
          const SizedBox.shrink(),
      ],
    );
  }
}
