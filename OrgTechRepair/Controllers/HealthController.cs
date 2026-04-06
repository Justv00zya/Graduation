using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrgTechRepair.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    /// <summary>Проверка доступности сервера (без авторизации).</summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", message = "OrgTechRepair API" });
    }
}
