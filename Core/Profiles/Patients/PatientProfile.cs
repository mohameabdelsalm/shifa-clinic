using AutoMapper;
using Core.Common;
using Core.DTO.Patients;
using Core.Entities.Patients;
using Core.Models.Patients;

namespace Core.Profiles.Patients;

public class PatientProfile : Profile
{
    public PatientProfile()
    {
        // Number and MRN are given by the repo; identifiers and guardians are saved as their own rows.
        CreateMap<AddPatientModel, Patient>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => PhoneNumbers.Normalize(src.PhoneNumber)))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email == null ? null : src.Email.Trim()))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address == null ? null : src.Address.Trim()));

        CreateMap<UpdatePatientModel, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => PhoneNumbers.Normalize(src.PhoneNumber)))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email == null ? null : src.Email.Trim()))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address == null ? null : src.Address.Trim()));

        CreateMap<Patient, SearchPatientsDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()));

        // Identifiers, guardians, consents and portal access come from other tables; the repo fills them.
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()))
            .ForMember(dest => dest.Identifiers, opt => opt.Ignore())
            .ForMember(dest => dest.Guardians, opt => opt.Ignore())
            .ForMember(dest => dest.Consents, opt => opt.Ignore())
            .ForMember(dest => dest.PortalAccess, opt => opt.Ignore());

        // PatientId is set by the repo.
        CreateMap<PatientGuardianModel, PatientGuardian>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship.Trim()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => PhoneNumbers.Normalize(src.PhoneNumber)));

        CreateMap<AddGuardianModel, PatientGuardian>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship.Trim()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => PhoneNumbers.Normalize(src.PhoneNumber)));

        // GrantedAt is set by the repo from the clock.
        CreateMap<GrantConsentModel, PatientConsent>();

        // The phone number is normalized and checked by the repo first.
        CreateMap<LinkPortalAccessModel, PatientAccountAccess>()
            .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore());

        CreateMap<PatientIdentifier, PatientIdentifierDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));
        CreateMap<PatientGuardian, PatientGuardianDto>();
        CreateMap<PatientConsent, PatientConsentDto>()
            .ForMember(dest => dest.ConsentType, opt => opt.MapFrom(src => src.ConsentType.ToString()));
        CreateMap<PatientAccountAccess, PatientPortalAccessDto>()
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship.ToString()));
    }
}
