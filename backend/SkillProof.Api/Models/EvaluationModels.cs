namespace SkillProof.Api.Models;

public record DiagnosticAnswerSubmission(
    int QuestionId,
    string Answer
);

public record EvaluationRequest(
    string RoleId,
    List<DiagnosticAnswerSubmission> Answers
);

public record SkillEvaluationItem(
    string Name,
    string Level,
    string Reason,
    List<string> Evidence
);

public record EvaluationResponse(
    string RoleId,
    List<SkillEvaluationItem> Skills,
    List<string> TopGaps
);
