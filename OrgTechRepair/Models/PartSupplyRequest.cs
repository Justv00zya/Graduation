namespace OrgTechRepair.Models;

/// <summary>Заявка сервисного инженера на выдачу запчасти со склада (обрабатывает кладовщик).</summary>
public class PartSupplyRequest
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public Part? Part { get; set; }
    /// <summary>Количество к выдаче со склада.</summary>
    public int Quantity { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    /// <summary>Привязка к заявке на ремонт (необязательно).</summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public string? Comment { get; set; }
    /// <summary>Pending, Completed, Rejected</summary>
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByUserId { get; set; }
    public string? WarehouseComment { get; set; }
}
