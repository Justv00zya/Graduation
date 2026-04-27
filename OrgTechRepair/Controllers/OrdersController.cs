using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;
using OrgTechRepair.Models;
using OrgTechRepair.Models.DTOs;
using OrgTechRepair.Services;
using System.Security.Claims;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderPdfService? _orderPdfService;

    public OrdersController(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<OrdersController> logger,
        IOrderPdfService? orderPdfService = null)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _orderPdfService = orderPdfService;
    }

    /// <summary>Быстрая заявка с главной страницы (без авторизации).</summary>
    [HttpPost("quick-request")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> QuickRequest([FromBody] QuickRequestDto dto)
    {
        var clientName = (dto.ClientName ?? "").Trim();
        var phone = (dto.Phone ?? "").Trim();
        var equipmentModel = (dto.EquipmentModel ?? "").Trim();
        var complaint = (dto.ComplaintDescription ?? "").Trim();

        if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(equipmentModel) || string.IsNullOrWhiteSpace(complaint))
        {
            return BadRequest(new { message = "Заполните все обязательные поля" });
        }

        var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Если пользователь авторизован как Client, заявка должна попадать именно в его карточку.
        // Это критично для раздела "Личный кабинет -> Мои заявки".
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Client? client = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null)
            {
                if (string.IsNullOrWhiteSpace(client.FullName)) client.FullName = clientName;
                if (string.IsNullOrWhiteSpace(client.Phone)) client.Phone = phone;
                if (!string.IsNullOrWhiteSpace(client.Email) && string.IsNullOrWhiteSpace(client.Address))
                {
                    // no-op: сохраняем текущие данные как есть
                }
            }
        }

        if (client == null)
        {
            var clients = await context.Clients.ToListAsync();
            client = clients.FirstOrDefault(c => new string((c.Phone ?? "").Where(char.IsDigit).ToArray()) == normalizedPhone);
        }

        if (client == null)
        {
            client = new Client { FullName = clientName, Phone = phone };
            context.Clients.Add(client);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(client.FullName)) client.FullName = clientName;
            if (string.IsNullOrWhiteSpace(client.Phone)) client.Phone = phone;
        }

        // Для новой карточки клиента получаем Id до создания заявки.
        if (client.Id == 0)
            await context.SaveChangesAsync();

        var order = new Order
        {
            OrderNumber = $"WEB-{DateTime.Now:yyyyMMddHHmmss}",
            ClientId = client.Id,
            EquipmentModel = equipmentModel,
            ComplaintDescription = complaint,
            OrderDate = DateTime.Now,
            Status = "Принят"
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        if (_orderPdfService != null)
        {
            try
            {
                await _orderPdfService.GenerateOrderPdfAsync(order, client);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF для заказа {OrderId} не сгенерирован", order.Id);
            }
        }

        var orderDto = await context.Orders
            .Include(o => o.Client)
            .Include(o => o.Employee)
            .Where(o => o.Id == order.Id)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId,
                ClientName = o.Client!.FullName,
                EquipmentModel = o.EquipmentModel,
                ConditionDescription = o.ConditionDescription,
                ComplaintDescription = o.ComplaintDescription,
                EmployeeId = o.EmployeeId,
                EmployeeName = o.Employee != null ? $"{o.Employee.LastName} {o.Employee.FirstName}" : null,
                Cost = o.Cost,
                OrderDate = o.OrderDate,
                CompletionDate = o.CompletionDate,
                Status = o.Status
            })
            .FirstAsync();

        return Ok(orderDto);
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var orders = await context.Orders
            .Include(o => o.Client)
            .Include(o => o.Employee)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId,
                ClientName = o.Client!.FullName,
                EquipmentModel = o.EquipmentModel,
                ConditionDescription = o.ConditionDescription,
                ComplaintDescription = o.ComplaintDescription,
                EmployeeId = o.EmployeeId,
                EmployeeName = o.Employee != null ? $"{o.Employee.LastName} {o.Employee.FirstName}" : null,
                Cost = o.Cost,
                OrderDate = o.OrderDate,
                CompletionDate = o.CompletionDate,
                Status = o.Status
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var order = await context.Orders
            .Include(o => o.Client)
            .Include(o => o.Employee)
            .Where(o => o.Id == id)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId,
                ClientName = o.Client!.FullName,
                EquipmentModel = o.EquipmentModel,
                ConditionDescription = o.ConditionDescription,
                ComplaintDescription = o.ComplaintDescription,
                EmployeeId = o.EmployeeId,
                EmployeeName = o.Employee != null ? $"{o.Employee.LastName} {o.Employee.FirstName}" : null,
                Cost = o.Cost,
                OrderDate = o.OrderDate,
                CompletionDate = o.CompletionDate,
                Status = o.Status
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    // POST: api/orders
    [HttpPost]
    [Authorize(Roles = "Manager,OfficeManager,ServiceEngineer,Administrator")]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var order = new Order
        {
            OrderNumber = dto.OrderNumber,
            ClientId = dto.ClientId,
            EquipmentModel = dto.EquipmentModel,
            ConditionDescription = dto.ConditionDescription,
            ComplaintDescription = dto.ComplaintDescription,
            EmployeeId = dto.EmployeeId,
            Cost = dto.Cost,
            OrderDate = dto.OrderDate,
            Status = dto.Status
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    // PUT: api/orders/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,OfficeManager,Engineer,ServiceEngineer,Administrator")]
    public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var order = await context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.OrderNumber = dto.OrderNumber;
        order.ClientId = dto.ClientId;
        order.EquipmentModel = dto.EquipmentModel;
        order.ConditionDescription = dto.ConditionDescription;
        order.ComplaintDescription = dto.ComplaintDescription;
        order.EmployeeId = dto.EmployeeId;
        order.Cost = dto.Cost;
        order.CompletionDate = dto.CompletionDate;
        order.Status = dto.Status;

        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/orders/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var order = await context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        return NoContent();
    }
}

