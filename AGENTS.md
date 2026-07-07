AGENT.md
Project: Job Application Tracker MVP
Purpose

This system is a minimal job application tracker with AI-assisted job analysis.

It allows a user to:

Store a basic profile (skills and experience)
Paste job descriptions
Generate AI analysis comparing profile vs job
Track application status over time

This is NOT a job board, CRM, or full ATS system.

Tech Stack (fixed)
Backend
ASP.NET Core Web API (.NET 8)
Entity Framework Core
SQL Server or SQLite (SQLite preferred for MVP simplicity)
Frontend
React (Vite)
Fetch API (no Redux, no external state management)
Core Principle

Keep everything minimal, functional, and linear.

If a feature does not directly support:

storing a profile
saving a job
analysing job vs profile
tracking status

then it is out of scope.

Domain Model (MVP only)
JobApplication

Represents a single applied or saved job.

Fields:

Id (int)
Company (string)
Role (string)
Description (string)
Status (string)
CreatedAt (DateTime)

Allowed Status values:

Saved
Applied
Interview
Offer
Rejected
Profile

Single user profile (no multi-user support in MVP)

Fields:

Id (int)
Summary (string)
Skills (string) (comma-separated list)
Experience (string) (free text)
JobAnalysisResult

AI-generated analysis tied to a job.

Fields:

Id (int)
JobApplicationId (int)
MatchScore (int 0–100)
MissingSkills (string)
Strengths (string)
Suggestions (string)
CoverLetterDraft (string)
API Endpoints (MVP only)
Profile
GET /api/profile
POST /api/profile
Jobs
GET /api/jobs
GET /api/jobs/{id}
POST /api/jobs
PATCH /api/jobs/{id}/status
Analysis
POST /api/jobs/{id}/analyse

This endpoint:

Reads Profile
Reads JobApplication
Calls AI service
Stores JobAnalysisResult
Returns result
AI Service Rules

The AI service must:

Input:

Profile
Job description

Output:

Match score (0–100)
Missing skills
Strengths
Improvement suggestions
Cover letter draft

Rules:

Do not introduce external data
Do not assume missing profile info
Do not generate multiple variants
Return single structured response only
Frontend Pages (MVP only)
1. Profile Page
Edit profile
Save profile
2. Job List Page
List all jobs
Show status
Click job to view details
Button: “Analyse job”
3. Job Detail Page
View job description
View AI analysis
Update status
Explicitly Out of Scope

Do NOT implement:

authentication or multi-user support
CV PDF generation
cover letter editing UI
email sending or automation
job scraping or APIs from job boards
Kanban boards or complex UI dashboards
analytics dashboards
notifications system
advanced AI agents or multi-step reasoning pipelines
role-based access control
Data Handling Rules
Profile is single global record (no user separation)
JobApplication stores raw pasted job description only
AI outputs are stored as-is (no post-processing logic beyond storage)
No versioning system in MVP
Code Structure Guidance
Backend structure
Controllers
Models
Data (DbContext)
Services (AI service only)

No additional architecture layers.

AI Integration Constraint

Only one AI call per job analysis.

No:

chaining prompts
multi-agent flows
refinement loops
Definition of Done (MVP)

The MVP is complete when:

A profile can be created and retrieved
A job can be added via API
Jobs can be listed
A job can be analysed via AI endpoint
Analysis is stored and retrieved
Job status can be updated
React UI can perform all above actions
Stability Rule

If a feature adds complexity without improving the core loop:

Do not implement it.