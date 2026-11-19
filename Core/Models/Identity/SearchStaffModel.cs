using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Identity;

namespace Core.Models.Identity;

/// <summary>Search: name or email. SortBy: fullName, email, status, date (default).</summary>
public class SearchStaffModel : PaginationParameters
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MembershipStatusEnum? Status { get; set; }

    public long? RoleId { get; set; }
}
