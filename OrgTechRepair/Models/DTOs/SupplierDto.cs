namespace OrgTechRepair.Models.DTOs;

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? INN { get; set; }
    public string? AccountNumber { get; set; }
    public string? Phone { get; set; }
}

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? INN { get; set; }
    public string? AccountNumber { get; set; }
    public string? Phone { get; set; }
}

public class UpdateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? INN { get; set; }
    public string? AccountNumber { get; set; }
    public string? Phone { get; set; }
}
