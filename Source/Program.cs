using UserManagementApi.Models;
using UserManagementApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UserStore>();

var app = builder.Build();

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
