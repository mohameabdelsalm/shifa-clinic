using AutoMapper;
using Core.DTO.Clinical;
using Core.Entities.Clinical;
using Core.Models.Clinical;
using Core.Validations.Clinical;

namespace Core.Profiles.Clinical;

public class VisitProfile : Profile
{
    public VisitProfile()
    {
        CreateMap<Visit, VisitListDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // The content comes from the child tables; the repo fills it.
        CreateMap<Visit, VisitDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .ForMember(dest => dest.Vitals, opt => opt.Ignore())
            .ForMember(dest => dest.Diagnoses, opt => opt.Ignore())
            .ForMember(dest => dest.Prescription, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.Addenda, opt => opt.Ignore());

        CreateMap<SaveClinicalNoteModel, ClinicalNote>()
            .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint == null ? null : src.ChiefComplaint.Trim()))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes == null ? null : src.Notes.Trim()));
        CreateMap<ClinicalNote, ClinicalNoteDto>();

        // RecordedByUserId and RecordedAt are set by the repo.
        CreateMap<AddVitalSignsModel, VitalSign>();
        CreateMap<VitalSign, VitalSignDto>();

        // VisitId is set by the repo.
        CreateMap<DiagnosisItemModel, Diagnosis>()
            .ForMember(dest => dest.Icd10Code, opt => opt.MapFrom(src => ClinicalRules.NormalizeIcd10(src.Icd10Code)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description == null ? null : src.Description.Trim()));
        CreateMap<Diagnosis, DiagnosisDto>();

        // PrescriptionId is set by the repo.
        CreateMap<PrescriptionItemModel, PrescriptionItem>()
            .ForMember(dest => dest.DrugName, opt => opt.MapFrom(src => src.DrugName.Trim()))
            .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage.Trim()))
            .ForMember(dest => dest.Frequency, opt => opt.MapFrom(src => src.Frequency.Trim()))
            .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => src.Instructions == null ? null : src.Instructions.Trim()));
        CreateMap<Prescription, PrescriptionDto>()
            .ForMember(dest => dest.Items, opt => opt.Ignore());
        CreateMap<PrescriptionItem, PrescriptionItemDto>();

        CreateMap<VisitAttachment, VisitAttachmentDto>()
            .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.CreationDate));

        // AuthorUserId is set by the repo from the signed-in user.
        CreateMap<AddAddendumModel, ClinicalAddendum>()
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason.Trim()))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content.Trim()));
        CreateMap<ClinicalAddendum, ClinicalAddendumDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreationDate));

        // The prescription's notes and items are filled by the portal repo.
        CreateMap<Visit, PortalPrescriptionDto>()
            .ForMember(dest => dest.VisitId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.IssuedAt, opt => opt.MapFrom(src => src.CompletedAt ?? src.OpenedAt))
            .ForMember(dest => dest.Notes, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore());
    }
}
