using Microsoft.EntityFrameworkCore;

namespace SkillProof.Api.Data.Catalog;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<RoleSkill> RoleSkills => Set<RoleSkill>();
    public DbSet<Subskill> Subskills => Set<Subskill>();
    public DbSet<SkillSubskill> SkillSubskills => Set<SkillSubskill>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionSubskill> QuestionSubskills => Set<QuestionSubskill>();
    public DbSet<Rubric> Rubrics => Set<Rubric>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<QuestionFrameworkSource> QuestionFrameworkSources => Set<QuestionFrameworkSource>();
    public DbSet<QuestionInterviewEvidence> QuestionInterviewEvidence => Set<QuestionInterviewEvidence>();
    public DbSet<CanonicalSkill> CanonicalSkills => Set<CanonicalSkill>();
    public DbSet<RoleRoadmapNode> RoleRoadmapNodes => Set<RoleRoadmapNode>();
    public DbSet<RoadmapRelationship> RoadmapRelationships => Set<RoadmapRelationship>();
    public DbSet<LegacySkillMapping> LegacySkillMappings => Set<LegacySkillMapping>();
    public DbSet<V3Question> V3Questions => Set<V3Question>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<CuratedProject> CuratedProjects => Set<CuratedProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role
        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Title).IsRequired();
        });

        // Skill
        modelBuilder.Entity<Skill>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).IsRequired();
            b.Property(s => s.SkillType).IsRequired();
        });

        // RoleSkill (Join Table)
        modelBuilder.Entity<RoleSkill>(b =>
        {
            b.HasKey(rs => new { rs.RoleId, rs.SkillId });
            b.Property(rs => rs.Category).IsRequired();

            b.HasOne(rs => rs.Role)
                .WithMany(r => r.RoleSkills)
                .HasForeignKey(rs => rs.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(rs => rs.Skill)
                .WithMany(s => s.RoleSkills)
                .HasForeignKey(rs => rs.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Subskill
        modelBuilder.Entity<Subskill>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).IsRequired();
        });

        // SkillSubskill (Join Table)
        modelBuilder.Entity<SkillSubskill>(b =>
        {
            b.HasKey(ss => new { ss.SkillId, ss.SubskillId });

            b.HasOne(ss => ss.Skill)
                .WithMany(s => s.SkillSubskills)
                .HasForeignKey(ss => ss.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(ss => ss.Subskill)
                .WithMany(s => s.SkillSubskills)
                .HasForeignKey(ss => ss.SubskillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Question
        modelBuilder.Entity<Question>(b =>
        {
            b.HasKey(q => q.Id);
            b.Property(q => q.QuestionText).IsRequired();
            b.Property(q => q.Difficulty).IsRequired();
            b.Property(q => q.QuestionType).IsRequired();
            b.Property(q => q.Provenance).IsRequired();
            b.Property(q => q.VerificationStatus).IsRequired();

            b.HasOne(q => q.Role)
                .WithMany(r => r.Questions)
                .HasForeignKey(q => q.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(q => q.Skill)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // QuestionSubskill (Join Table)
        modelBuilder.Entity<QuestionSubskill>(b =>
        {
            b.HasKey(qs => new { qs.QuestionId, qs.SubskillId });

            b.HasOne(qs => qs.Question)
                .WithMany(q => q.QuestionSubskills)
                .HasForeignKey(qs => qs.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(qs => qs.Subskill)
                .WithMany(s => s.QuestionSubskills)
                .HasForeignKey(qs => qs.SubskillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Rubric
        modelBuilder.Entity<Rubric>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.QuestionId).IsUnique();

            b.HasOne(r => r.Question)
                .WithOne(q => q.Rubric)
                .HasForeignKey<Rubric>(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Source
        modelBuilder.Entity<Source>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Publisher).IsRequired();
            b.Property(s => s.Title).IsRequired();
            b.Property(s => s.Url).IsRequired();
            b.Property(s => s.SourceType).IsRequired();
            b.Property(s => s.VerificationStatus).IsRequired();
        });

        // QuestionFrameworkSource (Join Table)
        modelBuilder.Entity<QuestionFrameworkSource>(b =>
        {
            b.HasKey(qfs => new { qfs.QuestionId, qfs.SourceId });

            b.HasOne(qfs => qfs.Question)
                .WithMany(q => q.FrameworkSources)
                .HasForeignKey(qfs => qfs.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(qfs => qfs.Source)
                .WithMany(s => s.QuestionFrameworkSources)
                .HasForeignKey(qfs => qfs.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuestionInterviewEvidence (Join Table with Metadata)
        modelBuilder.Entity<QuestionInterviewEvidence>(b =>
        {
            b.HasKey(qie => new { qie.QuestionId, qie.SourceId });
            b.Property(qie => qie.EvidenceType).IsRequired();
            b.Property(qie => qie.EvidenceStrength).IsRequired();
            b.Property(qie => qie.Notes).IsRequired();

            b.HasOne(qie => qie.Question)
                .WithMany(q => q.InterviewEvidence)
                .HasForeignKey(qie => qie.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(qie => qie.Source)
                .WithMany(s => s.QuestionInterviewEvidence)
                .HasForeignKey(qie => qie.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CanonicalSkill
        modelBuilder.Entity<CanonicalSkill>(b =>
        {
            b.HasKey(cs => cs.Id);
            b.Property(cs => cs.DisplayName).IsRequired();
            b.Property(cs => cs.Classification).IsRequired();
            b.Property(cs => cs.SourceKind).IsRequired();
        });

        // RoleRoadmapNode (Join Table)
        modelBuilder.Entity<RoleRoadmapNode>(b =>
        {
            b.HasKey(rrn => new { rrn.RoleId, rrn.CanonicalSkillId });
            b.Property(rrn => rrn.Category).IsRequired();
            b.Property(rrn => rrn.Importance).IsRequired();

            b.HasOne(rrn => rrn.Role)
                .WithMany(r => r.RoleRoadmapNodes)
                .HasForeignKey(rrn => rrn.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(rrn => rrn.CanonicalSkill)
                .WithMany(cs => cs.RoleRoadmapNodes)
                .HasForeignKey(rrn => rrn.CanonicalSkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoadmapRelationship
        modelBuilder.Entity<RoadmapRelationship>(b =>
        {
            b.HasKey(rel => rel.Id);
            b.Property(rel => rel.RelationshipType).IsRequired();

            b.HasOne(rel => rel.SourceSkill)
                .WithMany(cs => cs.OutgoingRelationships)
                .HasForeignKey(rel => rel.SourceSkillId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(rel => rel.TargetSkill)
                .WithMany(cs => cs.IncomingRelationships)
                .HasForeignKey(rel => rel.TargetSkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LegacySkillMapping
        modelBuilder.Entity<LegacySkillMapping>(b =>
        {
            b.HasKey(lsm => new { lsm.LegacySkillId, lsm.CanonicalSkillId });
            b.Property(lsm => lsm.RoleId).IsRequired();
            b.Property(lsm => lsm.MappingType).IsRequired();

            b.HasOne(lsm => lsm.CanonicalSkill)
                .WithMany(cs => cs.LegacySkillMappings)
                .HasForeignKey(lsm => lsm.CanonicalSkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // V3Question
        modelBuilder.Entity<V3Question>(b =>
        {
            b.HasKey(q => q.Id);
            b.Property(q => q.RoleId).IsRequired();
            b.Property(q => q.CanonicalSkillId).IsRequired();
            b.Property(q => q.Difficulty).IsRequired();
            b.Property(q => q.QuestionType).IsRequired();
            b.Property(q => q.QuestionText).IsRequired();

            b.HasOne(q => q.Role)
                .WithMany()
                .HasForeignKey(q => q.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(q => q.CanonicalSkill)
                .WithMany()
                .HasForeignKey(q => q.CanonicalSkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LearningResource
        modelBuilder.Entity<LearningResource>(b =>
        {
            b.HasKey(lr => lr.Id);
            b.Property(lr => lr.Title).IsRequired();
            b.Property(lr => lr.SourceName).IsRequired();
            b.Property(lr => lr.SourceUrl).IsRequired();
            b.Property(lr => lr.ResourceType).IsRequired();
            b.Property(lr => lr.Level).IsRequired();
        });

        // CuratedProject
        modelBuilder.Entity<CuratedProject>(b =>
        {
            b.HasKey(cp => cp.Id);
            b.Property(cp => cp.Title).IsRequired();
            b.Property(cp => cp.Source).IsRequired();
            b.Property(cp => cp.ProjectType).IsRequired();
            b.Property(cp => cp.Difficulty).IsRequired();
            b.Property(cp => cp.Description).IsRequired();
        });
    }
}
