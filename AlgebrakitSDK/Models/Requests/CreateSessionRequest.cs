using AlgebrakitSDK.Models.AkExercise;
using AlgebrakitSDK.Models.Shared;
using System.Text.Json.Serialization;

namespace AlgebrakitSDK.Models.Requests;

/// <summary>
/// Represents a request to create new exercise sessions.
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// List of exercises to create sessions for. Specify one of exerciseId, sessionId, or exerciseSpec.
    /// </summary>
    [JsonPropertyName("exercises")]
    public List<Exercise> Exercises { get; set; } = new();

    /// <summary>
    /// Optional reference to a predefined scoring model.
    /// </summary>
    [JsonPropertyName("scoringModel")]
    public string? ScoringModel { get; set; }

    /// <summary>
    /// Turns off step-by-step evaluation, hints, and feedback for the student.
    /// </summary>
    [JsonPropertyName("assessmentMode")]
    public bool AssessmentMode { get; set; }

    /// <summary>
    /// Prevents obtaining solutions before the session is locked.
    /// </summary>
    [JsonPropertyName("requireLockForSolution")]
    public bool RequireLockForSolution { get; set; }

    /// <summary>
    /// API version to use for the request. Default is 2.
    /// </summary>
    [JsonPropertyName("apiVersion")]
    public int ApiVersion { get; set; } = 2;

    [JsonPropertyName("studentFeedbackType")]
    public AK_StudentFeedbackType? StudentFeedbackType { get; set; }
}