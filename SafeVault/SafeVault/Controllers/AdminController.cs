using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    [Authorize(Roles = "admin")]
    [HttpGet("dashboard")]
    public IActionResult GetAdminDashboard()
    {
        return Ok("Welcome to the admin dashboard!");
    }
}