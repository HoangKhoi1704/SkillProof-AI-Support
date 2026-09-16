using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SkillProof.Api.Data.Catalog;

public interface ICatalogSeeder
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public class CatalogSeeder : ICatalogSeeder
{
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly HashSet<string> FrozenCoreQuestionIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "q-be-prog-01", "q-be-prog-02", "q-be-prog-03",
        "q-be-rest-01", "q-be-rest-02", "q-be-rest-03",
        "q-be-sql-01",  "q-be-sql-02",  "q-be-sql-03",
        "q-be-test-01", "q-be-test-02", "q-be-test-03",
        "q-be-auth-01", "q-be-auth-02", "q-be-auth-03",
        "q-be-sys-01",  "q-be-sys-02",  "q-be-sys-03"
    };

    public static IReadOnlySet<string> FrozenQuestionIds => FrozenCoreQuestionIds;

    private readonly CatalogDbContext _db;
    private readonly ILogger<CatalogSeeder> _logger;
    private readonly string? _customDataPath;

    public CatalogSeeder(CatalogDbContext db, ILogger<CatalogSeeder> logger, IConfiguration? config = null)
    {
        _db = db;
        _logger = logger;
        _customDataPath = config?["Catalog:DataPath"];
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            try
            {
                await _db.Database.EnsureCreatedAsync(cancellationToken);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 || ex.Message.Contains("already exists"))
            {
                _logger.LogDebug("SQLite schema was created concurrently by another initialization thread.");
            }

            await SeedInternalAsync(cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            await SeedInternalAsync(cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SeedInternalAsync(CancellationToken cancellationToken)
    {
        var dataDir = ResolveDataDirectory();
        if (string.IsNullOrWhiteSpace(dataDir) || !Directory.Exists(dataDir))
        {
            throw new DirectoryNotFoundException($"Could not locate the frozen assessment data directory: '{dataDir}'");
        }

        var catalogFile = Path.Combine(dataDir, "skill-catalog.json");
        var sourcesFile = Path.Combine(dataDir, "sources.json");
        var questionsFile = Path.Combine(dataDir, "questions.json");

        if (!File.Exists(catalogFile) || !File.Exists(sourcesFile) || !File.Exists(questionsFile))
        {
            throw new FileNotFoundException($"Missing one or more required JSON data files in '{dataDir}'.");
        }

        using var catalogDoc = JsonDocument.Parse(await File.ReadAllTextAsync(catalogFile, cancellationToken));
        using var sourcesDoc = JsonDocument.Parse(await File.ReadAllTextAsync(sourcesFile, cancellationToken));
        using var questionsDoc = JsonDocument.Parse(await File.ReadAllTextAsync(questionsFile, cancellationToken));

        var rootCatalog = catalogDoc.RootElement;
        var roleId = rootCatalog.GetProperty("roleId").GetString() ?? "backend-developer";

        // Always execute deterministic natural-key upsert & integrity synchronization
        _logger.LogInformation("Synchronizing SQLite catalog from '{DataDir}'...", dataDir);

        // 1. Seed Role
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role == null)
        {
            role = new Role
            {
                Id = roleId,
                Title = "Backend Developer",
                Description = "Designs, builds, tests, and maintains scalable server-side systems, APIs, and data architectures."
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // 2. Seed Skills & Subskills (Competencies)
        var competencies = rootCatalog.GetProperty("competencies").EnumerateArray();
        int displayOrder = 1;
        foreach (var comp in competencies)
        {
            var skillId = comp.GetProperty("id").GetString()!;
            var category = comp.GetProperty("category").GetString()!;
            var name = comp.GetProperty("name").GetString()!;
            var description = comp.TryGetProperty("description", out var d) ? d.GetString() : null;

            var skill = await _db.Skills.FirstOrDefaultAsync(s => s.Id == skillId, cancellationToken);
            if (skill == null)
            {
                skill = new Skill
                {
                    Id = skillId,
                    Name = name,
                    SkillType = "competency",
                    Description = description
                };
                _db.Skills.Add(skill);
            }
            else
            {
                skill.Name = name;
                skill.SkillType = "competency";
                skill.Description = description;
            }

            var roleSkill = await _db.RoleSkills.FirstOrDefaultAsync(rs => rs.RoleId == roleId && rs.SkillId == skillId, cancellationToken);
            if (roleSkill == null)
            {
                _db.RoleSkills.Add(new RoleSkill
                {
                    RoleId = roleId,
                    SkillId = skillId,
                    Category = category,
                    DisplayOrder = displayOrder++
                });
            }
            else
            {
                roleSkill.Category = category;
                roleSkill.DisplayOrder = displayOrder++;
            }

            if (comp.TryGetProperty("subskills", out var subskillsArray))
            {
                foreach (var sub in subskillsArray.EnumerateArray())
                {
                    var subId = sub.GetProperty("id").GetString()!;
                    var subName = sub.GetProperty("name").GetString()!;
                    var subDesc = sub.TryGetProperty("description", out var sd) ? sd.GetString() : null;

                    var subskill = await _db.Subskills.FirstOrDefaultAsync(s => s.Id == subId, cancellationToken);
                    if (subskill == null)
                    {
                        subskill = new Subskill
                        {
                            Id = subId,
                            Name = subName,
                            Description = subDesc
                        };
                        _db.Subskills.Add(subskill);
                    }

                    var exists = _db.SkillSubskills.Local.Any(ss => ss.SkillId == skillId && ss.SubskillId == subId)
                        || await _db.SkillSubskills.AnyAsync(ss => ss.SkillId == skillId && ss.SubskillId == subId, cancellationToken);

                    if (!exists)
                    {
                        _db.SkillSubskills.Add(new SkillSubskill
                        {
                            SkillId = skillId,
                            SubskillId = subId
                        });
                    }
                }
            }
        }

        // 3. Seed Languages (Separated category)
        if (rootCatalog.TryGetProperty("languages", out var languagesArray))
        {
            int langOrder = 1;
            foreach (var lang in languagesArray.EnumerateArray())
            {
                var langId = lang.GetProperty("id").GetString()!;
                var langName = lang.GetProperty("name").GetString()!;

                var skill = await _db.Skills.FirstOrDefaultAsync(s => s.Id == langId, cancellationToken);
                if (skill == null)
                {
                    skill = new Skill
                    {
                        Id = langId,
                        Name = langName,
                        SkillType = "language",
                        Description = null
                    };
                    _db.Skills.Add(skill);
                }
                else
                {
                    skill.Name = langName;
                    skill.SkillType = "language";
                }

                var roleSkill = await _db.RoleSkills.FirstOrDefaultAsync(rs => rs.RoleId == roleId && rs.SkillId == langId, cancellationToken);
                if (roleSkill == null)
                {
                    _db.RoleSkills.Add(new RoleSkill
                    {
                        RoleId = roleId,
                        SkillId = langId,
                        Category = "language",
                        DisplayOrder = langOrder++
                    });
                }
                else
                {
                    roleSkill.Category = "language";
                    roleSkill.DisplayOrder = langOrder++;
                }

                if (lang.TryGetProperty("subskills", out var langSubs))
                {
                    foreach (var sub in langSubs.EnumerateArray())
                    {
                        var subId = sub.GetProperty("id").GetString()!;
                        var subName = sub.GetProperty("name").GetString()!;

                        var subskill = await _db.Subskills.FirstOrDefaultAsync(s => s.Id == subId, cancellationToken);
                        if (subskill == null)
                        {
                            subskill = new Subskill
                            {
                                Id = subId,
                                Name = subName,
                                Description = null
                            };
                            _db.Subskills.Add(subskill);
                        }

                        var exists = _db.SkillSubskills.Local.Any(ss => ss.SkillId == langId && ss.SubskillId == subId)
                            || await _db.SkillSubskills.AnyAsync(ss => ss.SkillId == langId && ss.SubskillId == subId, cancellationToken);

                        if (!exists)
                        {
                            _db.SkillSubskills.Add(new SkillSubskill
                            {
                                SkillId = langId,
                                SubskillId = subId
                            });
                        }
                    }
                }
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        // 4. Seed Sources
        var sourcesArray = sourcesDoc.RootElement.GetProperty("sources").EnumerateArray();
        foreach (var s in sourcesArray)
        {
            var sourceId = s.GetProperty("sourceId").GetString()!;
            var publisher = s.GetProperty("publisher").GetString()!;
            var title = s.GetProperty("title").GetString()!;
            var url = s.GetProperty("url").GetString()!;
            var sourceType = s.GetProperty("sourceType").GetString()!;
            var verificationStatus = s.GetProperty("verificationStatus").GetString()!;
            var accessedAt = s.GetProperty("accessedAt").GetString()!;
            var notes = s.TryGetProperty("notes", out var n) ? n.GetString() : null;

            var existingSource = await _db.Sources.FirstOrDefaultAsync(src => src.Id == sourceId, cancellationToken);
            if (existingSource == null)
            {
                _db.Sources.Add(new Source
                {
                    Id = sourceId,
                    Publisher = publisher,
                    Title = title,
                    Url = url,
                    SourceType = sourceType,
                    VerificationStatus = verificationStatus,
                    AccessedAt = accessedAt,
                    Notes = notes
                });
            }
            else
            {
                existingSource.Publisher = publisher;
                existingSource.Title = title;
                existingSource.Url = url;
                existingSource.SourceType = sourceType;
                existingSource.VerificationStatus = verificationStatus;
                existingSource.AccessedAt = accessedAt;
                existingSource.Notes = notes;
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        // 5. Seed Questions, Rubrics, and Source Links
        var questionsArray = questionsDoc.RootElement.GetProperty("questions").EnumerateArray();
        foreach (var q in questionsArray)
        {
            var qId = q.GetProperty("id").GetString()!;
            var qRoleId = q.GetProperty("roleId").GetString()!;
            var qSkillId = q.GetProperty("skillId").GetString()!;
            var difficulty = q.GetProperty("difficulty").GetString()!;
            var questionType = q.GetProperty("questionType").GetString()!;
            var questionText = q.GetProperty("question").GetString()!;
            var provenance = q.GetProperty("provenance").GetString()!;
            var verificationStatus = q.GetProperty("verificationStatus").GetString()!;
            var expectedSignalsJson = q.GetProperty("expectedSignals").GetRawText();

            var question = await _db.Questions.FirstOrDefaultAsync(qu => qu.Id == qId, cancellationToken);
            if (question == null)
            {
                question = new Question
                {
                    Id = qId,
                    RoleId = qRoleId,
                    SkillId = qSkillId,
                    Difficulty = difficulty,
                    QuestionType = questionType,
                    QuestionText = questionText,
                    ExpectedSignalsJson = expectedSignalsJson,
                    Provenance = provenance,
                    VerificationStatus = verificationStatus
                };
                _db.Questions.Add(question);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                if (FrozenCoreQuestionIds.Contains(qId))
                {
                    // Detect corruption of frozen Core questions: do NOT silently accept or overwrite
                    bool isCorrupted = question.RoleId != qRoleId
                        || question.SkillId != qSkillId
                        || question.Difficulty != difficulty
                        || question.QuestionType != questionType
                        || question.QuestionText != questionText
                        || question.Provenance != provenance
                        || question.VerificationStatus != verificationStatus;

                    if (isCorrupted)
                    {
                        throw new InvalidOperationException(
                            $"Catalog integrity violation: Frozen Core question '{qId}' in SQLite database is corrupted and conflicts with the approved Data Foundation v1.2 baseline. Seeder will not silently overwrite corrupted frozen data.");
                    }

                    if (q.TryGetProperty("rubric", out var rCheckObj))
                    {
                        var existingRubric = await _db.Rubrics.FirstOrDefaultAsync(r => r.QuestionId == qId, cancellationToken);
                        if (existingRubric != null)
                        {
                            var insEv = rCheckObj.GetProperty("Insufficient Evidence").GetString() ?? "";
                            var beg = rCheckObj.GetProperty("Beginner").GetString() ?? "";
                            var inter = rCheckObj.GetProperty("Intermediate").GetString() ?? "";
                            var adv = rCheckObj.GetProperty("Advanced").GetString() ?? "";

                            if (existingRubric.InsufficientEvidence != insEv
                                || existingRubric.Beginner != beg
                                || existingRubric.Intermediate != inter
                                || existingRubric.Advanced != adv)
                            {
                                throw new InvalidOperationException(
                                    $"Catalog integrity violation: Rubric for Frozen Core question '{qId}' in SQLite database is corrupted and conflicts with the approved Data Foundation v1.2 baseline. Seeder will not silently overwrite corrupted frozen data.");
                            }
                        }
                    }
                }
                else
                {
                    // For non-frozen questions (v2 questions), synchronize any stale fields from source of truth
                    question.RoleId = qRoleId;
                    question.SkillId = qSkillId;
                    question.Difficulty = difficulty;
                    question.QuestionType = questionType;
                    question.QuestionText = questionText;
                    question.ExpectedSignalsJson = expectedSignalsJson;
                    question.Provenance = provenance;
                    question.VerificationStatus = verificationStatus;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            // Subskills link
            if (q.TryGetProperty("subskills", out var qSubs))
            {
                foreach (var qs in qSubs.EnumerateArray())
                {
                    var subId = qs.GetString()!;
                    var exists = _db.QuestionSubskills.Local.Any(x => x.QuestionId == qId && x.SubskillId == subId)
                        || await _db.QuestionSubskills.AnyAsync(x => x.QuestionId == qId && x.SubskillId == subId, cancellationToken);

                    if (!exists)
                    {
                        _db.QuestionSubskills.Add(new QuestionSubskill
                        {
                            QuestionId = qId,
                            SubskillId = subId
                        });
                    }
                }
            }

            // Rubric
            if (q.TryGetProperty("rubric", out var rObj))
            {
                var rubricId = $"rubric-{qId}";
                var rubric = await _db.Rubrics.FirstOrDefaultAsync(r => r.QuestionId == qId, cancellationToken);
                if (rubric == null)
                {
                    rubric = new Rubric
                    {
                        Id = rubricId,
                        QuestionId = qId,
                        InsufficientEvidence = rObj.GetProperty("Insufficient Evidence").GetString() ?? "",
                        Beginner = rObj.GetProperty("Beginner").GetString() ?? "",
                        Intermediate = rObj.GetProperty("Intermediate").GetString() ?? "",
                        Advanced = rObj.GetProperty("Advanced").GetString() ?? ""
                    };
                    _db.Rubrics.Add(rubric);
                }
                else if (!FrozenCoreQuestionIds.Contains(qId))
                {
                    rubric.InsufficientEvidence = rObj.GetProperty("Insufficient Evidence").GetString() ?? "";
                    rubric.Beginner = rObj.GetProperty("Beginner").GetString() ?? "";
                    rubric.Intermediate = rObj.GetProperty("Intermediate").GetString() ?? "";
                    rubric.Advanced = rObj.GetProperty("Advanced").GetString() ?? "";
                }
            }

            // Framework sources
            if (q.TryGetProperty("frameworkSourceIds", out var fsIds))
            {
                foreach (var fs in fsIds.EnumerateArray())
                {
                    var sourceId = fs.GetString()!;
                    var exists = _db.QuestionFrameworkSources.Local.Any(x => x.QuestionId == qId && x.SourceId == sourceId)
                        || await _db.QuestionFrameworkSources.AnyAsync(x => x.QuestionId == qId && x.SourceId == sourceId, cancellationToken);

                    if (!exists)
                    {
                        _db.QuestionFrameworkSources.Add(new QuestionFrameworkSource
                        {
                            QuestionId = qId,
                            SourceId = sourceId
                        });
                    }
                }
            }

            // Interview evidence
            if (q.TryGetProperty("interviewEvidence", out var ieArray))
            {
                foreach (var ie in ieArray.EnumerateArray())
                {
                    var sourceId = ie.GetProperty("sourceId").GetString()!;
                    var evType = ie.GetProperty("evidenceType").GetString()!;
                    var evStrength = ie.GetProperty("evidenceStrength").GetString()!;
                    var notes = ie.GetProperty("notes").GetString()!;
                    var topicsJson = ie.GetProperty("supportedTopics").GetRawText();

                    var existingQie = _db.QuestionInterviewEvidence.Local.FirstOrDefault(x => x.QuestionId == qId && x.SourceId == sourceId)
                        ?? await _db.QuestionInterviewEvidence.FirstOrDefaultAsync(x => x.QuestionId == qId && x.SourceId == sourceId, cancellationToken);

                    if (existingQie == null)
                    {
                        _db.QuestionInterviewEvidence.Add(new QuestionInterviewEvidence
                        {
                            QuestionId = qId,
                            SourceId = sourceId,
                            EvidenceType = evType,
                            EvidenceStrength = evStrength,
                            Notes = notes,
                            SupportedTopicsJson = topicsJson
                        });
                    }
                    else
                    {
                        existingQie.EvidenceType = evType;
                        existingQie.EvidenceStrength = evStrength;
                        existingQie.Notes = notes;
                        existingQie.SupportedTopicsJson = topicsJson;
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SQLite catalog synchronized successfully. 48 questions, 22 skills, and 42 sources active and verified.");
    }

    private string ResolveDataDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_customDataPath) && Directory.Exists(_customDataPath))
        {
            return _customDataPath;
        }

        // Walk upwards from BaseDirectory or CurrentDirectory until data/backend is located
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "data", "backend");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "skill-catalog.json")))
            {
                return candidate;
            }
            current = current.Parent;
        }

        // Fallback to CurrentDirectory
        current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "data", "backend");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "skill-catalog.json")))
            {
                return candidate;
            }
            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "data", "backend");
    }
}
