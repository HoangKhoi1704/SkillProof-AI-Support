namespace SkillProof.Api.Data.Catalog;

public class Role
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrimaryDemoRole { get; set; } = false;
    public string? RoadmapSourceUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;

    public ICollection<RoleSkill> RoleSkills { get; set; } = new List<RoleSkill>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<RoleRoadmapNode> RoleRoadmapNodes { get; set; } = new List<RoleRoadmapNode>();
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
    public string? ReferenceExplanation { get; set; }
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

public class CanonicalSkill
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string SourceKind { get; set; } = string.Empty;
    public string? RoadmapSource { get; set; }
    public string? RoadmapNodeId { get; set; }
    public string? RoadmapLabel { get; set; }
    public string? Description { get; set; }

    public ICollection<RoleRoadmapNode> RoleRoadmapNodes { get; set; } = new List<RoleRoadmapNode>();
    public ICollection<RoadmapRelationship> OutgoingRelationships { get; set; } = new List<RoadmapRelationship>();
    public ICollection<RoadmapRelationship> IncomingRelationships { get; set; } = new List<RoadmapRelationship>();
    public ICollection<LegacySkillMapping> LegacySkillMappings { get; set; } = new List<LegacySkillMapping>();
}

public class RoleRoadmapNode
{
    public string RoleId { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;

    public string CanonicalSkillId { get; set; } = string.Empty;
    public CanonicalSkill CanonicalSkill { get; set; } = null!;

    public string Category { get; set; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
    public bool AssessmentEligible { get; set; }
    public bool MandatoryFundamental { get; set; }
    public bool IsToolkitOnly { get; set; }
    public bool IsOptional { get; set; }
    public bool HasQuestionCoverage { get; set; }
    public int DisplayOrder { get; set; }
}

public class RoadmapRelationship
{
    public int Id { get; set; }
    public string SourceSkillId { get; set; } = string.Empty;
    public CanonicalSkill SourceSkill { get; set; } = null!;

    public string TargetSkillId { get; set; } = string.Empty;
    public CanonicalSkill TargetSkill { get; set; } = null!;

    public string RelationshipType { get; set; } = string.Empty;
    public string? Rationale { get; set; }
}

public class LegacySkillMapping
{
    public string LegacySkillId { get; set; } = string.Empty;
    public string CanonicalSkillId { get; set; } = string.Empty;
    public CanonicalSkill CanonicalSkill { get; set; } = null!;

    public string RoleId { get; set; } = string.Empty;
    public string MappingType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class V3Question
{
    public string Id { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;
    public string CanonicalSkillId { get; set; } = string.Empty;
    public CanonicalSkill CanonicalSkill { get; set; } = null!;
    public string Difficulty { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? ReferenceExplanation { get; set; }
    public string ExpectedSignalsJson { get; set; } = "[]";
    public string RubricJson { get; set; } = "{}";
    public string FrameworkSourceIdsJson { get; set; } = "[]";
    public string? SourceLocator { get; set; }
    public string Provenance { get; set; } = "concept-derived";
    public string AdaptationStatus { get; set; } = "concept-derived";
    public string VerificationStatus { get; set; } = "specification-grounded";
    public string VerifiedAt { get; set; } = "2026-09-16";
}

public class LearningResource
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty; // "official-doc", "guide", "tutorial", "interactive-practice", "reference"
    public string CanonicalSkillIdsJson { get; set; } = "[]";
    public string RoleIdsJson { get; set; } = "[]";
    public string Level { get; set; } = string.Empty; // "foundation", "applied", "advanced"
    public bool IsOfficial { get; set; }
    public string VerificationStatus { get; set; } = "verified";
    public string VerifiedAt { get; set; } = string.Empty;
    public string? Locator { get; set; }
}

public class CuratedProject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string? SourceLocator { get; set; }
    public string Provenance { get; set; } = string.Empty;
    public string RoleIdsJson { get; set; } = "[]";
    public string CanonicalSkillIdsJson { get; set; } = "[]";
    public string RoadmapTargetsJson { get; set; } = "[]";
    public string ProjectType { get; set; } = string.Empty; // "practice" | "portfolio"
    public string Difficulty { get; set; } = string.Empty; // "foundation" | "intermediate" | "advanced"
    public string EstimatedScope { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DeliverablesJson { get; set; } = "[]";
    public string EvidenceRequirementsJson { get; set; } = "[]";
    public string VerificationStatus { get; set; } = "verified";
    public string VerifiedAt { get; set; } = string.Empty;
}

