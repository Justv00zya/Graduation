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
public class EmployeesController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<EmployeesController> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // GET: api/employees
    [HttpGet]
    [Authorize(Roles = "Accountant,Director,Administrator")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var employees = await context.Employees
            .Include(e => e.Position)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                TabNumber = e.TabNumber,
                FirstName = e.FirstName,
                LastName = e.LastName,
                MiddleName = e.MiddleName,
                PositionId = e.PositionId,
                PositionName = e.Position!.Name,
                DateOfBirth = e.DateOfBirth,
                INN = e.INN,
                Address = e.Address,
                HireDate = e.HireDate
            })
            .ToListAsync();

        return Ok(employees);
    }

    // GET: api/employees/5
    [HttpGet("{id}")]
    [Authorize(Roles = "Accountant,Director,Administrator")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var employee = await context.Employees
            .Include(e => e.Position)
            .Where(e => e.Id == id)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                TabNumber = e.TabNumber,
                FirstName = e.FirstName,
                LastName = e.LastName,
                MiddleName = e.MiddleName,
                PositionId = e.PositionId,
                PositionName = e.Position!.Name,
                DateOfBirth = e.DateOfBirth,
                INN = e.INN,
                Address = e.Address,
                HireDate = e.HireDate
            })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }
}
