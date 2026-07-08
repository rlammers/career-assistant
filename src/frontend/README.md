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

- **React 18** – UI framework
- **React Router** – Client-side routing (to be installed)
- **TypeScript** – Type safety
- **Vite** – Build tool and dev server

## Features (MVP)

- Profile page (edit and save user profile)
- Job list page (view all saved jobs)
- Job detail page (view job description and analysis)
- API integration with backend (mocked for now)

## API Integration

All API calls are made via the Fetch API to `http://localhost:5000/api` (backend URL will be configurable).

Endpoints consumed:

- `GET /api/profile`
- `POST /api/profile`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PATCH /api/jobs/{id}/status`
- `POST /api/jobs/{id}/analyse`
