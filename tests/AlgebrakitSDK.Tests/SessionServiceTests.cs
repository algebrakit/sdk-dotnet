using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AlgebrakitSDK;
using AlgebrakitSDK.Models.Requests;
using AlgebrakitSDK.Models.Shared;
using AlgebrakitSDK.Services;

namespace AlgebrakitSDK.Tests;

public class SessionServiceTests
{
    /// <summary>Answers every request with the next response, and records the requests.</summary>
    private sealed class FakeHandler(params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] responses) : HttpMessageHandler
    {
        private int _next;
        public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, body));
            var response = await responses[Math.Min(_next++, responses.Length - 1)](request);
            await Task.Delay(0, cancellationToken);
            return response;
        }
    }

    private static Func<HttpRequestMessage, Task<HttpResponseMessage>> Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static string Key(HttpRequestMessage request) => string.Join(",", request.Headers.GetValues("X-API-KEY"));

    [Fact]
    public async Task UsesTheProductionApiWhenTheClientHasNoBaseAddress()
    {
        var handler = new FakeHandler(Json("{}"));
        await new SessionService(new HttpClient(handler), "my-key").GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" });

        Assert.Equal("https://api.algebrakit.com/session/info", handler.Requests[0].Request.RequestUri!.ToString());
        Assert.Equal("my-key", Key(handler.Requests[0].Request));
    }

    [Fact]
    public void SimpleConstructorUsesTheGivenBaseUrl()
    {
        var service = new SessionService("key", baseUrl: "https://prod.api.sg-1.algebrakit.com");
        var baseAddress = typeof(SessionService).GetField("_baseAddress", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service);

        Assert.Equal(new Uri("https://prod.api.sg-1.algebrakit.com"), baseAddress);
    }

    [Fact]
    public async Task ASharedHttpClientSendsTheKeyOnceAndIsNotModified()
    {
        // 1.0.0 added the key to the client's default headers, so a second service sent it twice (401)
        var handler = new FakeHandler(Json("{}"));
        var shared = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var first = new SessionService(shared, "key");
        _ = new SessionService(shared, "key");

        await first.GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" });

        Assert.Equal("key", Key(handler.Requests[0].Request));
        Assert.Empty(shared.DefaultRequestHeaders);
    }

    [Fact]
    public async Task CallsTheRightEndpointForEachMethod()
    {
        var handler = new FakeHandler(
            Json("""[{"success":true,"sessions":[]}]"""), Json("{}"), Json("{}"), Json("{}"), Json("[]"), Json("""{"success":true,"valid":true}"""), Json("{}"));
        var service = new SessionService(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, "key");

        await service.CreateSessionAsync(new CreateSessionRequest { Exercises = [new ExerciseById { ExerciseId = "e1" }] });
        await service.GetSessionScoreAsync(new SessionScoreRequest { SessionId = "s1" });
        await service.LockOrUnlockSessionAsync(new SessionLockRequest { Action = "LOCK", SessionIds = ["s1"] });
        await service.GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" });
        await service.RetrieveSessionsAsync(new SessionRetrieveRequest { SessionIds = ["s1"] });
        await service.ValidateExerciseAsync(new ExerciseValidateRequest { ExerciseId = "e1" });
        await service.GetExerciseInfoAsync(new ExerciseInfoRequest { Id = "e1" });

        Assert.Equal(
            [
                "POST /session/create",
                "POST /session/score",
                "POST /sessions/lock",
                "POST /session/info",
                "POST /session/retrieve",
                "POST /exercise/validate",
                "POST /exercise/published-info",
            ],
            handler.Requests.Select(r => $"{r.Request.Method} {r.Request.RequestUri!.AbsolutePath}"));
    }

    [Fact]
    public async Task CreateSessionSendsApiVersion2AndNoEmptyScoringModel()
    {
        var handler = new FakeHandler(Json("""[{"success":true,"sessions":[]}]"""));
        await new SessionService(new HttpClient(handler), "key")
            .CreateSessionAsync(new CreateSessionRequest { Exercises = [new ExerciseById { ExerciseId = "e1" }] });

        var body = JsonDocument.Parse(handler.Requests[0].Body).RootElement;
        Assert.Equal(2, body.GetProperty("apiVersion").GetInt32());
        Assert.Equal("e1", body.GetProperty("exercises")[0].GetProperty("exerciseId").GetString());
        Assert.Equal("latest", body.GetProperty("exercises")[0].GetProperty("version").GetString());
        Assert.False(body.TryGetProperty("scoringModel", out _));
    }

    [Fact]
    public async Task ErrorsIncludeTheApiMessageAndStatusCode()
    {
        var handler = new FakeHandler(Json(
            """{"success":false,"error":"Could not validate credentials: the provided api-key is not valid"}""",
            HttpStatusCode.Unauthorized));
        var service = new SessionService(new HttpClient(handler), "wrong");

        var e = await Assert.ThrowsAsync<AlgebrakitApiException>(() => service.GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" }));
        Assert.Equal("Algebrakit API error 401: Could not validate credentials: the provided api-key is not valid", e.Message);
        Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
    }

    [Fact]
    public async Task NetworkErrorsHaveNoStatusCode()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Connection refused"));
        var service = new SessionService(new HttpClient(handler), "key");

        var e = await Assert.ThrowsAsync<AlgebrakitApiException>(() => service.GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" }));
        Assert.Equal("Algebrakit API request failed: Connection refused", e.Message);
        Assert.Null(e.StatusCode);
    }

    [Fact]
    public async Task TimeoutsAreReportedAsApiExceptions()
    {
        var handler = new FakeHandler(async _ => { await Task.Delay(1000); return new HttpResponseMessage(HttpStatusCode.OK); });
        var service = new SessionService(new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) }, "key");

        var e = await Assert.ThrowsAsync<AlgebrakitApiException>(() => service.GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" }));
        Assert.Equal("Algebrakit API request timed out.", e.Message);
    }

    [Fact]
    public async Task CancellingTheRequestThrowsOperationCanceled()
    {
        var handler = new FakeHandler(async _ => { await Task.Delay(1000); return new HttpResponseMessage(HttpStatusCode.OK); });
        var service = new SessionService(new HttpClient(handler), "key");
        using var cancellation = new CancellationTokenSource(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetSessionInfoAsync(new SessionInfoRequest { SessionId = "s1" }, cancellation.Token));
    }

    [Fact]
    public void TheApiKeyIsRequired()
    {
        Assert.Throws<ArgumentException>(() => new SessionService(""));
    }
}
