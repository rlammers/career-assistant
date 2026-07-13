# Career Assistant Frontend

React + Vite + TypeScript frontend for the Job Application Tracker MVP.

## Project Structure

```
src/
├── components/        # React components
├── pages/            # Page-level components
├── services/         # API service layer
├── App.tsx           # Root component
└── main.tsx          # Entry point
```

## Development

Start the development server:

```bash
npm run dev
```

The app will be available at `http://localhost:5173`.

## Build

Compile for production:

```bash
npm run build
```

Output is in the `dist/` directory.

## Dependencies

- **React** – UI framework
- **React Router** – Client-side routing
- **TypeScript** – Type safety
- **Vite** – Build tool and dev server

## Features (MVP)

- Profile page (edit and save user profile)
- Job list page (view all saved jobs)
- Job detail page (view job description and analysis)
- API integration with backend

## API Integration

All API calls are made via the Fetch API to `http://localhost:5117/api` by default. Set `VITE_API_BASE_URL` to override it.

## Microsoft Entra authentication

Authentication is disabled unless `VITE_AUTH_ENABLED` is explicitly set to `true`. For local Entra testing, create an ignored `.env.local` file:

```dotenv
VITE_AUTH_ENABLED=true
VITE_ENTRA_TENANT_ID=<tenant-guid>
VITE_ENTRA_SPA_CLIENT_ID=<spa-client-guid>
VITE_ENTRA_API_SCOPE=api://<api-client-guid>/access_as_user
VITE_ENTRA_REDIRECT_URI=http://localhost:5173/
```

Production builds validate this configuration through the same parser used by the browser. `VITE_AUTH_ENABLED` accepts only an unset/empty value, `false`, or `true`; enabled builds require the tenant ID, SPA client ID, and fully qualified delegated API scope. The production Dockerfile accepts those four public values as build arguments and never accepts client secrets or other sensitive configuration. When `VITE_ENTRA_REDIRECT_URI` is unset, the browser uses `window.location.origin`, which must exactly match an Entra redirect URI registration.

These identifiers are safe frontend configuration, but environment-specific production values should be supplied by the deployment platform. The SPA uses authorization code with PKCE and does not use a client secret.

Endpoints consumed:

- `GET /api/profile`
- `POST /api/profile`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PATCH /api/jobs/{id}/status`
- `POST /api/jobs/{id}/analyse`
