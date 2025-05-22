using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [Authorize(Roles = "user,admin")]
    [HttpGet("profile")]
    public IActionResult GetUserProfile()
    {
        Console.WriteLine("GetUserProfile endpoint hit");
        return Ok("Welcome to your profile!");
    }
}