using System.Security.Claims;

using AuthLab.Auth;
using AuthLab.Leave;

using Microsoft.AspNetCore.Authorization;

namespace AuthLab.Tests;

public class PolicyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static ClaimsPrincipal User(string name, string role, string hireDate = "2020-01-01") => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, name),
        new Claim(ClaimTypes.Role, role),
        new Claim(LabClaims.HireDate, hireDate),
    ], "test"));

    [Theory]
    [InlineData("2024-11-10", true)]   // exactly two years
    [InlineData("2024-11-11", false)]  // one day short
    [InlineData("not-a-date", false)]
    public async Task Tenure_requirement_uses_the_hire_date_claim(string hireDate, bool expected)
    {
        var requirement = new MinimumTenureRequirement(2);
        var context = new AuthorizationHandlerContext([requirement], User("x", "Employee", hireDate), null);

        await new MinimumTenureHandler(new FixedTime(Now)).HandleAsync(context);

        Assert.Equal(expected, context.HasSucceeded);
    }

    [Theory]
    [InlineData("carol", "Employee", LeaveStatus.Pending, true)]    // owner, pending
    [InlineData("carol", "Employee", LeaveStatus.Approved, false)]  // owner, already approved
    [InlineData("bob", "Manager", LeaveStatus.Pending, false)]      // someone else's request
    [InlineData("alice", "Admin", LeaveStatus.Approved, true)]      // admins can edit anything
    public async Task Leave_request_edit_rules(string user, string role, LeaveStatus status, bool expected)
    {
        var request = new LeaveRequest(1, "carol", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), "Trip") { Status = status };
        var requirement = new EditLeaveRequestRequirement();
        var context = new AuthorizationHandlerContext([requirement], User(user, role), request);

        await new EditLeaveRequestHandler().HandleAsync(context);

        Assert.Equal(expected, context.HasSucceeded);
    }

    [Theory]
    [InlineData("/admin", "/admin")]
    [InlineData("//evil.example.com", "/")]
    [InlineData("https://evil.example.com", "/")]
    [InlineData("/\\evil.example.com", "/")]
    [InlineData(null, "/")]
    public void Return_urls_are_restricted_to_this_site(string? input, string expected)
    {
        Assert.Equal(expected, LocalUrl.OrHome(input));
    }

    [Fact]
    public void Passwords_are_hashed_and_verified()
    {
        var store = new InMemoryUserStore(new FixedTime(Now));

        Assert.NotNull(store.Validate("ALICE", InMemoryUserStore.DemoPassword));
        Assert.Null(store.Validate("alice", "wrong"));
        Assert.DoesNotContain(store.All, u => u.PasswordHash.Contains(InMemoryUserStore.DemoPassword, StringComparison.Ordinal));
    }
}
