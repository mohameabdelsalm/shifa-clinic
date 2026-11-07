using Core.Common;

namespace Core.Models.Patients;

public class SearchPatientsModel : PaginationParameters
{
    /// <summary>Name, MRN, phone number, or an exact national ID / passport number.</summary>
    public string? Text { get; set; }
}
