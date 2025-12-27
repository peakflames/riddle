# Incremental Phase Implementation Workflow

<purpose>
This workflow provides a repeatable process for implementing project phases incrementally. It emphasizes small batches, validation through testing, and explicit user approval before any git push or merge operations.
</purpose>

---

## Prerequisites

Before starting any objective:

1. **Verify Repository State**
   ```bash
   git status
   git branch -a
   git log --oneline -5
   ```

2. **Confirm Build Passes**
   ```bash
   python build.py
   ```

3. **Review Current Assessment**
   - Read `docs/phase1_objectives_assessment.md` (or relevant phase assessment)
   - Identify the target objective and its dependencies

---

## Phase Start Procedure (One-Time per Phase)

<phase_initialization>
1. Ensure `develop` branch is current:
   ```bash
   git checkout develop
   git pull origin develop
   ```

2. Read the implementation plan:
   - `read_file` on `docs/implementation_plan.md`
   - `read_file` on `docs/phase{N}_implementation_plan.md` if exists

3. Review project rules:
   - `read_file` on `.clinerules/AGENT.md`

4. Create phase assessment document if not exists:
   - `docs/phase{N}_objectives_assessment.md`
</phase_initialization>

---

## Objective Implementation Cycle

<objective_cycle>
Repeat this cycle for each objective within a phase.

### Step 1: Create Feature Branch

```bash
git checkout develop
git pull origin develop
git checkout -b feature/phase{N}-obj{M}-{short-description}
```

**Example:** `feature/phase1-obj3-google-oauth`

### Step 2: Context Recovery (CRITICAL for LLM Implementers)

<context_recovery>
**ASSUME NO PRIOR MEMORY.** Every conversation is a fresh start.

1. **Read Assessment Document**
   ```
   read_file docs/phase{N}_objectives_assessment.md
   ```

2. **Read Relevant Source Files**
   - Use `list_files` to understand current structure
   - Use `read_file` on files you plan to modify
   - Use `search_files` to find existing patterns

3. **Verify Current State via Git**
   ```bash
   git status
   git log --oneline -10
   git diff develop --stat
   ```

4. **Run Existing Tests** (if any)
   ```bash
   python build.py
   ```
</context_recovery>

### Step 3: Implementation (Small Batches)

<implementation_steps>
For each file change:

1. **Read Current State**
   ```
   read_file {path/to/file}
   ```

2. **Make Focused Change**
   - Use `replace_in_file` for targeted edits
   - Use `write_to_file` for new files or complete rewrites

3. **Build Immediately**
   ```bash
   python build.py
   ```

4. **Verify Change**
   ```bash
   git diff {path/to/file}
   ```

5. **Stage If Successful**
   ```bash
   git add {path/to/file}
   ```

**RULE:** Never chain multiple file changes without building between each.
</implementation_steps>

### Step 4: Functional Verification

<verification>
1. **Start Application**
   ```bash
   python build.py start
   ```

2. **Test Functionality**
   - Use Playwright MCP for browser-based verification
   - Use `browser_navigate` to test pages
   - Use `browser_snapshot` to capture state
   - Check console for errors

3. **Review Logs**
   ```bash
   type riddle.log
   ```
   Or on Unix:
   ```bash
   tail -50 riddle.log
   ```

4. **Stop Application**
   ```bash
   python build.py stop
   ```
</verification>

### Step 5: Commit (Atomic, Descriptive)

<commit_protocol>
1. **Review All Changes**
   ```bash
   git status
   git diff --cached
   ```

2. **Commit with Descriptive Message**
   ```bash
   git commit -m "{type}({scope}): {description}

   - {bullet point 1}
   - {bullet point 2}

   Objective: Phase {N} Obj {M}"
   ```

   **Types:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`

3. **Present Commit to User for Review**
   - Show `git log --oneline -1`
   - Show `git diff develop --stat`
</commit_protocol>

### Step 6: Update Verification Checklist

<checklist_update>
Create or update the objective-specific verification checklist:

**File:** `docs/verification/phase{N}-obj{M}-checklist.md`

```markdown
# Phase {N} Objective {M} Verification Checklist

**Objective:** {Title}
**Branch:** feature/phase{N}-obj{M}-{desc}
**Status:** In Progress | Complete | Blocked

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Files Modified
- `path/to/file1.cs` - {what changed}
- `path/to/file2.razor` - {what changed}

## Tests Performed
- [ ] Build passes (`python build.py`)
- [ ] Application starts (`python build.py start`)
- [ ] Functional test: {description}
- [ ] No console errors

## Commits
- `{hash}` - {message}

## Blockers / Issues
- {Any issues encountered}

## Approval
- [ ] User approved changes
- [ ] Ready for push/merge
```
</checklist_update>

### Step 7: Await User Approval

<approval_gate>
**STOP AND WAIT FOR EXPLICIT USER APPROVAL**

Present to user:
1. Summary of changes made
2. Verification checklist status
3. Git diff summary
4. Request: "Ready to push to origin? Please confirm."

**DO NOT** execute `git push` or `git merge` without user confirmation.
</approval_gate>

### Step 8: Push and Merge (User-Initiated)

<push_merge>
Only after user approval:

1. **Push Feature Branch**
   ```bash
   git push origin feature/phase{N}-obj{M}-{desc}
   ```

2. **Merge to Develop** (if approved)
   ```bash
   git checkout develop
   git merge feature/phase{N}-obj{M}-{desc}
   git push origin develop
   ```

3. **Update Assessment Document**
   - Mark objective as complete in `docs/phase{N}_objectives_assessment.md`
</push_merge>

---

## LLM Implementer Guidelines

<llm_guidelines>
### Memory Rules
- **NEVER** assume you know file contents from previous conversations
- **ALWAYS** `read_file` before using `replace_in_file`
- **ALWAYS** verify changes with `git diff` after editing

### Build Discipline
- Run `python build.py` after EVERY file modification
- If build fails, fix immediately before proceeding
- Never claim completion without successful build

### Testing Requirements
- Create functional test pages/endpoints when appropriate
- Use Playwright MCP for UI verification
- Check `riddle.log` for runtime errors

### Git Usage
- Use `git status` frequently to understand state
- Use `git diff` to verify changes match intent
- Use `git log` to understand recent history
- Never `git push` without user approval

### Documentation
- Update `.clinerules/AGENT.md` with lessons learned
- Keep verification checklists current
- Record blockers immediately when encountered

### When Stuck
1. Use `git log` and `git diff` to understand what changed
2. Use `search_files` to find similar patterns in codebase
3. Read error messages carefully - they often contain the solution
4. Surface findings to user rather than guessing blindly
</llm_guidelines>

---

## Git Commands Reference

<git_reference>
### Understanding State
```bash
# Current branch and status
git status

# Recent commits
git log --oneline -10

# Changes since develop
git diff develop

# Changes in staging
git diff --cached

# File history
git log --oneline -10 -- {path/to/file}
```

### Debugging Issues
```bash
# What changed in a specific commit
git show {commit-hash}

# Find commits that touched a file
git log --oneline -- {path/to/file}

# Compare branches
git diff develop..feature/branch-name

# Find when a line was added
git blame {path/to/file}
```

### Recovery
```bash
# Discard unstaged changes to a file
git checkout -- {path/to/file}

# Unstage a file (keep changes)
git reset HEAD {path/to/file}

# Reset to last commit (careful!)
git reset --hard HEAD

# Reset to develop (lose all branch changes!)
git reset --hard develop
```
</git_reference>

---

## Verification Checklist Template

<template>
Copy this template when starting a new objective:

```markdown
# Phase {N} Objective {M}: {Title}

**Branch:** `feature/phase{N}-obj{M}-{short-desc}`
**Started:** {date}
**Status:** ⬜ Not Started | 🟡 In Progress | ✅ Complete | 🔴 Blocked

## Objective Description
{Brief description of what this objective accomplishes}

## Acceptance Criteria
- [ ] {Criterion 1}
- [ ] {Criterion 2}
- [ ] {Criterion 3}

## Dependencies
- {Objective X must be complete}
- {External: Google Cloud Console setup}

## Implementation Steps
- [ ] Step 1: {description}
- [ ] Step 2: {description}
- [ ] Step 3: {description}

## Files to Modify
| File | Change Type | Description |
|------|-------------|-------------|
| `path/to/file` | New/Modify | {what} |

## Verification Steps
- [ ] `python build.py` passes
- [ ] `python build.py start` runs without errors
- [ ] {Specific functional test}
- [ ] No console errors in browser
- [ ] `riddle.log` shows no errors

## Commits
| Hash | Message |
|------|---------|
| | |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| | |

## User Approval
- [ ] Changes reviewed by user
- [ ] Approved for push to origin
- [ ] Merged to develop
```
</template>

---

## Quick Reference

<quick_reference>
### Start Objective
```bash
git checkout develop && git pull origin develop
git checkout -b feature/phase{N}-obj{M}-{desc}
```

### Implementation Loop
```
read_file → edit → python build.py → git diff → git add
```

### Verify
```bash
python build.py start
# Test with Playwright MCP
python build.py stop
```

### Commit
```bash
git status
git diff --cached
git commit -m "type(scope): description"
```

### Await Approval → Push
```bash
# Only after user says "approved"
git push origin feature/phase{N}-obj{M}-{desc}
```
</quick_reference>
