// MockStateServices.cs is intentionally empty.
// We use REAL services (IGameStateService, ICombatService) backed by an in-memory database.
// This provides more realistic testing without duplicating service logic in mocks.
//
// For unit tests that need to verify specific notification calls,
// use Moq to mock only INotificationService (see ToolExecutorNotificationTests).
