# CodeAtlas Architecture Documentation

## 1. Executive Summary

CodeAtlas is an AI-assisted Git repository hosting and code understanding platform. The platform is architected as a **modular monolith** in ASP.NET Core (.NET 10) paired with a modern React + TypeScript + Vite frontend.

This architecture balances high development velocity and operational simplicity on developer workstations with strict architectural boundaries that enable future extraction into independent microservices or background workers when scaling requires it.

---

## 2. Solution Architecture

```
                       ┌─────────────────────────┐
                       │   React + TS Frontend   │
                       │     (Vite on :5173)     │
                       └────────────┬────────────┘
                                    │ HTTP / REST
                                    ▼
                       ┌─────────────────────────┐
                       │     CodeAtlas.Api       │
                       │   (:5149 / net10.0)     │
                       └────────────┬────────────┘
                                    │
    ┌──────────────┬────────────────┼──────────────┬──────────────┐
    ▼              ▼                ▼              ▼              ▼
┌────────┐  ┌─────────────┐  ┌──────────────┐ ┌──────────┐ ┌─────────────┐
│Identity│  │Repositories│  │ CodeAnalysis │ │Architect.│ │ AiAssistant │
└────────┘  └─────────────┘  └──────┬───────┘ └──────────┘ └─────────────┘
                                    │                ▲
                                    ▼                │
                             ┌──────────────┐        │
                             │     Jobs     │        │
                             └──────┬───────┘        │
                                    │                │
                                    ▼                │
                       ┌─────────────────────────┐   │
                       │    CodeAtlas.Worker     ├───┘
                       │ (Background Analysis)   │
                       └─────────────────────────┘
```

---

## 3. Core Architectural Boundaries

### 3.1 Host Application (`CodeAtlas.Api`)
* Acts as the composition root for all modules.
* Configures middleware: CORS policy (scoped to trusted frontend origins), HTTPS redirection, and standard health checks.
* Exposes core operational endpoints:
  * `GET /health`: Standard liveness/readiness probe.
  * `GET /api/v1/system/status`: Returns current environment, service name, version, and the list of loaded active modules.

### 3.2 Common Abstractions (`CodeAtlas.Common`)
* Provides the module lifecycle contract:
  ```csharp
  public interface IModule
  {
      string Name { get; }
      void RegisterModule(IServiceCollection services, IConfiguration configuration);
      void MapEndpoints(IEndpointRouteBuilder endpoints);
  }
  ```
* Provides standard DTOs (`SystemStatusResponse`) and DI extension helpers (`AddConfiguredModules`, `MapConfiguredModules`).

### 3.3 Domain Modules
Each module is encapsulated in its own physical assembly to enforce compile-time separation:
1. **Identity** (`CodeAtlas.Modules.Identity`): User profiles, permissions, organization tenancies, and authentication tokens.
2. **Repositories** (`CodeAtlas.Modules.Repositories`): Repository registry, branch tracking, commit browsing, and integration hooks for Gitea.
3. **CodeAnalysis** (`CodeAtlas.Modules.CodeAnalysis`): Codebase AST parsing, symbol resolution, and dependency graph generation.
4. **Architecture** (`CodeAtlas.Modules.Architecture`): Automated architectural model generation (C4 models) and module boundary compliance.
5. **AiAssistant** (`CodeAtlas.Modules.AiAssistant`): AI model abstraction, RAG indexing, semantic query execution, and code explanation.
6. **Jobs** (`CodeAtlas.Modules.Jobs`): Background task definitions, queue abstractions, and progress tracking.

### 3.4 Background Analysis Worker (`CodeAtlas.Worker`)
* An independent `Worker Service` project utilizing `Microsoft.Extensions.Hosting.BackgroundService`.
* Designed to run either in-process during local development or extractable into a standalone process or cloud worker for compute-intensive AST analysis.

---

## 4. Cross-Cutting Design Principles

1. **Explicit Module Registration**: Modules are explicitly declared in `Program.cs`. Reflection-based assembly scanning is intentionally avoided to ensure deterministic startup and explicit dependency tracking.
2. **No Monolithic Coupling**: Modules never reference each other directly. Shared data contracts reside in `CodeAtlas.Common`.
3. **Configuration Separation**: Application configuration is managed exclusively via `appsettings.json`, environment variables, and user secrets.
4. **Zero-Docker Local Development**: Designed to restore, build, and test on standard developer hardware without requiring Docker or hypervisor virtualization.

