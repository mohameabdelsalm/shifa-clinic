using Core.Common;

namespace Core.Interfaces.Billing;

public interface IPortalInvoiceRepo
{
    Task<Response> GetMine(CancellationToken cancellationToken);
}
