using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillProof.Api.Data.Catalog;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneI41CatalogSynchronizationHardeningTests
{
    private static (CatalogDbContext db, SqliteConnection connection, CatalogSeeder seeder) CreateIsolatedCatalog()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new CatalogDbContext(options);
        db.Database.EnsureCreated();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var seederLogger = loggerFactory.CreateLogger<CatalogSeeder>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Catalog:DataPath"] = ResolveDataDirectory()
            })
            .Build();

        var seeder = new CatalogSeeder(db, seederLogger, config);
        return (db, connection, seeder);
    }

    private static string ResolveDataDirectory()
    {
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

        return Path.Combine(AppContext.BaseDirectory, "data", "backend");
    }

    [Fact]
    public async Task A_SeedingTwice_ProducesIdenticalFinalState_Idempotent()
    {
        var (db, conn, seeder) = CreateIsolatedCatalog();
        using (conn)
        using (db)
        {
            await seeder.SeedAsync();

            var q1 = await db.Questions.CountAsync();
            var r1 = await db.Rubrics.CountAsync();
            var s1 = await db.Sources.CountAsync();
            var sk1 = await db.Skills.CountAsync();
            var sub1 = await db.Subskills.CountAsync();
            var qs1 = await db.QuestionSubskills.CountAsync();
            var qfs1 = await db.QuestionFrameworkSources.CountAsync();
            var qie1 = await db.QuestionInterviewEvidence.CountAsync();

            Assert.Equal(48, q1);
            Assert.Equal(48, r1);
            Assert.Equal(42, s1);
            Assert.Equal(22, sk1);
            Assert.Equal(57, sub1);
            Assert.Equal(63, qs1);
            Assert.Equal(103, qfs1);
            Assert.Equal(22, qie1);

            // Re-run seeding second time (no count-only bypass; executes full synchronization)
            await seeder.SeedAsync();

            Assert.Equal(q1, await db.Questions.CountAsync());
            Assert.Equal(r1, await db.Rubrics.CountAsync());
            Assert.Equal(s1, await db.Sources.CountAsync());
            Assert.Equal(sk1, await db.Skills.CountAsync());
            Assert.Equal(sub1, await db.Subskills.CountAsync());
            Assert.Equal(qs1, await db.QuestionSubskills.CountAsync());
            Assert.Equal(qfs1, await db.QuestionFrameworkSources.CountAsync());
            Assert.Equal(qie1, await db.QuestionInterviewEvidence.CountAsync());
        }
    }

    [Fact]
    public async Task B_DatabaseWith47Questions_IsRepairedTo48()
    {
        var (db, conn, seeder) = CreateIsolatedCatalog();
        using (conn)
        using (db)
        {
            await seeder.SeedAsync();
            Assert.Equal(48, await db.Questions.CountAsync());

            // Delete 1 v2 question (e.g. q-be-rust-03)
            var target = await db.Questions.FirstAsync(q => q.Id == "q-be-rust-03");
            db.Questions.Remove(target);
            await db.SaveChangesAsync();

            Assert.Equal(47, await db.Questions.CountAsync());

            // Run synchronization to repair
            await seeder.SeedAsync();

            Assert.Equal(48, await db.Questions.CountAsync());
            var restored = await db.Questions
                .Include(q => q.Rubric)
                .Include(q => q.FrameworkSources)
                .FirstOrDefaultAsync(q => q.Id == "q-be-rust-03");

            Assert.NotNull(restored);
            Assert.NotNull(restored.Rubric);
            Assert.NotEmpty(restored.FrameworkSources);
            Assert.Equal("rust", restored.SkillId);
            Assert.Equal("advanced-reasoning", restored.Difficulty);
        }
    }

    [Fact]
    public async Task C_DatabaseWithMissingQuestionFrameworkSourcesRelationship_IsRepaired()
    {
        var (db, conn, seeder) = CreateIsolatedCatalog();
        using (conn)
        using (db)
        {
            await seeder.SeedAsync();
            var initialCount = await db.QuestionFrameworkSources.CountAsync();
            Assert.Equal(103, initialCount);

            // Remove 1 Framework Source relationship
            var link = await db.QuestionFrameworkSources.FirstAsync(x => x.QuestionId == "q-be-nosql-01");
            var deletedSourceId = link.SourceId;
            db.QuestionFrameworkSources.Remove(link);
            await db.SaveChangesAsync();

            Assert.Equal(102, await db.QuestionFrameworkSources.CountAsync());

            // Re-synchronize
            await seeder.SeedAsync();

            Assert.Equal(103, await db.QuestionFrameworkSources.CountAsync());
            var exists = await db.QuestionFrameworkSources.AnyAsync(x => x.QuestionId == "q-be-nosql-01" && x.SourceId == deletedSourceId);
            Assert.True(exists);
        }
    }

    [Fact]
    public async Task D_DatabaseWithStaleV2QuestionField_IsSynchronizedFromSourceOfTruth()
    {
        var (db, conn, seeder) = CreateIsolatedCatalog();
        using (conn)
        using (db)
        {
            await seeder.SeedAsync();
            var v2Question = await db.Questions.FirstAsync(q => q.Id == "q-be-nosql-01");
            var approvedText = v2Question.QuestionText;

            // Introduce stale/outdated question text
            v2Question.QuestionText = "STALE OUTDATED QUESTION TEXT IN DEV DB";
            await db.SaveChangesAsync();

            var stale = await db.Questions.AsNoTracking().FirstAsync(q => q.Id == "q-be-nosql-01");
            Assert.Equal("STALE OUTDATED QUESTION TEXT IN DEV DB", stale.QuestionText);

            // Re-synchronize
            await seeder.SeedAsync();

            // Verify question text is repaired back to approved source of truth
            var repaired = await db.Questions.AsNoTracking().FirstAsync(q => q.Id == "q-be-nosql-01");
            Assert.Equal(approvedText, repaired.QuestionText);
        }
    }

    [Fact]
    public async Task E_CorruptedFrozenCoreQuestion_ThrowsAndIsNotSilentlyOverwritten()
    {
        var (db, conn, seeder) = CreateIsolatedCatalog();
        using (conn)
        using (db)
        {
            await seeder.SeedAsync();

            // Intentionally corrupt a frozen Core question
            var frozenQ = await db.Questions.FirstAsync(q => q.Id == "q-be-prog-01");
            frozenQ.QuestionText = "CORRUPTED FROZEN QUESTION TEXT IN SQLite";
            await db.SaveChangesAsync();

            // Seeder must detect the corruption and throw an InvalidOperationException
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
            Assert.Contains("Catalog integrity violation", ex.Message);
            Assert.Contains("q-be-prog-01", ex.Message);

            // Corrupted record in DB must NOT be silently overwritten
            var checkQ = await db.Questions.AsNoTracking().FirstAsync(q => q.Id == "q-be-prog-01");
            Assert.Equal("CORRUPTED FROZEN QUESTION TEXT IN SQLite", checkQ.QuestionText);
        }
    }

    [Fact]
    public async Task F_FinalDatabase_ContainsApprovedCatalogRelationships()
    {
        var (db, conn, seeder) = CreateIsolatedCatalog();
        using (conn)
        using (db)
        {
            await seeder.SeedAsync();

            // Verify top-level counts
            Assert.Equal(48, await db.Questions.CountAsync());
            Assert.Equal(48, await db.Rubrics.CountAsync());
            Assert.Equal(42, await db.Sources.CountAsync());
            Assert.Equal(22, await db.Skills.CountAsync());
            Assert.Equal(57, await db.Subskills.CountAsync());
            Assert.Equal(63, await db.QuestionSubskills.CountAsync());
            Assert.Equal(103, await db.QuestionFrameworkSources.CountAsync());
            Assert.Equal(22, await db.QuestionInterviewEvidence.CountAsync());

            // Verify all 48 questions have non-empty rubrics
            var questions = await db.Questions
                .Include(q => q.Rubric)
                .Include(q => q.QuestionSubskills)
                .Include(q => q.FrameworkSources)
                .ToListAsync();

            foreach (var q in questions)
            {
                Assert.NotNull(q.Rubric);
                Assert.False(string.IsNullOrWhiteSpace(q.Rubric.InsufficientEvidence));
                Assert.False(string.IsNullOrWhiteSpace(q.Rubric.Beginner));
                Assert.False(string.IsNullOrWhiteSpace(q.Rubric.Intermediate));
                Assert.False(string.IsNullOrWhiteSpace(q.Rubric.Advanced));

                Assert.NotEmpty(q.QuestionSubskills);
                Assert.NotEmpty(q.FrameworkSources);
            }
        }
    }
}
