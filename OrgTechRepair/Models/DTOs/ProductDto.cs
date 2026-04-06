namespace OrgTechRepair.Models.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int SupplierId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class UpdateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int SupplierId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
