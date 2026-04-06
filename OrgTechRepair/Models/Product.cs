namespace OrgTechRepair.Models;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    /// <summary>Относительный URL картинки, например /uploads/products/5_abc.jpg (отдаётся из wwwroot).</summary>
    public string? ImageUrl { get; set; }
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
