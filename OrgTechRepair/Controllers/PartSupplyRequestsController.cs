using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;
using OrgTechRepair.Models;
using OrgTechRepair.Models.DTOs;
using System.Security.Claims;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PartSupplyRequestsController : ControllerBase
{
    private const string StatusPending = "Pending";
    private const string StatusCompleted = "Completed";
    private const string StatusRejected = "Rejected";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<PartSupplyRequestsController> _logger;

    public PartSupplyRequestsController(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        UserManager<IdentityUser> userManager,
        ILogger<PartSupplyRequestsController> logger)
    {
        _contextFactory = contextFactory;
        _userManager = userManager;
        _logger = logger;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>Очередь заявок для кладовщика и руководства (по умолчанию только ожидающие).</summary>
    [HttpGet]
    [Authorize(Roles = "WarehouseKeeper,Manager,OfficeManager,Director,Administrator")]
    public async Task<ActionResult<IEnumerable<object>>> GetQueue([FromQuery] string? status = StatusPending)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.PartSupplyRequests
            .AsNoTracking()
            .Include(r => r.Part)
            .Include(r => r.Order)
            .AsQueryable();

        if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            q = q.Where(r => r.Status == status);

        var list = await q
            .OrderBy(r => r.Status == StatusPending ? 0 : 1)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        var userIds = list.SelectMany(r => new[] { r.RequestedByUserId, r.ProcessedByUserId })
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
        var names = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToDictionaryAsync(x => x.Id, x => x.UserName ?? x.Id);

        string Name(string? id) => id != null && names.TryGetValue(id, out var n) ? n : (id ?? "");

        var result = list.Select(r => new
        {
            r.Id,
            r.PartId,
            partCode = r.Part?.Code,
            partName = r.Part?.Name,
            stockQty = r.Part?.Quantity,
            r.Quantity,
            r.RequestedByUserId,
            requestedByUserName = Name(r.RequestedByUserId),
            r.OrderId,
            orderNumber = r.Order?.OrderNumber,
            r.Comment,
            r.Status,
            r.CreatedAt,
            r.ProcessedAt,
            r.ProcessedByUserId,
            processedByUserName = Name(r.ProcessedByUserId),
            r.WarehouseComment
        });

        return Ok(result);
    }

    /// <summary>Мои заявки (сервисный инженер / инженер).</summary>
    [HttpGet("my")]
    [Authorize(Roles = "ServiceEngineer,Engineer")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyRequests()
    {
        var uid = CurrentUserId;
        if (string.IsNullOrEmpty(uid))
            return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.PartSupplyRequests
            .AsNoTracking()
            .Include(r => r.Part)
            .Include(r => r.Order)
            .Where(r => r.RequestedByUserId == uid)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = list.Select(r => new
        {
            r.Id,
            r.PartId,
            partCode = r.Part?.Code,
            partName = r.Part?.Name,
            r.Quantity,
            r.OrderId,
            orderNumber = r.Order?.OrderNumber,
            r.Comment,
            r.Status,
            r.CreatedAt,
            r.ProcessedAt,
            r.WarehouseComment
        });

        return Ok(result);
    }

    /// <summary>Создать заявку на выдачу со склада.</summary>
    [HttpPost]
    [Authorize(Roles = "ServiceEngineer,Engineer")]
    public async Task<ActionResult<object>> Create([FromBody] CreatePartSupplyRequestDto dto)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrEmpty(uid))
            return Unauthorized();

        if (dto.Quantity < 1)
            return BadRequest(new { message = "Укажите количество не менее 1" });

        await using var context = await _contextFactory.CreateDbContextAsync();
        var part = await context.Parts.FindAsync(dto.PartId);
        if (part == null)
            return NotFound(new { message = "Запчасть не найдена" });

        if (dto.OrderId is int oid)
        {
            var orderExists = await context.Orders.AnyAsync(o => o.Id == oid);
            if (!orderExists)
                return BadRequest(new { message = "Заявка на ремонт не найдена" });
        }

        var entity = new PartSupplyRequest
        {
            PartId = dto.PartId,
            Quantity = dto.Quantity,
            RequestedByUserId = uid,
            OrderId = dto.OrderId,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            Status = StatusPending,
            CreatedAt = DateTime.UtcNow
        };
        context.PartSupplyRequests.Add(entity);
        await context.SaveChangesAsync();

        _logger.LogInformation("Part supply request {Id}: part {PartId} x{Qty} by {User}",
            entity.Id, dto.PartId, dto.Quantity, uid);

        return Ok(new
        {
            entity.Id,
            entity.Status,
            entity.CreatedAt
        });
    }

    /// <summary>Выдать со склада: списать остаток, привязать к заявке на ремонт при необходимости.</summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "WarehouseKeeper,Manager,OfficeManager,Director,Administrator")]
    public async Task<ActionResult> Complete(int id, [FromBody] WarehouseRespondDto? dto)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrEmpty(uid))
            return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var req = await context.PartSupplyRequests
                .Include(r => r.Part)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
                return NotFound();

            if (req.Status != StatusPending)
                return BadRequest(new { message = "Заявка уже обработана" });

            if (req.Part == null)
                return BadRequest(new { message = "Запчасть не найдена" });

            if (req.Part.Quantity < req.Quantity)
                return BadRequest(new { message = $"На складе недостаточно: есть {req.Part.Quantity}, запрошено {req.Quantity}" });

            req.Part.Quantity -= req.Quantity;

            if (req.OrderId is int orderId)
            {
                var existing = await context.OrderParts
                    .FirstOrDefaultAsync(op => op.OrderId == orderId && op.PartId == req.PartId);
                if (existing != null)
                    existing.Quantity += req.Quantity;
                else
                    context.OrderParts.Add(new OrderPart
                    {
                        OrderId = orderId,
                        PartId = req.PartId,
                        Quantity = req.Quantity
                    });
            }

            req.Status = StatusCompleted;
            req.ProcessedAt = DateTime.UtcNow;
            req.ProcessedByUserId = uid;
            req.WarehouseComment = string.IsNullOrWhiteSpace(dto?.Comment) ? null : dto!.Comment!.Trim();

            await context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Complete part supply request {Id} failed", id);
            return StatusCode(500, new { message = "Ошибка при выдаче" });
        }
    }

    /// <summary>Отклонить заявку (без списания со склада).</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "WarehouseKeeper,Manager,OfficeManager,Director,Administrator")]
    public async Task<ActionResult> Reject(int id, [FromBody] WarehouseRespondDto? dto)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrEmpty(uid))
            return Unauthorized();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var req = await context.PartSupplyRequests.FindAsync(id);
        if (req == null)
            return NotFound();

        if (req.Status != StatusPending)
            return BadRequest(new { message = "Заявка уже обработана" });

        req.Status = StatusRejected;
        req.ProcessedAt = DateTime.UtcNow;
        req.ProcessedByUserId = uid;
        req.WarehouseComment = string.IsNullOrWhiteSpace(dto?.Comment) ? null : dto!.Comment!.Trim();
        await context.SaveChangesAsync();
        return Ok();
    }
}
