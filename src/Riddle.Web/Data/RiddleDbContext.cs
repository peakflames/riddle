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
    /// Game sessions
    /// </summary>
    public DbSet<RiddleSession> RiddleSessions => Set<RiddleSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configure RiddleSession
        builder.Entity<RiddleSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DmUserId);
            
            // Foreign key relationship to ApplicationUser
            entity.HasOne(e => e.DmUser)
                .WithMany()
                .HasForeignKey(e => e.DmUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // String length constraints
            entity.Property(e => e.CampaignName).HasMaxLength(200);
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

            // Ignore NotMapped properties (they use the JSON columns)
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
    }
}
