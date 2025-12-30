using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Models;

namespace Riddle.Web.Data;

/// <summary>
/// Database context for Riddle application
/// </summary>
public class RiddleDbContext : IdentityDbContext<ApplicationUser>
{
    public RiddleDbContext(DbContextOptions<RiddleDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Campaign instances (root entities for campaigns)
    /// </summary>
    public DbSet<CampaignInstance> CampaignInstances => Set<CampaignInstance>();

    /// <summary>
    /// Play sessions (individual game nights)
    /// </summary>
    public DbSet<PlaySession> PlaySessions => Set<PlaySession>();

    /// <summary>
    /// Character templates (reusable characters for DMs to import into campaigns)
    /// </summary>
    public DbSet<CharacterTemplate> CharacterTemplates => Set<CharacterTemplate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configure CampaignInstance
        builder.Entity<CampaignInstance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DmUserId);
            
            // Foreign key relationship to ApplicationUser
            entity.HasOne(e => e.DmUser)
                .WithMany()
                .HasForeignKey(e => e.DmUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // String length constraints
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.CampaignModule).HasMaxLength(200);
            entity.Property(e => e.CurrentChapterId).HasMaxLength(100);
            entity.Property(e => e.CurrentLocationId).HasMaxLength(100);
            entity.Property(e => e.LastNarrativeSummary).HasMaxLength(5000);
            entity.Property(e => e.CurrentSceneImageUri).HasMaxLength(500);
            entity.Property(e => e.CurrentReadAloudText).HasMaxLength(5000);

            // JSON columns (stored as text in SQLite)
            entity.Property(e => e.CompletedMilestonesJson).HasColumnType("text");
            entity.Property(e => e.KnownNpcIdsJson).HasColumnType("text");
            entity.Property(e => e.DiscoveredLocationsJson).HasColumnType("text");
            entity.Property(e => e.PartyStateJson).HasColumnType("text");
            entity.Property(e => e.ActiveQuestsJson).HasColumnType("text");
            entity.Property(e => e.ActiveCombatJson).HasColumnType("text");
            entity.Property(e => e.NarrativeLogJson).HasColumnType("text");
            entity.Property(e => e.PreferencesJson).HasColumnType("text");
            entity.Property(e => e.ActivePlayerChoicesJson).HasColumnType("text");

            // Ignore NotMapped properties (they use JSON columns)
            entity.Ignore(e => e.CompletedMilestones);
            entity.Ignore(e => e.KnownNpcIds);
            entity.Ignore(e => e.DiscoveredLocations);
            entity.Ignore(e => e.PartyState);
            entity.Ignore(e => e.ActiveQuests);
            entity.Ignore(e => e.ActiveCombat);
            entity.Ignore(e => e.NarrativeLog);
            entity.Ignore(e => e.Preferences);
            entity.Ignore(e => e.ActivePlayerChoices);
        });

        // Configure CharacterTemplate
        builder.Entity<CharacterTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Unique constraint: Name + OwnerId (system templates vs user templates)
            // This allows both a system "Gandalf" and user-owned "Gandalf" to coexist
            entity.HasIndex(e => new { e.Name, e.OwnerId }).IsUnique();
            
            // Indexes for fast filtering
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Class);
            entity.HasIndex(e => e.Race);
            
            // Foreign key to ApplicationUser (optional - null for system templates)
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // String length constraints
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SourceFile).HasMaxLength(200);
            entity.Property(e => e.Race).HasMaxLength(100);
            entity.Property(e => e.Class).HasMaxLength(100);
            
            // JSON column (stored as text in SQLite)
            entity.Property(e => e.CharacterJson).HasColumnType("text");
            
            // Ignore NotMapped property (uses JSON column)
            entity.Ignore(e => e.Character);
            entity.Ignore(e => e.IsSystemTemplate);
            entity.Ignore(e => e.DisplayRaceClass);
            entity.Ignore(e => e.DisplayLevel);
        });

        // Configure PlaySession
        builder.Entity<PlaySession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CampaignInstanceId);
            
            // Foreign key relationship to CampaignInstance
            entity.HasOne(e => e.CampaignInstance)
                .WithMany(c => c.PlaySessions)
                .HasForeignKey(e => e.CampaignInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            // String length constraints
            entity.Property(e => e.StartLocationId).HasMaxLength(100);
            entity.Property(e => e.EndLocationId).HasMaxLength(100);
            entity.Property(e => e.DmNotes).HasMaxLength(5000);
            entity.Property(e => e.Title).HasMaxLength(200);

            // JSON column (stored as text in SQLite)
            entity.Property(e => e.KeyEventsJson).HasColumnType("text");

            // Ignore NotMapped property (uses JSON column)
            entity.Ignore(e => e.KeyEvents);
        });
    }
}
