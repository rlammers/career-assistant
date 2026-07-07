# Job Application Tracker MVP

## Purpose

Minimal job application tracker with AI-assisted job analysis.

**User capabilities:**

- Store a basic profile (skills and experience)
- Paste job descriptions
- Generate AI analysis comparing profile vs job
- Track application status over time

**What this is NOT:** job board, CRM, or full ATS system

---

## Tech Stack (fixed)

**Backend**

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQLite (preferred for MVP simplicity)

**Frontend**

- React (Vite)
- Fetch API (no Redux, no external state management)

---

## Core Principle

Keep everything minimal, functional, and linear.

✅ **In scope:** Features that directly support storing profiles, saving jobs, analyzing job vs profile, tracking status

❌ **Out of scope:** Everything else

---

## Domain Model (MVP only)

### JobApplication

Represents a single applied or saved job.

| Field          | Type     | Notes                      |
| -------------- | -------- | -------------------------- |
| Id             | int      | Primary key                |
| Company        | string   |                            |
| Role           | string   |                            |
| JobDescription | string   | Raw pasted job description |
| Status         | string   | See allowed values below   |
| CreatedAt      | DateTime |                            |

**Allowed Status values:**

- `Saved`
- `Applied`
- `Interview`
- `Offer`
- `Rejected`

### Profile

Single user profile (no multi-user support in MVP).

| Field      | Type   | Notes                |
| ---------- | ------ | -------------------- |
| Id         | int    | Primary key          |
| Summary    | string |                      |
| Skills     | string | Comma-separated list |
| Experience | string | Free text            |

### JobAnalysisResult

AI-generated analysis tied to a job.

| Field            | Type   | Notes                        |
| ---------------- | ------ | ---------------------------- |
| Id               | int    | Primary key                  |
| JobApplicationId | int    | Foreign key → JobApplication |
| MatchScore       | int    | 0–100 range                  |
| MissingSkills    | string |                              |
| Strengths        | string |                              |
| Suggestions      | string |                              |
| CoverLetterDraft | string |                              |

---

## API Endpoints (MVP only)

### Profile

```
GET    /api/profile              → Retrieve user profile
POST   /api/profile              → Create or update profile
```

### Jobs

```
GET    /api/jobs                 → List all job applications
GET    /api/jobs/{id}            → Get single job application
POST   /api/jobs                 → Create new job application
PATCH  /api/jobs/{id}/status     → Update job status
```

### Analysis

```
POST   /api/jobs/{id}/analyse    → Analyze job against profile
```

**Analysis endpoint behavior:**

1. Reads user Profile
2. Reads JobApplication by id
3. Mocks AI analysis locally
4. Stores JobAnalysisResult
5. Returns result

---

## AI Service Rules

The AI service must:

**Input:**

- Profile (user summary, skills, experience)
- Job description (raw text)

**Output:**

- Match score (0–100)
- Missing skills
- Strengths
- Improvement suggestions
- Cover letter draft

**Constraints:**

- Do not introduce external data
- Do not assume missing profile info
- Do not generate multiple variants
- Return single structured response only
- One AI call per analysis (no chaining, multi-agent flows, or refinement loops)

---

## Frontend Pages (MVP only)

### 1. Profile Page

- Edit profile
- Save profile

### 2. Job List Page

- List all jobs
- Show status
- Click job to view details
- Button: "Analyse job"

### 3. Job Detail Page

- View job description
- View AI analysis
- Update status

---

## Explicitly Out of Scope

Do NOT implement:

- Authentication or multi-user support
- CV PDF generation
- Cover letter editing UI
- Email sending or automation
- Job scraping or APIs from job boards
- Kanban boards or complex UI dashboards
- Analytics dashboards
- Notifications system
- Advanced AI agents or multi-step reasoning pipelines
- Role-based access control

---

## Data Handling Rules

- Profile is single global record (no user separation)
- JobApplication stores raw pasted job description only
- AI outputs are stored as-is (no post-processing logic beyond storage)
- No versioning system in MVP

---

## Code Structure Guidance

**Backend structure:**

- Controllers
- Models
- Data (DbContext)
- Services (AI service only)

No additional architecture layers.

---

## AI Integration Constraint

Only one AI call per job analysis.

No:

- Chaining prompts
- Multi-agent flows
- Refinement loops

---

## Definition of Done (MVP)

The MVP is complete when:

- A profile can be created and retrieved
- A job can be added via API
- Jobs can be listed
- A job can be analysed via AI endpoint
- Analysis is stored and retrieved
- Job status can be updated
- React UI can perform all above actions

---

## Stability Rule

If a feature adds complexity without improving the core loop:

**Do not implement it.**
