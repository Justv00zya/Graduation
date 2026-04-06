namespace OrgTechRepair.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public string EquipmentModel { get; set; } = string.Empty;
    public string? ConditionDescription { get; set; }
    public string? ComplaintDescription { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal? Cost { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Status { get; set; } = "Принят"; // Принят, В работе, Выполнен, Отменен
    public ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
    public ICollection<Work> Works { get; set; } = new List<Work>();
}
