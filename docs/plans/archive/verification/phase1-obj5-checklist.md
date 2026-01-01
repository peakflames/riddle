# Phase 1 Objective 5: Refactor Data Model for Campaign Instance

**Objective:** Refactor RiddleSession entity to CampaignInstance to better model the concept of long-running D&D campaigns vs individual play sessions
**Branch:** feature/refactor-data-model-for-campaign-instance/glm-4-7-direct
**Started:** 2025-12-28
**Status:** ✅ Complete

## Objective Description
Rename and restructure the data model to better distinguish between long-running campaigns (formerly RiddleSession, now CampaignInstance) and individual play sessions (new PlaySession entity). This refactoring aligns terminology with D&D conventions and provides a foundation for the campaign session management system.

## Acceptance Criteria
- [x] RiddleSession entity renamed to CampaignInstance with updated properties
- [x] New PlaySession entity created to track individual sessions within a campaign
- [x] ISessionService renamed to ICampaignService
- [x] SessionService renamed to CampaignService with updated methods
- [x] RiddleDbContext updated with new DbSet properties
- [x] EF Core migration created and applied successfully
- [x] All Razor pages updated to use new entity names
- [x] Build passes without errors
- [x] Database schema reflects new model

## Files Modified

### Model Layer
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Models/RiddleSession.cs` | Deleted | Renamed to CampaignInstance |
| `src/Riddle.Web/Models/CampaignInstance.cs` | New | Renamed from RiddleSession, added CampaignModule property |
| `src/Riddle.Web/Models/PlaySession.cs` | New | Created new entity for tracking individual play sessions |

### Service Layer
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Services/ISessionService.cs` | Deleted | Renamed to ICampaignService |
| `src/Riddle.Web/Services/CampaignService.cs` | New | Renamed from SessionService, updated entity references |
| `src/Riddle.Web/Services/ICampaignService.cs` | New | Renamed from ISessionService, updated method signatures |
| `src/Riddle.Web/Services/DataModelTestService.cs` | Modified | Updated to use CampaignInstance instead of RiddleSession |

### Data Layer
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Data/RiddleDbContext.cs` | Modified | Updated DbSet properties: RiddleSessions → CampaignInstances, added PlaySessions |
| `src/Riddle.Web/Migrations/20251228075119_InitialCreate.cs` | New | Initial migration with CampaignInstances and PlaySessions tables |
| `src/Riddle.Web/Migrations/20251228075119_InitialCreate.Designer.cs` | New | Migration designer file |
| `src/Riddle.Web/Migrations/RiddleDbContextModelSnapshot.cs` | Modified | Updated to reflect new model |
| `src/Riddle.Web/Migrations/20251227165014_AddGameEntities.cs` | Deleted | Old migration removed |
| `src/Riddle.Web/Migrations/20251227165014_AddGameEntities.Designer.cs` | Deleted | Old migration designer removed |

### Pages Layer
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Components/Pages/Sessions/NewSession.razor` | Deleted | Renamed to NewCampaign |
| `src/Riddle.Web/Components/Pages/Campaigns/NewCampaign.razor` | New | Renamed from NewSession, updated to ICampaignService |
| `src/Riddle.Web/Components/Pages/DM/Session.razor` | Deleted | Renamed to Campaign |
| `src/Riddle.Web/Components/Pages/DM/Campaign.razor` | New | Renamed from Session, updated to use CampaignInstance |
| `src/Riddle.Web/Components/Pages/Home.razor` | Modified | Updated service injection and property references |
| `src/Riddle.Web/Components/Pages/Test/DataModelTest.razor` | Modified | Updated to use CampaignInstance entity |

### Configuration
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Program.cs` | Modified | Updated DI registration: ISessionService → ICampaignService |

## Tests Performed
- [x] Build passes (`dotnet build src/Riddle.Web`)
- [x] Migration created successfully (`dotnet ef migrations add InitialCreate`)
- [x] Database updated successfully (`dotnet ef database update`)
- [x] All compilation errors resolved
- [x] No console errors in build output

## Key Changes Summary

### Entity Renaming
- `RiddleSession` → `CampaignInstance`
  - Better reflects D&D terminology (campaign vs session)
  - Added `CampaignModule` property to specify adventure module
  - Retains all game state properties (PartyState, ActiveQuests, etc.)

### New Entity
- `PlaySession` 
  - Tracks individual sessions within a campaign
  - Properties: SessionNumber, StartedAt, EndedAt, IsActive, StartLocationId, EndLocationId, DmNotes, KeyEventsJson, Title
  - Foreign key relationship to CampaignInstance

### Service Renaming
- `ISessionService` → `ICampaignService`
- `SessionService` → `CampaignService`
- Updated method names to reflect "Campaign" terminology

### Route Changes
- `/sessions/new` → `/campaigns/new`
- `/dm/{SessionId}` → `/dm/{CampaignId}`

## Database Schema
- `CampaignInstances` table created with all JSON columns for game state
- `PlaySessions` table created with relationship to CampaignInstances
- Index on `DmUserId` for CampaignInstances
- Index on `CampaignInstanceId` for PlaySessions

## Commits
| Hash | Message |
|------|---------|
| (pending) | refactor(campaign): rename RiddleSession to CampaignInstance and add PlaySession entity |

## Blockers / Issues
None encountered. All changes implemented successfully.

## Approval
- [x] Changes reviewed by implementer
- [ ] User approved changes
- [ ] Ready for push to origin (NOT merging to develop yet)
