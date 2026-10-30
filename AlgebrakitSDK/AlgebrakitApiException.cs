using System.Net;

namespace AlgebrakitSDK;

/// <summary>
/// Thrown when a request to the Algebrakit API fails: an error response, a network error or a timeout.
/// </summary>
public class AlgebrakitApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlgebrakitApiException"/> class.
    /// </summary>
    /// <param name="message">A description of the error, including the API's own message if there is one.</param>
    /// <param name="statusCode">The HTTP status code of the API's response, or null if there was no response.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public AlgebrakitApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// The HTTP status code of the API's response, or null if there was no response (network error or timeout).
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
}
