using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;
using OrgTechRepair.Models;
using OrgTechRepair.Models.DTOs;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProductsController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProductsController> _logger;

    private static readonly HashSet<string> AllowedImageExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public ProductsController(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWebHostEnvironment env,
        ILogger<ProductsController> logger)
    {
        _contextFactory = contextFactory;
        _env = env;
        _logger = logger;
    }

    private static ProductDto MapDto(Product p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Model = p.Model,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name,
        Price = p.Price,
        Quantity = p.Quantity,
        ImageUrl = p.ImageUrl
    };

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var products = await context.Products
            .Include(p => p.Supplier)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Model = p.Model,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier!.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products
            .Include(p => p.Supplier)
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Model = p.Model,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier!.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                ImageUrl = p.ImageUrl
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    [Authorize(Roles = "Manager,OfficeManager,WarehouseKeeper,Administrator")]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var product = new Product
        {
            Code = dto.Code,
            Name = dto.Name,
            Model = dto.Model,
            SupplierId = dto.SupplierId,
            Price = dto.Price,
            Quantity = dto.Quantity
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var created = await context.Products.Include(p => p.Supplier).FirstAsync(p => p.Id == product.Id);
        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, MapDto(created));
    }

    // PUT: api/products/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,OfficeManager,WarehouseKeeper,Administrator")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        product.Code = dto.Code;
        product.Name = dto.Name;
        product.Model = dto.Model;
        product.SupplierId = dto.SupplierId;
        product.Price = dto.Price;
        product.Quantity = dto.Quantity;

        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Загрузить или заменить фото товара (multipart, поле file).</summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Manager,OfficeManager,WarehouseKeeper,Administrator")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ProductDto>> UploadProductImage(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл не передан (ожидается поле file)." });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExt.Contains(ext))
            return BadRequest(new { error = "Допустимы изображения: jpg, png, webp, gif." });

        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
            return NotFound();

        DeletePhysicalImageIfAny(product.ImageUrl);

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
            await file.CopyToAsync(stream, cancellationToken);

        product.ImageUrl = $"/uploads/products/{fileName}";
        await context.SaveChangesAsync(cancellationToken);

        var reloaded = await context.Products.Include(p => p.Supplier).FirstAsync(p => p.Id == id, cancellationToken);
        return Ok(MapDto(reloaded));
    }

    /// <summary>Удалить фото товара.</summary>
    [HttpDelete("{id:int}/image")]
    [Authorize(Roles = "Manager,OfficeManager,WarehouseKeeper,Administrator")]
    public async Task<IActionResult> DeleteProductImage(int id, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
            return NotFound();

        DeletePhysicalImageIfAny(product.ImageUrl);
        product.ImageUrl = null;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // DELETE: api/products/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager,OfficeManager,WarehouseKeeper,Administrator")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        DeletePhysicalImageIfAny(product.ImageUrl);
        context.Products.Remove(product);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private void DeletePhysicalImageIfAny(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
            return;
        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(_env.WebRootPath, relative);
        try
        {
            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось удалить файл изображения: {Path}", full);
        }
    }
}
