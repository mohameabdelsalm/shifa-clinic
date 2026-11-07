namespace Core.Models.Clinic;

public class LinkDoctorAccountModel
{
    public long DoctorId { get; set; }

    /// <summary>The doctor's staff membership; null removes the link.</summary>
    public long? MembershipId { get; set; }
}
