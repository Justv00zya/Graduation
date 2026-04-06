namespace OrgTechRepair.Models.DTOs;

public class QuickRequestDto
{
    public string ClientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string EquipmentModel { get; set; } = string.Empty;
    public string? ComplaintDescription { get; set; }
}
