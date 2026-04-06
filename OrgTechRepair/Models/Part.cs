namespace OrgTechRepair.Models;

public class Part
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
}
