using System.Text;
using UserManagementApi.Models;
using UserManagementApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UserStore>();

var validToken = builder.Configuration["Authentication:Token"] ?? "demo-token";

var app = builder.Build();

// Error handling middleware
app.Use(async (context, next) =>
{
    // This is catch any exceptions that wasn't handled before
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Unhandled exception occurred while processing {Method} {Path}",
            context.Request.Method, context.Request.Path
        );

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error." }); // Write back to client with JSON data
    }
});

app.Use(async (context, next) =>
{
    // Check if there's a authorization header and there's a bearer
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
        return;
    }

    // Check if the token matches the valid token
    var token = authHeader["Bearer ".Length..].Trim();
    if (!string.Equals(token, validToken, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
        return;
    }

    context.Items["AuthenticatedUser"] = "valid-user";
    await next.Invoke(context);
});

app.Use(async (context, next) =>
{
    var requestBody = string.Empty;
    if (context.Request.Body.CanSeek)
    {
        context.Request.Body.Position = 0;
    }

    if (context.Request.ContentLength > 0)
    {
        context.Request.EnableBuffering(); // Allow the request body to be read more than once
        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        requestBody = await reader.ReadToEndAsync(); // Read the entire body
        context.Request.Body.Position = 0; // Rewind the request body so downstream code can read it
    }

    var originalResponseBody = context.Response.Body;
    await using var responseBody = new MemoryStream();
    context.Response.Body = responseBody; // Temporarily capture the response in memory so it can be read/logged

    var startTime = DateTimeOffset.UtcNow; // Start measuring how long downstream request processing takes
    try
    {
        await next(context); // Continue through the remaining middleware/endpoint and wait for it to finish

        // Rewind the captured response to the beginning and start reading again
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(
            responseBody,
            Encoding.UTF8,
            leaveOpen: true
        ).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody; // Restore the original reponse stream

        // Calculate how long downstream request processing took
        // Log the request, response, status, and duration
        var durationMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
        app.Logger.LogInformation(
            "Audit Request: {Method} {Path} Query={Query} StatusCode={StatusCode} DurationMs={DurationMs} RequestBody={RequestBody} ResponseBody={ResponseBody}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.Value,
            context.Response.StatusCode,
            durationMs,
            requestBody,
            responseText);
    }
    catch (Exception ex)
    {
        context.Response.Body = originalResponseBody; // Restore the original reponse stream
        app.Logger.LogError( // Log the exception that happened
            ex,
            "Audit Error: {Method} {Path} Query={Query} RequestBody={RequestBody}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.Value,
            requestBody);
        throw;
    }
});

app.MapGet("/users", (UserStore store) =>
    Results.Ok(store.GetAll()));

app.MapGet("/users/{id:int}", (int id, UserStore store) =>
{
    var user = store.GetById(id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPost("/users", (CreateUserRequest request, UserStore store) =>
{
    try
    {
        var user = store.Create(request);
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/users/{id:int}", (int id, UpdateUserRequest request, UserStore store) =>
{
    try
    {
        var updatedUser = store.Update(id, request);
        return updatedUser is null ? Results.NotFound() : Results.Ok(updatedUser);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/users/{id:int}", (int id, UserStore store) =>
{
    var deleted = store.Delete(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();
