using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SafeVault.Repositories;
using SafeVault.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSingleton<UserRepository>(sp =>
    new UserRepository(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<DatabaseInitializer>(); // Register the DatabaseInitializer service

// Configure JWT authentication
var secretKey = builder.Configuration.GetValue<string>("Jwt:Secret");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Audience"),
            ValidAudience = builder.Configuration.GetValue<string>("Jwt:Issuer"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Map the "role" claim to the expected "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    var roleClaim = claimsIdentity.FindFirst("role");
                    if (roleClaim != null)
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                    }
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
var app = builder.Build();

// Initialize the database
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
databaseInitializer.Initialize(builder.Configuration.GetConnectionString("DefaultConnection"));

// Configure middleware
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseRouting();
app.UseAuthentication(); // Add authentication middleware
app.UseAuthorization();  // Add authorization middleware
app.MapControllers();

app.Run();