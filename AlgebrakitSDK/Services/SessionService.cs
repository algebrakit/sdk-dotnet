using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlgebrakitSDK.Models.Requests;
using AlgebrakitSDK.Models.Responses;

namespace AlgebrakitSDK.Services;

/// <summary>
/// Client for the Algebrakit web service API. Create one instance and reuse it.
/// </summary>
/// <example>
/// <code>
/// var sessionService = new SessionService(Environment.GetEnvironmentVariable("ALGEBRAKIT_API_KEY")!);
/// </code>
/// </example>
public class SessionService
{
    /// <summary>The Algebrakit API in the default region (Ireland).</summary>
    public const string DefaultBaseUrl = "https://api.algebrakit.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly Uri _baseAddress;

    /// <summary>
    /// Creates a service with its own <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="apiKey">Your Algebrakit API key. Keep it on your server; never send it to the browser.</param>
    /// <param name="baseUrl">Base URL of the Algebrakit API, for example to use another region. Defaults to <see cref="DefaultBaseUrl"/>.</param>
    /// <param name="timeout">Request timeout. Defaults to 30 seconds.</param>
    public SessionService(string apiKey, string baseUrl = DefaultBaseUrl, TimeSpan? timeout = null)
        : this(new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = timeout ?? TimeSpan.FromSeconds(30) }, apiKey)
    {
    }

    /// <summary>
    /// Creates a service that uses your own <see cref="HttpClient"/>, for example from <c>IHttpClientFactory</c>.
    /// The client isn't modified: the API key is sent with each request. If the client has no
    /// <see cref="HttpClient.BaseAddress"/>, <see cref="DefaultBaseUrl"/> is used.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    /// <param name="apiKey">Your Algebrakit API key.</param>
    public SessionService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = string.IsNullOrEmpty(apiKey) ? throw new ArgumentException("The API key is required.", nameof(apiKey)) : apiKey;
        _baseAddress = httpClient.BaseAddress ?? new Uri(DefaultBaseUrl);
    }

    /// <summary>
    /// Creates new exercise sessions.
    /// </summary>
    /// <param name="request">The exercises to create sessions for.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>For each exercise, whether it succeeded and the created sessions.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateSessionRequest, CreateSessionResponse>("/session/create", request, cancellationToken);

    /// <summary>
    /// Retrieves scores for a specific session.
    /// </summary>
    /// <param name="request">The session ID, and whether to lock the session.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response containing session scores.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<SessionScoreResponse> GetSessionScoreAsync(SessionScoreRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SessionScoreRequest, SessionScoreResponse>("/session/score", request, cancellationToken);

    /// <summary>
    /// Locks or unlocks exercise sessions.
    /// </summary>
    /// <param name="request">The action (<c>LOCK</c>, <c>UNLOCK</c> or <c>FINALIZE</c>) and the session IDs.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response for the lock or unlock action.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<SessionLockResponse> LockOrUnlockSessionAsync(SessionLockRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SessionLockRequest, SessionLockResponse>("/sessions/lock", request, cancellationToken);

    /// <summary>
    /// Retrieves detailed information about a session.
    /// </summary>
    /// <param name="request">The request containing the session ID.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response containing session information.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<SessionInfoResponse> GetSessionInfoAsync(SessionInfoRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SessionInfoRequest, SessionInfoResponse>("/session/info", request, cancellationToken);

    /// <summary>
    /// Retrieves existing sessions by their IDs.
    /// </summary>
    /// <param name="request">The request containing the session IDs.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response containing the session data.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<SessionRetrieveResponse> RetrieveSessionsAsync(SessionRetrieveRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SessionRetrieveRequest, SessionRetrieveResponse>("/session/retrieve", request, cancellationToken);

    /// <summary>
    /// Validates an exercise.
    /// </summary>
    /// <param name="request">The exercise ID or specification to validate.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response containing validation results.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<ExerciseValidateResponse> ValidateExerciseAsync(ExerciseValidateRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ExerciseValidateRequest, ExerciseValidateResponse>("/exercise/validate", request, cancellationToken);

    /// <summary>
    /// Retrieves published information about an exercise (structure, versions, history).
    /// </summary>
    /// <param name="request">The request containing the exercise ID.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response containing exercise information.</returns>
    /// <exception cref="AlgebrakitApiException">The request failed.</exception>
    public Task<ExerciseInfoResponse> GetExerciseInfoAsync(ExerciseInfoRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ExerciseInfoRequest, ExerciseInfoResponse>("/exercise/published-info", request, cancellationToken);

    /// <summary>
    /// Sends a request to the API and returns the deserialized response.
    /// </summary>
    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress, path))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("X-API-KEY", _apiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            throw new AlgebrakitApiException($"Algebrakit API request failed: {e.Message}", null, e);
        }
        catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its timeout as a cancellation; the caller didn't cancel
            throw new AlgebrakitApiException("Algebrakit API request timed out.", null, e);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new AlgebrakitApiException(
                    $"Algebrakit API error {(int)response.StatusCode}: {ErrorMessage(body, response.ReasonPhrase)}",
                    response.StatusCode);
            }

            try
            {
                return JsonSerializer.Deserialize<TResponse>(body)
                    ?? throw new AlgebrakitApiException($"The Algebrakit API returned an empty response for {path}.", response.StatusCode);
            }
            catch (JsonException e)
            {
                throw new AlgebrakitApiException($"Could not read the Algebrakit API response for {path}: {e.Message}", response.StatusCode, e);
            }
        }
    }

    /// <summary>
    /// Extracts the error message from an API error response body,
    /// for example <c>{"success":false,"error":"Could not validate credentials: ..."}</c>.
    /// </summary>
    private static string ErrorMessage(string body, string? reasonPhrase)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var name in new[] { "error", "msg", "message" })
                {
                    if (document.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        return value.GetString()!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON: use the body as it is
        }
        return string.IsNullOrWhiteSpace(body) ? reasonPhrase ?? "no response body" : body;
    }
}
