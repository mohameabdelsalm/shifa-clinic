using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Patients;

namespace Core.Models.Patients;

/// <summary>Search: name, MRN, phone or an exact identifier. SortBy: fullName, mrn, dateOfBirth, date (default).</summary>
public class SearchPatientsModel : PaginationParameters
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GenderEnum? Gender { get; set; }
}
