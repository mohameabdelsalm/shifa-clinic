using System.Net;

namespace Core.Common;

/// <summary>
/// Thrown when a business rule rejects a request. GlobalExceptionHandler turns it into a
/// <see cref="Response"/> with <see cref="Status"/> as the HTTP status code.
/// <see cref="ErrorMessage"/> is shown to the user, so it must be a localized message.
/// </summary>
public class BusinessException : Exception
{
    public string? ErrorMessage { get; set; }
    public HttpStatusCode Status { get; set; } = HttpStatusCode.InternalServerError;

    public BusinessException()
    {
    }

    public BusinessException(string errorMessage) : base(errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public BusinessException(string errorMessage, HttpStatusCode status) : base(errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = status;
    }
}
