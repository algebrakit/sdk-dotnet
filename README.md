# Algebrakit SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/Algebrakit.Sdk)](https://www.nuget.org/packages/Algebrakit.Sdk)
[![CI](https://github.com/algebrakit/sdk-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/algebrakit/sdk-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/algebrakit/sdk-dotnet/blob/main/LICENSE)

The official server-side SDK for the [Algebrakit](https://algebrakit.com) API. Your server uses it to create exercise sessions, get scores and manage sessions. Students then work on the exercises in your web page, through the Algebrakit widget.

For how Algebrakit works, the full API reference and the widget's options, see the [Algebrakit documentation](https://docs.algebrakit.com/).

> **Want to see it working first?** [algebrakit-examples](https://github.com/algebrakit/algebrakit-examples/tree/main/minimal/dotnet) has a small ASP.NET Core app built on this SDK that runs on localhost in a few minutes.

## Before you start

- .NET 8 or newer, or anything that supports .NET Standard 2.0 (such as .NET Framework 4.6.2+).
- An Algebrakit API key. Don't have one yet? [Contact Algebrakit](https://algebrakit.com/contact-us).

Keep the API key on your server. Never send it to the browser.

## Install

```bash
dotnet add package Algebrakit.Sdk
```

## Quick start

Create a service and a session for an exercise:

```csharp
using AlgebrakitSDK.Models.Requests;
using AlgebrakitSDK.Models.Shared;
using AlgebrakitSDK.Services;

var sessionService = new SessionService(Environment.GetEnvironmentVariable("ALGEBRAKIT_API_KEY")!);

var result = (await sessionService.CreateSessionAsync(new CreateSessionRequest
{
    Exercises = [new ExerciseById { ExerciseId = "fa42e943-8213-41a6-8a91-8c22a929ffe9" }],
}))[0];
var sessionId = result.Sessions[0].SessionId;
```

A session holds one student's work on one exercise. Store its ID with the student, so they can continue later and you can get their score.

Create one `SessionService` and reuse it, for example as a singleton in ASP.NET Core:

```csharp
builder.Services.AddSingleton(new SessionService(builder.Configuration["ALGEBRAKIT_API_KEY"]!));
```

### Show the session to a student

In your web page, the Algebrakit widget shows the session:

```html
<script>
  AlgebraKIT = { config: { proxy: { url: '/proxy/algebrakit' } } };
</script>
<script src="https://widgets.algebrakit.com"></script>

<akit-exercise session-id="@sessionId"></akit-exercise>
```

The widget sends its API calls to `/proxy/algebrakit/*` on your server. Your server adds the API key and forwards them to the Algebrakit API. The [minimal .NET example](https://github.com/algebrakit/algebrakit-examples/tree/main/minimal/dotnet) implements all of this; for production, see [Proxy setup](https://docs.algebrakit.com/pages/420-proxy-setup/).

## API

`SessionService` has one method per API call. Each takes a request object and an optional `CancellationToken`, and returns the response.

| Method | Endpoint | What it does |
|---|---|---|
| `CreateSessionAsync` | `POST /session/create` | Create sessions for one or more exercises: `ExerciseById`, `ExerciseBySpec` (an exercise defined in code) or `ExerciseBySession` (a copy of an existing session's exercise) |
| `GetSessionScoreAsync` | `POST /session/score` | Get a session's score, and optionally lock it |
| `GetSessionInfoAsync` | `POST /session/info` | Get a session's details: its elements, the student's work and the scoring |
| `LockOrUnlockSessionAsync` | `POST /sessions/lock` | `LOCK`, `UNLOCK` or `FINALIZE` sessions |
| `RetrieveSessionsAsync` | `POST /session/retrieve` | Get existing sessions by ID |
| `ValidateExerciseAsync` | `POST /exercise/validate` | Check an exercise ID or specification before you use it |
| `GetExerciseInfoAsync` | `POST /exercise/published-info` | Get the published versions and structure of an exercise |

### Get the score

```csharp
var scoring = (await sessionService.GetSessionScoreAsync(new SessionScoreRequest { SessionId = sessionId, LockSession = true })).Scoring;
Console.WriteLine($"{scoring.MarksEarned} of {scoring.MarksTotal} marks");
```

### Lock a session

```csharp
await sessionService.LockOrUnlockSessionAsync(new SessionLockRequest { Action = "LOCK", SessionIds = [sessionId] });
```

## Configuration

```csharp
var sessionService = new SessionService(
    apiKey,
    baseUrl: "https://prod.api.sg-1.algebrakit.com", // optional, see the regions below
    timeout: TimeSpan.FromSeconds(10)); // optional; the default is 30 seconds
```

| Region | Base URL |
|---|---|
| EU (default) | `https://api.algebrakit.com` |
| Singapore | `https://prod.api.sg-1.algebrakit.com` |

You can also pass your own `HttpClient`, for example one from `IHttpClientFactory`: `new SessionService(httpClient, apiKey)`. The SDK sends the API key with each request and doesn't change the client. If the client has no `BaseAddress`, the default region is used.

## Error handling

The methods throw an `AlgebrakitApiException` when a request fails. Its message includes the API's explanation, and `StatusCode` holds the HTTP status (or `null` for a network error or timeout):

```csharp
using AlgebrakitSDK;

try
{
    await sessionService.GetSessionInfoAsync(new SessionInfoRequest { SessionId = sessionId });
}
catch (AlgebrakitApiException e)
{
    Console.Error.WriteLine(e.Message);
    // Algebrakit API error 401: Could not validate credentials: the provided api-key is not valid
}
```

## More

- [algebrakit-examples](https://github.com/algebrakit/algebrakit-examples): runnable examples in .NET, TypeScript and PHP
- [Documentation](https://docs.algebrakit.com/): [SDKs](https://docs.algebrakit.com/pages/320-SDK/), [API reference](https://docs.algebrakit.com/pages/310-API/), [web components](https://docs.algebrakit.com/pages/410-webcomponents/) and [learning events](https://docs.algebrakit.com/pages/450-learning-events/)
- [OpenAPI specification](https://api.algebrakit.com/openapi/yaml)
- SDKs for other languages: [TypeScript](https://github.com/algebrakit/sdk-typescript) and [PHP](https://github.com/algebrakit/sdk-php)
- A tour of every API call: [`demo/Tour`](https://github.com/algebrakit/sdk-dotnet/tree/main/demo/Tour). Run it with `ALGEBRAKIT_API_KEY=<your API key> dotnet run --project demo/Tour`.

## Development

```bash
dotnet test     # unit tests, no API key needed
```

To release: update the version in `AlgebrakitSDK/AlgebrakitSDK.csproj` and [CHANGELOG.md](https://github.com/algebrakit/sdk-dotnet/blob/main/CHANGELOG.md), then push a tag such as `v1.1.0` (or create the release on GitHub). GitHub Actions publishes the package to NuGet with trusted publishing.

## License

[MIT](https://github.com/algebrakit/sdk-dotnet/blob/main/LICENSE)
