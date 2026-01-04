# Flowbite Blazor Patterns

> **Keywords:** SpinnerSize, BadgeColor, ButtonColor, Alert, Textarea, TabPanel, TableRow, EditForm
> **Related:** [Blazor Server Gotchas](./blazor-server-gotchas.md), [Blazor Components](./blazor-components.md)

This document covers Flowbite Blazor component API patterns and quirks.

---

## Component API Quick Reference

- **SpinnerSize**: Use `SpinnerSize.Sm`, `SpinnerSize.Lg`, `SpinnerSize.Xl` - NOT `SpinnerSize.Small`, `SpinnerSize.Large`, etc.
- **BadgeColor**: Requires explicit `@using Flowbite.Blazor.Enums` in some contexts. Note: `BadgeColor.Dark` does NOT exist - use `BadgeColor.Gray` for dark tones
- **ButtonColor**: Use `ButtonColor.Green` for success-style buttons, NOT `ButtonColor.Success` (which doesn't exist)
- **ButtonSize**: Use `ButtonSize.Small`, `ButtonSize.ExtraSmall` - NOT `ButtonSize.Sm`, `ButtonSize.Xs`
- **CardSize**: Use `CardSize.ExtraLarge`, not `CardSize.XLarge`

Always check Flowbite Blazor docs or reference dashboard project for exact API signatures.

---

## Alert Component

Use **`TextEmphasis`** and **`Text`** parameters, NOT ChildContent:

```razor
@* ✅ CORRECT - Use TextEmphasis and Text parameters *@
<Alert Color="AlertColor.Failure" TextEmphasis="Error!" Text="@_errorMessage" />

@* ❌ WRONG - ChildContent renders empty green box *@
<Alert Color="AlertColor.Success">@_successMessage</Alert>
```

Color values: `AlertColor.Failure` (red), `AlertColor.Success` (green), `AlertColor.Info` (blue), `AlertColor.Warning` (yellow)

---

## EditForm Context Conflicts

When EditForm is inside AuthorizeView, add `Context="editContext"` parameter to EditForm to avoid context name collision.

---

## Icon Components

Use Flowbite icon components (e.g., `<BookOpenIcon Class="w-5 h-5" />`) from `Flowbite.Blazor.Icons` namespace.

---

## TableRow onclick Limitation

Flowbite `TableRow` component does NOT support `@onclick` event handlers - use click handlers on inner elements (e.g., checkbox, button) instead.

---

## CRITICAL: Textarea Binding in TabPanels

**Flowbite Blazor `<Textarea>` does NOT bind correctly** when placed inside `<TabPanel>` components. Both `@bind-Value` and explicit `Value`/`ValueChanged` patterns fail - the model values remain empty/null when the form submits.

**Workaround:** Use native HTML `<textarea>` with `@bind` and Tailwind classes for styling:

```razor
<!-- ❌ BROKEN - Flowbite Textarea in TabPanel -->
<Textarea Id="personality" @bind-Value="_model.PersonalityTraits" Rows="2" />

<!-- ✅ WORKS - Native HTML textarea with @bind -->
<textarea id="personality" @bind="_model.PersonalityTraits" rows="2" 
  class="block p-2.5 w-full text-sm text-gray-900 bg-gray-50 rounded-lg border border-gray-300 
         focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-700 dark:border-gray-600 
         dark:placeholder-gray-400 dark:text-white dark:focus:ring-primary-500 dark:focus:border-primary-500">
</textarea>
```

Note: Flowbite Textarea works fine in non-TabPanel contexts (like simple modals or forms).
