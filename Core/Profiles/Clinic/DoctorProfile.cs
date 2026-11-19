using AutoMapper;
using Core.DTO.Clinic;
using Core.Entities.Clinic;
using Core.Models.Clinic;

namespace Core.Profiles.Clinic;

public class DoctorProfile : Profile
{
    public DoctorProfile()
    {
        CreateMap<AddDoctorModel, Doctor>()
            .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src => src.FullNameEn.Trim()))
            .ForMember(dest => dest.FullNameAr, opt => opt.MapFrom(src => src.FullNameAr.Trim()))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title == null ? null : src.Title.Trim()));

        CreateMap<UpdateDoctorModel, Doctor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src => src.FullNameEn.Trim()))
            .ForMember(dest => dest.FullNameAr, opt => opt.MapFrom(src => src.FullNameAr.Trim()))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title == null ? null : src.Title.Trim()));

        // Specialties, branches, schedules and time off come from other tables; the repo fills them.
        CreateMap<Doctor, DoctorDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.SpecialtyIds, opt => opt.Ignore())
            .ForMember(dest => dest.BranchIds, opt => opt.Ignore())
            .ForMember(dest => dest.Schedules, opt => opt.Ignore())
            .ForMember(dest => dest.TimeOffs, opt => opt.Ignore());

        // DoctorBranchId is set by the repo (the schedule belongs to one doctor-branch row).
        CreateMap<ScheduleItemModel, DoctorSchedule>();

        // BranchId comes from the doctor-branch row; the repo sets it.
        CreateMap<DoctorSchedule, DoctorScheduleDto>()
            .ForMember(dest => dest.BranchId, opt => opt.Ignore())
            .ForMember(dest => dest.DayOfWeek, opt => opt.MapFrom(src => src.DayOfWeek.ToString()));

        CreateMap<AddTimeOffModel, DoctorTimeOff>()
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason == null ? null : src.Reason.Trim()));

        CreateMap<DoctorTimeOff, DoctorTimeOffDto>();
    }
}
