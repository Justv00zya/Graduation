using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;
using OrgTechRepair.Models.DTOs;
using System.Security.Claims;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Client")]
public class ClientCabinetController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ClientCabinetController(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>Профиль клиента (данные карточки клиента, привязанной к текущему пользователю).</summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ClientCabinetProfileDto>> GetProfile()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var client = await context.Clients
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null)
            return NotFound(new { message = "Профиль клиента не найден. Обратитесь к администратору." });

        return Ok(new ClientCabinetProfileDto
        {
            Id = client.Id,
            FullName = client.FullName,
            Email = client.Email,
            Phone = client.Phone,
            Address = client.Address
        });
    }

    /// <summary>Обновить профиль клиента (только свои данные).</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ClientCabinetProfileDto dto)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return NotFound();

        client.FullName = dto.FullName ?? client.FullName;
        client.Email = dto.Email ?? client.Email;
        client.Phone = dto.Phone;
        client.Address = dto.Address;
        await context.SaveChangesAsync();
        return Ok(new { message = "Профиль обновлён" });
    }

    /// <summary>Мои заявки (только заявки клиента, привязанного к текущему пользователю).</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return Ok(Array.Empty<OrderDto>());

        var orders = await context.Orders
            .Include(o => o.Client)
            .Include(o => o.Employee)
            .Where(o => o.ClientId == client.Id)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId,
                ClientName = o.Client != null ? o.Client.FullName : null,
                EquipmentModel = o.EquipmentModel,
                ConditionDescription = o.ConditionDescription,
                ComplaintDescription = o.ComplaintDescription,
                EmployeeId = o.EmployeeId,
                EmployeeName = o.Employee != null ? o.Employee.LastName + " " + o.Employee.FirstName : null,
                Cost = o.Cost,
                OrderDate = o.OrderDate,
                CompletionDate = o.CompletionDate,
                Status = o.Status
            })
            .ToListAsync();
        return Ok(orders);
    }

    /// <summary>Одна заявка клиента (только своя).</summary>
    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetMyOrder(int id)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return NotFound();

        var order = await context.Orders
            .Include(o => o.Client)
            .Include(o => o.Employee)
            .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == client.Id);
        if (order == null) return NotFound();

        return Ok(new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            ClientId = order.ClientId,
            ClientName = order.Client?.FullName,
            EquipmentModel = order.EquipmentModel,
            ConditionDescription = order.ConditionDescription,
            ComplaintDescription = order.ComplaintDescription,
            EmployeeId = order.EmployeeId,
            EmployeeName = order.Employee != null ? order.Employee.LastName + " " + order.Employee.FirstName : null,
            Cost = order.Cost,
            OrderDate = order.OrderDate,
            CompletionDate = order.CompletionDate,
            Status = order.Status
        });
    }
}

public class ClientCabinetProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
