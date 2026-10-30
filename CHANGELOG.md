# Changelog

All notable changes to the `Algebrakit.Sdk` NuGet package are listed here. The project follows [semantic versioning](https://semver.org).

## 1.1.0

Code compiled against 1.0.0 needs to be recompiled: the methods have a new optional parameter, and errors are reported with a new exception type.

### Fixed
- Using one `HttpClient` for more than one `SessionService` sent the API key twice, and the API answered 401. The API key is now sent with each request, and your `HttpClient` is no longer changed.
- A request without `ScoringModel` sent `"scoringModel": ""`. It's now left out.

### Added
- `new SessionService(apiKey)`, with optional `baseUrl` and `timeout` (30 seconds by default). `SessionService.DefaultBaseUrl` is the default region.
- An `HttpClient` without a `BaseAddress` now uses the default region, instead of failing.
- An optional `CancellationToken` on every method.
- `AlgebrakitApiException`, thrown when a request fails. Its message includes the API's explanation, for example `Algebrakit API error 401: Could not validate credentials: the provided api-key is not valid`, and `StatusCode` holds the HTTP status. It replaces the `HttpRequestException` from 1.0.0.
- Unit tests and CI.

### Changed
- One README for GitHub and nuget.org. It covers how to show a session in a web page, all API methods, regions and error handling, and its code samples compile (the 1.0.0 quick start didn't).

### Removed
- The web demo `PlayExercise`, replaced by the [minimal .NET example](https://github.com/algebrakit/algebrakit-examples/tree/main/minimal/dotnet) in algebrakit-examples. `DemoApp` is replaced by `demo/Tour`.

## 1.0.0

- First release on NuGet: `CreateSessionAsync`, `GetSessionScoreAsync`, `LockOrUnlockSessionAsync`, `GetSessionInfoAsync`, `RetrieveSessionsAsync`, `ValidateExerciseAsync` and `GetExerciseInfoAsync`.
- Targets .NET Standard 2.0 and .NET 8, with XML documentation and a symbol package for debugging.
