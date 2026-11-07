namespace Core.Models.Clinic;

public class AddDoctorModel
{
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public List<long> SpecialtyIds { get; set; } = [];
    public List<long> BranchIds { get; set; } = [];
}
