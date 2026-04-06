using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Accountant,Director,Administrator")]
public class ReportsController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ReportsController(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>Отчёт по продажам за период.</summary>
    [HttpGet("sales")]
    public async Task<ActionResult<IEnumerable<object>>> GetSalesReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var from = dateFrom ?? DateTime.Today.AddMonths(-1);
        var to = dateTo ?? DateTime.Today;
        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.Sales
            .Include(s => s.Client)
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .OrderBy(s => s.SaleDate)
            .Select(s => new
            {
                s.Id,
                s.SaleNumber,
                s.SaleDate,
                ClientName = s.Client != null ? s.Client.FullName : (string?)null,
                s.TotalAmount
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Отчёт по заявкам за период.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<object>>> GetOrdersReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var from = dateFrom ?? DateTime.Today.AddMonths(-1);
        var to = dateTo ?? DateTime.Today;
        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.Orders
            .Include(o => o.Client)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to)
            .OrderBy(o => o.OrderDate)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                ClientName = o.Client != null ? o.Client.FullName : (string?)null,
                o.Status,
                o.Cost
            })
            .ToListAsync();
        return Ok(list);
    }
}
