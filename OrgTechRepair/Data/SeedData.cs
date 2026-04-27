using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrgTechRepair.Models;

namespace OrgTechRepair.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles (включая роли по диаграмме: офис-менеджер, кассир, кладовщик)
        string[] roles = { "Administrator", "Manager", "OfficeManager", "Engineer", "ServiceEngineer", "Accountant", "Cashier", "WarehouseKeeper", "Director", "Client" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default admin user
        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new IdentityUser { UserName = "admin", Email = "admin@printservice.ru" };
            var result = await userManager.CreateAsync(admin, "111111");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrator");
            }
            else
            {
                // Log errors if user creation fails
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    var logger = loggerFactory.CreateLogger("SeedData");
                    foreach (var error in result.Errors)
                    {
                        logger.LogError("Error creating admin user: {Error}", error.Description);
                    }
                }
            }
        }

        // Seed positions
        if (!context.Positions.Any())
        {
            context.Positions.AddRange(
                new Position { Name = "Менеджер", Salary = 35000 },
                new Position { Name = "Бухгалтер", Salary = 40000 },
                new Position { Name = "Инженер", Salary = 45000 },
                new Position { Name = "Директор", Salary = 80000 }
            );
            await context.SaveChangesAsync();
        }

        // Seed work types
        if (!context.WorkTypes.Any())
        {
            context.WorkTypes.AddRange(
                new WorkType { Name = "Диагностика", Price = 500 },
                new WorkType { Name = "Ремонт", Price = 1500 },
                new WorkType { Name = "Заправка картриджа", Price = 800 },
                new WorkType { Name = "Чистка", Price = 600 }
            );
            await context.SaveChangesAsync();
        }

        // Seed demo employee (for команды)
        if (!context.Employees.Any())
        {
            var managerPosition = await context.Positions.FirstOrDefaultAsync(p => p.Name == "Менеджер");
            if (managerPosition != null)
            {
                context.Employees.Add(new Employee
                {
                    TabNumber = "DEMO-001",
                    PositionId = managerPosition.Id,
                    FirstName = "Демо",
                    LastName = "Менеджер",
                    MiddleName = "Системы",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    INN = null,
                    Address = "г. Демоград, ул. Примерная, д. 1",
                    HireDate = DateTime.Today.AddYears(-2)
                });
                await context.SaveChangesAsync();
            }
        }

        // Seed demo client
        if (!context.Clients.Any())
        {
            context.Clients.Add(new Client
            {
                FullName = "ООО \"Демо-клиент\"",
                Address = "г. Демоград, ул. Клиентская, д. 10",
                Phone = "+7 (900) 000-00-00"
            });
            await context.SaveChangesAsync();
        }

        // Seed demo Identity users (один сотрудник, один клиент)
        if (await userManager.FindByNameAsync("demo_manager") == null)
        {
            var demoManager = new IdentityUser
            {
                UserName = "demo_manager",
                Email = "demo_manager@example.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(demoManager, "111111");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(demoManager, "Manager");
            }
        }

        if (await userManager.FindByNameAsync("demo_client") == null)
        {
            var demoClient = new IdentityUser
            {
                UserName = "demo_client",
                Email = "demo_client@example.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(demoClient, "111111");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(demoClient, "Client");
        }

        // Привязка демо-клиента к пользователю demo_client (для личного кабинета)
        var demoClientUser = await userManager.FindByNameAsync("demo_client");
        if (demoClientUser != null)
        {
            var clientWithoutUser = await context.Clients.FirstOrDefaultAsync(c => c.UserId == null);
            if (clientWithoutUser != null)
            {
                clientWithoutUser.UserId = demoClientUser.Id;
                clientWithoutUser.Email = demoClientUser.Email;
                await context.SaveChangesAsync();
            }
        }

        // Демо-пользователь: сервисный инженер (урезанные возможности менеджера)
        if (await userManager.FindByNameAsync("demo_service") == null)
        {
            var demoService = new IdentityUser
            {
                UserName = "demo_service",
                Email = "demo_service@example.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(demoService, "111111");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(demoService, "ServiceEngineer");
            }
        }

        // Демо: офис-менеджер, кассир, кладовщик (роли по диаграмме)
        if (await userManager.FindByNameAsync("demo_office") == null)
        {
            var u = new IdentityUser
            {
                UserName = "demo_office",
                Email = "demo_office@example.com",
                EmailConfirmed = true
            };
            var r = await userManager.CreateAsync(u, "111111");
            if (r.Succeeded)
                await userManager.AddToRoleAsync(u, "OfficeManager");
        }

        if (await userManager.FindByNameAsync("demo_cashier") == null)
        {
            var u = new IdentityUser
            {
                UserName = "demo_cashier",
                Email = "demo_cashier@example.com",
                EmailConfirmed = true
            };
            var r = await userManager.CreateAsync(u, "111111");
            if (r.Succeeded)
                await userManager.AddToRoleAsync(u, "Cashier");
        }

        if (await userManager.FindByNameAsync("demo_warehouse") == null)
        {
            var u = new IdentityUser
            {
                UserName = "demo_warehouse",
                Email = "demo_warehouse@example.com",
                EmailConfirmed = true
            };
            var r = await userManager.CreateAsync(u, "111111");
            if (r.Succeeded)
                await userManager.AddToRoleAsync(u, "WarehouseKeeper");
        }

        // Поставщики для товаров
        if (!context.Suppliers.Any())
        {
            context.Suppliers.AddRange(
                new Supplier { Name = "ООО Картридж-Сервис", Address = "г. Москва, ул. Складская, 5", Phone = "+7 (495) 111-22-33", INN = "7701112233" },
                new Supplier { Name = "ИП Заправка.ру", Address = "г. Москва, пр. Торговый, 12", Phone = "+7 (495) 444-55-66", INN = "7704445566" },
                new Supplier { Name = "ЗАО Офис-Снаб", Address = "г. Санкт-Петербург, Невский пр., 100", Phone = "+7 (812) 333-44-55", INN = "7803334455" }
            );
            await context.SaveChangesAsync();
        }

        // Товары (расходники для оргтехники)
        if (!context.Products.Any())
        {
            var sup1 = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "ООО Картридж-Сервис");
            var sup2 = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "ИП Заправка.ру");
            var sup3 = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "ЗАО Офис-Снаб");
            if (sup1 != null && sup2 != null && sup3 != null)
            {
                context.Products.AddRange(
                    new Product { Code = "CRG-HP-85", SupplierId = sup1.Id, Name = "Картридж HP 85A", Model = "CE278A", Price = 3200, Quantity = 25 },
                    new Product { Code = "CRG-HP-36", SupplierId = sup1.Id, Name = "Картридж HP 36 (чёрный)", Model = "W2030X", Price = 2800, Quantity = 18 },
                    new Product { Code = "CRG-CAN-725", SupplierId = sup1.Id, Name = "Картридж Canon 725", Model = "PG-745", Price = 950, Quantity = 40 },
                    new Product { Code = "CRG-CAN-726", SupplierId = sup1.Id, Name = "Картридж Canon 726", Model = "CL-746", Price = 1200, Quantity = 35 },
                    new Product { Code = "TON-SAMS-MLT", SupplierId = sup2.Id, Name = "Тонер Samsung MLT-D104S", Model = "MLT-D104S", Price = 2100, Quantity = 12 },
                    new Product { Code = "DRM-XEROX", SupplierId = sup2.Id, Name = "Драм-картридж Xerox 013R01129", Model = "013R01129", Price = 8500, Quantity = 5 },
                    new Product { Code = "PAP-A4-500", SupplierId = sup3.Id, Name = "Бумага А4 500 листов", Model = "Снегурочка", Price = 280, Quantity = 120 },
                    new Product { Code = "PAP-A4-250", SupplierId = sup3.Id, Name = "Бумага А4 250 листов", Model = "SvetoCopy", Price = 165, Quantity = 80 },
                    new Product { Code = "FUSER-HP", SupplierId = sup2.Id, Name = "Вал термоблока HP LJ Pro M404", Model = "RM2-7989", Price = 4200, Quantity = 8 },
                    new Product { Code = "CLEAN-KIT", SupplierId = sup3.Id, Name = "Набор для чистки принтера", Model = "Universal", Price = 450, Quantity = 30 }
                );
                await context.SaveChangesAsync();
            }
        }

        // Запчасти и тонер для вкладки сервиса
        if (!context.Parts.Any())
        {
            var sup1 = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "ООО Картридж-Сервис");
            var sup2 = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "ИП Заправка.ру");
            var sup3 = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "ЗАО Офис-Снаб");
            if (sup1 != null && sup2 != null && sup3 != null)
            {
                context.Parts.AddRange(
                    new Part { Code = "PRT-ROL-HPM404", SupplierId = sup2.Id, Name = "Ролик подачи HP LJ M404", Price = 1250, Quantity = 14 },
                    new Part { Code = "PRT-FUSER-RM2", SupplierId = sup2.Id, Name = "Узел термозакрепления RM2-7990", Price = 6900, Quantity = 4 },
                    new Part { Code = "PRT-DRUM-DR2355", SupplierId = sup1.Id, Name = "Фотобарабан Brother DR-2355", Price = 5200, Quantity = 6 },
                    new Part { Code = "PRT-CHIP-278A", SupplierId = sup1.Id, Name = "Чип картриджа HP 78A/85A", Price = 180, Quantity = 90 },
                    new Part { Code = "PRT-BLADE-CANON", SupplierId = sup1.Id, Name = "Лезвие очистки Canon 725/728", Price = 420, Quantity = 36 },
                    new Part { Code = "TON-HP-100G", SupplierId = sup3.Id, Name = "Тонер HP универсальный (100 г)", Price = 540, Quantity = 55 },
                    new Part { Code = "TON-CANON-100G", SupplierId = sup3.Id, Name = "Тонер Canon универсальный (100 г)", Price = 560, Quantity = 48 },
                    new Part { Code = "TON-XEROX-100G", SupplierId = sup3.Id, Name = "Тонер Xerox универсальный (100 г)", Price = 610, Quantity = 32 },
                    new Part { Code = "PRT-GREASE-FUSER", SupplierId = sup2.Id, Name = "Термосмазка для печки (20 г)", Price = 350, Quantity = 22 },
                    new Part { Code = "PRT-WIPER-UNIV", SupplierId = sup1.Id, Name = "Ракель универсальный A4", Price = 380, Quantity = 28 }
                );
                await context.SaveChangesAsync();
            }
        }

        // Демо-заявки на ремонт (таблица на странице /orders)
        if (!context.Orders.Any() && context.Clients.Any())
        {
            if (await context.Clients.CountAsync() < 3)
            {
                context.Clients.AddRange(
                    new Client { FullName = "Иванов Пётр Сергеевич", Phone = "+79001112233", Address = "г. Москва, ул. Садовая, д. 15" },
                    new Client { FullName = "ООО «ТехноПлюс»", Phone = "+74951234567", Address = "г. Москва, ул. Заводская, д. 3" }
                );
                await context.SaveChangesAsync();
            }

            var clients = await context.Clients.OrderBy(c => c.Id).ToListAsync();
            var emp = await context.Employees.FirstOrDefaultAsync();
            int? empId = emp?.Id;
            Client C(int index) => clients[Math.Min(index, clients.Count - 1)];
            var today = DateTime.Today;

            context.Orders.AddRange(
                new Order
                {
                    OrderNumber = "ZR-2025-001",
                    ClientId = C(0).Id,
                    EquipmentModel = "HP LaserJet Pro M404dn",
                    ConditionDescription = "Внешний вид удовлетворительный",
                    ComplaintDescription = "Полосы на оттиске, износ РВ",
                    EmployeeId = empId,
                    Cost = 4500,
                    OrderDate = today.AddDays(-12),
                    CompletionDate = today.AddDays(-10),
                    Status = "Выполнен"
                },
                new Order
                {
                    OrderNumber = "ZR-2025-002",
                    ClientId = C(1).Id,
                    EquipmentModel = "Canon PIXMA MG2540S",
                    ConditionDescription = "Царапины на крышке сканера",
                    ComplaintDescription = "Не захватывает бумагу из нижнего лотка",
                    OrderDate = today.AddDays(-10),
                    Status = "В работе",
                    EmployeeId = empId,
                    Cost = 1800
                },
                new Order
                {
                    OrderNumber = "ZR-2025-003",
                    ClientId = C(2).Id,
                    EquipmentModel = "Brother DCP-L2500DR",
                    ComplaintDescription = "Ошибка замятия (Jam Inside)",
                    OrderDate = today.AddDays(-7),
                    Status = "Принят",
                    EmployeeId = null
                },
                new Order
                {
                    OrderNumber = "ZR-2025-004",
                    ClientId = C(0).Id,
                    EquipmentModel = "Samsung Xpress M2020W",
                    ConditionDescription = "Комплект полный",
                    ComplaintDescription = "Слабая печать, выцветание",
                    OrderDate = today.AddDays(-5),
                    Status = "В работе",
                    EmployeeId = empId,
                    Cost = 3200
                },
                new Order
                {
                    OrderNumber = "ZR-2025-005",
                    ClientId = C(1).Id,
                    EquipmentModel = "Xerox Phaser 3020",
                    ComplaintDescription = "Заправка, счётчик остановлен",
                    OrderDate = today.AddDays(-3),
                    Status = "Принят",
                    EmployeeId = null
                },
                new Order
                {
                    OrderNumber = "ZR-2025-006",
                    ClientId = C(2).Id,
                    EquipmentModel = "Epson L805",
                    ConditionDescription = "Сублимационная установка",
                    ComplaintDescription = "Полосы при цветной фотопечати",
                    OrderDate = today.AddDays(-2),
                    Status = "Выполнен",
                    EmployeeId = empId,
                    Cost = 5600,
                    CompletionDate = today.AddDays(-1)
                },
                new Order
                {
                    OrderNumber = "ZR-2025-007",
                    ClientId = C(0).Id,
                    EquipmentModel = "Kyocera ECOSYS P2235dw",
                    ComplaintDescription = "Отказ в печати, код ошибки 6000",
                    OrderDate = today.AddDays(-1),
                    Status = "Принят",
                    EmployeeId = null
                },
                new Order
                {
                    OrderNumber = "ZR-2025-008",
                    ClientId = C(1).Id,
                    EquipmentModel = "HP DeskJet 2320",
                    ComplaintDescription = "Отказ клиента после диагностики",
                    OrderDate = today.AddDays(-4),
                    Status = "Отменен",
                    EmployeeId = empId,
                    Cost = null
                }
            );
            await context.SaveChangesAsync();
        }

        // Демо-продажи (если есть клиенты и товары)
        if (!context.Sales.Any() && context.Clients.Any() && context.Products.Any())
        {
            var client = await context.Clients.FirstOrDefaultAsync();
            var products = await context.Products.Take(5).ToListAsync();
            if (client != null && products.Count >= 2)
            {
                var sale1 = new Sale
                {
                    SaleNumber = $"S-{DateTime.Now:yyyyMMdd}-001",
                    ClientId = client.Id,
                    SaleDate = DateTime.Today.AddDays(-5),
                    TotalAmount = 0
                };
                context.Sales.Add(sale1);
                await context.SaveChangesAsync();

                decimal total1 = 0;
                foreach (var p in products.Take(2))
                {
                    var qty = p.Id % 2 == 0 ? 2 : 1;
                    var totalPrice = p.Price * qty;
                    total1 += totalPrice;
                    context.SaleItems.Add(new SaleItem
                    {
                        SaleId = sale1.Id,
                        ProductId = p.Id,
                        Quantity = qty,
                        UnitPrice = p.Price,
                        TotalPrice = totalPrice
                    });
                }
                sale1.TotalAmount = total1;
                context.Sales.Update(sale1);

                var sale2 = new Sale
                {
                    SaleNumber = $"S-{DateTime.Now:yyyyMMdd}-002",
                    ClientId = client.Id,
                    SaleDate = DateTime.Today.AddDays(-2),
                    TotalAmount = 0
                };
                context.Sales.Add(sale2);
                await context.SaveChangesAsync();

                var prod2 = await context.Products.Skip(2).Take(2).ToListAsync();
                decimal total2 = 0;
                foreach (var p in prod2)
                {
                    var totalPrice = p.Price * 1;
                    total2 += totalPrice;
                    context.SaleItems.Add(new SaleItem
                    {
                        SaleId = sale2.Id,
                        ProductId = p.Id,
                        Quantity = 1,
                        UnitPrice = p.Price,
                        TotalPrice = totalPrice
                    });
                }
                sale2.TotalAmount = total2;
                context.Sales.Update(sale2);

                await context.SaveChangesAsync();
            }
        }
    }
}
