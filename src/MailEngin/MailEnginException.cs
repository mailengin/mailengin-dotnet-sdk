using System.Net;

namespace MailEngin;

public sealed class MailEnginException : Exception
{
    public MailEnginException(
        string message,
        HttpStatusCode? status = null,
        string? errorCode = null,
        string? requestId = null,
        double? retryAfter = null,
        string? body = null,
        Exception? innerException = null) : base(message, innerException)
    {
        Status = status;
        ErrorCode = errorCode;
        RequestId = requestId;
        RetryAfter = retryAfter;
        Body = body;
    }

    public HttpStatusCode? Status { get; }
    public string? ErrorCode { get; }
    public string? RequestId { get; }
    public double? RetryAfter { get; }
    public string? Body { get; }

    public bool IsRetryable =>
        ErrorCode is "network_error" or "request_timeout" ||
        Status is HttpStatusCode.RequestTimeout or (HttpStatusCode)429 ||
        (Status.HasValue && (int)Status.Value >= 500);
}
