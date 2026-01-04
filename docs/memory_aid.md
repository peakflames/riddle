# Memory Aid - Knowledge Base Index

This directory contains lessons learned, gotchas, and patterns discovered through development of Project Riddle. When encountering a tricky problem or learning a non-obvious pattern, **document it here** to prevent future regressions.

---

## Quick Reference

| Topic | File | Key Areas |
|-------|------|-----------|
| **Blazor Server** | [blazor-server-gotchas.md](./memory_aid/blazor-server-gotchas.md) | HttpClient vs IWebHostEnvironment, .NET 10 setup, shell commands |
| **Flowbite Blazor** | [flowbite-blazor.md](./memory_aid/flowbite-blazor.md) | Component API (enums, sizes), Textarea binding, Alert params |
| **EF Core & Database** | [ef-core-patterns.md](./memory_aid/ef-core-patterns.md) | Migrations, JSON-backed properties, CharacterTemplates |
| **Blazor Components** | [blazor-components.md](./memory_aid/blazor-components.md) | [Parameter] anti-pattern, EventCallback, combat state |
| **LLM Tools** | [llm-tools.md](./memory_aid/llm-tools.md) | Name vs ID, dual data sources, enum parsing |
| **SignalR** | [signalr-patterns.md](./memory_aid/signalr-patterns.md) | Hub auth, Docker URLs, handler registration |
| **Testing** | [testing-patterns.md](./memory_aid/testing-patterns.md) | WebApplicationFactory, Playwright, E2E structure |
| **D&D Rules** | [dnd-rules.md](./memory_aid/dnd-rules.md) | Death saves, PC vs enemy at 0 HP, conditions |

---

## 🚨 CRITICAL Items (High-Impact Gotchas)

These issues cause silent failures or hard-to-debug problems. **Read before touching related code:**

1. **HttpClient Not Available in Blazor Server** → Use `IWebHostEnvironment` for wwwroot files  
   📄 [blazor-server-gotchas.md](./memory_aid/blazor-server-gotchas.md#critical-httpclient-not-available-in-blazor-server-by-default)

2. **[Parameter] Mutation Anti-Pattern** → Never modify parameters directly; use EventCallback  
   📄 [blazor-components.md](./memory_aid/blazor-components.md#critical-parameter-mutation-anti-pattern)

3. **JSON-Backed [NotMapped] Property Pattern** → Capture list once before modifying  
   📄 [ef-core-patterns.md](./memory_aid/ef-core-patterns.md#critical-json-backed-notmapped-property-pattern)

4. **Flowbite Textarea in TabPanels** → Use native `<textarea>` instead  
   📄 [flowbite-blazor.md](./memory_aid/flowbite-blazor.md#critical-textarea-binding-in-tabpanels)

5. **Persist State to Database, Not In-Memory** → Static dictionaries lost on restart  
   📄 [ef-core-patterns.md](./memory_aid/ef-core-patterns.md#critical-persist-state-to-database-not-in-memory)

6. **IServerSideBlazorBuilder Fluent API** → Chain hub options before `Build()`  
   📄 [signalr-patterns.md](./memory_aid/signalr-patterns.md#critical-iserversideblazorbuilder-fluent-api)

---

## Contributing

### When to Add an Entry
- You spent significant time debugging an issue
- The solution was non-obvious or counter-intuitive
- The pattern differs from other frameworks/projects you've used
- Future-you would benefit from a reminder

### How to Add an Entry
1. Choose the appropriate topic file in `docs/memory_aid/`
2. Add a new section with:
   - **Clear heading** describing the issue
   - **Problem/Symptom** - What went wrong
   - **Root cause** - Why it happened
   - **Solution** - The fix (with code examples if helpful)
3. If it's a critical gotcha, add it to the CRITICAL Items list above

### Entry Format
```markdown
## CRITICAL: Descriptive Title (if high-impact)
## Descriptive Title (if normal priority)

**Problem:** What happened / symptoms observed

**Root cause:** Technical explanation

**Fix:**
```csharp
// Code example
```
```

---

## Related Documentation

- **Developer Rules**: [docs/developer_rules.md](./developer_rules.md) - Coding standards, project structure, git workflow
- **SignalR Reference**: [docs/signalr/](./signalr/) - Architecture, event definitions, flow diagrams
- **Flowbite API**: [docs/flowbite_blazor_docs.md](./flowbite_blazor_docs.md) - Component API reference
