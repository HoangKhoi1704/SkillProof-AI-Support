namespace SkillProof.Api.Data.Catalog;

public class Role
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RoleSkill> RoleSkills { get; set; } = new List<RoleSkill>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Skill
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SkillType { get; set; } = string.Empty; // "competency" | "language"
    public string? Description { get; set; }

    public ICollection<RoleSkill> RoleSkills { get; set; } = new List<RoleSkill>();
    public ICollection<SkillSubskill> SkillSubskills { get; set; } = new List<SkillSubskill>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class RoleSkill
{
    public string RoleId { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;

    public string SkillId { get; set; } = string.Empty;
    public Skill Skill { get; set; } = null!;

    public string Category { get; set; } = string.Empty; // "core" | "recommended" | "optional" | "language"
    public int DisplayOrder { get; set; }
}

public class Subskill
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<SkillSubskill> SkillSubskills { get; set; } = new List<SkillSubskill>();
    public ICollection<QuestionSubskill> QuestionSubskills { get; set; } = new List<QuestionSubskill>();
}

public class SkillSubskill
{
    public string SkillId { get; set; } = string.Empty;
    public Skill Skill { get; set; } = null!;

    public string SubskillId { get; set; } = string.Empty;
    public Subskill Subskill { get; set; } = null!;
}

public class Question
{
    public string Id { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;

    public string SkillId { get; set; } = string.Empty;
    public Skill Skill { get; set; } = null!;

    public string Difficulty { get; set; } = string.Empty; // "foundation" | "applied" | "advanced-reasoning"
    public string QuestionType { get; set; } = string.Empty; // "knowledge" | "practical_case" | "reasoning" | "interview"
    public string QuestionText { get; set; } = string.Empty;
    public string ExpectedSignalsJson { get; set; } = "[]";
    public string Provenance { get; set; } = "skillproof-curated";
    public string VerificationStatus { get; set; } = string.Empty; // "framework-supported-only" | "interview-practice-supported"

    public Rubric? Rubric { get; set; }
    public ICollection<QuestionSubskill> QuestionSubskills { get; set; } = new List<QuestionSubskill>();
    public ICollection<QuestionFrameworkSource> FrameworkSources { get; set; } = new List<QuestionFrameworkSource>();
    public ICollection<QuestionInterviewEvidence> InterviewEvidence { get; set; } = new List<QuestionInterviewEvidence>();
}

public class QuestionSubskill
{
    public string QuestionId { get; set; } = string.Empty;
    public Question Question { get; set; } = null!;

    public string SubskillId { get; set; } = string.Empty;
    public Subskill Subskill { get; set; } = null!;
}

public class Rubric
{
    public string Id { get; set; } = string.Empty;

    public string QuestionId { get; set; } = string.Empty;
    public Question Question { get; set; } = null!;

    public string InsufficientEvidence { get; set; } = string.Empty;
    public string Beginner { get; set; } = string.Empty;
    public string Intermediate { get; set; } = string.Empty;
    public string Advanced { get; set; } = string.Empty;
}

public class Source
{
    public string Id { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string AccessedAt { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public ICollection<QuestionFrameworkSource> QuestionFrameworkSources { get; set; } = new List<QuestionFrameworkSource>();
    public ICollection<QuestionInterviewEvidence> QuestionInterviewEvidence { get; set; } = new List<QuestionInterviewEvidence>();
}

public class QuestionFrameworkSource
{
    public string QuestionId { get; set; } = string.Empty;
    public Question Question { get; set; } = null!;

    public string SourceId { get; set; } = string.Empty;
    public Source Source { get; set; } = null!;
}

public class QuestionInterviewEvidence
{
    public string QuestionId { get; set; } = string.Empty;
    public Question Question { get; set; } = null!;

    public string SourceId { get; set; } = string.Empty;
    public Source Source { get; set; } = null!;

    public string EvidenceType { get; set; } = string.Empty;
    public string SupportedTopicsJson { get; set; } = "[]";
    public string EvidenceStrength { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
