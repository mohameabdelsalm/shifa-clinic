namespace Core.Models.Clinic;

public class UpdateDoctorModel
{
    public long Id { get; set; }
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public List<long> SpecialtyIds { get; set; } = [];

    /// <summary>The doctor's branches inside the caller's own branch scope; branches outside it are left as they are.</summary>
    public List<long> BranchIds { get; set; } = [];
}
