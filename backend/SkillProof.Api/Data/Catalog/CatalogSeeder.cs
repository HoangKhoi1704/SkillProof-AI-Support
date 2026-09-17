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

            try
            {
                await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Questions ADD COLUMN ReferenceExplanation TEXT NULL;", cancellationToken);
            }
            catch
            {
                // Ignore if column already exists
            }

            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""LearningResources"" (
                        ""Id"" TEXT NOT NULL CONSTRAINT ""PK_LearningResources"" PRIMARY KEY,
                        ""Title"" TEXT NOT NULL,
                        ""SourceName"" TEXT NOT NULL,
                        ""SourceUrl"" TEXT NOT NULL,
                        ""ResourceType"" TEXT NOT NULL,
                        ""CanonicalSkillIdsJson"" TEXT NOT NULL,
                        ""RoleIdsJson"" TEXT NOT NULL,
                        ""Level"" TEXT NOT NULL,
                        ""IsOfficial"" INTEGER NOT NULL,
                        ""VerificationStatus"" TEXT NOT NULL,
                        ""VerifiedAt"" TEXT NOT NULL,
                        ""Locator"" TEXT NULL
                    );
                ", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to create LearningResources table: {Message}", ex.Message);
            }

            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""CuratedProjects"" (
                        ""Id"" TEXT NOT NULL CONSTRAINT ""PK_CuratedProjects"" PRIMARY KEY,
                        ""Title"" TEXT NOT NULL,
                        ""Source"" TEXT NOT NULL,
                        ""SourceUrl"" TEXT NULL,
                        ""SourceLocator"" TEXT NULL,
                        ""Provenance"" TEXT NOT NULL,
                        ""RoleIdsJson"" TEXT NOT NULL,
                        ""CanonicalSkillIdsJson"" TEXT NOT NULL,
                        ""RoadmapTargetsJson"" TEXT NOT NULL,
                        ""ProjectType"" TEXT NOT NULL,
                        ""Difficulty"" TEXT NOT NULL,
                        ""EstimatedScope"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""DeliverablesJson"" TEXT NOT NULL,
                        ""EvidenceRequirementsJson"" TEXT NOT NULL,
                        ""VerificationStatus"" TEXT NOT NULL,
                        ""VerifiedAt"" TEXT NOT NULL
                    );
                ", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to create CuratedProjects table: {Message}", ex.Message);
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

        await EnsureV3SchemaAsync(cancellationToken);
        await SeedV3CatalogAsync(dataDir, cancellationToken);

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
                    // Detect corruption of frozen Core questions: do NOT silently accept or overwrite core text
                    bool isCorrupted = question.RoleId != qRoleId
                        || question.SkillId != qSkillId
                        || question.Difficulty != difficulty
                        || question.QuestionType != questionType
                        || question.QuestionText != questionText
                        || question.Provenance != provenance;

                    if (isCorrupted)
                    {
                        throw new InvalidOperationException(
                            $"Catalog integrity violation: Frozen Core question '{qId}' in SQLite database is corrupted and conflicts with the approved Data Foundation v2.0 baseline. Seeder will not silently overwrite corrupted frozen data.");
                    }

                    // Synchronize provenance status from approved source of truth
                    question.VerificationStatus = verificationStatus;

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
                                    $"Catalog integrity violation: Rubric for Frozen Core question '{qId}' in SQLite database is corrupted and conflicts with the approved Data Foundation v2.0 baseline. Seeder will not silently overwrite corrupted frozen data.");
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
                }
                await _db.SaveChangesAsync(cancellationToken);
            }

            // Subskills link (Reconciliation: add missing + remove obsolete)
            var currentSubskillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (q.TryGetProperty("subskills", out var qSubs))
            {
                foreach (var qs in qSubs.EnumerateArray())
                {
                    var subId = qs.GetString()!;
                    currentSubskillIds.Add(subId);
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

            var existingSubskills = await _db.QuestionSubskills.Where(x => x.QuestionId == qId).ToListAsync(cancellationToken);
            foreach (var existingQs in existingSubskills)
            {
                if (!currentSubskillIds.Contains(existingQs.SubskillId))
                {
                    _db.QuestionSubskills.Remove(existingQs);
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

            // Framework sources (Reconciliation: add missing + remove obsolete)
            var currentFsIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (q.TryGetProperty("frameworkSourceIds", out var fsIds))
            {
                foreach (var fs in fsIds.EnumerateArray())
                {
                    var sourceId = fs.GetString()!;
                    currentFsIds.Add(sourceId);
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

            var existingFsList = await _db.QuestionFrameworkSources.Where(x => x.QuestionId == qId).ToListAsync(cancellationToken);
            foreach (var existingFs in existingFsList)
            {
                if (!currentFsIds.Contains(existingFs.SourceId))
                {
                    _db.QuestionFrameworkSources.Remove(existingFs);
                }
            }

            // Interview evidence (Reconciliation: add missing/update + remove obsolete)
            var currentIeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (q.TryGetProperty("interviewEvidence", out var ieArray))
            {
                foreach (var ie in ieArray.EnumerateArray())
                {
                    var sourceId = ie.GetProperty("sourceId").GetString()!;
                    currentIeIds.Add(sourceId);
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

            var existingIeList = await _db.QuestionInterviewEvidence.Where(x => x.QuestionId == qId).ToListAsync(cancellationToken);
            foreach (var existingIe in existingIeList)
            {
                if (!currentIeIds.Contains(existingIe.SourceId))
                {
                    _db.QuestionInterviewEvidence.Remove(existingIe);
                }
            }
        }

        // Bidirectional reconciliation: Remove any obsolete questions not in the 48 backend questions
        var validQuestionIds = questionsDoc.RootElement.GetProperty("questions").EnumerateArray()
            .Select(q => q.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var obsoleteQuestions = await _db.Questions.Where(q => !validQuestionIds.Contains(q.Id)).ToListAsync(cancellationToken);
        if (obsoleteQuestions.Count > 0)
        {
            _db.Questions.RemoveRange(obsoleteQuestions);
        }

        // Bidirectional reconciliation: Remove any obsolete skills not in the 22 backend catalog skills
        var validSkillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var comp in competencies)
        {
            validSkillIds.Add(comp.GetProperty("id").GetString()!);
        }
        if (rootCatalog.TryGetProperty("languages", out var langs))
        {
            foreach (var l in langs.EnumerateArray())
            {
                validSkillIds.Add(l.GetProperty("id").GetString()!);
            }
        }
        var obsoleteSkills = await _db.Skills.Where(s => !validSkillIds.Contains(s.Id)).ToListAsync(cancellationToken);
        if (obsoleteSkills.Count > 0)
        {
            _db.Skills.RemoveRange(obsoleteSkills);
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

    private string ResolveV3DataDirectory(string backendDataDir)
    {
        var parent = Path.GetDirectoryName(backendDataDir);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            var candidate = Path.Combine(parent, "v3");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "canonical-skills.json")))
            {
                return candidate;
            }
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "data", "v3");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "canonical-skills.json")))
            {
                return candidate;
            }
            current = current.Parent;
        }

        current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "data", "v3");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "canonical-skills.json")))
            {
                return candidate;
            }
            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "data", "v3");
    }

    private async Task EnsureV3SchemaAsync(CancellationToken cancellationToken)
    {
        try { await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Roles ADD COLUMN IsPrimaryDemoRole INTEGER NOT NULL DEFAULT 0;", cancellationToken); } catch { }
        try { await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Roles ADD COLUMN RoadmapSourceUrl TEXT NULL;", cancellationToken); } catch { }
        try { await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Roles ADD COLUMN DisplayOrder INTEGER NOT NULL DEFAULT 0;", cancellationToken); } catch { }
        try { await _db.Database.ExecuteSqlRawAsync("ALTER TABLE Questions ADD COLUMN ReferenceExplanation TEXT NULL;", cancellationToken); } catch { }

        await _db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS CanonicalSkills (
    Id TEXT NOT NULL CONSTRAINT PK_CanonicalSkills PRIMARY KEY,
    DisplayName TEXT NOT NULL,
    Classification TEXT NOT NULL,
    SourceKind TEXT NOT NULL,
    RoadmapSource TEXT NULL,
    RoadmapNodeId TEXT NULL,
    RoadmapLabel TEXT NULL,
    Description TEXT NULL
);

CREATE TABLE IF NOT EXISTS RoleRoadmapNodes (
    RoleId TEXT NOT NULL,
    CanonicalSkillId TEXT NOT NULL,
    Category TEXT NOT NULL,
    Importance TEXT NOT NULL,
    AssessmentEligible INTEGER NOT NULL,
    MandatoryFundamental INTEGER NOT NULL,
    IsToolkitOnly INTEGER NOT NULL,
    IsOptional INTEGER NOT NULL,
    HasQuestionCoverage INTEGER NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    CONSTRAINT PK_RoleRoadmapNodes PRIMARY KEY (RoleId, CanonicalSkillId),
    CONSTRAINT FK_RoleRoadmapNodes_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES Roles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RoleRoadmapNodes_CanonicalSkills_CanonicalSkillId FOREIGN KEY (CanonicalSkillId) REFERENCES CanonicalSkills (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS RoadmapRelationships (
    Id INTEGER NOT NULL CONSTRAINT PK_RoadmapRelationships PRIMARY KEY AUTOINCREMENT,
    SourceSkillId TEXT NOT NULL,
    TargetSkillId TEXT NOT NULL,
    RelationshipType TEXT NOT NULL,
    Rationale TEXT NULL,
    CONSTRAINT FK_RoadmapRelationships_CanonicalSkills_SourceSkillId FOREIGN KEY (SourceSkillId) REFERENCES CanonicalSkills (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_RoadmapRelationships_CanonicalSkills_TargetSkillId FOREIGN KEY (TargetSkillId) REFERENCES CanonicalSkills (Id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS LegacySkillMappings (
    LegacySkillId TEXT NOT NULL,
    CanonicalSkillId TEXT NOT NULL,
    RoleId TEXT NOT NULL,
    MappingType TEXT NOT NULL,
    Notes TEXT NULL,
    CONSTRAINT PK_LegacySkillMappings PRIMARY KEY (LegacySkillId, CanonicalSkillId),
    CONSTRAINT FK_LegacySkillMappings_CanonicalSkills_CanonicalSkillId FOREIGN KEY (CanonicalSkillId) REFERENCES CanonicalSkills (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS V3Questions (
    Id TEXT NOT NULL CONSTRAINT PK_V3Questions PRIMARY KEY,
    RoleId TEXT NOT NULL,
    CanonicalSkillId TEXT NOT NULL,
    Difficulty TEXT NOT NULL,
    QuestionType TEXT NOT NULL,
    QuestionText TEXT NOT NULL,
    ReferenceExplanation TEXT NULL,
    ExpectedSignalsJson TEXT NOT NULL,
    RubricJson TEXT NOT NULL,
    FrameworkSourceIdsJson TEXT NOT NULL,
    SourceLocator TEXT NULL,
    Provenance TEXT NOT NULL,
    AdaptationStatus TEXT NOT NULL,
    VerificationStatus TEXT NOT NULL,
    VerifiedAt TEXT NOT NULL,
    CONSTRAINT FK_V3Questions_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES Roles (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_V3Questions_CanonicalSkills_CanonicalSkillId FOREIGN KEY (CanonicalSkillId) REFERENCES CanonicalSkills (Id) ON DELETE RESTRICT
);
", cancellationToken);
    }

    private async Task SeedV3CatalogAsync(string backendDataDir, CancellationToken cancellationToken)
    {
        var v3Dir = ResolveV3DataDirectory(backendDataDir);
        if (!Directory.Exists(v3Dir))
        {
            _logger.LogWarning("V3 data directory '{V3Dir}' not found. Skipping V3 catalog seeding.", v3Dir);
            return;
        }

        var rolesFile = Path.Combine(v3Dir, "roles.json");
        var skillsFile = Path.Combine(v3Dir, "canonical-skills.json");
        var roleNodesFile = Path.Combine(v3Dir, "role-roadmap-nodes.json");
        var relsFile = Path.Combine(v3Dir, "roadmap-relationships.json");
        var mappingsFile = Path.Combine(v3Dir, "legacy-mappings.json");

        if (!File.Exists(rolesFile) || !File.Exists(skillsFile) || !File.Exists(roleNodesFile))
        {
            _logger.LogWarning("Required V3 data files missing in '{V3Dir}'. Skipping V3 catalog seeding.", v3Dir);
            return;
        }

        _logger.LogInformation("Synchronizing SQLite V3 canonical catalog from '{V3Dir}'...", v3Dir);

        // A. Seed Roles
        using var rolesDoc = JsonDocument.Parse(await File.ReadAllTextAsync(rolesFile, cancellationToken));
        var rolesArray = rolesDoc.RootElement.GetProperty("roles").EnumerateArray();
        var activeRoleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rElem in rolesArray)
        {
            var rId = rElem.GetProperty("id").GetString()!;
            activeRoleIds.Add(rId);
            var title = rElem.GetProperty("title").GetString()!;
            var desc = rElem.TryGetProperty("description", out var d) ? d.GetString() : null;
            var isPrimary = rElem.TryGetProperty("isPrimaryDemoRole", out var ip) && ip.GetBoolean();
            var sourceUrl = rElem.TryGetProperty("roadmapSourceUrl", out var su) ? su.GetString() : null;
            var order = rElem.TryGetProperty("displayOrder", out var ord) ? ord.GetInt32() : 0;

            var existingRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == rId, cancellationToken);
            if (existingRole == null)
            {
                _db.Roles.Add(new Role
                {
                    Id = rId,
                    Title = title,
                    Description = desc,
                    IsPrimaryDemoRole = isPrimary,
                    RoadmapSourceUrl = sourceUrl,
                    DisplayOrder = order
                });
            }
            else
            {
                existingRole.Title = title;
                existingRole.Description = desc;
                existingRole.IsPrimaryDemoRole = isPrimary;
                existingRole.RoadmapSourceUrl = sourceUrl;
                existingRole.DisplayOrder = order;
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        // B. Seed Canonical Skills
        using var skillsDoc = JsonDocument.Parse(await File.ReadAllTextAsync(skillsFile, cancellationToken));
        var skillsArray = skillsDoc.RootElement.GetProperty("canonicalSkills").EnumerateArray();
        var activeSkillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sElem in skillsArray)
        {
            var sId = sElem.GetProperty("id").GetString()!;
            activeSkillIds.Add(sId);
            var displayName = sElem.GetProperty("displayName").GetString()!;
            var classification = sElem.GetProperty("classification").GetString()!;
            var sourceKind = sElem.GetProperty("sourceKind").GetString()!;
            var roadmapSource = sElem.TryGetProperty("roadmapSource", out var rs) && rs.ValueKind != JsonValueKind.Null ? rs.GetString() : null;
            var roadmapNodeId = sElem.TryGetProperty("roadmapNodeId", out var rni) && rni.ValueKind != JsonValueKind.Null ? rni.GetString() : null;
            var roadmapLabel = sElem.TryGetProperty("roadmapLabel", out var rl) && rl.ValueKind != JsonValueKind.Null ? rl.GetString() : null;
            var desc = sElem.TryGetProperty("description", out var sd) && sd.ValueKind != JsonValueKind.Null ? sd.GetString() : null;

            var existingSkill = await _db.CanonicalSkills.FirstOrDefaultAsync(s => s.Id == sId, cancellationToken);
            if (existingSkill == null)
            {
                _db.CanonicalSkills.Add(new CanonicalSkill
                {
                    Id = sId,
                    DisplayName = displayName,
                    Classification = classification,
                    SourceKind = sourceKind,
                    RoadmapSource = roadmapSource,
                    RoadmapNodeId = roadmapNodeId,
                    RoadmapLabel = roadmapLabel,
                    Description = desc
                });
            }
            else
            {
                existingSkill.DisplayName = displayName;
                existingSkill.Classification = classification;
                existingSkill.SourceKind = sourceKind;
                existingSkill.RoadmapSource = roadmapSource;
                existingSkill.RoadmapNodeId = roadmapNodeId;
                existingSkill.RoadmapLabel = roadmapLabel;
                existingSkill.Description = desc;
            }
        }

        // Bidirectional: remove any CanonicalSkill not in activeSkillIds
        var allDbSkills = await _db.CanonicalSkills.ToListAsync(cancellationToken);
        foreach (var dbSkill in allDbSkills)
        {
            if (!activeSkillIds.Contains(dbSkill.Id))
            {
                _db.CanonicalSkills.Remove(dbSkill);
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        // C. Seed RoleRoadmapNodes
        using var nodesDoc = JsonDocument.Parse(await File.ReadAllTextAsync(roleNodesFile, cancellationToken));
        var nodesArray = nodesDoc.RootElement.GetProperty("roleRoadmapNodes").EnumerateArray();
        var activeJunctions = new HashSet<(string roleId, string skillId)>();

        foreach (var nElem in nodesArray)
        {
            var rId = nElem.GetProperty("roleId").GetString()!;
            var sId = nElem.GetProperty("canonicalSkillId").GetString()!;
            activeJunctions.Add((rId, sId));

            var category = nElem.GetProperty("category").GetString()!;
            var importance = nElem.GetProperty("importance").GetString()!;
            var assessEligible = nElem.GetProperty("assessmentEligible").GetBoolean();
            var mandatory = nElem.GetProperty("mandatoryFundamental").GetBoolean();
            var isToolkit = nElem.GetProperty("isToolkitOnly").GetBoolean();
            var isOptional = nElem.GetProperty("isOptional").GetBoolean();
            var hasQuestions = nElem.GetProperty("hasQuestionCoverage").GetBoolean();
            var displayOrder = nElem.GetProperty("displayOrder").GetInt32();

            var existingNode = await _db.RoleRoadmapNodes.FirstOrDefaultAsync(rn => rn.RoleId == rId && rn.CanonicalSkillId == sId, cancellationToken);
            if (existingNode == null)
            {
                _db.RoleRoadmapNodes.Add(new RoleRoadmapNode
                {
                    RoleId = rId,
                    CanonicalSkillId = sId,
                    Category = category,
                    Importance = importance,
                    AssessmentEligible = assessEligible,
                    MandatoryFundamental = mandatory,
                    IsToolkitOnly = isToolkit,
                    IsOptional = isOptional,
                    HasQuestionCoverage = hasQuestions,
                    DisplayOrder = displayOrder
                });
            }
            else
            {
                existingNode.Category = category;
                existingNode.Importance = importance;
                existingNode.AssessmentEligible = assessEligible;
                existingNode.MandatoryFundamental = mandatory;
                existingNode.IsToolkitOnly = isToolkit;
                existingNode.IsOptional = isOptional;
                existingNode.HasQuestionCoverage = hasQuestions;
                existingNode.DisplayOrder = displayOrder;
            }
        }

        // Bidirectional: remove stale junctions
        var allDbNodes = await _db.RoleRoadmapNodes.ToListAsync(cancellationToken);
        foreach (var dbNode in allDbNodes)
        {
            if (!activeJunctions.Contains((dbNode.RoleId, dbNode.CanonicalSkillId)))
            {
                _db.RoleRoadmapNodes.Remove(dbNode);
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        // D. Seed RoadmapRelationships
        if (File.Exists(relsFile))
        {
            using var relsDoc = JsonDocument.Parse(await File.ReadAllTextAsync(relsFile, cancellationToken));
            var relsArray = relsDoc.RootElement.GetProperty("relationships").EnumerateArray();
            
            var existingRels = await _db.RoadmapRelationships.ToListAsync(cancellationToken);
            _db.RoadmapRelationships.RemoveRange(existingRels);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var rElem in relsArray)
            {
                var src = rElem.GetProperty("sourceSkillId").GetString()!;
                var tgt = rElem.GetProperty("targetSkillId").GetString()!;
                var relType = rElem.TryGetProperty("relationType", out var rt) ? rt.GetString()!
                    : (rElem.TryGetProperty("relationshipType", out var rtp) ? rtp.GetString()! : "recommended-before");
                var rat = rElem.TryGetProperty("description", out var descVal) ? descVal.GetString()
                    : (rElem.TryGetProperty("rationale", out var rVal) ? rVal.GetString() : null);

                _db.RoadmapRelationships.Add(new RoadmapRelationship
                {
                    SourceSkillId = src,
                    TargetSkillId = tgt,
                    RelationshipType = relType,
                    Rationale = rat
                });
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        // E. Seed LegacySkillMappings
        if (File.Exists(mappingsFile))
        {
            using var mapDoc = JsonDocument.Parse(await File.ReadAllTextAsync(mappingsFile, cancellationToken));
            if (mapDoc.RootElement.TryGetProperty("skillMappings", out var smArray))
            {
                var activeMappings = new HashSet<(string legacyId, string canonicalId)>();
                foreach (var sm in smArray.EnumerateArray())
                {
                    var legacyId = sm.GetProperty("legacyId").GetString()!;
                    var canonicalId = sm.GetProperty("canonicalId").GetString()!;
                    var rId = sm.GetProperty("roleId").GetString()!;
                    var mType = sm.GetProperty("mappingType").GetString()!;
                    var notes = sm.TryGetProperty("notes", out var nv) ? nv.GetString() : null;

                    activeMappings.Add((legacyId, canonicalId));

                    var existingMap = await _db.LegacySkillMappings.FirstOrDefaultAsync(m => m.LegacySkillId == legacyId && m.CanonicalSkillId == canonicalId, cancellationToken);
                    if (existingMap == null)
                    {
                        _db.LegacySkillMappings.Add(new LegacySkillMapping
                        {
                            LegacySkillId = legacyId,
                            CanonicalSkillId = canonicalId,
                            RoleId = rId,
                            MappingType = mType,
                            Notes = notes
                        });
                    }
                    else
                    {
                        existingMap.RoleId = rId;
                        existingMap.MappingType = mType;
                        existingMap.Notes = notes;
                    }
                }

                var allDbMaps = await _db.LegacySkillMappings.ToListAsync(cancellationToken);
                foreach (var dbMap in allDbMaps)
                {
                    if (!activeMappings.Contains((dbMap.LegacySkillId, dbMap.CanonicalSkillId)))
                    {
                        _db.LegacySkillMappings.Remove(dbMap);
                    }
                }
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        // F. Seed V3 Questions (Frontend and Data Analyst)
        var feQuestionsFile = Path.Combine(v3Dir, "questions", "frontend.json");
        var daQuestionsFile = Path.Combine(v3Dir, "questions", "data-analyst.json");
        await SeedV3QuestionsFileAsync(feQuestionsFile, cancellationToken);
        await SeedV3QuestionsFileAsync(daQuestionsFile, cancellationToken);

        // G. Seed V3 Learning Resources
        var resourcesFile = Path.Combine(v3Dir, "learning-resources.json");
        await SeedLearningResourcesAsync(resourcesFile, cancellationToken);

        // H. Seed V3 Curated Projects
        var projectsFile = Path.Combine(v3Dir, "projects.json");
        await SeedProjectsAsync(projectsFile, cancellationToken);

        _logger.LogInformation("V3 canonical catalog synchronized successfully: {Roles} roles, {Skills} canonical skills, {Junctions} junctions.",
            activeRoleIds.Count, activeSkillIds.Count, activeJunctions.Count);
    }

    private async Task SeedV3QuestionsFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("V3 question file '{FilePath}' not found.", filePath);
            return;
        }

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(filePath, cancellationToken));
        var root = doc.RootElement;
        var questionsArray = root.GetProperty("questions").EnumerateArray();

        foreach (var q in questionsArray)
        {
            var qId = q.GetProperty("id").GetString()!;
            var qRoleId = q.GetProperty("roleId").GetString()!;
            var qSkillId = q.GetProperty("skillId").GetString()!;
            var difficulty = q.GetProperty("difficulty").GetString()!;
            var questionType = q.GetProperty("questionType").GetString()!;
            var questionText = q.GetProperty("question").GetString()!;
            var refExplanation = q.TryGetProperty("referenceExplanation", out var re) ? re.GetString() ?? "" : "";
            var provenance = q.TryGetProperty("provenance", out var pr) ? pr.GetString() ?? "concept-derived" : "concept-derived";
            var adaptationStatus = q.TryGetProperty("adaptationStatus", out var asProp) ? asProp.GetString() ?? "concept-derived" : "concept-derived";
            var verificationStatus = q.TryGetProperty("verificationStatus", out var vs) ? vs.GetString() ?? "specification-grounded" : "specification-grounded";
            var verifiedAt = q.TryGetProperty("verifiedAt", out var va) ? va.GetString() ?? "2026-09-16" : "2026-09-16";
            var sourceLocator = q.TryGetProperty("sourceLocator", out var sl) ? sl.GetString() : null;
            var expectedSignalsJson = q.GetProperty("expectedSignals").GetRawText();
            var rubricJson = q.TryGetProperty("rubric", out var rObj) ? rObj.GetRawText() : "{}";
            var frameworkSourcesJson = q.TryGetProperty("frameworkSourceIds", out var fsObj) ? fsObj.GetRawText() : "[]";

            var question = await _db.V3Questions.FirstOrDefaultAsync(qu => qu.Id == qId, cancellationToken);
            if (question == null)
            {
                question = new V3Question
                {
                    Id = qId,
                    RoleId = qRoleId,
                    CanonicalSkillId = qSkillId,
                    Difficulty = difficulty,
                    QuestionType = questionType,
                    QuestionText = questionText,
                    ReferenceExplanation = refExplanation,
                    ExpectedSignalsJson = expectedSignalsJson,
                    RubricJson = rubricJson,
                    FrameworkSourceIdsJson = frameworkSourcesJson,
                    SourceLocator = sourceLocator,
                    Provenance = provenance,
                    AdaptationStatus = adaptationStatus,
                    VerificationStatus = verificationStatus,
                    VerifiedAt = verifiedAt
                };
                _db.V3Questions.Add(question);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                question.RoleId = qRoleId;
                question.CanonicalSkillId = qSkillId;
                question.Difficulty = difficulty;
                question.QuestionType = questionType;
                question.QuestionText = questionText;
                question.ReferenceExplanation = refExplanation;
                question.ExpectedSignalsJson = expectedSignalsJson;
                question.RubricJson = rubricJson;
                question.FrameworkSourceIdsJson = frameworkSourcesJson;
                question.SourceLocator = sourceLocator;
                question.Provenance = provenance;
                question.AdaptationStatus = adaptationStatus;
                question.VerificationStatus = verificationStatus;
                question.VerifiedAt = verifiedAt;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task SeedLearningResourcesAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("V3 learning resources file '{FilePath}' not found.", filePath);
            return;
        }

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(filePath, cancellationToken));
        var root = doc.RootElement;
        var resourcesArray = root.GetProperty("learningResources").EnumerateArray();
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in resourcesArray)
        {
            var id = r.GetProperty("id").GetString()!;
            activeIds.Add(id);
            var title = r.GetProperty("title").GetString()!;
            var sourceName = r.GetProperty("sourceName").GetString()!;
            var sourceUrl = r.GetProperty("sourceUrl").GetString()!;
            var resourceType = r.GetProperty("resourceType").GetString()!;
            var canonicalSkillsJson = r.GetProperty("canonicalSkillIds").GetRawText();
            var roleIdsJson = r.GetProperty("roleIds").GetRawText();
            var level = r.GetProperty("level").GetString()!;
            var isOfficial = r.GetProperty("isOfficial").GetBoolean();
            var verificationStatus = r.GetProperty("verificationStatus").GetString()!;
            var verifiedAt = r.GetProperty("verifiedAt").GetString()!;
            var locator = r.TryGetProperty("locator", out var loc) && loc.ValueKind != JsonValueKind.Null ? loc.GetString() : null;

            var existing = await _db.LearningResources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing == null)
            {
                _db.LearningResources.Add(new LearningResource
                {
                    Id = id,
                    Title = title,
                    SourceName = sourceName,
                    SourceUrl = sourceUrl,
                    ResourceType = resourceType,
                    CanonicalSkillIdsJson = canonicalSkillsJson,
                    RoleIdsJson = roleIdsJson,
                    Level = level,
                    IsOfficial = isOfficial,
                    VerificationStatus = verificationStatus,
                    VerifiedAt = verifiedAt,
                    Locator = locator
                });
            }
            else
            {
                existing.Title = title;
                existing.SourceName = sourceName;
                existing.SourceUrl = sourceUrl;
                existing.ResourceType = resourceType;
                existing.CanonicalSkillIdsJson = canonicalSkillsJson;
                existing.RoleIdsJson = roleIdsJson;
                existing.Level = level;
                existing.IsOfficial = isOfficial;
                existing.VerificationStatus = verificationStatus;
                existing.VerifiedAt = verifiedAt;
                existing.Locator = locator;
            }
        }

        var allInDb = await _db.LearningResources.ToListAsync(cancellationToken);
        foreach (var item in allInDb)
        {
            if (!activeIds.Contains(item.Id))
            {
                _db.LearningResources.Remove(item);
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProjectsAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("V3 projects file '{FilePath}' not found.", filePath);
            return;
        }

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(filePath, cancellationToken));
        var root = doc.RootElement;
        var projectsArray = root.GetProperty("projects").EnumerateArray();
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in projectsArray)
        {
            var id = p.GetProperty("id").GetString()!;
            activeIds.Add(id);
            var title = p.GetProperty("title").GetString()!;
            var source = p.GetProperty("source").GetString()!;
            var sourceUrl = p.TryGetProperty("sourceUrl", out var su) && su.ValueKind != JsonValueKind.Null ? su.GetString() : null;
            var sourceLocator = p.TryGetProperty("sourceLocator", out var sl) && sl.ValueKind != JsonValueKind.Null ? sl.GetString() : null;
            var provenance = p.GetProperty("provenance").GetString()!;
            var roleIdsJson = p.GetProperty("roleIds").GetRawText();
            var canonicalSkillIdsJson = p.GetProperty("canonicalSkillIds").GetRawText();
            var roadmapTargetsJson = p.GetProperty("roadmapTargets").GetRawText();
            var projectType = p.GetProperty("projectType").GetString()!;
            var difficulty = p.GetProperty("difficulty").GetString()!;
            var estimatedScope = p.GetProperty("estimatedScope").GetString()!;
            var description = p.GetProperty("description").GetString()!;
            var deliverablesJson = p.GetProperty("deliverables").GetRawText();
            var evidenceRequirementsJson = p.GetProperty("evidenceRequirements").GetRawText();
            var verificationStatus = p.GetProperty("verificationStatus").GetString()!;
            var verifiedAt = p.GetProperty("verifiedAt").GetString()!;

            var existing = await _db.CuratedProjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing == null)
            {
                _db.CuratedProjects.Add(new CuratedProject
                {
                    Id = id,
                    Title = title,
                    Source = source,
                    SourceUrl = sourceUrl,
                    SourceLocator = sourceLocator,
                    Provenance = provenance,
                    RoleIdsJson = roleIdsJson,
                    CanonicalSkillIdsJson = canonicalSkillIdsJson,
                    RoadmapTargetsJson = roadmapTargetsJson,
                    ProjectType = projectType,
                    Difficulty = difficulty,
                    EstimatedScope = estimatedScope,
                    Description = description,
                    DeliverablesJson = deliverablesJson,
                    EvidenceRequirementsJson = evidenceRequirementsJson,
                    VerificationStatus = verificationStatus,
                    VerifiedAt = verifiedAt
                });
            }
            else
            {
                existing.Title = title;
                existing.Source = source;
                existing.SourceUrl = sourceUrl;
                existing.SourceLocator = sourceLocator;
                existing.Provenance = provenance;
                existing.RoleIdsJson = roleIdsJson;
                existing.CanonicalSkillIdsJson = canonicalSkillIdsJson;
                existing.RoadmapTargetsJson = roadmapTargetsJson;
                existing.ProjectType = projectType;
                existing.Difficulty = difficulty;
                existing.EstimatedScope = estimatedScope;
                existing.Description = description;
                existing.DeliverablesJson = deliverablesJson;
                existing.EvidenceRequirementsJson = evidenceRequirementsJson;
                existing.VerificationStatus = verificationStatus;
                existing.VerifiedAt = verifiedAt;
            }
        }

        var allInDb = await _db.CuratedProjects.ToListAsync(cancellationToken);
        foreach (var item in allInDb)
        {
            if (!activeIds.Contains(item.Id))
            {
                _db.CuratedProjects.Remove(item);
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
