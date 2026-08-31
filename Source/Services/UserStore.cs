using System.Collections.Concurrent;
using UserManagementApi.Models;

namespace UserManagementApi.Services;

public sealed class UserStore
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 1;

    public IReadOnlyCollection<User> GetAll()
    {
        return _users.Values
            .OrderBy(user => user.Id)
            .ToList();
    }

    public User? GetById(int id)
    {
        return _users.TryGetValue(id, out var user) ? user : null;
    }

    public User Create(CreateUserRequest request)
    {
        ValidateRequest(request.FirstName, request.LastName, request.Email);

        // Get the current time for when this user was created
        // Update also gets the same time since it needs a starting DateTime value
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Interlocked.Increment(ref _nextId) - 1,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _users[user.Id] = user;
        return user;
    }

    public User? Update(int id, UpdateUserRequest request)
    {
        if (!_users.TryGetValue(id, out var existing))
        {
            return null;
        }

        // Validate the firtname, lastname, and email before updating
        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? existing.FirstName : request.FirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(request.LastName) ? existing.LastName : request.LastName.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? existing.Email : request.Email.Trim();

        ValidateRequest(firstName, lastName, email);

        // Create a new user instead of using the same user object
        var updated = new User
        {
            Id = existing.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _users[id] = updated;
        return updated;
    }

    public bool Delete(int id)
    {
        return _users.TryRemove(id, out _);
    }

    private static void ValidateRequest(string firstName, string lastName, string email)
    {
        // Exceptions are handled by middleware
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("A valid email is required.", nameof(email));
        }
    }
}
