using AutoMapper;
using Core.DTO.Notifications;
using Core.Entities.Notifications;

namespace Core.Profiles.Notifications;

/// <summary>Reads only: notifications are created by the event handlers, never from request models.</summary>
public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.Kind, opt => opt.MapFrom(src => src.Kind.ToString()))
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
