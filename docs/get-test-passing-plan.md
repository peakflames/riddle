# SignalR Integration Tests - Get Tests Passing Plan

## Summary

**Status:** ✅ ALL TESTS PASSING  
**Total Test Runtime:** ~316 seconds  
**Total Tests:** 55 tests across 6 test collections  
**Passing:** 55 tests  
**Failing:** 0 tests  

---

## Test Collection Status

| Collection                    | Runtime | Pass | Fail | Status      |
| ----------------------------- | ------- | ---- | ---- | ----------- |
| AtmosphericEventTests         | 83.3s   | 10   | 0    | ✅ ALL PASS  |
| NotificationServiceTests      | 144ms   | 13   | 0    | ✅ ALL PASS  |
| ToolExecutorNotificationTests | 789ms   | ?    | 0    | ✅ ALL PASS  |
| PlayerChoiceEventTests        | ~400ms  | 6    | 0    | ✅ ALL PASS  |
| CharacterStateEventTests      | ~60s    | 7    | 0    | ✅ ALL PASS  |
| CombatEventTests              | ~102s   | 8    | 0    | ✅ ALL PASS  |

---

## Priority Order for Fixing

Per user request, the priority order is:
1. **PlayerChoiceEventTests** (1 failing)
2. **CharacterStateEventTests** (4 failing)
3. **CombatEventTests** (6 failing)

---

## Detailed Test Results

### 1. PlayerChoiceEventTests ✅ COMPLETE

| Test Name                                          | Duration | Status  | Notes                                     |
| -------------------------------------------------- | -------- | ------- | ----------------------------------------- |
| PlayerChoices_BroadcastsToPlayers                  | ~50ms    | ✅ PASS | FIXED - renamed & corrected assertions    |
| PlayerChoices_PayloadContainsCorrectChoices        | 6.0ms    | ✅ PASS |                                           |
| PlayerChoiceSubmitted_BroadcastsToAllClients       | 4.0ms    | ✅ PASS |                                           |
| PlayerChoiceSubmitted_PayloadContainsCorrectData   | 5.0ms    | ✅ PASS |                                           |
| FullChoiceFlow_ChoicesDeliveredAndResponseReceived | 4.0ms    | ✅ PASS |                                           |
| MultiplePlayersSubmitChoices_AllReceivedByDm       | 403ms    | ✅ PASS |                                           |

**Status:** All 6 tests passing. Test was renamed from `PlayerChoices_BroadcastsToAllClients` to `PlayerChoices_BroadcastsToPlayers` because PlayerChoices only goes to players (DM originates the choices).

---

### 2. CharacterStateEventTests ✅ COMPLETE

| Test Name                                           | Duration | Status  | Notes                                    |
| --------------------------------------------------- | -------- | ------- | ---------------------------------------- |
| CharacterStateUpdated_BroadcastsToBothDmAndPlayers  | 7.0ms    | ✅ PASS |                                          |
| CharacterStateUpdated_PayloadContainsCorrectData    | ~10s     | ✅ PASS | FIXED - use `.ToString()` for Value      |
| CharacterStateUpdated_HpReductionBroadcasted        | ~8s      | ✅ PASS | FIXED - use `.ToString()` for Value      |
| CharacterStateUpdated_ConditionAddedBroadcasted     | 5.0ms    | ✅ PASS |                                          |
| CharacterStateUpdated_MultipleConditionsBroadcasted | 3.0ms    | ✅ PASS |                                          |
| CharacterStateUpdated_TempHpBroadcasted             | ~8s      | ✅ PASS | FIXED - use `.ToString()` for Value      |
| CharacterStateUpdated_UsesCorrectPropertyCasing     | ~8s      | ✅ PASS | FIXED - use `.ToString()` for Value      |

**Status:** All 7 tests passing. Root cause was JSON deserialization returning `JsonElement` for the `object Value` property. Using `.Be(30)` failed because `JsonElement` != `int`. Fix: Use `.Value?.ToString().Should().Be("30")` for type-agnostic comparison.

---

### 3. CombatEventTests (Priority: LOW) 🟠

| Test Name                                | Duration | Status | Notes                        |
| ---------------------------------------- | -------- | ------ | ---------------------------- |
| DiagnosticTest_ClientsAllBroadcast       | 1.0s     | ✅ PASS |                              |
| CombatStarted_BroadcastsToAllClients     | 2.0ms    | ✅ PASS |                              |
| CombatStarted_PayloadContainsCorrectData | 6.0ms    | ❌ FAIL | Fast failure = payload issue |
| CombatEnded_BroadcastsToAllClients       | 5.0s     | ❌ FAIL | TIMEOUT                      |
| TurnAdvanced_BroadcastsToAllClients      | 5.0s     | ❌ FAIL | TIMEOUT                      |
| TurnAdvanced_IncludesRoundNumber         | 5.0s     | ❌ FAIL | TIMEOUT                      |
| InitiativeSet_BroadcastsToAllClients     | 5.0s     | ❌ FAIL | TIMEOUT                      |
| FullCombatFlow_AllEventsDelivered        | 315ms    | ❌ FAIL | Partial failure              |

**Analysis:** Mixed failure modes:
- 4 tests timeout at 5.0s = events not being received (`CombatEnded`, `TurnAdvanced`, `InitiativeSet`)
- 1 test fails fast = payload validation issue (`CombatStarted_PayloadContainsCorrectData`)
- 1 test partial failure = some events work, others don't (`FullCombatFlow`)

---

## Fix Order Checklist

### Phase 1: PlayerChoiceEventTests ✅ COMPLETE (6/6 tests pass)
- [x] Fixed `PlayerChoices_BroadcastsToAllClients` → renamed to `PlayerChoices_BroadcastsToPlayers`
  - Root cause: Test expected DM to receive PlayerChoices, but DM sends choices (doesn't need to receive)
  - Fix: Changed assertions to verify only players receive, and DM count is 0

### Phase 2: CharacterStateEventTests ✅ COMPLETE (7/7 tests pass)
- [x] Fixed `CharacterStateUpdated_PayloadContainsCorrectData` - use `.ToString()` for Value
- [x] Fixed `CharacterStateUpdated_HpReductionBroadcasted` - use `.ToString()` for Value
- [x] Fixed `CharacterStateUpdated_TempHpBroadcasted` - use `.ToString()` for Value
- [x] Fixed `CharacterStateUpdated_UsesCorrectPropertyCasing` - use `.ToString()` for Value
  - Root cause: `CharacterStatePayload.Value` is `object`, JSON deserializes numbers as `JsonElement`
  - Fix: Compare with `.Value?.ToString().Should().Be("30")` instead of `.Value.Should().Be(30)`

### Phase 3: CombatEventTests ✅ COMPLETE (8/8 tests pass)
- [x] Root cause: Multi-argument events didn't match single-arg client handlers
- [x] Created `TurnAdvancedPayload` and `InitiativeSetPayload` records in `GameHubEvents.cs`
- [x] Updated `INotificationService` and `NotificationService` to use single payload objects
- [x] Updated `CombatService.cs` callers to use new payload signatures
- [x] Added `RegisterNoArgHandler` in `TestSignalRClient.cs` for `CombatEnded` (no-payload event)
- [x] Fixed test data assertion bug (CurrentHp/MaxHp swapped)

---

## Key Patterns from Passing Tests

**AtmosphericEventTests (all passing)** uses these patterns that work:
1. `CreateTestClientsAsync()` helper method for client setup
2. Proper handler registration before triggering events
3. `TaskCompletionSource` with 5-second timeout for async assertions
4. JSON deserialization with `PropertyNameCaseInsensitive = true`

---

## Common Failure Patterns

| Pattern               | Symptom                             | Likely Cause                                                |
| --------------------- | ----------------------------------- | ----------------------------------------------------------- |
| 5.0s timeout          | Event never received                | Wrong event name, handler not registered, or event not sent |
| Fast failure (<100ms) | Assertion failed                    | Payload property mismatch, null values, wrong casing        |
| Partial success       | Some tests pass, related tests fail | Inconsistent test setup or event naming                     |

---

---

## Test Commands

Use the Python build automation for running tests:

```bash
# Run all tests
python build.py test

# Run filtered tests (by class name)
python build.py test --filter PlayerChoiceEventTests
python build.py test --filter CharacterStateEventTests
python build.py test --filter CombatEventTests

# Run a single test
python build.py test --filter PlayerChoices_BroadcastsToPlayers
```

---

## Next Action

**Start with:** `CombatStarted_PayloadContainsCorrectData` test in `CombatEventTests.cs`

Read the test file to understand:
1. What event name is being listened for
2. How the handler is registered
3. What method triggers the event
4. Compare with passing AtmosphericEventTests pattern

Likely fixes needed for CombatEventTests:
- Timeout tests (CombatEnded, TurnAdvanced, InitiativeSet) = wrong event name or event not sent
- Fast-fail test (CombatStarted_PayloadContainsCorrectData) = probably same JsonElement issue
