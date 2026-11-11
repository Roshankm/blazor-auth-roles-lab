using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

namespace AuthLab.Auth;

public static class LabClaims
{
    public const string Department = "department";
    public const string HireDate = "hire_date";
}

public sealed record DemoUser(string UserName, string DisplayName, string Role, string Department, DateOnly HireDate)
{
    public string PasswordHash { get; init; } = string.Empty;

    public ClaimsPrincipal ToPrincipal(string authenticationType)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, UserName),
            new(ClaimTypes.Name, UserName),
            new("display_name", DisplayName),
            new(ClaimTypes.Role, Role),
            new(LabClaims.Department, Department),
            new(LabClaims.HireDate, HireDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }
}

public interface IUserStore
{
    IReadOnlyList<DemoUser> All { get; }

    /// <summary>Returns the user only when the password matches.</summary>
    DemoUser? Validate(string userName, string password);
}

/// <summary>
/// Three demo users with different roles, departments and hire dates, so every policy in the
/// lab passes for someone and fails for someone else. Passwords are hashed with ASP.NET Core
/// Identity's <see cref="PasswordHasher{TUser}"/>, never stored in plain text.
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    public const string DemoPassword = "Passw0rd!";

    private readonly PasswordHasher<DemoUser> _hasher = new();

    public InMemoryUserStore(TimeProvider time)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        All =
        [
            Create(new DemoUser("alice", "Alice (Admin)", "Admin", "IT", today.AddYears(-8))),
            Create(new DemoUser("bob", "Bob (Manager)", "Manager", "Finance", today.AddYears(-3))),
            Create(new DemoUser("carol", "Carol (Employee)", "Employee", "Finance", today.AddMonths(-4))),
        ];
    }

    public IReadOnlyList<DemoUser> All { get; }

    public DemoUser? Validate(string userName, string password)
    {
        var user = All.FirstOrDefault(u => string.Equals(u.UserName, userName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null || string.IsNullOrEmpty(password))
        {
            return null;
        }

        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed
            ? null
            : user;
    }

    private DemoUser Create(DemoUser user) => user with { PasswordHash = _hasher.HashPassword(user, DemoPassword) };
}
