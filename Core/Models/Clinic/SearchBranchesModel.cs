using Core.Common;

namespace Core.Models.Clinic;

public class SearchBranchesModel : PaginationParameters
{
    public string? Text { get; set; }
}
