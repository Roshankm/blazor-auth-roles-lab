using System.Globalization;

using AuthLab.Leave;

using Microsoft.AspNetCore.Authorization;

namespace AuthLab.Auth;

public static class Policies
{
    /// <summary>Role-based: managers and admins.</summary>
    public const string CanApproveLeave = nameof(CanApproveLeave);

    /// <summary>Claim-based: anyone in the Finance department, whatever their role.</summary>
    public const string FinanceOnly = nameof(FinanceOnly);

    /// <summary>Custom requirement: at least two years since the hire date claim.</summary>
    public const string SeniorStaff = nameof(SeniorStaff);

    /// <summary>Resource-based: depends on which leave request is being edited.</summary>
    public const string EditLeaveRequest = nameof(EditLeaveRequest);

    public static readonly string[] All = [CanApproveLeave, FinanceOnly, SeniorStaff];

    public static AuthorizationBuilder AddLabPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(CanApproveLeave, p => p.RequireRole("Manager", "Admin"))
        .AddPolicy(FinanceOnly, p => p.RequireClaim(LabClaims.Department, "Finance"))
        .AddPolicy(SeniorStaff, p => p.AddRequirements(new MinimumTenureRequirement(2)))
        .AddPolicy(EditLeaveRequest, p => p.AddRequirements(new EditLeaveRequestRequirement()));
}

public sealed record MinimumTenureRequirement(int Years) : IAuthorizationRequirement;

public sealed class MinimumTenureHandler(TimeProvider time) : AuthorizationHandler<MinimumTenureRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumTenureRequirement requirement)
    {
        var claim = context.User.FindFirst(LabClaims.HireDate)?.Value;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        if (DateOnly.TryParseExact(claim, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hired)
            && hired.AddYears(requirement.Years) <= today)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class EditLeaveRequestRequirement : IAuthorizationRequirement;

/// <summary>Owners may edit their own pending requests; admins may edit anything.</summary>
public sealed class EditLeaveRequestHandler : AuthorizationHandler<EditLeaveRequestRequirement, LeaveRequest>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, EditLeaveRequestRequirement requirement, LeaveRequest resource)
    {
        var isOwner = string.Equals(resource.EmployeeUserName, context.User.Identity?.Name, StringComparison.OrdinalIgnoreCase);

        if ((isOwner && resource.Status == LeaveStatus.Pending) || context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
