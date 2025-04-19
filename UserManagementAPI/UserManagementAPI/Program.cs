/// <summary>
/// Represents the entry point of the User Management API application.
/// </summary>
/// <remarks>
/// This application provides a simple in-memory user management API with the following endpoints:
/// 
/// - GET "/" : Returns a root message.
/// - GET "/users" : Retrieves all users.
/// - GET "/users/{id:int}" : Retrieves a user by their ID.
/// - POST "/users" : Adds a new user.
/// - PUT "/users/{id:int}" : Updates an existing user by their ID.
/// - DELETE "/users/{id:int}" : Deletes a user by their ID.
/// 
/// The application uses an in-memory dictionary to store user data.
/// </remarks>
/// 
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// This section of code replaced witht the Exception Handling middleware
// // Global Exception Handling Middleware remains async because it does I/O.
// app.UseExceptionHandler(errorApp =>
// {
//     errorApp.Run(async context =>
//     {
//         context.Response.StatusCode = 500;
//         context.Response.ContentType = "application/json";
//         var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
//         var exception = exceptionFeature?.Error;

//         await context.Response.WriteAsJsonAsync(new
//         {
//             Error = "An unexpected error occurred.",
//             Details = exception?.Message
//         });
//     });
// });

// Register the custom exception handling middleware first:
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Register Token Validation Middleware
app.UseMiddleware<TokenValidationMiddleware>();

// Register RequestResponseLoggingMiddleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Use ConcurrentDictionary for thread safety.
var users = new ConcurrentDictionary<int, User>();
int nextUserId = 3; // Starting with 3 as we seed two users.
users.TryAdd(1, new User { Id = 1, Username = "user1", UserAge = 25, Email = "user1@example.com" });
users.TryAdd(2, new User { Id = 2, Username = "user2", UserAge = 30, Email = "user2@example.com" });


// GET "/" endpoint: Synchronous since it just returns a string.
app.MapGet("/", () => "I am root.");

// GET "/users" endpoint: Synchronous
app.MapGet("/users", () => Results.Ok(users.Values));

// GET "/users/{id:int}" endpoint: Synchronous lookup.
app.MapGet("/users/{id:int}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
    {
        return Results.Ok(user);
    }
    return Results.NotFound(new { Message = $"User with id {id} not found." });
});

// POST "/users" endpoint: Remains asynchronous because it reads JSON from the request.
app.MapPost("/users", async (HttpContext http) =>
{
    var newUser = await http.Request.ReadFromJsonAsync<User>();
    if (newUser is null)
    {
        return Results.BadRequest("Invalid user payload.");
    }

    var validationContext = new ValidationContext(newUser);
    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(newUser, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults);
    }

    // Allocate a new ID using the counter.
    int newId = nextUserId++;
    newUser.Id = newId;

    if (!users.TryAdd(newId, newUser))
    {
        return Results.Problem("Failed to add new user due to an internal error.");
    }

    return Results.Created($"/users/{newId}", newUser);
});

// PUT "/users/{id:int}" endpoint: Synchronous update.
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    if (users.ContainsKey(id))
    {
        updatedUser.Id = id;
        users[id] = updatedUser;
        return Results.Ok(updatedUser);
    }
    return Results.NotFound();
});

// DELETE "/users/{id:int}" endpoint: Synchronous delete.
app.MapDelete("/users/{id:int}", (int id) =>
{
    if (users.TryRemove(id, out var removedUser))
    {
        return Results.Ok();
    }
    return Results.NotFound();
});

// GET "/users/stream" endpoint: Remains asynchronous because it streams data.
async IAsyncEnumerable<User> GetUsersAsync()
{
    foreach (var user in users.Values)
    {
        await Task.Yield();  // Optional: simulate async work if needed.
        yield return user;
    }
}
app.MapGet("/users/stream", GetUsersAsync);

app.Run();

public class User
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Username is required.")]
    public required string Username { get; set; }
    
    [Range(1, 120, ErrorMessage = "User age must be between 1 and 120.")]
    public int UserAge { get; set; }
    
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }
}

// Logging middleware
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Enable buffering to allow the request body to be read multiple times
        context.Request.EnableBuffering();
        string requestBody = string.Empty;

        if (context.Request.ContentLength.HasValue && context.Request.ContentLength > 0)
        {
            using (StreamReader reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                // Reset stream position so that downstream middleware/controllers can read it
                context.Request.Body.Position = 0;
            }
        }

        // Log Request Details
        _logger.LogInformation("HTTP Request Information:\n" +
                               "Method: {Method}\n" +
                               "Path: {Path}\n" +
                               "Body: {Body}",
                               context.Request.Method,
                               context.Request.Path,
                               requestBody);

        // Swap out the original response stream to capture the response
        var originalBodyStream = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Invoke the rest of the middleware
        await _next(context);

        // Reset the response body stream position before reading it
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        // Log Response Details
        _logger.LogInformation("HTTP Response Information:\n" +
                               "Status Code: {StatusCode}\n" +
                               "Body: {Body}",
                               context.Response.StatusCode,
                               responseBody);

        // Copy the response body back to the original response stream
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);
    }
}

// Error handling middleware
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "Unhandled exception occurred.");

            // If the response hasn't started yet, clear it and return a JSON error response.
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    Error = "An unexpected error occurred.",
                    Details = ex.Message
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
            else
            {
                // If the response has already started, rethrow the exception.
                throw;
            }
        }
    }
}

// Authentication middleware
public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    // For demo purposes we're using a hard-coded token.
    // In a real application, use proper token validation (JWT, etc.).
    private const string VALID_TOKEN = "YOUR_SECRET_TOKEN";

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Check for the Authorization header.
        if (!context.Request.Headers.TryGetValue("Authorization", out var tokenHeader))
        {
            _logger.LogWarning("Unauthorized access attempt: missing Authorization header.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new 
            { 
                Error = "Unauthorized", 
                Message = "No token provided." 
            });
            return;
        }

        // Extract token value
        string token = tokenHeader.ToString();
        
        // Optionally strip "Bearer " if present.
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring("Bearer ".Length).Trim();
        }

        // Simple token validation check.
        if (!string.Equals(token, VALID_TOKEN, StringComparison.Ordinal))
        {
            _logger.LogWarning("Unauthorized access attempt: invalid token.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new 
            { 
                Error = "Unauthorized", 
                Message = "Invalid token." 
            });
            return;
        }

        // Token is valid; pass control to the next middleware/component.
        await _next(context);
    }
}
