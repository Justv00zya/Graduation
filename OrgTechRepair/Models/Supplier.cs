namespace OrgTechRepair.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? INN { get; set; }
    public string? AccountNumber { get; set; }
    public string? Phone { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Part> Parts { get; set; } = new List<Part>();
}
