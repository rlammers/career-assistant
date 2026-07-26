---
name: react-component
description: Implement or modify React components, pages, forms, client-side API interactions, and styling in this repository. Use when Codex changes frontend behavior, user interactions, layout, accessibility, or presentation in the React application.
---

# Build React changes

Follow established repository patterns first. Prefer simple function components, local state, readable code, and explicit data flow. Do not add state-management libraries or abstractions unless the existing codebase requires them or local state is clearly insufficient.

## Component design

Use function components and React hooks.

Keep state as close as practical to where it is used. Lift state only when sibling components need the same source of truth. Split a component when it has a clear reusable responsibility or has become difficult to understand and safely change; do not fragment straightforward UI into unnecessary files.

Keep API calls in the repository’s established location and pattern. Handle loading, success, empty, and error states explicitly.

## Accessibility and UI behavior

Use semantic HTML first.

- Associate labels with form controls.
- Use native buttons, links, inputs, and validation behavior where appropriate.
- Ensure interactive controls are keyboard-operable and have clear names.
- Manage focus when a dialog, navigation change, or validation error makes it necessary.
- Use ARIA only to supplement semantics that native HTML cannot provide.
- Ensure text and controls remain understandable without color alone.

Keep interactive UI visually stable. Reserve space for transient loading, success, error, and validation feedback when practical so controls and surrounding content do not jump during interaction.

Show validation and error messages close to the affected control or action. Use broader notifications only for application-wide outcomes or when an inline message would not be visible. Follow an existing repository feedback pattern when one exists.

## Implementation

Inspect the affected page, component, styles, API call, and tests. Make the
smallest coherent change and handle the loading, empty, error, and success
states it actually affects. Avoid inventing client-side data, exposing secrets,
or duplicating backend validation.

## Verification

Run the relevant frontend linting, tests, and production build before handoff when those commands are available.

For purely cosmetic changes with no behavioral effect, a production build is normally sufficient; run additional checks when the change affects interaction, accessibility, rendering logic, API behavior, or a core workflow.

Report the commands run and their outcomes, plus any material accessibility or
compatibility issue. Keep minor improvements as follow-ups rather than
expanding the current change.
