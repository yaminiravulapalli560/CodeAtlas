# AGENTS.md — Guidance for AI Coding Agents

This repository serves as the engineering foundation for **CodeAtlas**, an AI-assisted Git repository hosting and code understanding platform. When contributing or modifying code in this repository, all AI coding agents must adhere to the rules below.

---

## 1. Architectural Guardrails

* **Modular Monolith Integrity**:
  * All business capabilities must reside inside their respective module (`src/backend/Modules/CodeAtlas.Modules.<Name>`).
  * Modules must reference **only** `CodeAtlas.Common`.
  * **Strictly no circular dependencies**: A module must never reference another module directly. Inter-module communication in the future will be mediated via public abstractions in `Common` or domain events.
* **Deterministic Host Registration**:
  * Do not introduce reflection-based assembly scanning for module discovery unless explicitly requested. Modules must be instantiated and registered deterministically in `src/backend/CodeAtlas.Api/Program.cs`.
* **Extractable Worker**:
  * Background tasks and compute-heavy jobs belong in `CodeAtlas.Worker` or `CodeAtlas.Modules.Jobs`, never in the API HTTP request pipeline.

---

## 2. Environment & Policy Constraints

* **No Docker Dependency**:
  * Hardware virtualization is unavailable in this environment. Never assume Docker or Docker Compose is installed or running.
  * All services, tests, and tooling must run directly on Windows via standard .NET CLI and Node.js/npm.
* **Corporate Laptop Restrictions**:
  * Never attempt to elevate privileges (e.g. `runas`, admin cmd, UAC prompts) or modify Windows security boundaries.
  * If network access or package download is blocked by enterprise proxy/firewall, stop and report the issue immediately.
* **No Real Secrets**:
  * Never commit secrets, passwords, connection strings, private keys, or API tokens.
  * Use local environment variables or template settings (`.env.example`, `appsettings.Development.json`).

---

## 3. Verification & Quality Requirements

Before concluding any work, verify all changes using the following local commands:

```powershell
# Backend validation
dotnet build CodeAtlas.sln
dotnet test tests/backend/CodeAtlas.Api.Tests/CodeAtlas.Api.Tests.csproj

# Frontend validation
cd src/frontend
npm run build
```

Do not report that a test or build passed unless you have executed it and verified its exit code and output.

