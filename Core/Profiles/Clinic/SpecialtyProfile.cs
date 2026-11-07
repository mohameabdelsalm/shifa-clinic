using AutoMapper;
using Core.DTO.Clinic;
using Core.Entities.Clinic;
using Core.Models.Clinic;

namespace Core.Profiles.Clinic;

public class SpecialtyProfile : Profile
{
    public SpecialtyProfile()
    {
        CreateMap<AddSpecialtyModel, Specialty>()
            .ForMember(dest => dest.NameEn, opt => opt.MapFrom(src => src.NameEn.Trim()))
            .ForMember(dest => dest.NameAr, opt => opt.MapFrom(src => src.NameAr.Trim()));

        CreateMap<UpdateSpecialtyModel, Specialty>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NameEn, opt => opt.MapFrom(src => src.NameEn.Trim()))
            .ForMember(dest => dest.NameAr, opt => opt.MapFrom(src => src.NameAr.Trim()));

        CreateMap<Specialty, SpecialtyDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
