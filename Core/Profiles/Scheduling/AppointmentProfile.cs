using AutoMapper;
using Core.DTO.Scheduling;
using Core.Entities.Scheduling;

namespace Core.Profiles.Scheduling;

public class AppointmentProfile : Profile
{
    public AppointmentProfile()
    {
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString()));

        CreateMap<Appointment, PortalAppointmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<AvailableSlot, AvailableSlotDto>()
            .ForMember(dest => dest.StartAt, opt => opt.MapFrom(src => src.StartUtc))
            .ForMember(dest => dest.EndAt, opt => opt.MapFrom(src => src.EndUtc));
    }
}
