using AutoMapper;
using Core.Entities.Tenancy;
using Core.Models.Tenancy;

namespace Core.Profiles.Tenancy;

public class PlanProfile : Profile
{
    public PlanProfile()
    {
        // Plan codes are stored upper case; entitlements are saved as their own rows.
        CreateMap<AddPlanModel, Plan>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        CreateMap<UpdatePlanModel, Plan>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        // PlanId is set by the repo.
        CreateMap<PlanEntitlementModel, PlanEntitlement>();
    }
}
