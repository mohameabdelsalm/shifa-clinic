using AutoMapper;
using Core.DTO.Clinical;
using Core.Entities.Clinical;
using Core.Models.Clinical;

namespace Core.Profiles.Clinical;

public class PatientClinicalProfile : Profile
{
    public PatientClinicalProfile()
    {
        // RecordedByUserId is set by the repo.
        CreateMap<AddAllergyModel, PatientAllergy>()
            .ForMember(dest => dest.Allergen, opt => opt.MapFrom(src => src.Allergen.Trim()))
            .ForMember(dest => dest.Reaction, opt => opt.MapFrom(src => src.Reaction == null ? null : src.Reaction.Trim()));
        CreateMap<PatientAllergy, PatientAllergyDto>()
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.Severity.ToString()))
            .ForMember(dest => dest.RecordedAt, opt => opt.MapFrom(src => src.CreationDate));

        CreateMap<AddConditionModel, PatientCondition>()
            .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Condition.Trim()))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes == null ? null : src.Notes.Trim()));
        CreateMap<PatientCondition, PatientConditionDto>();
    }
}
