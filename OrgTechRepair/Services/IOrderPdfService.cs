using OrgTechRepair.Models;

namespace OrgTechRepair.Services;

public interface IOrderPdfService
{
    Task<string> GenerateOrderPdfAsync(Order order, Client client);
}
