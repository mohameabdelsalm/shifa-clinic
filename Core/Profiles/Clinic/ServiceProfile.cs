using AutoMapper;
using Core.DTO.Clinic;
using Core.Entities.Clinic;
using Core.Models.Clinic;

namespace Core.Profiles.Clinic;

public class ServiceProfile : Profile
{
    public ServiceProfile()
    {
        CreateMap<AddServiceModel, Service>()
            .ForMember(dest => dest.NameEn, opt => opt.MapFrom(src => src.NameEn.Trim()))
            .ForMember(dest => dest.NameAr, opt => opt.MapFrom(src => src.NameAr.Trim()));

        CreateMap<UpdateServiceModel, Service>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NameEn, opt => opt.MapFrom(src => src.NameEn.Trim()))
            .ForMember(dest => dest.NameAr, opt => opt.MapFrom(src => src.NameAr.Trim()));

        // Current prices come from another table; the repo fills them.
        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Prices, opt => opt.Ignore());

        CreateMap<ServicePrice, ServicePriceDto>();
    }
}
