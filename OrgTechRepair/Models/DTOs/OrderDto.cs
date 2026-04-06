namespace OrgTechRepair.Models.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public string EquipmentModel { get; set; } = string.Empty;
    public string? ConditionDescription { get; set; }
    public string? ComplaintDescription { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public decimal? Cost { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateOrderDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string EquipmentModel { get; set; } = string.Empty;
    public string? ConditionDescription { get; set; }
    public string? ComplaintDescription { get; set; }
    public int? EmployeeId { get; set; }
    public decimal? Cost { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Принят";
}

public class UpdateOrderDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string EquipmentModel { get; set; } = string.Empty;
    public string? ConditionDescription { get; set; }
    public string? ComplaintDescription { get; set; }
    public int? EmployeeId { get; set; }
    public decimal? Cost { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
