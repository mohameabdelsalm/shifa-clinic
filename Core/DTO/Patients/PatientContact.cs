namespace Core.DTO.Patients;

/// <summary>
/// Where to reach a patient for service messages: the patient's own phone, else a phone linked for portal access
/// (e.g. a guardian), and the e-mail. No medical or identity details.
/// </summary>
public sealed record PatientContact(string FullName, string? PhoneNumber, string? Email);
