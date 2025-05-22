using Microsoft.AspNetCore.Mvc;
using SafeVault.Services;
using SafeVault.Helpers;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace SafeVault.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(UserService userService, IConfiguration config)
        {
            _userService = userService;
            _configuration = config;
        }

        // Endpoint for user registration
        [HttpPost("register")]
        public IActionResult Register([FromForm] string username, [FromForm] string email, 
        [FromForm] string password, [FromForm] string role)
        {
            // Sanitize and validate user input
            var (sanitizedUsername, sanitizedEmail, valid) = InputSanitizer.ValidateAndSanitize(username, email);

            if (!valid)
            {
                return BadRequest(new { error = "Invalid input detected." });
            }

            // Attempt to register the user
            if (_userService.Register(sanitizedUsername, sanitizedEmail, password, role))
            {
                return Ok(new { message = "User registered successfully." });
            }

            return BadRequest(new { error = "Username or email already exists." });
        }

        // Endpoint for user login
        [HttpPost("login")]
        public IActionResult Login([FromForm] string username, [FromForm] string password)
        {
            var user = _userService.Authenticate(username, password);
            if (user == null)
                return Unauthorized(new { error = "Invalid username or password." });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Determine the redirection URL based on the user's role
            string redirectUrl = Url.Action("GetUserProfile", "User", null, Request.Scheme);

            var ok = Ok(new
            {
                Token = tokenHandler.WriteToken(token),
                RedirectUrl = redirectUrl
            });

            Console.WriteLine("Authcontroller: " + ok);
            return ok;
        }
    }
}