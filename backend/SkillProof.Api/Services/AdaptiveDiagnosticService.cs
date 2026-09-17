using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IAdaptiveDiagnosticService
{
    Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> StartSessionAsync(
        AdaptiveSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> SubmitAnswerAsync(
        string sessionId,
        AdaptiveAnswerRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> AdvanceNextQuestionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, CareerReadinessProfile? Profile, string? ErrorCode, string? ErrorMessage)> GetProfileAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

public class AdaptiveDiagnosticService : IAdaptiveDiagnosticService
{
    private readonly CatalogDbContext _db;
    private readonly IAdaptiveSessionStore _sessionStore;
    private readonly IAdaptiveDiagnosticEngine _adaptiveEngine;
    private readonly IAdaptiveProfileBuilder _profileBuilder;
    private readonly IConfiguration _config;
    private readonly ILogger<AdaptiveDiagnosticService> _logger;

    public AdaptiveDiagnosticService(
        CatalogDbContext db,
        IAdaptiveSessionStore sessionStore,
        IAdaptiveDiagnosticEngine adaptiveEngine,
        IAdaptiveProfileBuilder profileBuilder,
        IConfiguration config,
        ILogger<AdaptiveDiagnosticService> logger)
    {
        _db = db;
        _sessionStore = sessionStore;
        _adaptiveEngine = adaptiveEngine;
        _profileBuilder = profileBuilder;
        _config = config;
        _logger = logger;
    }

    private record AdaptiveQuestionModel(
        string Id,
        string RoleId,
        string SkillId,
        string Difficulty,
        string QuestionType,
        string QuestionText,
        string? ReferenceExplanation,
        string ExpectedSignalsJson
    );

    private async Task<AdaptiveQuestionModel?> FindQuestionAsync(string questionId, CancellationToken cancellationToken)
    {
        var q = await _db.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);
        if (q != null)
        {
            return new AdaptiveQuestionModel(q.Id, q.RoleId, q.SkillId, q.Difficulty, q.QuestionType, q.QuestionText, q.ReferenceExplanation, q.ExpectedSignalsJson);
        }
        var v3 = await _db.V3Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);
        if (v3 != null)
        {
            return new AdaptiveQuestionModel(v3.Id, v3.RoleId, v3.CanonicalSkillId, v3.Difficulty, v3.QuestionType, v3.QuestionText, v3.ReferenceExplanation, v3.ExpectedSignalsJson);
        }
        return null;
    }

    private async Task<Dictionary<string, List<AdaptiveQuestionModel>>> GetQuestionsBySkillForRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        var normalizedRoleId = roleId.Trim().ToLowerInvariant();
        var legacyMappings = await _db.LegacySkillMappings.AsNoTracking().ToListAsync(cancellationToken);
        var legacyToCanonical = legacyMappings.ToDictionary(m => m.LegacySkillId.ToLowerInvariant(), m => m.CanonicalSkillId.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, List<AdaptiveQuestionModel>>(StringComparer.OrdinalIgnoreCase);

        if (normalizedRoleId == "backend-developer")
        {
            var dbQs = await _db.Questions.AsNoTracking()
                .Where(q => q.RoleId.ToLower() == normalizedRoleId)
                .ToListAsync(cancellationToken);
            foreach (var q in dbQs)
            {
                var model = new AdaptiveQuestionModel(q.Id, q.RoleId, q.SkillId, q.Difficulty, q.QuestionType, q.QuestionText, q.ReferenceExplanation, q.ExpectedSignalsJson);
                var sId = q.SkillId.ToLowerInvariant();
                if (!result.ContainsKey(sId)) result[sId] = new List<AdaptiveQuestionModel>();
                result[sId].Add(model);

                if (legacyToCanonical.TryGetValue(sId, out var canId))
                {
                    if (!result.ContainsKey(canId)) result[canId] = new List<AdaptiveQuestionModel>();
                    result[canId].Add(model);
                }
            }
        }
        else
        {
            var v3Qs = await _db.V3Questions.AsNoTracking()
                .Where(q => q.RoleId.ToLower() == normalizedRoleId)
                .ToListAsync(cancellationToken);
            foreach (var q in v3Qs)
            {
                var model = new AdaptiveQuestionModel(q.Id, q.RoleId, q.CanonicalSkillId, q.Difficulty, q.QuestionType, q.QuestionText, q.ReferenceExplanation, q.ExpectedSignalsJson);
                var sId = q.CanonicalSkillId.ToLowerInvariant();
                if (!result.ContainsKey(sId)) result[sId] = new List<AdaptiveQuestionModel>();
                result[sId].Add(model);
            }
        }

        return result;
    }

    public async Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> StartSessionAsync(
        AdaptiveSessionRequest request,
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

        // Fetch canonical framework nodes for V3 roles
        var roleNodes = await _db.RoleRoadmapNodes.AsNoTracking()
            .Where(rn => rn.RoleId.ToLower() == normalizedRoleId)
            .OrderBy(rn => rn.DisplayOrder)
            .ToListAsync(cancellationToken);

        var canonicalSkills = await _db.CanonicalSkills.AsNoTracking()
            .ToDictionaryAsync(cs => cs.Id.ToLowerInvariant(), cs => cs, cancellationToken);

        // Fetch legacy role skills for V2 roles
        var roleSkills = await _db.RoleSkills.AsNoTracking()
            .Where(rs => rs.RoleId.ToLower() == normalizedRoleId)
            .Include(rs => rs.Skill)
            .OrderBy(rs => rs.DisplayOrder)
            .ToListAsync(cancellationToken);

        var roleSkillMap = roleSkills.ToDictionary(rs => rs.SkillId.ToLowerInvariant(), rs => rs, StringComparer.OrdinalIgnoreCase);
        var allCatalogSkills = await _db.Skills.AsNoTracking()
            .ToDictionaryAsync(s => s.Id.ToLowerInvariant(), s => s, cancellationToken);

        // Validate primary language if provided
        string? normalizedLangId = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryLanguageId))
        {
            normalizedLangId = request.PrimaryLanguageId.Trim().ToLowerInvariant();
            if (!allCatalogSkills.ContainsKey(normalizedLangId) && !canonicalSkills.ContainsKey(normalizedLangId))
            {
                return (false, null, "VALIDATION_ERROR", $"Primary language '{request.PrimaryLanguageId}' does not exist in catalog.");
            }
        }

        var questionsBySkill = await GetQuestionsBySkillForRoleAsync(normalizedRoleId, cancellationToken);
        var legacyMappings = await _db.LegacySkillMappings.AsNoTracking().ToListAsync(cancellationToken);
        var legacyToCanonical = legacyMappings.ToDictionary(m => m.LegacySkillId.ToLowerInvariant(), m => m.CanonicalSkillId.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
        var canonicalToLegacy = legacyMappings.ToDictionary(m => m.CanonicalSkillId.ToLowerInvariant(), m => m.LegacySkillId.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

        string ResolveSkillDisplayName(string id)
        {
            var lower = id.ToLowerInvariant();
            if (canonicalSkills.TryGetValue(lower, out var cs)) return cs.DisplayName;
            if (roleSkillMap.TryGetValue(lower, out var rs)) return rs.Skill.Name;
            if (allCatalogSkills.TryGetValue(lower, out var sk)) return sk.Name;
            if (legacyToCanonical.TryGetValue(lower, out var cId) && canonicalSkills.TryGetValue(cId, out var cs2)) return cs2.DisplayName;
            if (canonicalToLegacy.TryGetValue(lower, out var lId) && allCatalogSkills.TryGetValue(lId, out var sk2)) return sk2.Name;
            return id;
        }

        var orderedSkillIds = new List<string>();
        var unsupportedSkillIds = new List<string>();

        bool isAssessmentV2 = normalizedRoleId != "backend-developer"
            || distinctRequested.Any(s => s.Contains('.'))
            || request.IncludeMandatoryFundamentals == true;

        if (isAssessmentV2 && roleNodes.Count > 0)
        {
            // Assessment V2 Composition: ROLE MANDATORY FUNDAMENTALS + USER SELECTED SKILLS
            var mandatoryIds = roleNodes
                .Where(rn => rn.MandatoryFundamental && rn.AssessmentEligible)
                .Select(rn => rn.CanonicalSkillId.ToLowerInvariant())
                .ToList();

            var combinedSkillIds = mandatoryIds
                .Union(distinctRequested, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var rn in roleNodes)
            {
                var sId = rn.CanonicalSkillId.ToLowerInvariant();
                if (combinedSkillIds.Contains(sId))
                {
                    if (questionsBySkill.TryGetValue(sId, out var qList) && qList.Count > 0)
                    {
                        if (!orderedSkillIds.Contains(sId))
                        {
                            orderedSkillIds.Add(sId);
                        }
                    }
                    else
                    {
                        unsupportedSkillIds.Add(sId);
                    }
                }
            }
        }
        else
        {
            // Focused / legacy selection mode (preserves single-skill test sessions e.g. ["sql"])
            var sortedRequested = distinctRequested
                .OrderBy(sId => roleSkillMap.TryGetValue(sId, out var rs) ? rs.DisplayOrder : int.MaxValue)
                .ToList();

            foreach (var sId in sortedRequested)
            {
                if (questionsBySkill.TryGetValue(sId, out var qList) && qList.Count > 0)
                {
                    if (!orderedSkillIds.Contains(sId))
                    {
                        orderedSkillIds.Add(sId);
                    }
                }
                else
                {
                    unsupportedSkillIds.Add(sId);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedLangId))
        {
            if (questionsBySkill.TryGetValue(normalizedLangId, out var qList) && qList.Count > 0)
            {
                if (!orderedSkillIds.Contains(normalizedLangId))
                {
                    orderedSkillIds.Add(normalizedLangId);
                }
            }
            else
            {
                unsupportedSkillIds.Add(normalizedLangId);
            }
        }

        if (orderedSkillIds.Count == 0)
        {
            return (false, null, "NO_ASSESSABLE_SKILLS", "None of the requested competencies or languages have questions in the catalog.");
        }

        // Initialize Session State
        var sessionId = $"sess-{Guid.NewGuid():N}";
        var firstSkillId = orderedSkillIds[0];
        var firstSkillQuestions = questionsBySkill[firstSkillId];
        var firstAppliedQ = firstSkillQuestions.FirstOrDefault(q => q.Difficulty.Equals("applied", StringComparison.OrdinalIgnoreCase))
                           ?? firstSkillQuestions.First();

        var sessionState = new DiagnosticSessionState
        {
            SessionId = sessionId,
            RoleId = role.Id,
            SelectedSkillIds = distinctRequested,
            PrimaryLanguageId = normalizedLangId,
            OrderedSkillIds = orderedSkillIds,
            CurrentSkillIndex = 0,
            CurrentStage = "applied",
            CurrentQuestionId = firstAppliedQ.Id,
            UnsupportedSkillIds = unsupportedSkillIds,
            Status = "in-progress"
        };

        // Initialize SkillAssessmentStates
        foreach (var sId in orderedSkillIds)
        {
            var skillName = ResolveSkillDisplayName(sId);
            var appliedQ = questionsBySkill[sId].FirstOrDefault(q => q.Difficulty.Equals("applied", StringComparison.OrdinalIgnoreCase))
                          ?? questionsBySkill[sId].First();

            sessionState.SkillStates[sId] = new SkillAssessmentState
            {
                SkillId = sId,
                SkillName = skillName,
                FirstQuestionId = appliedQ.Id,
                FirstDifficulty = appliedQ.Difficulty
            };
        }

        _sessionStore.CreateSession(sessionState);

        var firstSkillName = ResolveSkillDisplayName(firstSkillId);
        var publicFirstQ = new AdaptivePublicQuestionDto(
            firstAppliedQ.Id,
            firstAppliedQ.SkillId,
            firstSkillName,
            firstAppliedQ.Difficulty,
            firstAppliedQ.QuestionType,
            firstAppliedQ.QuestionText
        );

        var progress = new AdaptiveProgressDto(
            CurrentSkillIndex: 0,
            TotalSkills: orderedSkillIds.Count,
            CurrentSkillId: firstSkillId,
            CurrentSkillName: firstSkillName,
            Stage: "applied",
            CompletedSkills: 0,
            TotalAnswered: 0
        );

        var response = new AdaptiveSessionResponse(
            SessionId: sessionId,
            RoleId: role.Id,
            Status: "in-progress",
            CurrentQuestion: publicFirstQ,
            Progress: progress,
            Skills: null,
            TopGaps: null,
            UnsupportedSkillIds: unsupportedSkillIds
        );

        return (true, response, null, null);
    }

    public async Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> SubmitAnswerAsync(
        string sessionId,
        AdaptiveAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionStore.TryGetSession(sessionId, out var session) || session == null)
        {
            return (false, null, "SESSION_NOT_FOUND", $"Diagnostic session '{sessionId}' was not found.");
        }

        if (session.Status == "completed")
        {
            return (false, null, "SESSION_ALREADY_COMPLETED", "This diagnostic session has already been completed.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.QuestionId))
        {
            return (false, null, "VALIDATION_ERROR", "Field 'questionId' is required.");
        }

        // Security boundary: Client must not answer a question other than current expected question
        if (!string.Equals(request.QuestionId.Trim(), session.CurrentQuestionId, StringComparison.OrdinalIgnoreCase))
        {
            return (false, null, "INVALID_QUESTION_SUBMISSION", $"Submitted question ID '{request.QuestionId}' does not match expected current question '{session.CurrentQuestionId}'.");
        }

        var currentSkillId = session.OrderedSkillIds[session.CurrentSkillIndex];
        if (!session.SkillStates.TryGetValue(currentSkillId, out var skillState) || skillState == null)
        {
            return (false, null, "INTERNAL_ERROR", $"Skill state for '{currentSkillId}' was missing.");
        }

        // Evaluate answer
        var (level, reason, evidence) = EvaluateAnswer(session.RoleId, session.CurrentQuestionId, request.Answer);

        // Fetch all questions for current role to select follow-up or next skill question
        var questionsBySkill = await GetQuestionsBySkillForRoleAsync(session.RoleId, cancellationToken);

        // Fetch current question entity to get server-side reference explanation
        var currentQEntity = await FindQuestionAsync(session.CurrentQuestionId, cancellationToken);

        List<string> whatYouCovered;
        if (level.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase))
        {
            whatYouCovered = new List<string> { "Not enough evidence to assess this area yet." };
        }
        else if (evidence != null && evidence.Count > 0)
        {
            whatYouCovered = evidence;
        }
        else
        {
            whatYouCovered = new List<string> { reason };
        }

        List<string> whatCouldBeStronger;
        if (level.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
        {
            whatCouldBeStronger = new List<string> { "Strong response demonstrating mastery. Continue exploring domain edge cases and system limits." };
        }
        else if (level.Equals("Intermediate", StringComparison.OrdinalIgnoreCase))
        {
            whatCouldBeStronger = new List<string> { "Deepen your reasoning on high-scale tradeoffs, resilience boundaries, and failure mitigation." };
        }
        else if (level.Equals("Beginner", StringComparison.OrdinalIgnoreCase))
        {
            whatCouldBeStronger = new List<string> { "Provide concrete practical examples, error-handling mechanisms, and architectural context." };
        }
        else
        {
            whatCouldBeStronger = new List<string> { "Provide more detailed technical steps, reasoning, and practical explanations." };
        }

        var referenceExplanation = !string.IsNullOrWhiteSpace(currentQEntity?.ReferenceExplanation)
            ? currentQEntity.ReferenceExplanation
            : ("Reference explanation for " + (currentQEntity?.QuestionText ?? "the question"));

        var explanation = new PostAnswerExplanationDto(
            QuestionId: session.CurrentQuestionId,
            SkillId: currentSkillId,
            SkillName: skillState.SkillName,
            EvaluatedLevel: level,
            WhatYouCovered: whatYouCovered,
            WhatCouldBeStronger: whatCouldBeStronger,
            ReferenceExplanation: referenceExplanation
        );

        session.LastExplanation = explanation;
        session.AwaitingNextQuestion = true;

        if (session.CurrentStage == "applied")
        {
            // First question answered! Record Applied evaluation
            skillState.FirstAnswer = request.Answer ?? string.Empty;
            skillState.FirstLevel = level;
            skillState.FirstReason = reason;
            skillState.FirstEvidence = evidence;

            // Determine Branch
            var branch = _adaptiveEngine.DetermineBranch(level);
            skillState.Branch = branch;

            // Select follow-up question from SQLite matching the branch difficulty
            var skillQuestions = questionsBySkill[currentSkillId];
            var followUpQ = skillQuestions.FirstOrDefault(q => q.Difficulty.Equals(branch, StringComparison.OrdinalIgnoreCase));
            if (followUpQ == null)
            {
                // Fallback to any question different from first
                followUpQ = skillQuestions.FirstOrDefault(q => !string.Equals(q.Id, skillState.FirstQuestionId, StringComparison.OrdinalIgnoreCase))
                            ?? skillQuestions.First();
            }

            skillState.FollowUpQuestionId = followUpQ.Id;
            skillState.FollowUpDifficulty = followUpQ.Difficulty;

            // Transition session to stage 2: follow-up
            session.CurrentStage = "follow-up";
            session.CurrentQuestionId = followUpQ.Id;
            _sessionStore.UpdateSession(session);

            var publicFollowUpQ = new AdaptivePublicQuestionDto(
                followUpQ.Id,
                followUpQ.SkillId,
                skillState.SkillName,
                followUpQ.Difficulty,
                followUpQ.QuestionType,
                followUpQ.QuestionText
            );

            var totalAnswered = CalculateTotalAnswered(session);

            var progress = new AdaptiveProgressDto(
                CurrentSkillIndex: session.CurrentSkillIndex,
                TotalSkills: session.OrderedSkillIds.Count,
                CurrentSkillId: currentSkillId,
                CurrentSkillName: skillState.SkillName,
                Stage: "follow-up",
                CompletedSkills: session.CurrentSkillIndex,
                TotalAnswered: totalAnswered
            );

            var response = new AdaptiveSessionResponse(
                SessionId: session.SessionId,
                RoleId: session.RoleId,
                Status: "in-progress",
                CurrentQuestion: publicFollowUpQ,
                Progress: progress,
                Skills: null,
                TopGaps: null,
                UnsupportedSkillIds: session.UnsupportedSkillIds,
                LastExplanation: explanation
            );

            return (true, response, null, null);
        }
        else // follow-up stage
        {
            // Second question answered! Record follow-up evaluation
            skillState.FollowUpAnswer = request.Answer ?? string.Empty;
            skillState.FollowUpLevel = level;
            skillState.FollowUpReason = reason;
            skillState.FollowUpEvidence = evidence;

            // Run deterministic 14-rule aggregation matrix
            var finalLevel = _adaptiveEngine.DetermineFinalLevel(
                skillState.FirstLevel!,
                level,
                skillState.Branch!
            );

            var aggregatedEvidence = _adaptiveEngine.AggregateEvidence(
                skillState.FirstEvidence,
                evidence
            );

            var finalReason = _adaptiveEngine.FormatFinalReason(
                skillState.FirstReason,
                reason
            );

            skillState.FinalLevel = finalLevel;
            skillState.EvidenceSummary = aggregatedEvidence;
            skillState.FinalReason = finalReason;
            skillState.IsCompleted = true;

            // Advance to next skill
            session.CurrentSkillIndex++;

            if (session.CurrentSkillIndex < session.OrderedSkillIds.Count)
            {
                // Move to next skill's Applied question
                var nextSkillId = session.OrderedSkillIds[session.CurrentSkillIndex];
                var nextSkillState = session.SkillStates[nextSkillId];
                var nextSkillQuestions = questionsBySkill[nextSkillId];
                var nextAppliedQ = nextSkillQuestions.FirstOrDefault(q => q.Difficulty.Equals("applied", StringComparison.OrdinalIgnoreCase))
                                  ?? nextSkillQuestions.First();

                session.CurrentStage = "applied";
                session.CurrentQuestionId = nextAppliedQ.Id;
                _sessionStore.UpdateSession(session);

                var publicNextQ = new AdaptivePublicQuestionDto(
                    nextAppliedQ.Id,
                    nextAppliedQ.SkillId,
                    nextSkillState.SkillName,
                    nextAppliedQ.Difficulty,
                    nextAppliedQ.QuestionType,
                    nextAppliedQ.QuestionText
                );

                var totalAnswered = CalculateTotalAnswered(session);

                var progress = new AdaptiveProgressDto(
                    CurrentSkillIndex: session.CurrentSkillIndex,
                    TotalSkills: session.OrderedSkillIds.Count,
                    CurrentSkillId: nextSkillId,
                    CurrentSkillName: nextSkillState.SkillName,
                    Stage: "applied",
                    CompletedSkills: session.CurrentSkillIndex,
                    TotalAnswered: totalAnswered
                );

                var response = new AdaptiveSessionResponse(
                    SessionId: session.SessionId,
                    RoleId: session.RoleId,
                    Status: "in-progress",
                    CurrentQuestion: publicNextQ,
                    Progress: progress,
                    Skills: null,
                    TopGaps: null,
                    UnsupportedSkillIds: session.UnsupportedSkillIds,
                    LastExplanation: explanation
                );

                return (true, response, null, null);
            }
            else
            {
                // All skills completed!
                session.Status = "completed";
                session.CurrentQuestionId = string.Empty;

                var completedSkills = session.OrderedSkillIds
                    .Select(sId => session.SkillStates[sId])
                    .Select(s => new AdaptiveSkillResultDto(
                        SkillId: s.SkillId,
                        SkillName: s.SkillName,
                        FinalLevel: s.FinalLevel ?? "Insufficient Evidence",
                        Reason: s.FinalReason ?? string.Empty,
                        Evidence: s.EvidenceSummary,
                        QuestionsAnswered: 2,
                        Branch: s.Branch ?? "none",
                        AppliedDifficulty: s.FirstDifficulty,
                        AppliedLevel: s.FirstLevel ?? "Insufficient Evidence",
                        FollowUpDifficulty: s.FollowUpDifficulty ?? "none",
                        FollowUpLevel: s.FollowUpLevel ?? "Insufficient Evidence"
                    ))
                    .ToList();

                // Compute Top Gaps using existing pipeline
                var skillEvalItems = completedSkills
                    .Select(s => new SkillEvaluationItem(s.SkillName, s.FinalLevel, s.Reason, s.Evidence))
                    .ToList();

                var topGaps = DeterministicDiagnosticEvaluator.CalculateTopGaps(skillEvalItems);

                // Fetch catalog subskills for deterministic profile derivation
                var skillSubskills = await _db.SkillSubskills.AsNoTracking()
                    .Where(ss => session.OrderedSkillIds.Contains(ss.SkillId))
                    .Include(ss => ss.Subskill)
                    .ToListAsync(cancellationToken);

                var subskillsBySkillId = skillSubskills
                    .GroupBy(ss => ss.SkillId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => (IReadOnlyList<string>)g.Select(x => x.Subskill.Name).ToList(),
                        StringComparer.OrdinalIgnoreCase
                    );

                var roleNodes = await _db.RoleRoadmapNodes.AsNoTracking()
                    .Where(rn => rn.RoleId.ToLower() == session.RoleId.ToLower())
                    .ToListAsync(cancellationToken);

                var canonicalSkills = await _db.CanonicalSkills.AsNoTracking()
                    .ToDictionaryAsync(cs => cs.Id, cs => cs.DisplayName, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var profile = _profileBuilder.BuildProfile(session, subskillsBySkillId, roleNodes, canonicalSkills);
                session.Profile = profile;
                _sessionStore.UpdateSession(session);

                var response = new AdaptiveSessionResponse(
                    SessionId: session.SessionId,
                    RoleId: session.RoleId,
                    Status: "completed",
                    CurrentQuestion: null,
                    Progress: null,
                    Skills: completedSkills,
                    TopGaps: topGaps,
                    UnsupportedSkillIds: session.UnsupportedSkillIds,
                    Profile: profile,
                    LastExplanation: explanation
                );

                return (true, response, null, null);
            }
        }
    }

    public Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> AdvanceNextQuestionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionStore.TryGetSession(sessionId, out var session) || session == null)
        {
            return Task.FromResult<(bool, AdaptiveSessionResponse?, string?, string?)>((false, null, "SESSION_NOT_FOUND", $"Diagnostic session '{sessionId}' was not found."));
        }

        session.AwaitingNextQuestion = false;
        session.LastExplanation = null;
        _sessionStore.UpdateSession(session);

        return GetSessionAsync(sessionId, cancellationToken);
    }

    public async Task<(bool Success, AdaptiveSessionResponse? Response, string? ErrorCode, string? ErrorMessage)> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionStore.TryGetSession(sessionId, out var session) || session == null)
        {
            return (false, null, "SESSION_NOT_FOUND", $"Diagnostic session '{sessionId}' was not found.");
        }

        if (session.Status == "completed")
        {
            if (session.Profile == null)
            {
                var skillSubskills = await _db.SkillSubskills.AsNoTracking()
                    .Where(ss => session.OrderedSkillIds.Contains(ss.SkillId))
                    .Include(ss => ss.Subskill)
                    .ToListAsync(cancellationToken);

                var subskillsBySkillId = skillSubskills
                    .GroupBy(ss => ss.SkillId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => (IReadOnlyList<string>)g.Select(x => x.Subskill.Name).ToList(),
                        StringComparer.OrdinalIgnoreCase
                    );

                var roleNodes = await _db.RoleRoadmapNodes.AsNoTracking()
                    .Where(rn => rn.RoleId.ToLower() == session.RoleId.ToLower())
                    .ToListAsync(cancellationToken);

                var canonicalSkills = await _db.CanonicalSkills.AsNoTracking()
                    .ToDictionaryAsync(cs => cs.Id, cs => cs.DisplayName, StringComparer.OrdinalIgnoreCase, cancellationToken);

                session.Profile = _profileBuilder.BuildProfile(session, subskillsBySkillId, roleNodes, canonicalSkills);
                _sessionStore.UpdateSession(session);
            }

            var completedSkills = session.OrderedSkillIds
                .Select(sId => session.SkillStates[sId])
                .Select(s => new AdaptiveSkillResultDto(
                    SkillId: s.SkillId,
                    SkillName: s.SkillName,
                    FinalLevel: s.FinalLevel ?? "Insufficient Evidence",
                    Reason: s.FinalReason ?? string.Empty,
                    Evidence: s.EvidenceSummary,
                    QuestionsAnswered: 2,
                    Branch: s.Branch ?? "none",
                    AppliedDifficulty: s.FirstDifficulty,
                    AppliedLevel: s.FirstLevel ?? "Insufficient Evidence",
                    FollowUpDifficulty: s.FollowUpDifficulty ?? "none",
                    FollowUpLevel: s.FollowUpLevel ?? "Insufficient Evidence"
                ))
                .ToList();

            var skillEvalItems = completedSkills
                .Select(s => new SkillEvaluationItem(s.SkillName, s.FinalLevel, s.Reason, s.Evidence))
                .ToList();

            var topGaps = DeterministicDiagnosticEvaluator.CalculateTopGaps(skillEvalItems);

            var response = new AdaptiveSessionResponse(
                SessionId: session.SessionId,
                RoleId: session.RoleId,
                Status: "completed",
                CurrentQuestion: null,
                Progress: null,
                Skills: completedSkills,
                TopGaps: topGaps,
                UnsupportedSkillIds: session.UnsupportedSkillIds,
                Profile: session.Profile,
                LastExplanation: session.LastExplanation
            );

            return (true, response, null, null);
        }
        else
        {
            var currentSkillId = session.OrderedSkillIds[session.CurrentSkillIndex];
            var skillState = session.SkillStates[currentSkillId];

            // Reconstruct current question
            var currentQ = await FindQuestionAsync(session.CurrentQuestionId, cancellationToken);

            if (currentQ == null)
            {
                return (false, null, "QUESTION_NOT_FOUND", $"Current question '{session.CurrentQuestionId}' was not found.");
            }

            var publicQ = new AdaptivePublicQuestionDto(
                currentQ.Id,
                currentQ.SkillId,
                skillState.SkillName,
                currentQ.Difficulty,
                currentQ.QuestionType,
                currentQ.QuestionText
            );

            var totalAnswered = CalculateTotalAnswered(session);

            var progress = new AdaptiveProgressDto(
                CurrentSkillIndex: session.CurrentSkillIndex,
                TotalSkills: session.OrderedSkillIds.Count,
                CurrentSkillId: currentSkillId,
                CurrentSkillName: skillState.SkillName,
                Stage: session.CurrentStage,
                CompletedSkills: session.CurrentSkillIndex,
                TotalAnswered: totalAnswered
            );

            var response = new AdaptiveSessionResponse(
                SessionId: session.SessionId,
                RoleId: session.RoleId,
                Status: "in-progress",
                CurrentQuestion: publicQ,
                Progress: progress,
                Skills: null,
                TopGaps: null,
                UnsupportedSkillIds: session.UnsupportedSkillIds,
                LastExplanation: session.LastExplanation
            );

            return (true, response, null, null);
        }
    }

    public async Task<(bool Success, CareerReadinessProfile? Profile, string? ErrorCode, string? ErrorMessage)> GetProfileAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionStore.TryGetSession(sessionId, out var session) || session == null)
        {
            return (false, null, "SESSION_NOT_FOUND", $"Diagnostic session '{sessionId}' was not found.");
        }

        if (session.Status != "completed")
        {
            return (false, null, "SESSION_IN_PROGRESS", $"Diagnostic session '{sessionId}' is still in progress. Complete all questions before retrieving the profile.");
        }

        if (session.Profile != null)
        {
            return (true, session.Profile, null, null);
        }

        var skillSubskills = await _db.SkillSubskills.AsNoTracking()
            .Where(ss => session.OrderedSkillIds.Contains(ss.SkillId))
            .Include(ss => ss.Subskill)
            .ToListAsync(cancellationToken);

        var subskillsBySkillId = skillSubskills
            .GroupBy(ss => ss.SkillId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Subskill.Name).ToList(),
                StringComparer.OrdinalIgnoreCase
            );

        var profile = _profileBuilder.BuildProfile(session, subskillsBySkillId);
        session.Profile = profile;
        _sessionStore.UpdateSession(session);

        return (true, profile, null, null);
    }

    private (string Level, string Reason, List<string> Evidence) EvaluateAnswer(
        string roleId,
        string questionId,
        string answer)
    {
        // Reuses deterministic diagnostic evaluator with 0 external API calls during regression
        return DeterministicDiagnosticEvaluator.EvaluateSingleAnswer(
            roleId,
            questionId,
            answer
        );
    }

    private static int CalculateTotalAnswered(DiagnosticSessionState session)
    {
        int count = 0;
        foreach (var s in session.SkillStates.Values)
        {
            if (!string.IsNullOrWhiteSpace(s.FirstAnswer)) count++;
            if (!string.IsNullOrWhiteSpace(s.FollowUpAnswer)) count++;
        }
        return count;
    }
}
