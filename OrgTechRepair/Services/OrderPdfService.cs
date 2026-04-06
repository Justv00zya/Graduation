using OrgTechRepair.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrgTechRepair.Services;

public class OrderPdfService : IOrderPdfService
{
    public Task<string> GenerateOrderPdfAsync(Order order, Client client)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopPath))
        {
            desktopPath = Directory.GetCurrentDirectory();
        }

        var fileName = $"Zayavka_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(desktopPath, fileName);

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

        return Task.FromResult(filePath);
    }
}
