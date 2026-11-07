using AutoMapper;
using Core.DTO.Clinic;
using Core.Entities.Clinic;
using Core.Models.Clinic;

namespace Core.Profiles.Clinic;

public class BranchProfile : Profile
{
    public BranchProfile()
    {
        // Codes are stored upper case; an empty time zone keeps the default (or the current value on update).
        CreateMap<AddBranchModel, Branch>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone == null ? null : src.Phone.Trim()))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address == null ? null : src.Address.Trim()))
            .ForMember(dest => dest.TimeZoneId, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.TimeZoneId)));

        CreateMap<UpdateBranchModel, Branch>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone == null ? null : src.Phone.Trim()))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address == null ? null : src.Address.Trim()))
            .ForMember(dest => dest.TimeZoneId, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.TimeZoneId)));

        CreateMap<Branch, BranchDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
