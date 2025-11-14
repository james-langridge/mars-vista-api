# Decision 001: Controller-Based API vs Minimal API

**Date:** 2025-10-12
**Story:** 001 - Initialize .NET Project Structure
**Status:** Active

## Context

.NET provides two approaches for building Web APIs:
1. **Minimal APIs** - Lightweight, functional style with endpoints defined directly in Program.cs
2. **Controller-Based APIs** - Object-oriented style with endpoints organized in controller classes

The Mars Vista API will eventually have multiple endpoints for rovers, photos, cameras, manifests, and potentially advanced features like panoramas and location search. We need to choose the foundational architecture style.

## Alternatives Considered

### Option 1: Minimal API
**Pros:**
- Less boilerplate code for simple APIs
- Faster startup time (marginal)
- More functional programming style
- Good for microservices with few endpoints
- Modern .NET recommendation for simple scenarios

**Cons:**
- Endpoints scattered in Program.cs as the API grows
- Harder to organize and maintain with many endpoints
- Less tooling support (compared to controllers)
- Middleware and filters applied differently
- Testing requires more setup
- No built-in controller conventions (routing, model binding)

### Option 2: Controller-Based API (CHOSEN)
**Pros:**
- **Clear organization** - Related endpoints grouped in controller classes
- **Better for larger APIs** - Scales well as features are added
- **Familiar patterns** - Industry standard, easier for team onboarding
- **Rich attribute routing** - `[Route]`, `[ApiController]`, `[HttpGet]`, etc.
- **Built-in conventions** - Model binding, validation, response types
- **Better testability** - Controllers are just classes, easy to unit test
- **Swagger integration** - Better automatic API documentation
- **Aligns with Rails structure** - The original Mars Photo API uses controllers

**Cons:**
- Slightly more boilerplate (controller classes, attributes)
- Marginally slower startup (negligible for this use case)

## Decision

**Use Controller-Based API with the `--use-controllers` flag.**

## Reasoning

1. **Scalability** - The Mars Vista API will have multiple resource types (rovers, photos, cameras, manifests) and potentially 10+ endpoints. Controllers provide better organization.
2. **Maintainability** - Grouping related endpoints (e.g., all rover operations in `RoversController`) makes the codebase easier to navigate and maintain.
3. **Rails parity** - The original NASA Mars Photo API uses Rails controllers. Using .NET controllers provides conceptual alignment.
4. **Testability** - Controllers are plain classes, making them straightforward to unit test without HTTP concerns.
5. **Team familiarity** - Controller-based APIs are the industry standard and most .NET developers are familiar with them.
6. **Grug-approved** - Controllers provide a "cut point" that hides complexity (the implementation) behind a simple interface (the HTTP endpoints). This aligns with the deep module philosophy.

## Trade-offs Accepted

- Slightly more code (controller classes, attributes) in exchange for better organization
- Following established patterns over "modern minimalism" for long-term maintainability

## Implementation Notes

```bash
# .NET 8+ defaults to minimal API
dotnet new webapi -n MarsVista.Api -o src/MarsVista.Api

# Must use --use-controllers flag for controller-based API
dotnet new webapi -n MarsVista.Api -o src/MarsVista.Api --use-controllers
```

## References

- [Choose between controller-based and minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis)
- [Controller-based APIs in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Minimal APIs overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Comparing Minimal APIs and Controllers](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis#aspnet-core-minimal-apis-vs-apis-with-controllers)
