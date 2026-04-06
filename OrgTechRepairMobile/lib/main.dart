import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/app_theme.dart';
import 'core/auth_provider.dart';
import 'features/auth/login_screen.dart';
import 'features/auth/register_screen.dart';
import 'features/auth/forgot_password_screen.dart';
import 'features/auth/reset_password_screen.dart';
import 'features/home/home_screen.dart';
import 'features/orders/orders_list_screen.dart';
import 'features/orders/order_edit_screen.dart';
import 'features/orders/order_view_screen.dart';
import 'features/clients/clients_list_screen.dart';
import 'features/clients/client_edit_screen.dart';
import 'features/products/products_list_screen.dart';
import 'features/products/product_edit_screen.dart';
import 'features/suppliers/suppliers_list_screen.dart';
import 'features/suppliers/supplier_edit_screen.dart';
import 'features/sales/sales_list_screen.dart';
import 'features/sales/sale_view_screen.dart';
import 'features/employees/employees_list_screen.dart';
import 'features/parts/parts_list_screen.dart';
import 'features/reports/reports_screen.dart';
import 'features/admin/admin_screen.dart';
import 'features/client_cabinet/client_cabinet_screen.dart';
import 'features/client_cabinet/client_order_view_screen.dart';
import 'features/settings/server_connection_screen.dart';

void main() {
  runApp(const OrgTechRepairApp());
}

class OrgTechRepairApp extends StatelessWidget {
  const OrgTechRepairApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => AuthProvider()..loadFromStorage(),
      child: MaterialApp(
        title: 'ВузяПринт',
        theme: AppTheme.light(),
        darkTheme: AppTheme.dark(),
        themeMode: ThemeMode.system,
        initialRoute: '/',
        onGenerateRoute: (settings) {
          switch (settings.name) {
            case '/':
              return MaterialPageRoute(builder: (_) => const HomeScreen());
            case '/login':
              return MaterialPageRoute(builder: (_) => const LoginScreen());
            case '/register':
              return MaterialPageRoute(builder: (_) => const RegisterScreen());
            case '/forgot-password':
              return MaterialPageRoute(builder: (_) => const ForgotPasswordScreen());
            case '/reset-password':
              final args = settings.arguments as Map<String, String>?;
              return MaterialPageRoute(
                builder: (_) => ResetPasswordScreen(
                  email: args?['email'] ?? '',
                  token: args?['token'] ?? '',
                ),
              );
            case '/orders':
              return MaterialPageRoute(builder: (_) => const OrdersListScreen());
            case '/orders/create':
              return MaterialPageRoute(builder: (_) => const OrderEditScreen());
            case '/orders/edit':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => OrderEditScreen(orderId: id));
            case '/orders/view':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => OrderViewScreen(orderId: id ?? 0));
            case '/clients':
              return MaterialPageRoute(builder: (_) => const ClientsListScreen());
            case '/clients/create':
              return MaterialPageRoute(builder: (_) => const ClientEditScreen());
            case '/clients/edit':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => ClientEditScreen(clientId: id));
            case '/products':
              return MaterialPageRoute(builder: (_) => const ProductsListScreen());
            case '/products/create':
              return MaterialPageRoute(builder: (_) => const ProductEditScreen());
            case '/products/edit':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => ProductEditScreen(productId: id));
            case '/suppliers':
              return MaterialPageRoute(builder: (_) => const SuppliersListScreen());
            case '/suppliers/create':
              return MaterialPageRoute(builder: (_) => const SupplierEditScreen());
            case '/suppliers/edit':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => SupplierEditScreen(supplierId: id));
            case '/sales':
              return MaterialPageRoute(builder: (_) => const SalesListScreen());
            case '/sales/view':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => SaleViewScreen(saleId: id ?? 0));
            case '/employees':
              return MaterialPageRoute(builder: (_) => const EmployeesListScreen());
            case '/parts':
              return MaterialPageRoute(builder: (_) => const PartsListScreen());
            case '/reports':
              return MaterialPageRoute(builder: (_) => const ReportsScreen());
            case '/admin':
              return MaterialPageRoute(builder: (_) => const AdminScreen());
            case '/server-connection':
              return MaterialPageRoute(builder: (_) => const ServerConnectionScreen());
            case '/client-cabinet':
              return MaterialPageRoute(builder: (_) => const ClientCabinetScreen());
            case '/client-cabinet/order':
              final id = settings.arguments as int?;
              return MaterialPageRoute(builder: (_) => ClientOrderViewScreen(orderId: id ?? 0));
            default:
              return MaterialPageRoute(builder: (_) => const HomeScreen());
          }
        },
      ),
    );
  }
}

/// Общий scaffold с drawer для авторизованных пользователей.
Widget buildAppScaffold(
  BuildContext context, {
  required String title,
  required Widget body,
  List<Widget>? actions,
}) {
  final auth = context.watch<AuthProvider>();
  final cs = Theme.of(context).colorScheme;
  return Scaffold(
    appBar: AppBar(
      title: title == 'ВузяПринт'
          ? Text(
              'ВУЗЯПРИНТ',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.w800,
                letterSpacing: 1.4,
                shadows: const [
                  Shadow(color: Color(0xAA2AE9FF), blurRadius: 10),
                  Shadow(color: Color(0x77FF3FD0), blurRadius: 18),
                ],
              ),
            )
          : Text(title),
      actions: actions,
    ),
    drawer: auth.isAuthenticated ? _buildDrawer(context, auth) : null,
    body: DecoratedBox(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          stops: const [0, 0.22, 1],
          colors: [
            cs.surfaceContainerLow.withValues(alpha: 0.85),
            cs.surface,
            cs.surface,
          ],
        ),
      ),
      child: body,
    ),
  );
}

Drawer _buildDrawer(BuildContext context, AuthProvider auth) {
  final canOrders = auth.isManagerOrDirectorOrAdmin || auth.isServiceEngineer;
  final canProducts = auth.isManagerOrDirectorOrAdmin;
  final canSales = auth.isManagerOrDirectorOrAdmin;
  final canClients = auth.isManagerOrDirectorOrAdmin || auth.isServiceEngineer;
  final canSuppliers = auth.isManagerOrDirectorOrAdmin;
  final canParts = auth.isEngineerOrDirectorOrAdmin || auth.isServiceEngineer;
  final canEmployees = auth.isAccountantOrDirectorOrAdmin;
  final canReports = auth.isAccountantOrDirectorOrAdmin;
  final canAdmin = auth.isAdministrator;

  return Drawer(
    backgroundColor: Theme.of(context).colorScheme.surface,
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.horizontal(right: Radius.circular(20))),
    child: ListView(
      padding: EdgeInsets.zero,
      children: [
        DrawerHeader(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                Theme.of(context).colorScheme.primaryContainer,
                Theme.of(context).colorScheme.tertiaryContainer,
              ],
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'ВузяПринт',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onPrimaryContainer,
                ),
              ),
              if (auth.username != null)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text(
                    auth.username!,
                    style: TextStyle(color: Theme.of(context).colorScheme.onPrimaryContainer.withValues(alpha: 0.9)),
                  ),
                ),
            ],
          ),
        ),
        ListTile(
          leading: const Icon(Icons.home),
          title: const Text('Главная'),
          onTap: () {
            Navigator.pop(context);
            Navigator.pushReplacementNamed(context, '/');
          },
        ),
        if (auth.isClient)
          ListTile(
            leading: const Icon(Icons.person),
            title: const Text('Личный кабинет'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/client-cabinet'); },
          ),
        ListTile(
          leading: const Icon(Icons.dns_rounded),
          title: const Text('Сервер API'),
          subtitle: Text(
            auth.serverUrl.isEmpty ? 'Не задан' : auth.serverUrl,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          onTap: () {
            Navigator.pop(context);
            Navigator.pushNamed(context, '/server-connection');
          },
        ),
        if (canOrders) ...[
          const Divider(),
          ListTile(
            leading: const Icon(Icons.assignment),
            title: const Text('Заявки'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/orders'); },
          ),
        ],
        if (canProducts)
          ListTile(
            leading: const Icon(Icons.inventory_2),
            title: const Text('Товары'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/products'); },
          ),
        if (canSales)
          ListTile(
            leading: const Icon(Icons.shopping_cart),
            title: const Text('Продажи'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/sales'); },
          ),
        if (canClients)
          ListTile(
            leading: const Icon(Icons.people),
            title: const Text('Клиенты'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/clients'); },
          ),
        if (canSuppliers)
          ListTile(
            leading: const Icon(Icons.local_shipping),
            title: const Text('Поставщики'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/suppliers'); },
          ),
        if (canParts)
          ListTile(
            leading: const Icon(Icons.build),
            title: const Text('Запчасти и тонер'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/parts'); },
          ),
        if (canEmployees)
          ListTile(
            leading: const Icon(Icons.badge),
            title: const Text('Сотрудники'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/employees'); },
          ),
        if (canReports)
          ListTile(
            leading: const Icon(Icons.description),
            title: const Text('Отчеты'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/reports'); },
          ),
        if (canAdmin)
          ListTile(
            leading: const Icon(Icons.settings),
            title: const Text('Администрирование'),
            onTap: () { Navigator.pop(context); Navigator.pushNamed(context, '/admin'); },
          ),
        const Divider(),
        ListTile(
          leading: const Icon(Icons.logout),
          title: const Text('Выход'),
          onTap: () async {
            Navigator.pop(context);
            await auth.logout();
            if (context.mounted) Navigator.pushReplacementNamed(context, '/');
          },
        ),
      ],
    ),
  );
}
