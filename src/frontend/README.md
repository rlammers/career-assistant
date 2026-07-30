# Career Assistant frontend

React, TypeScript, and Vite frontend for Career Assistant. For full-stack setup, Microsoft Entra configuration, Docker Compose, and verification workflows, use the [development guide](../../docs/development.md).

## Project structure

```text
src/
|-- auth/              # Microsoft Entra configuration and token handling
|-- components/        # Shared React components
|-- pages/             # Page-level components
|-- services/          # Fetch-based API service layer
|-- App.tsx            # Root application component
`-- main.tsx           # Browser entry point
```

## Development

Install dependencies and start the development server:

```powershell
npm install
npm run dev
```

The frontend runs at `http://localhost:5173`.

API calls use `http://localhost:5117/api` by default. Set `VITE_API_BASE_URL` to override it.

## Checks

```powershell
npm run lint
npm run test
npm run build
```

The production build is written to `dist/`.

## Frontend responsibilities

- Profile creation and editing
- Job creation, editing, status tracking, analysis, and deletion
- Fetch API integration with the backend
- Microsoft Entra sign-in, sign-out, access-denied, and expired-session states
- Stable loading, validation, error, and success feedback

Authentication is disabled unless `VITE_AUTH_ENABLED=true`. Enabled builds require the tenant ID, SPA client ID, and fully qualified delegated API scope. The SPA uses authorization code with PKCE and does not use a client secret. See the [local authentication workflow](../../docs/development.md#verify-microsoft-entra-authentication-locally) for configuration and safety requirements.
