# Algebrakit .NET SDK

Official .NET SDK for the [Algebrakit](https://www.algebrakit.com) Webservice API. Create exercise sessions, retrieve scores, lock/unlock sessions, validate exercises, and fetch session info.

Targets **.NET 8.0** and **.NET Standard 2.0** (so it also runs on .NET Framework 4.6.1+, .NET Core 2.0+, and later).

## Installation

```bash
dotnet add package Algebrakit.Sdk
```

## Quick start

```csharp
using System;
using System.Net.Http;
using AlgebrakitSDK.Services;
using AlgebrakitSDK.Models.Requests;

using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.algebrakit.com") };
var sessions = new SessionService(httpClient, "your-api-key");

// Validate an exercise
var validation = await sessions.ValidateExerciseAsync(
    new ExerciseValidateRequest { ExerciseId = "your-exercise-id" });
Console.WriteLine($"Valid: {validation.Valid}, Marks: {validation.Marks}");

// Create a session
var created = await sessions.CreateSessionAsync(new CreateSessionRequest
{
    Exercises = { new() { ExerciseId = "your-exercise-id" } }
});
```

## API surface

`SessionService` exposes:

| Method | Purpose |
|---|---|
| `CreateSessionAsync` | Create new exercise sessions |
| `GetSessionScoreAsync` | Retrieve scoring results for a session |
| `LockOrUnlockSessionAsync` | Lock or unlock sessions |
| `GetSessionInfoAsync` | Fetch detailed session information |
| `RetrieveSessionsAsync` | Retrieve existing sessions |
| `ValidateExerciseAsync` | Validate an exercise definition |
| `GetExerciseInfoAsync` | Read published info for an exercise |

## Dependency injection

`SessionService` takes an `HttpClient` and an API key, so it works with `IHttpClientFactory`.
Because the constructor needs the key (`new SessionService(httpClient, apiKey)`), supply it via
`AddTypedClient` — registering only the `HttpClient` is not enough, as the container cannot
resolve the `string apiKey` parameter on its own:

```csharp
builder.Services.AddHttpClient<SessionService>(client =>
{
    client.BaseAddress = new Uri("https://api.algebrakit.com");
})
.AddTypedClient((httpClient, sp) =>
    new SessionService(httpClient, sp.GetRequiredService<IConfiguration>()["ALGEBRAKIT_API_KEY"]!));
```

Then inject `SessionService` wherever you need it. The example reads the key from the
`ALGEBRAKIT_API_KEY` environment variable (or any other configuration source).

## License

Licensed under the [MIT License](https://github.com/algebrakit/sdk-dotnet/blob/main/LICENSE).
