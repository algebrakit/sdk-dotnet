// Algebrakit SDK tour: calls each part of the API once and prints a short summary.
//
//   ALGEBRAKIT_API_KEY=<your API key> dotnet run --project demo/Tour
//
// To show an exercise to a student in the browser, see the runnable web examples:
// https://github.com/algebrakit/algebrakit-examples

using AlgebrakitSDK;
using AlgebrakitSDK.Models.AkExercise;
using AlgebrakitSDK.Models.Requests;
using AlgebrakitSDK.Models.Shared;
using AlgebrakitSDK.Services;

const string ExerciseId = "fa42e943-8213-41a6-8a91-8c22a929ffe9"; // "Solve the equation 6(p - 1) = 4p + 10"

var apiKey = Environment.GetEnvironmentVariable("ALGEBRAKIT_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("Set ALGEBRAKIT_API_KEY first, for example: ALGEBRAKIT_API_KEY=<your API key> dotnet run --project demo/Tour");
    return 1;
}
var apiUrl = Environment.GetEnvironmentVariable("ALGEBRAKIT_API_URL") ?? SessionService.DefaultBaseUrl;
var service = new SessionService(apiKey, apiUrl);

// An exercise defined in code instead of in the Algebrakit CMS (AK_Exercise format)
var exerciseSpec = new AK_Exercise
{
    StudentProfile = "uk_KS5",
    QuestionMode = AK_QuestionMode.ALL_AT_ONCE,
    Symbols = [new AK_Symbol { Name = "x", Type = AK_SymbolType.VARIABLE }],
    Elements =
    [
        new AK_Element
        {
            Blocks =
            [
                new AK_ContentBlock { Content = "Simplify the following expression." },
                new AK_InteractionBlock
                {
                    Interaction = new AK_InteractionMultistep
                    {
                        SolutionPart = new AK_MultistepPart { Task = new AK_TaskSimplify { Expression = "2x + 3x" } },
                    },
                },
            ],
        },
    ],
};

try
{
    Console.WriteLine($"Algebrakit SDK tour\nAPI: {apiUrl}");

    Console.WriteLine("\n1. Validate an exercise (ValidateExerciseAsync)");
    var validation = await service.ValidateExerciseAsync(new ExerciseValidateRequest { ExerciseId = ExerciseId });
    Console.WriteLine($"   valid: {validation.Valid}, marks: {validation.Marks}");

    Console.WriteLine("\n2. Create a session (CreateSessionAsync)");
    var result = (await service.CreateSessionAsync(new CreateSessionRequest { Exercises = [new ExerciseById { ExerciseId = ExerciseId }] }))[0];
    if (!result.Success) throw new InvalidOperationException(result.Msg ?? "Session creation failed");
    var sessionId = result.Sessions[0].SessionId;
    Console.WriteLine($"   session ID: {sessionId}");
    Console.WriteLine($"   show it in a page with: <akit-exercise session-id=\"{sessionId}\"></akit-exercise>");

    Console.WriteLine("\n3. Get session info (GetSessionInfoAsync)");
    var info = await service.GetSessionInfoAsync(new SessionInfoRequest { SessionId = sessionId });
    Console.WriteLine($"   elements: {info.Elements.Count}");

    Console.WriteLine("\n4. Get the score (GetSessionScoreAsync)");
    var scoring = (await service.GetSessionScoreAsync(new SessionScoreRequest { SessionId = sessionId })).Scoring;
    Console.WriteLine($"   marks: {scoring.MarksEarned} of {scoring.MarksTotal}, finished: {scoring.Finished}");

    Console.WriteLine("\n5. Lock and unlock the session (LockOrUnlockSessionAsync)");
    await service.LockOrUnlockSessionAsync(new SessionLockRequest { Action = "LOCK", SessionIds = [sessionId] });
    Console.WriteLine("   locked: the student can no longer change their answers");
    await service.LockOrUnlockSessionAsync(new SessionLockRequest { Action = "UNLOCK", SessionIds = [sessionId] });
    Console.WriteLine("   unlocked");

    Console.WriteLine("\n6. Create a session from an exercise defined in code (ExerciseBySpec)");
    var specValidation = await service.ValidateExerciseAsync(new ExerciseValidateRequest { ExerciseSpec = exerciseSpec });
    Console.WriteLine($"   valid: {specValidation.Valid}, marks: {specValidation.Marks}");
    var specResult = (await service.CreateSessionAsync(new CreateSessionRequest { Exercises = [new ExerciseBySpec { ExerciseSpec = exerciseSpec }] }))[0];
    Console.WriteLine($"   session ID: {specResult.Sessions[0].SessionId}");

    Console.WriteLine("\nDone.");
    return 0;
}
catch (AlgebrakitApiException e)
{
    Console.Error.WriteLine($"\nDemo failed: {e.Message}");
    return 1;
}
