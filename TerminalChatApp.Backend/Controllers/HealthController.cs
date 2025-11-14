/*
// Uncomment this controller when using PostgreSQL to monitor database health

using Microsoft.AspNetCore.Mvc;
using TerminalChatApp.Backend.Services;

namespace TerminalChatApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DatabaseService databaseService, ILogger<HealthController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get application health status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HealthStatus>> GetHealth()
    {
        try
        {
            var dbHealth = await _databaseService.GetDatabaseHealthAsync();
            
            var healthStatus = new HealthStatus
            {
                Status = dbHealth.CanConnect ? "Healthy" : "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = dbHealth
            };

            if (dbHealth.CanConnect)
            {
                return Ok(healthStatus);
            }
            else
            {
                return StatusCode(503, healthStatus); // Service Unavailable
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            return StatusCode(503, new HealthStatus
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Get detailed database information (admin only in production)
    /// </summary>
    [HttpGet("database")]
    public async Task<ActionResult<DatabaseHealthInfo>> GetDatabaseHealth()
    {
        try
        {
            var dbHealth = await _databaseService.GetDatabaseHealthAsync();
            return Ok(dbHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Clean test data (development only)
    /// </summary>
    [HttpPost("clean-test-data")]
    public async Task<ActionResult> CleanTestData()
    {
        // Only allow in development environment
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return Forbid("This endpoint is only available in development environment");
        }

        try
        {
            await _databaseService.CleanTestDataAsync();
            return Ok(new { Message = "Test data cleaned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean test data");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Reseed test data (development only)
    /// </summary>
    [HttpPost("reseed")]
    public async Task<ActionResult> ReseedData()
    {
        // Only allow in development environment
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return Forbid("This endpoint is only available in development environment");
        }

        try
        {
            await _databaseService.CleanTestDataAsync();
            await _databaseService.SeedDataAsync();
            return Ok(new { Message = "Test data reseeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reseed test data");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DatabaseHealthInfo? Database { get; set; }
    public string? ErrorMessage { get; set; }
}
*/

// This file is commented out by default. Uncomment when using PostgreSQL and need health monitoring.