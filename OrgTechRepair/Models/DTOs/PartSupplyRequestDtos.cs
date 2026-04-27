namespace OrgTechRepair.Models.DTOs;

public class CreatePartSupplyRequestDto
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public int? OrderId { get; set; }
    public string? Comment { get; set; }
}

public class WarehouseRespondDto
{
    public string? Comment { get; set; }
}
