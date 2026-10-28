# ADR 0001: Modular Monolith Architecture

* **Status**: Accepted
* **Date**: 2026-10-28
* **Deciders**: Engineering Team
* **Technical Context**: CodeAtlas Foundation Setup

---

## Context

CodeAtlas is envisioned as an integrated platform combining Git repository hosting, deep semantic code analysis, architecture visualization, and AI assistance. 

When establishing the engineering foundation, we had to choose between three primary architectural patterns:
1. **Traditional Monolith**: All domains and layers bundled in a single project or unstructured namespace hierarchy.
2. **Microservices Architecture**: Separate network services for each domain (Identity, Repositories, Analysis, AI, Jobs) orchestrated with Docker / Kubernetes.
3. **Modular Monolith**: A single deployable host with physically separated assemblies enforcing domain boundaries at compile-time, with background analysis extractable as an independent worker.

Crucially, developer laptops operate in a restricted enterprise Windows environment where hardware virtualization is disabled and Docker is unavailable.

---

## Decision

We decided to adopt a **Modular Monolith** architecture implemented in ASP.NET Core (.NET 10).

Key characteristics of our implementation:
1. **Physical Assembly Separation**: Each business module (`Identity`, `Repositories`, `CodeAnalysis`, `Architecture`, `AiAssistant`, `Jobs`) is an isolated `.csproj` class library.
2. **Strict Boundary Enforcement**: Modules reference only `CodeAtlas.Common`. Direct cross-module project references are forbidden to avoid circular dependencies and tight coupling.
3. **Explicit Composition Root**: The host application (`CodeAtlas.Api`) explicitly instantiates and registers each module without reflection or runtime assembly scanning.
4. **Extractable Background Analysis Worker**: An independent worker project (`CodeAtlas.Worker`) is structured to execute long-running analysis jobs, runnable in-process or extractable into a standalone container/process.
5. **Standardized Contracts**: Modules implement a uniform `IModule` contract for DI registration and minimal API endpoint routing.

---

## Consequences

### Positive
* **Zero-Docker Local Development**: The entire backend builds, runs, and tests locally using standard `dotnet` CLI commands without container virtualization.
* **Compile-Time Safety**: Circular dependencies between business domains are prevented by the C# compiler.
* **Low Operational Overhead**: A single deployable API host simplifies early-stage development, logging, and debugging.
* **Future Microservices Extractability**: Because module boundaries are physically enforced from day one, extracting any module into a standalone microservice or serverless worker in the future requires minimal refactoring.

### Negative / Trade-offs
* **Initial Overhead**: Managing 10 distinct project files requires slightly more initial configuration than a single flat project.
* **In-Memory Coupling**: Modules currently execute within the same CLR process, requiring disciplined adherence to boundary rules for data access and event publishing.

