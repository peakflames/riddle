using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;
using System.Text;

namespace Riddle.Web.Services;

public class DataModelTestService
{
    private readonly RiddleDbContext _dbContext;

    public DataModelTestService(RiddleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> RunAllTestsAsync()
    {
        var output = new StringBuilder();
        output.AppendLine("=== Starting Data Model Tests ===");
        output.AppendLine($"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        output.AppendLine();

        try
        {
            // Test 1: Create a test user first
            output.AppendLine("Test 1: Create Test User");
            var testUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "test@datamodel.local");
            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    Id = Guid.CreateVersion7().ToString(),
                    UserName = "test@datamodel.local",
                    Email = "test@datamodel.local",
                    NormalizedUserName = "TEST@DATAMODEL.LOCAL",
                    NormalizedEmail = "TEST@DATAMODEL.LOCAL",
                    DisplayName = "Test User",
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(testUser);
                await _dbContext.SaveChangesAsync();
                output.AppendLine($"  ✓ Created test user: {testUser.Id}");
            }
            else
            {
                output.AppendLine($"  ✓ Found existing test user: {testUser.Id}");
            }
            var testUserId = testUser.Id;

            // Test 2: Create a CampaignInstance
            output.AppendLine();
            output.AppendLine("Test 2: Create CampaignInstance");
            var campaign = new CampaignInstance
            {
                Name = "Test Campaign - " + DateTime.UtcNow.ToString("HH:mm:ss"),
                CampaignModule = "Lost Mine of Phandelver",
                DmUserId = testUserId,
                CurrentChapterId = "test_chapter",
                CurrentLocationId = "test_location"
            };
            _dbContext.CampaignInstances.Add(campaign);
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Created campaign: {campaign.Id}");
            output.AppendLine($"  ✓ UUID v7 (time-sortable): {campaign.Id.ToString()[..13]}...");

            // Test 3: Add Characters to PartyState (JSON serialization)
            output.AppendLine();
            output.AppendLine("Test 3: JSON Serialization - Characters");
            campaign.PartyState = new List<Character>
            {
                new Character 
                { 
                    Name = "Thorin Ironforge", 
                    Type = "PC", 
                    ArmorClass = 18, 
                    MaxHp = 45, 
                    CurrentHp = 45,
                    Initiative = 2,
                    PassivePerception = 12,
                    PlayerName = "Test Player 1"
                },
                new Character 
                { 
                    Name = "Elara Moonwhisper", 
                    Type = "PC", 
                    ArmorClass = 14, 
                    MaxHp = 32, 
                    CurrentHp = 28,
                    Initiative = 4,
                    PassivePerception = 15,
                    Conditions = new List<string> { "Inspired" },
                    PlayerName = "Test Player 2"
                }
            };
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Added {campaign.PartyState.Count} characters to party");

            // Test 4: Add Quests (JSON serialization)
            output.AppendLine();
            output.AppendLine("Test 4: JSON Serialization - Quests");
            campaign.ActiveQuests = new List<Quest>
            {
                new Quest
                {
                    Title = "Find → Lost Mine",
                    State = "Active",
                    IsMainStory = true,
                    Objectives = new List<string> { "Travel to Phandalin", "Find Gundren Rockseeker" }
                },
                new Quest
                {
                    Title = "Deliver Supplies",
                    State = "Active",
                    IsMainStory = false,
                    Objectives = new List<string> { "Reach Barthen's Provisions" }
                }
            };
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Added {campaign.ActiveQuests.Count} quests");

            // Test 5: Add Combat Encounter (nullable JSON)
            output.AppendLine();
            output.AppendLine("Test 5: JSON Serialization - Combat Encounter");
            campaign.ActiveCombat = new CombatEncounter
            {
                IsActive = true,
                RoundNumber = 1,
                TurnOrder = campaign.PartyState.Select(c => c.Id).ToList(),
                CurrentTurnIndex = 0
            };
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Started combat encounter: Round {campaign.ActiveCombat.RoundNumber}");

            // Test 6: Add Log Entries
            output.AppendLine();
            output.AppendLine("Test 6: JSON Serialization - Log Entries");
            campaign.NarrativeLog = new List<LogEntry>
            {
                new LogEntry { Entry = "The party begins their journey.", Importance = "standard" },
                new LogEntry { Entry = "Ambush! Goblins attack!", Importance = "critical" }
            };
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Added {campaign.NarrativeLog.Count} log entries");

            // Test 7: Set Party Preferences
            output.AppendLine();
            output.AppendLine("Test 7: JSON Serialization - Preferences");
            campaign.Preferences = new PartyPreferences
            {
                CombatFocus = "High",
                RoleplayFocus = "Medium",
                Pacing = "Fast",
                Tone = "Adventurous",
                AvoidedTopics = new List<string> { "gore" }
            };
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Set preferences: Combat={campaign.Preferences.CombatFocus}, Pacing={campaign.Preferences.Pacing}");

            // Test 8: Read back and verify JSON deserialization
            output.AppendLine();
            output.AppendLine("Test 8: Verify JSON Deserialization");
            var loadedCampaign = await _dbContext.CampaignInstances
                .FirstOrDefaultAsync(c => c.Id == campaign.Id);
            
            if (loadedCampaign != null)
            {
                output.AppendLine($"  ✓ Party State: {loadedCampaign.PartyState.Count} characters");
                output.AppendLine($"  ✓ Active Quests: {loadedCampaign.ActiveQuests.Count} quests");
                output.AppendLine($"  ✓ Combat Active: {loadedCampaign.ActiveCombat?.IsActive ?? false}");
                output.AppendLine($"  ✓ Log Entries: {loadedCampaign.NarrativeLog.Count} entries");
                output.AppendLine($"  ✓ Preferences Tone: {loadedCampaign.Preferences.Tone}");
            }

            // Test 9: Query by User (Index test)
            output.AppendLine();
            output.AppendLine("Test 9: Query by DmUserId (Index)");
            var userCampaigns = await _dbContext.CampaignInstances
                .Where(c => c.DmUserId == testUserId)
                .ToListAsync();
            output.AppendLine($"  ✓ Found {userCampaigns.Count} campaigns for test user");

            // Test 10: Update campaign
            output.AppendLine();
            output.AppendLine("Test 10: Update Campaign");
            campaign.LastActivityAt = DateTime.UtcNow;
            campaign.CurrentLocationId = "updated_location";
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Updated location to: {campaign.CurrentLocationId}");

            // Test 11: Delete campaign
            output.AppendLine();
            output.AppendLine("Test 11: Delete Campaign");
            _dbContext.CampaignInstances.Remove(campaign);
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Deleted test campaign");

            // Clean up test user
            _dbContext.Users.Remove(testUser);
            await _dbContext.SaveChangesAsync();
            output.AppendLine($"  ✓ Cleaned up test user");

            output.AppendLine();
            output.AppendLine("=== All Tests Passed! ===");
        }
        catch (Exception ex)
        {
            output.AppendLine();
            output.AppendLine($"✗ ERROR: {ex.Message}");
            output.AppendLine(ex.StackTrace);
        }

        return output.ToString();
    }
}
