# CodeAtlas Implementation Status

This document records the current technical implementation state of the CodeAtlas engineering foundation and modules.

*Last Updated: October 2026 (Milestone 3A)*

---

## 1. Feature & Foundation Components

| Component | Status | Target / Tech | Details |
| :--- | :--- | :--- | :--- |
| **Solution Architecture** | ✅ Completed | .NET 10 (`net10.0`) | `CodeAtlas.sln` and `CodeAtlas.slnx` linking 10 projects |
| **API Host** | ✅ Completed | ASP.NET Core (`net10.0`) | `CodeAtlas.Api` with `/health`, `/api/v1/system/status`, and CORS |
| **Common Abstractions** | ✅ Completed | C# Class Library | `IModule`, `ModuleExtensions`, `SystemStatusResponse` |
| **Repositories Module** | 🟢 Functional (In-Memory) | Class Library (`net10.0`) | `IRepositoryStore` & `InMemoryRepositoryStore` singleton; `GET /api/v1/repositories`, `GET /api/v1/repositories/{id}`, `POST /api/v1/repositories` with input validation and case-insensitive uniqueness checks. *(Note: in-memory store loses data upon API restart)* |
| **CodeAnalysis Module** | 🟢 Functional (Read-Only Inspection) | Class Library (`net10.0`) | `IFolderInspectionService` & `FolderInspectionService`; `POST /api/v1/code-analysis/inspect` for read-only directory enumeration, extension breakdown, and evidence-based project classification (.NET, JS/TS, HTML/CSS). Enforces strict workspace root security boundary. |
| **Identity Module** | 🟡 Boundary Defined | Class Library (`net10.0`) | Module contract & boundary ready; auth/user stores pending |
| **Architecture Module** | 🟡 Boundary Defined | Class Library (`net10.0`) | Module contract & boundary ready; diagramming models pending |
| **AiAssistant Module** | 🟡 Boundary Defined | Class Library (`net10.0`) | Module contract & boundary ready; AI provider abstractions pending |
| **Jobs Module** | 🟡 Boundary Defined | Class Library (`net10.0`) | Module contract & boundary ready; task queue pending |
| **Analysis Worker** | ✅ Completed | Background Service (`net10.0`) | `CodeAtlas.Worker` scaffolded and runnable in standby |
| **Integration Test Suite** | ✅ Completed | xUnit + `Mvc.Testing` | 22 automated tests covering health, system, repository management, and folder inspection |
| **Web Frontend** | ✅ Completed | React 19 + TypeScript + Vite 6 | Real-time backend connectivity dashboard with repository management and local folder inspection view |
| **Documentation & Standards** | ✅ Completed | Markdown + EditorConfig | `.gitignore`, `.editorconfig`, `README.md`, `AGENTS.md`, ADR 0001 |

---

## 2. Important Architectural & Security Notes

> [!IMPORTANT]
> **Workspace Root Security Boundary (Milestone 3A):** Local folder inspection is restricted to the explicitly configured `Workspace:RootPath` setting in `appsettings.json` (defaults to `.`). Arbitrary directory access or path traversal attempts (such as `../`) outside this canonical boundary are rejected with `403 Forbidden` to prevent unauthorized filesystem exposure.
>
> **Read-Only Inspection Scope:** Inspection enumerates relative file paths and extensions only. It **never** reads file contents, executes repository scripts, runs builds, or modifies files.
>
> **In-Memory Repository Storage:** Repository data is maintained in-memory using `InMemoryRepositoryStore`. All registered repository records reset when the API process restarts. Database persistence (PostgreSQL / EF Core) will be added in a future milestone.

---

## 3. Planned Future Phases (Not Yet Implemented)

1. **Persistence & Data Access**:
   * PostgreSQL database integration using Entity Framework Core implementing `IRepositoryStore`.
   * Per-module migrations and schema isolation.
2. **Git Hosting Integration**:
   * Gitea client integration for repository creation, webhooks, and raw blob retrieval.
3. **AST Code Analysis & Symbols**:
   * Tree-sitter / Roslyn syntax parsing pipelines.
   * Symbol resolution and cross-file reference graphs.
4. **Controlled AI Services**:
   * LLM provider abstractions (Gemini, Claude, local models).
   * Embeddings pipeline and vector search for semantic codebase Q&A.
