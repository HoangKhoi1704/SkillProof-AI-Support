namespace SkillProof.Api.Models;

public record ErrorDetail(string Code, string Message);

public record ErrorResponse(ErrorDetail Error);
