using Core.Common;

namespace Core.Models.Clinic;

public class SearchDoctorsModel : PaginationParameters
{
    public string? Text { get; set; }
    public long? SpecialtyId { get; set; }
    public long? BranchId { get; set; }
}
