using System.Net;

namespace Shifa.Core.Common;

/// <summary>
/// The response shape used by every Shifa endpoint (same as the Trio ERP APIs).
/// The HTTP status code of the response always equals <see cref="Status"/>.
/// </summary>
public class Response
{
    public string Message { get; set; }
    public HttpStatusCode Status { get; set; }
    public dynamic? Data { get; set; }
    public bool? IsSuccess { get; set; }

    public Response(string message, HttpStatusCode status, bool? isSuccess = true, object? data = null)
    {
        Message = message;
        Status = status;
        IsSuccess = isSuccess;
        Data = data;
    }
}
