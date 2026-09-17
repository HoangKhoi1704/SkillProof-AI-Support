using Microsoft.EntityFrameworkCore;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface ICatalogService
{
    Task<RoleSkillsResponse?> GetRoleSkillsAsync(string roleId, CancellationToken cancellationToken = default);
    Task<(bool Success, DynamicQuestionSelectionResponse? Response, string? ErrorCode, string? ErrorMessage)> SelectQuestionsAsync(DynamicQuestionSelectionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleSummaryDto>> GetV3RolesAsync(CancellationToken cancellationToken = default);
    Task<RoleCanonicalFrameworkResponse?> GetRoleCanonicalFrameworkAsync(string roleId, CancellationToken cancellationToken = default);
}

public class CatalogService : ICatalogService
{
    private readonly CatalogDbContext _db;

    public CatalogService(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<RoleSkillsResponse?> GetRoleSkillsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return null;
        }

        var normalizedRoleId = roleId.Trim().ToLowerInvariant();

        var role = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id.ToLower() == normalizedRoleId, cancellationToken);

        if (role == null)
        {
            return null;
        }

        var roleSkills = await _db.RoleSkills.AsNoTracking()
            .Where(rs => rs.RoleId.ToLower() == normalizedRoleId)
            .Include(rs => rs.Skill)
                .ThenInclude(s => s.SkillSubskills)
                    .ThenInclude(ss => ss.Subskill)
            .OrderBy(rs => rs.DisplayOrder)
            .ToListAsync(cancellationToken);

        var coreSkills = new List<PublicSkillDto>();
        var recommendedSkills = new List<PublicSkillDto>();
        var optionalSkills = new List<PublicSkillDto>();
        var languageSkills = new List<PublicSkillDto>();

        foreach (var rs in roleSkills)
        {
            var subskills = rs.Skill.SkillSubskills
                .Select(ss => new PublicSubskillDto(
                    ss.Subskill.Id,
                    ss.Subskill.Name,
                    ss.Subskill.Description
                ))
                .ToList();

            var publicSkill = new PublicSkillDto(
                rs.Skill.Id,
                rs.Skill.Name,
                rs.Category,
                rs.Skill.SkillType,
                rs.Skill.Description,
                subskills
            );

            switch (rs.Category.ToLowerInvariant())
            {
                case "core":
                    coreSkills.Add(publicSkill);
                    break;
                case "recommended":
                    recommendedSkills.Add(publicSkill);
                    break;
                case "optional":
                    optionalSkills.Add(publicSkill);
                    break;
                case "language":
                    languageSkills.Add(publicSkill);
                    break;
            }
        }

        return new RoleSkillsResponse(
            role.Id,
            role.Title,
            role.Description,
            coreSkills,
            recommendedSkills,
            optionalSkills,
            languageSkills
        );
    }

    public async Task<(bool Success, DynamicQuestionSelectionResponse? Response, string? ErrorCode, string? ErrorMessage)> SelectQuestionsAsync(
        DynamicQuestionSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return (false, null, "VALIDATION_ERROR", "Request body cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.RoleId))
        {
            return (false, null, "VALIDATION_ERROR", "roleId is required.");
        }

        var normalizedRoleId = request.RoleId.Trim().ToLowerInvariant();

        var role = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id.ToLower() == normalizedRoleId, cancellationToken);

        if (role == null)
        {
            return (false, null, "NOT_FOUND", $"Role '{request.RoleId}' was not found.");
        }

        if (request.SelectedSkillIds == null || request.SelectedSkillIds.Count == 0)
        {
            return (false, null, "VALIDATION_ERROR", "At least one skill must be selected.");
        }

        var distinctRequested = request.SelectedSkillIds
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (distinctRequested.Count == 0)
        {
            return (false, null, "VALIDATION_ERROR", "At least one valid skill ID must be provided.");
        }

        var roleSkills = await _db.RoleSkills.AsNoTracking()
            .Where(rs => rs.RoleId.ToLower() == normalizedRoleId)
            .Include(rs => rs.Skill)
            .ToListAsync(cancellationToken);

        var roleSkillMap = roleSkills.ToDictionary(rs => rs.SkillId.ToLowerInvariant(), rs => rs, StringComparer.OrdinalIgnoreCase);
        var allCatalogSkills = await _db.Skills.AsNoTracking()
            .ToDictionaryAsync(s => s.Id.ToLowerInvariant(), s => s, cancellationToken);

        // Validate each selected competency
        foreach (var skillId in distinctRequested)
        {
            if (!allCatalogSkills.TryGetValue(skillId, out var catalogSkill))
            {
                return (false, null, "VALIDATION_ERROR", $"Skill '{skillId}' does not exist in catalog.");
            }

            if (!roleSkillMap.TryGetValue(skillId, out var mappedRoleSkill))
            {
                return (false, null, "VALIDATION_ERROR", $"Skill '{skillId}' does not belong to role '{request.RoleId}'.");
            }

            if (mappedRoleSkill.Category.Equals("language", StringComparison.OrdinalIgnoreCase) ||
                mappedRoleSkill.Skill.SkillType.Equals("language", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "VALIDATION_ERROR", $"'{skillId}' is a programming language and cannot be selected as an engineering competency.");
            }
        }

        // Validate primary language if provided
        if (!string.IsNullOrWhiteSpace(request.PrimaryLanguageId))
        {
            var langId = request.PrimaryLanguageId.Trim().ToLowerInvariant();

            if (!allCatalogSkills.TryGetValue(langId, out var langCatalogSkill))
            {
                return (false, null, "VALIDATION_ERROR", $"Primary language '{request.PrimaryLanguageId}' does not exist in catalog.");
            }

            if (!roleSkillMap.TryGetValue(langId, out var mappedRoleLang))
            {
                return (false, null, "VALIDATION_ERROR", $"Primary language '{request.PrimaryLanguageId}' does not belong to role '{request.RoleId}'.");
            }

            if (!mappedRoleLang.Category.Equals("language", StringComparison.OrdinalIgnoreCase) &&
                !mappedRoleLang.Skill.SkillType.Equals("language", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "VALIDATION_ERROR", $"'{request.PrimaryLanguageId}' is not a valid programming language.");
            }
        }

        // Query questions in SQLite for this role
        var allQuestions = await _db.Questions.AsNoTracking()
            .Where(q => q.RoleId.ToLower() == normalizedRoleId)
            .ToListAsync(cancellationToken);

        var questionsBySkill = allQuestions
            .GroupBy(q => q.SkillId.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        var assessedSkillIds = new List<string>();
        var unsupportedSkillIds = new List<string>();
        var selectedQuestions = new List<SelectedQuestionDto>();

        // 1. Evaluate selected competencies
        foreach (var skillId in distinctRequested)
        {
            if (questionsBySkill.TryGetValue(skillId, out var qList) && qList.Count > 0)
            {
                assessedSkillIds.Add(skillId);

                // Deterministic selection policy: select applied question
                var appliedQ = qList.FirstOrDefault(q => q.Difficulty.Equals("applied", StringComparison.OrdinalIgnoreCase))
                               ?? qList.First();

                var skillMeta = roleSkillMap[skillId].Skill;

                selectedQuestions.Add(new SelectedQuestionDto(
                    appliedQ.Id,
                    appliedQ.SkillId,
                    skillMeta.Name,
                    appliedQ.Difficulty,
                    appliedQ.QuestionType,
                    appliedQ.QuestionText,
                    appliedQ.QuestionText
                ));
            }
            else
            {
                unsupportedSkillIds.Add(skillId);
            }
        }

        // 2. Evaluate primary language independently if provided (Data Foundation v2)
        string? assessedPrimaryLanguageId = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryLanguageId))
        {
            var langId = request.PrimaryLanguageId.Trim().ToLowerInvariant();
            if (questionsBySkill.TryGetValue(langId, out var langQList) && langQList.Count > 0)
            {
                var appliedLangQ = langQList.FirstOrDefault(q => q.Difficulty.Equals("applied", StringComparison.OrdinalIgnoreCase))
                                   ?? langQList.First();

                var langMeta = roleSkillMap[langId].Skill;

                selectedQuestions.Add(new SelectedQuestionDto(
                    appliedLangQ.Id,
                    appliedLangQ.SkillId,
                    langMeta.Name,
                    appliedLangQ.Difficulty,
                    appliedLangQ.QuestionType,
                    appliedLangQ.QuestionText,
                    appliedLangQ.QuestionText
                ));

                assessedPrimaryLanguageId = langId;
            }
            else
            {
                unsupportedSkillIds.Add(langId);
            }
        }

        if (selectedQuestions.Count == 0)
        {
            return (false, null, "NO_ASSESSABLE_SKILLS", "None of the selected competencies or languages currently have assessment question coverage in this prototype. Please select at least one Core/Recommended competency or programming language.");
        }

        // Order questions deterministically: competencies by DisplayOrder first, then primary language
        selectedQuestions = selectedQuestions
            .OrderBy(q =>
            {
                if (roleSkillMap.TryGetValue(q.CompetencyId.ToLowerInvariant(), out var rs))
                {
                    return rs.Category.Equals("language", StringComparison.OrdinalIgnoreCase)
                        ? 1000 + rs.DisplayOrder
                        : rs.DisplayOrder;
                }
                return 999;
            })
            .ToList();

        var response = new DynamicQuestionSelectionResponse(
            role.Id,
            selectedQuestions,
            new AssessmentCoverageDto(distinctRequested, assessedSkillIds, unsupportedSkillIds, assessedPrimaryLanguageId)
        );

        return (true, response, null, null);
    }

    public async Task<IReadOnlyList<RoleSummaryDto>> GetV3RolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles.AsNoTracking()
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleSummaryDto(
            r.Id,
            r.Title,
            r.Description,
            r.IsPrimaryDemoRole,
            r.RoadmapSourceUrl,
            r.DisplayOrder
        )).ToList();
    }

    public async Task<RoleCanonicalFrameworkResponse?> GetRoleCanonicalFrameworkAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return null;
        }

        var normalizedRoleId = roleId.Trim().ToLowerInvariant();

        var role = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id.ToLower() == normalizedRoleId, cancellationToken);

        if (role == null)
        {
            return null;
        }

        var roleNodes = await _db.RoleRoadmapNodes.AsNoTracking()
            .Where(rrn => rrn.RoleId.ToLower() == normalizedRoleId)
            .Include(rrn => rrn.CanonicalSkill)
            .OrderBy(rrn => rrn.DisplayOrder)
            .ToListAsync(cancellationToken);

        var skillIds = roleNodes.Select(rn => rn.CanonicalSkillId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var relationships = await _db.RoadmapRelationships.AsNoTracking()
            .Where(rel => skillIds.Contains(rel.SourceSkillId) && skillIds.Contains(rel.TargetSkillId))
            .OrderBy(rel => rel.Id)
            .ToListAsync(cancellationToken);

        var nodeDtos = roleNodes.Select(rn => new CanonicalSkillNodeDto(
            rn.CanonicalSkillId,
            rn.CanonicalSkill.DisplayName,
            rn.CanonicalSkill.Classification,
            rn.Category,
            rn.Importance,
            rn.CanonicalSkill.SourceKind,
            rn.CanonicalSkill.RoadmapSource,
            rn.CanonicalSkill.RoadmapNodeId,
            rn.CanonicalSkill.RoadmapLabel,
            rn.CanonicalSkill.Description,
            rn.AssessmentEligible,
            rn.MandatoryFundamental,
            rn.IsToolkitOnly,
            rn.IsOptional,
            rn.HasQuestionCoverage,
            rn.DisplayOrder
        )).ToList();

        var relationshipDtos = relationships.Select(rel => new RoadmapRelationshipDto(
            rel.Id,
            rel.SourceSkillId,
            rel.TargetSkillId,
            rel.RelationshipType,
            rel.Rationale
        )).ToList();

        return new RoleCanonicalFrameworkResponse(
            role.Id,
            role.Title,
            role.Description,
            role.IsPrimaryDemoRole,
            role.RoadmapSourceUrl,
            nodeDtos,
            relationshipDtos
        );
    }
}
