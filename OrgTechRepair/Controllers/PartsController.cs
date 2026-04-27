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
public class PartsController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<PartsController> _logger;

    public PartsController(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<PartsController> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // GET: api/parts
    [HttpGet]
    [Authorize(Roles = "Engineer,ServiceEngineer,WarehouseKeeper,Director,Administrator")]
    public async Task<ActionResult<IEnumerable<object>>> GetParts()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var parts = await context.Parts
            .Include(p => p.Supplier)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier!.Name,
                p.Price,
                p.Quantity
            })
            .ToListAsync();

        return Ok(parts);
    }

    // GET: api/parts/5
    [HttpGet("{id}")]
    [Authorize(Roles = "Engineer,ServiceEngineer,WarehouseKeeper,Director,Administrator")]
    public async Task<ActionResult<object>> GetPart(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var part = await context.Parts
            .Include(p => p.Supplier)
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier!.Name,
                p.Price,
                p.Quantity
            })
            .FirstOrDefaultAsync();

        if (part == null)
        {
            return NotFound();
        }

        return Ok(part);
    }
}
