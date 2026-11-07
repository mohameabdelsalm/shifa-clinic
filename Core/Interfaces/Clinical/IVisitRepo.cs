using Core.Common;
using Core.DTO.Clinical;
using Core.Models.Clinical;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces.Clinical;

public interface IVisitRepo
{
    Task<Response> Open(OpenVisitModel model, CancellationToken cancellationToken);
    Task<Response> Search(SearchVisitsModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> SaveNote(SaveClinicalNoteModel model, CancellationToken cancellationToken);
    Task<Response> AddVitals(AddVitalSignsModel model, CancellationToken cancellationToken);
    Task<Response> SetDiagnoses(SetDiagnosesModel model, CancellationToken cancellationToken);
    Task<Response> SetPrescription(SetPrescriptionModel model, CancellationToken cancellationToken);
    Task<Response> AddAttachment(long visitId, IFormFile file, CancellationToken cancellationToken);
    Task<AttachmentFile> DownloadAttachment(long attachmentId, CancellationToken cancellationToken);
    Task<Response> Complete(long id, CancellationToken cancellationToken);
    Task<Response> AddAddendum(AddAddendumModel model, CancellationToken cancellationToken);
}
