using System.Collections.Concurrent;

namespace AuthLab.Leave;

public enum LeaveStatus
{
    Pending,
    Approved,
}

public sealed record LeaveRequest(int Id, string EmployeeUserName, DateOnly From, DateOnly To, string Reason)
{
    public LeaveStatus Status { get; init; } = LeaveStatus.Pending;
}

/// <summary>In-memory leave requests used to demonstrate resource-based authorization.</summary>
public sealed class LeaveService
{
    private readonly ConcurrentDictionary<int, LeaveRequest> _requests = new();

    public LeaveService()
    {
        var start = new DateOnly(2026, 12, 1);
        Add(new LeaveRequest(1, "carol", start, start.AddDays(4), "Family visit"));
        Add(new LeaveRequest(2, "bob", start.AddDays(14), start.AddDays(18), "Year-end break"));
        Add(new LeaveRequest(3, "carol", new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 7), "Summer holiday") { Status = LeaveStatus.Approved });
    }

    public IReadOnlyList<LeaveRequest> All() => _requests.Values.OrderBy(r => r.Id).ToList();

    public IReadOnlyList<LeaveRequest> Pending() => All().Where(r => r.Status == LeaveStatus.Pending).ToList();

    public LeaveRequest? Find(int id) => _requests.TryGetValue(id, out var r) ? r : null;

    public bool Approve(int id)
    {
        if (!_requests.TryGetValue(id, out var request) || request.Status != LeaveStatus.Pending)
        {
            return false;
        }

        _requests[id] = request with { Status = LeaveStatus.Approved };
        return true;
    }

    private void Add(LeaveRequest request) => _requests[request.Id] = request;
}
