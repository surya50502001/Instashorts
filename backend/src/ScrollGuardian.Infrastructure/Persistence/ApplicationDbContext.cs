using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using System.Reflection;

namespace ScrollGuardian.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserGoal> Goals => Set<UserGoal>();
    public DbSet<UserPreference> Preferences => Set<UserPreference>();
    public DbSet<PrivacyConsent> PrivacyConsents => Set<PrivacyConsent>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentAnalysis> ContentAnalyses => Set<ContentAnalysis>();
    public DbSet<ContentSession> ContentSessions => Set<ContentSession>();
    public DbSet<ContentEvent> ContentEvents => Set<ContentEvent>();
    public DbSet<ContentCategory> ContentCategories => Set<ContentCategory>();
    public DbSet<ContentTopic> ContentTopics => Set<ContentTopic>();
    public DbSet<UserTopic> UserTopics => Set<UserTopic>();
    public DbSet<KnowledgeCheck> KnowledgeChecks => Set<KnowledgeCheck>();
    public DbSet<KnowledgeQuestion> KnowledgeQuestions => Set<KnowledgeQuestion>();
    public DbSet<KnowledgeAnswer> KnowledgeAnswers => Set<KnowledgeAnswer>();
    public DbSet<LearningProgress> LearningProgresses => Set<LearningProgress>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationInteraction> RecommendationInteractions => Set<RecommendationInteraction>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<WeeklySummary> WeeklySummaries => Set<WeeklySummary>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User indexes & configuration
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.Property(u => u.FullName).HasMaxLength(100);

            b.HasOne(u => u.Preference)
                .WithOne(p => p.User)
                .HasForeignKey<UserPreference>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(u => u.PrivacyConsent)
                .WithOne(pc => pc.User)
                .HasForeignKey<PrivacyConsent>(pc => pc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(u => u.Subscription)
                .WithOne(s => s.User)
                .HasForeignKey<Subscription>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasKey(rt => rt.Id);
            b.HasIndex(rt => rt.Token).IsUnique();
            b.HasIndex(rt => rt.UserId);
        });

        // UserGoal
        modelBuilder.Entity<UserGoal>(b =>
        {
            b.HasKey(g => g.Id);
            b.HasIndex(g => g.UserId);
            b.Property(g => g.Title).IsRequired().HasMaxLength(200);
        });

        // ContentItem & ContentAnalysis
        modelBuilder.Entity<ContentItem>(b =>
        {
            b.HasKey(ci => ci.Id);
            b.HasIndex(ci => ci.ContentHash).IsUnique();
            b.HasIndex(ci => ci.Url);
            b.Property(ci => ci.Url).IsRequired();

            b.HasOne(ci => ci.Analysis)
                .WithOne(ca => ca.ContentItem)
                .HasForeignKey<ContentAnalysis>(ca => ca.ContentItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentSession & ContentEvent
        modelBuilder.Entity<ContentSession>(b =>
        {
            b.HasKey(cs => cs.Id);
            b.HasIndex(cs => cs.UserId);
            b.HasIndex(cs => cs.StartedAtUtc);
        });

        modelBuilder.Entity<ContentEvent>(b =>
        {
            b.HasKey(ce => ce.Id);
            b.HasIndex(ce => ce.SessionId);
            b.HasIndex(ce => ce.UserId);
            b.HasIndex(ce => ce.ContentItemId);
            b.HasIndex(ce => ce.TimestampUtc);

            b.HasOne(ce => ce.Session)
                .WithMany(s => s.Events)
                .HasForeignKey(ce => ce.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentCategory & Topics
        modelBuilder.Entity<ContentCategory>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Slug).IsUnique();
        });

        modelBuilder.Entity<ContentTopic>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.Slug);
            b.HasOne(t => t.Category)
                .WithMany(c => c.Topics)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.ParentTopic)
                .WithMany(p => p.ChildTopics)
                .HasForeignKey(t => t.ParentTopicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserTopic
        modelBuilder.Entity<UserTopic>(b =>
        {
            b.HasKey(ut => ut.Id);
            b.HasIndex(ut => new { ut.UserId, ut.TopicId }).IsUnique();
        });

        // Knowledge entities
        modelBuilder.Entity<KnowledgeCheck>(b =>
        {
            b.HasKey(kc => kc.Id);
            b.HasIndex(kc => kc.UserId);
            b.HasIndex(kc => kc.TriggeredAtUtc);
        });

        modelBuilder.Entity<KnowledgeQuestion>(b =>
        {
            b.HasKey(kq => kq.Id);
            b.HasIndex(kq => kq.KnowledgeCheckId);
            b.HasOne(kq => kq.KnowledgeCheck)
                .WithMany(kc => kc.Questions)
                .HasForeignKey(kq => kq.KnowledgeCheckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeAnswer>(b =>
        {
            b.HasKey(ka => ka.Id);
            b.HasIndex(ka => ka.KnowledgeCheckId);
            b.HasIndex(ka => ka.UserId);

            b.HasOne(ka => ka.KnowledgeCheck)
                .WithMany(kc => kc.Answers)
                .HasForeignKey(ka => ka.KnowledgeCheckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningProgress>(b =>
        {
            b.HasKey(lp => lp.Id);
            b.HasIndex(lp => new { lp.UserId, lp.TopicId }).IsUnique();
        });

        // Interventions & Recommendations
        modelBuilder.Entity<Intervention>(b =>
        {
            b.HasKey(i => i.Id);
            b.HasIndex(i => i.UserId);
            b.HasIndex(i => i.TriggeredAtUtc);
        });

        modelBuilder.Entity<Recommendation>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.UserId);
            b.HasIndex(r => r.CreatedAtUtc);
        });

        modelBuilder.Entity<RecommendationInteraction>(b =>
        {
            b.HasKey(ri => ri.Id);
            b.HasIndex(ri => ri.RecommendationId);
            b.HasIndex(ri => ri.UserId);
        });

        // Summaries & Audit
        modelBuilder.Entity<DailySummary>(b =>
        {
            b.HasKey(ds => ds.Id);
            b.HasIndex(ds => new { ds.UserId, ds.Date }).IsUnique();
        });

        modelBuilder.Entity<WeeklySummary>(b =>
        {
            b.HasKey(ws => ws.Id);
            b.HasIndex(ws => new { ws.UserId, ws.WeekStartDate }).IsUnique();
        });

        modelBuilder.Entity<Device>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => new { d.UserId, d.DeviceIdentifier }).IsUnique();
        });

        modelBuilder.Entity<Subscription>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.UserId).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.UserId);
            b.HasIndex(a => a.CreatedAtUtc);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
