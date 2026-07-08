# Career Assistant

AI-assisted job application management system. For learning and demo purposes while I'm on my job search. I am using some free tier AI agents to help out, but they are limited so it forces me to understand and take ownership of the generated code. Also learning where the agents are most useful and situations I should avoid wasting tokens on.

## Purpose

A personal career management tool that helps users:

- maintain a structured professional profile
- analyse job descriptions against their experience
- generate tailored application drafts
- track job applications

## Tech Stack

Backend:

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL

Frontend:

- React
- TypeScript

## Status

MVP in development.
Backend MVP complete.
Front end MVP to do.

## Backend API

Implemented backend controllers support the following endpoints:

- `GET /api/profile`
- `POST /api/profile`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PATCH /api/jobs/{id}/status`
- `POST /api/jobs/{id}/analyse` (mocked analysis response)
