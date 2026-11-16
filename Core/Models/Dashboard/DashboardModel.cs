namespace Core.Models.Dashboard;

/// <summary>Period and filters of the dashboard (like Trio HRM's DashboardModel).</summary>
public class DashboardModel
{
    /// <summary>First day (branch-local appointment dates). Default: 30 days before <see cref="DateTo"/>.</summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>Last day, inclusive. Default: today (UTC).</summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>Limit to these branches (always within the caller's branch scope). Empty = every branch in scope.</summary>
    public List<long>? BranchIds { get; set; }

    public long? DoctorId { get; set; }
}
