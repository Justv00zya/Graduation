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
public class ClientsController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ClientsController> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // GET: api/clients
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var clients = await context.Clients
            .Select(c => new ClientDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Address = c.Address,
                Phone = c.Phone
            })
            .ToListAsync();

        return Ok(clients);
    }

    // GET: api/clients/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDto>> GetClient(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var client = await context.Clients
            .Where(c => c.Id == id)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Address = c.Address,
                Phone = c.Phone
            })
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return NotFound();
        }

        return Ok(client);
    }

    // POST: api/clients
    [HttpPost]
    [Authorize(Roles = "Manager,OfficeManager,Administrator")]
    public async Task<ActionResult<ClientDto>> CreateClient(CreateClientDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var client = new Client
        {
            FullName = dto.FullName,
            Address = dto.Address,
            Phone = dto.Phone
        };

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    // PUT: api/clients/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,OfficeManager,Administrator")]
    public async Task<IActionResult> UpdateClient(int id, UpdateClientDto dto)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var client = await context.Clients.FindAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        client.FullName = dto.FullName;
        client.Address = dto.Address;
        client.Phone = dto.Phone;

        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/clients/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,OfficeManager,Administrator")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var client = await context.Clients.FindAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        context.Clients.Remove(client);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
