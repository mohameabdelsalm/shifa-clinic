using Core.DTO.Reporting;
using Core.Models.Reporting;

namespace Core.Interfaces.Reporting;

public interface IReportRepo
{
    Task<ReportOutput> DailyBranches(ReportModel model, CancellationToken cancellationToken);
    Task<ReportOutput> Revenue(ReportModel model, CancellationToken cancellationToken);
    Task<ReportOutput> Doctors(ReportModel model, CancellationToken cancellationToken);
}
