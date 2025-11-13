using MarsVista.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MarsVistaDbContext _context;

    public HealthController(MarsVistaDbContext context)
    {
        _context = context;
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            // Try to connect to the database
            var canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
                return Ok(new
                {
                    status = "healthy",
                    database = "connected",
                    message = "Successfully connected to PostgreSQL"
                });
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "disconnected",
                message = "Cannot connect to PostgreSQL"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "error",
                message = ex.Message
            });
        }
    }
}
