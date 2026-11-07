using AutoMapper;
using Core.Entities.Tenancy;
using Core.Models.Tenancy;

namespace Core.Profiles.Tenancy;

public class TenantProfile : Profile
{
    public TenantProfile()
    {
        // Slugs are stored lower case; an empty time zone keeps the default.
        CreateMap<AddTenantModel, Tenant>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug.Trim().ToLowerInvariant()))
            .ForMember(dest => dest.DefaultTimeZoneId, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.DefaultTimeZoneId)));
    }
}
