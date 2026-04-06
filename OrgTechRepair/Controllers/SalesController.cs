using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;
using OrgTechRepair.Models;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SalesController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SalesController> _logger;

    public SalesController(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<SalesController> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // GET: api/sales
    [HttpGet]
    [Authorize(Roles = "Manager,Accountant,Director,Administrator")]
    public async Task<ActionResult<IEnumerable<object>>> GetSales()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var sales = await context.Sales
            .Include(s => s.Client)
            .Select(s => new
            {
                s.Id,
                s.SaleNumber,
                ClientId = s.ClientId,
                ClientName = s.Client != null ? s.Client.FullName : null,
                s.SaleDate,
                s.TotalAmount
            })
            .ToListAsync();

        return Ok(sales);
    }

    // GET: api/sales/5
    [HttpGet("{id}")]
    [Authorize(Roles = "Manager,Accountant,Director,Administrator")]
    public async Task<ActionResult<object>> GetSale(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var sale = await context.Sales
            .Include(s => s.Client)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.SaleNumber,
                ClientId = s.ClientId,
                ClientName = s.Client != null ? s.Client.FullName : null,
                s.SaleDate,
                s.TotalAmount,
                Items = s.SaleItems.Select(si => new
                {
                    si.Id,
                    ProductId = si.ProductId,
                    ProductName = si.Product!.Name,
                    ProductImageUrl = si.Product.ImageUrl,
                    si.Quantity,
                    si.UnitPrice,
                    si.TotalPrice
                })
            })
            .FirstOrDefaultAsync();

        if (sale == null)
        {
            return NotFound();
        }

        return Ok(sale);
    }
}
