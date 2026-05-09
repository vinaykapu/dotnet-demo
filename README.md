# Project Board — .NET Core Demo App

A full-stack ASP.NET Core 8 project board with a kanban UI, REST API, and full CI/CD via GitHub Actions deploying to Render.

## Features

- **Kanban board** — drag tasks between To Do / In Progress / Done
- **Projects** — colour-coded, with per-project task filtering
- **Stats dashboard** — completion rates, priority counts
- **REST API** — full CRUD for projects and tasks
- **Swagger UI** — at `/swagger`

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/projects` | List all projects |
| POST | `/api/projects` | Create a project |
| DELETE | `/api/projects/:id` | Delete a project |
| GET | `/api/tasks` | List tasks (filter: `?projectId=` `?status=`) |
| POST | `/api/tasks` | Create a task |
| PUT | `/api/tasks/:id` | Update a task |
| DELETE | `/api/tasks/:id` | Delete a task |
| GET | `/api/stats` | Global stats |
| GET | `/health` | Health check |

## Local development

```bash
dotnet restore
dotnet run --project src/DotnetApp   # → http://localhost:8080
dotnet test
```

## Pipeline (GitHub Actions)

Every push to `main`: test → format check → build → Docker push → Render deploy

## Secrets needed

| Secret | Value |
|--------|-------|
| `RENDER_DEPLOY_HOOK_URL_DOTNET` | Render deploy hook URL |
