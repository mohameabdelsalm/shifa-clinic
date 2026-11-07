using Core.Common;

namespace Core.Models.Clinic;

public class SearchServicesModel : PaginationParameters
{
    public string? Text { get; set; }
    public long? SpecialtyId { get; set; }
}
