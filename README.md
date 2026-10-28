# CodeAtlas

CodeAtlas is an AI-assisted Git repository hosting and code understanding platform engineered as a modular monolith in ASP.NET Core (.NET 10) with a modern React + TypeScript + Vite frontend.

---

## Architecture Overview

CodeAtlas is designed with clear physical boundaries separating future business domains while keeping operational complexity minimal:

* **Host**: `CodeAtlas.Api` — ASP.NET Core Web API host providing routing, health checks, and CORS.
* **Common**: `CodeAtlas.Common` — Core contracts (`IModule`), shared response models, and module loaders.
* **Autonomous Modules**:
  * `CodeAtlas.Modules.CodeAnalysis` — Codebase inspection and static analysis boundary (`IFolderInspectionService`). Provides safe, read-only file enumeration and project classification within a secured workspace boundary.
  * `CodeAtlas.Modules.Repositories` — Repository metadata management (`IRepositoryStore`), cataloging, and future Gitea integration. *(Currently backed by an in-memory store; data resets on API restart).*
  * `CodeAtlas.Modules.Identity` — User authentication, authorization, and accounts boundary.
  * `CodeAtlas.Modules.Architecture` — Architecture visualization, C4 modeling, and boundary validation.
  * `CodeAtlas.Modules.AiAssistant` — Controlled AI service abstraction and semantic retrieval boundary.
  * `CodeAtlas.Modules.Jobs` — Asynchronous queue definitions and analysis scheduling boundary.
* **Background Worker**: `CodeAtlas.Worker` — Separately extractable background service for long-running code analysis.
* **Frontend**: `src/frontend` — React 19, TypeScript, and Vite single-page dashboard for system status, repository registration, and folder inspection.
* **Automated Tests**: `tests/backend/CodeAtlas.Api.Tests` — End-to-end integration test suite using `WebApplicationFactory`.

---

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/) (`10.0.x`)
* [Node.js](https://nodejs.org/) (`v20+` or `v24+`)
* [npm](https://www.npmjs.com/) (`v10+`)
* [Git](https://git-scm.com/)

> **Note:** Docker is **not** required to build, test, or run this foundation locally.

---

## Local Development (PowerShell)

### 1. Restore & Build Backend

```powershell
# Restore all solution projects
dotnet restore CodeAtlas.sln

# Build entire solution
dotnet build CodeAtlas.sln --no-restore
```

### 2. Run Backend Integration Tests

```powershell
dotnet test tests/backend/CodeAtlas.Api.Tests/CodeAtlas.Api.Tests.csproj --no-build
```

### 3. Run Backend API

```powershell
# Runs on http://localhost:5149
dotnet run --project src/backend/CodeAtlas.Api/CodeAtlas.Api.csproj
```

**Verifying backend endpoints:**
* Health Check: `http://localhost:5149/health`
* System Status: `http://localhost:5149/api/v1/system/status`
* Local Folder Inspection (Milestone 3A):
  * `POST http://localhost:5149/api/v1/code-analysis/inspect` (body: `{"directoryPath": "."}`)
* Repositories Management (Milestone 2):
  * `GET http://localhost:5149/api/v1/repositories` (list all repositories)
  * `GET http://localhost:5149/api/v1/repositories/{id}` (get single repository)
  * `POST http://localhost:5149/api/v1/repositories` (register repository metadata)
* Module Statuses:
  * `http://localhost:5149/api/v1/identity/status`
  * `http://localhost:5149/api/v1/repositories/status`
  * `http://localhost:5149/api/v1/code-analysis/status`
  * `http://localhost:5149/api/v1/architecture/status`
  * `http://localhost:5149/api/v1/ai-assistant/status`
  * `http://localhost:5149/api/v1/jobs/status`

> [!IMPORTANT]
> **Folder Inspection Security Boundary:** Local folder inspection is strictly constrained to the directory configured in `Workspace:RootPath` (in `appsettings.json`, default `.`). Path traversal outside this boundary is rejected with `403 Forbidden`. No file contents are read, and no code or scripts are ever executed.
>
> **Data Persistence Notice:** Repositories are currently stored in-memory using `InMemoryRepositoryStore`. All repository records reset when the API process restarts. Database persistence (PostgreSQL / EF Core) will be added in an upcoming milestone.

### 4. Run Background Analysis Worker (Optional)

```powershell
dotnet run --project src/backend/Workers/CodeAtlas.Worker/CodeAtlas.Worker.csproj
```

### 5. Install & Run Frontend

In a separate PowerShell terminal:

```powershell
cd src/frontend

# Install dependencies (first time only)
npm install

# Verify TypeScript compilation and production build
npm run build

# Start local development server (runs on http://localhost:5173)
npm run dev
```

Open `http://localhost:5173` in your browser. The dashboard displays platform connectivity, allows registering and listing repositories, and provides read-only local folder inspection.

---

## Project Structure

```
CodeAtlas/
├── .editorconfig
├── .gitignore
├── README.md
├── AGENTS.md
├── CodeAtlas.sln
├── CodeAtlas.slnx
├── docs/
│   ├── architecture.md
│   ├── implementation-status.md
│   └── adr/
│       └── 0001-modular-monolith-architecture.md
├── src/
│   ├── backend/
│   │   ├── CodeAtlas.Api/             # API Host & Startup (Workspace:RootPath configuration)
│   │   ├── CodeAtlas.Common/          # Shared interfaces & models
│   │   ├── Modules/                   # Autonomous business domains
│   │   │   ├── CodeAtlas.Modules.CodeAnalysis/   # Read-only Folder Inspection Service
│   │   │   ├── CodeAtlas.Modules.Repositories/   # Repository models & InMemoryRepositoryStore
│   │   │   ├── CodeAtlas.Modules.Identity/
│   │   │   ├── CodeAtlas.Modules.Architecture/
│   │   │   ├── CodeAtlas.Modules.AiAssistant/
│   │   │   └── CodeAtlas.Modules.Jobs/
│   │   └── Workers/
│   │       └── CodeAtlas.Worker/      # Extractable background worker
│   └── frontend/                      # React + TypeScript + Vite UI
└── tests/
    └── backend/
        └── CodeAtlas.Api.Tests/       # Integration test suite
```
