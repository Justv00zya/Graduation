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
public class SuppliersController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<SuppliersController> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Manager,Director,Administrator")]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.Suppliers
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                INN = s.INN,
                AccountNumber = s.AccountNumber,
                Phone = s.Phone
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Manager,Director,Administrator")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var s = await context.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        return Ok(new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            Address = s.Address,
            INN = s.INN,
            AccountNumber = s.AccountNumber,
            Phone = s.Phone
        });
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(CreateSupplierDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var s = new Supplier
        {
            Name = dto.Name,
            Address = dto.Address,
            INN = dto.INN,
            AccountNumber = dto.AccountNumber,
            Phone = dto.Phone
        };
        context.Suppliers.Add(s);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSupplier), new { id = s.Id }, new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            Address = s.Address,
            INN = s.INN,
            AccountNumber = s.AccountNumber,
            Phone = s.Phone
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<IActionResult> UpdateSupplier(int id, UpdateSupplierDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var s = await context.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        s.Name = dto.Name;
        s.Address = dto.Address;
        s.INN = dto.INN;
        s.AccountNumber = dto.AccountNumber;
        s.Phone = dto.Phone;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var s = await context.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        context.Suppliers.Remove(s);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
