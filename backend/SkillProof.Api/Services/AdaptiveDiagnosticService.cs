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

        // Fetch role skills to validate and preserve display ordering
        var roleSkills = await _db.RoleSkills.AsNoTracking()
            .Where(rs => rs.RoleId.ToLower() == normalizedRoleId)
            .Include(rs => rs.Skill)
            .OrderBy(rs => rs.DisplayOrder)
            .ToListAsync(cancellationToken);

        var roleSkillMap = roleSkills.ToDictionary(rs => rs.SkillId.ToLowerInvariant(), rs => rs, StringComparer.OrdinalIgnoreCase);
        var allCatalogSkills = await _db.Skills.AsNoTracking()
            .ToDictionaryAsync(s => s.Id.ToLowerInvariant(), s => s, cancellationToken);

        // Validate selected competencies
        foreach (var skillId in distinctRequested)
        {
            if (!allCatalogSkills.TryGetValue(skillId, out _))
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
        string? normalizedLangId = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryLanguageId))
        {
            normalizedLangId = request.PrimaryLanguageId.Trim().ToLowerInvariant();

            if (!allCatalogSkills.TryGetValue(normalizedLangId, out _))
            {
                return (false, null, "VALIDATION_ERROR", $"Primary language '{request.PrimaryLanguageId}' does not exist in catalog.");
            }

            if (!roleSkillMap.TryGetValue(normalizedLangId, out var mappedRoleLang))
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

        // Build deterministic ordered skills:
        // 1. Selected competencies in RoleSkills.DisplayOrder
        // 2. Primary language at the end
        var orderedSkillIds = new List<string>();
        var unsupportedSkillIds = new List<string>();

        foreach (var rs in roleSkills)
        {
            var sId = rs.SkillId.ToLowerInvariant();
            if (distinctRequested.Contains(sId))
            {
                if (questionsBySkill.TryGetValue(sId, out var qList) && qList.Count > 0)
                {
                    orderedSkillIds.Add(sId);
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
            var skillName = roleSkillMap[sId].Skill.Name;
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

        var firstSkillName = roleSkillMap[firstSkillId].Skill.Name;
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
        var roleQuestions = await _db.Questions.AsNoTracking()
            .Where(q => q.RoleId.ToLower() == session.RoleId.ToLower())
            .ToListAsync(cancellationToken);

        var questionsBySkill = roleQuestions
            .GroupBy(q => q.SkillId.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

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
                UnsupportedSkillIds: session.UnsupportedSkillIds
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
                    UnsupportedSkillIds: session.UnsupportedSkillIds
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

                var profile = _profileBuilder.BuildProfile(session, subskillsBySkillId);
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
                    Profile: profile
                );

                return (true, response, null, null);
            }
        }
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

                session.Profile = _profileBuilder.BuildProfile(session, subskillsBySkillId);
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
                Profile: session.Profile
            );

            return (true, response, null, null);
        }
        else
        {
            var currentSkillId = session.OrderedSkillIds[session.CurrentSkillIndex];
            var skillState = session.SkillStates[currentSkillId];

            // Reconstruct current question
            var currentQ = _db.Questions.AsNoTracking()
                .FirstOrDefault(q => q.Id == session.CurrentQuestionId);

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
                UnsupportedSkillIds: session.UnsupportedSkillIds
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
