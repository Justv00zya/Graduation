using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrgTechRepair.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrgTechRepair.Services;

public class OrderPdfService : IOrderPdfService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderPdfService>? _logger;

    public OrderPdfService(IConfiguration configuration, ILogger<OrderPdfService>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Папка для PDF заявок: из конфигурации или «Рабочий стол/Заявки на ремонт».</summary>
    public static string ResolveArchiveFolder(IConfiguration configuration)
    {
        var configured = configuration["OrdersArchive:FolderPath"]?.Trim();
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Directory.GetCurrentDirectory();

        return Path.Combine(desktop, "Заявки на ремонт");
    }

    private static string SafeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "order";
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    public Task<string> GenerateOrderPdfAsync(Order order, Client client)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var folder = ResolveArchiveFolder(_configuration);
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось создать папку для заявок: {Folder}", folder);
            throw;
        }

        var safeNum = SafeFileName(order.OrderNumber);
        var fileName = $"Zayavka_{order.Id}_{safeNum}.pdf";
        var filePath = Path.Combine(folder, fileName);

        try
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);
                        column.Item().Text("Заявка на ремонт принтера").FontSize(18).Bold();
                        column.Item().Text($"Номер заявки: {order.OrderNumber}");
                        column.Item().Text($"Дата создания: {order.OrderDate:dd.MM.yyyy HH:mm}");
                        column.Item().LineHorizontal(1);

                        column.Item().Text("Данные клиента").Bold();
                        column.Item().Text($"ФИО / Организация: {client.FullName}");
                        column.Item().Text($"Телефон: {client.Phone ?? "-"}");
                        if (!string.IsNullOrWhiteSpace(client.Address))
                        {
                            column.Item().Text($"Адрес: {client.Address}");
                        }

                        column.Item().LineHorizontal(1);
                        column.Item().Text("Данные заявки").Bold();
                        column.Item().Text($"Модель принтера: {order.EquipmentModel}");
                        if (!string.IsNullOrWhiteSpace(order.ConditionDescription))
                        {
                            column.Item().Text($"Состояние техники: {order.ConditionDescription}");
                        }
                        column.Item().Text("Описание проблемы:");
                        column.Item().Text(order.ComplaintDescription ?? "-");

                        if (order.Cost.HasValue)
                        {
                            column.Item().LineHorizontal(1);
                            column.Item().Text($"Стоимость ремонта: {order.Cost.Value:N2} руб.").Bold();
                        }

                        if (order.Status != null)
                        {
                            column.Item().Text($"Статус: {order.Status}");
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка записи PDF заявки {OrderId} в {Path}", order.Id, filePath);
            throw;
        }

        _logger?.LogInformation("PDF заявки сохранён: {Path}", filePath);
        return Task.FromResult(filePath);
    }
}
