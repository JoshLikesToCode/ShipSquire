# Claude.md - ShipSquire Operating Manual for AI Assistance

This document serves as the project operating manual for AI-assisted development. It captures the architecture, conventions, and workflows to ensure consistency and quality.

## Project Overview

**ShipSquire** is a developer ops portal combining Runbooks and Incidents management. Week 1 focuses on Services + Runbooks + Sections with full-stack CRUD capabilities.

**Tech Stack:**
- Backend: .NET 8 Minimal API + EF Core + PostgreSQL
- Frontend: React + TypeScript + Vite
- Infrastructure: Docker Compose
- CI/CD: GitHub Actions

## Repository Structure

```
/
├── api/                              # Backend monorepo
│   ├── ShipSquire.Domain/           # Pure domain layer
│   ├── ShipSquire.Application/      # Use cases & DTOs
│   ├── ShipSquire.Infrastructure/   # EF Core & repositories
│   ├── ShipSquire.Api/              # Minimal API endpoints
│   ├── ShipSquire.Tests.Unit/       # Unit tests
│   └── ShipSquire.Tests.Integration/ # Integration tests
├── web/                             # React frontend
│   └── src/
│       ├── pages/                   # Route pages
│       ├── components/              # Reusable components
│       ├── services/                # API client
│       └── types/                   # TypeScript definitions
├── infra/                           # Infrastructure (currently empty)
├── .github/workflows/               # CI/CD workflows
├── docker-compose.yml               # Production compose
├── docker-compose.dev.yml           # Dev-only Postgres
├── Makefile                         # Convenience commands
├── README.md                        # User-facing docs
└── Claude.md                        # This file
```

## Clean Architecture Principles

### Dependency Rules

1. **Domain** depends on nothing. Pure entities and interfaces.
2. **Application** depends only on Domain. Contains DTOs, services, business logic.
3. **Infrastructure** depends on Application + Domain. EF Core DbContext, repositories.
4. **Api** depends on Application + Infrastructure. Minimal API endpoints, middleware.

### Layer Responsibilities

**Domain (`ShipSquire.Domain`)**
- Entities: User, Service, Runbook, RunbookSection, RunbookVariable, Incident, etc.
- BaseEntity: Common Id, CreatedAt, UpdatedAt
- Interfaces: IRepository, IUserRepository, IServiceRepository, IRunbookRepository

**Application (`ShipSquire.Application`)**
- DTOs: Request/Response records (ServiceRequest, ServiceResponse, etc.)
- Services: ServiceService, RunbookService, RunbookSectionService, RunbookVariableService
- ICurrentUser: Abstraction for current authenticated user

**Infrastructure (`ShipSquire.Infrastructure`)**
- ShipSquireDbContext: EF Core context with entity configuration
- Repositories: Generic Repository<T>, specialized repositories
- Migrations: EF Core migrations

**Api (`ShipSquire.Api`)**
- Endpoints: Static endpoint mapping classes (HealthEndpoints, ServiceEndpoints, etc.)
- Middleware: CurrentUserMiddleware, CurrentUserService
- Program.cs: DI composition, middleware pipeline

## Coding Conventions

### .NET Backend

**General:**
- Use C# 12 features (records, primary constructors, top-level statements)
- Nullable reference types enabled
- Implicit usings enabled
- Use `DateTimeOffset` for all timestamps
- Use `Guid` for all IDs

**Naming:**
- Entities: PascalCase (User, Service, Runbook)
- DTOs: PascalCase with suffix (ServiceRequest, ServiceResponse)
- Services: PascalCase with "Service" suffix (ServiceService, RunbookService)
- Interfaces: PascalCase with "I" prefix (IRepository, ICurrentUser)
- Private fields: _camelCase

**DTO Conventions:**
- Use `record` types for all DTOs
- Request DTOs for incoming data (ServiceRequest, RunbookRequest)
- Response DTOs for outgoing data (ServiceResponse, RunbookResponse)
- Use nullable types (`string?`) for optional fields
- Use ISO 8601 date strings in responses

**Endpoint Conventions:**
- Group endpoints by resource in static classes (e.g., ServiceEndpoints)
- Use `MapGet`, `MapPost`, `MapPatch`, `MapDelete`
- Name routes with `WithName("GetService")`
- Tag routes with `WithTags("Services")`
- Document responses with `Produces<T>(statusCode)`
- Return appropriate HTTP status codes:
  - 200 OK: Successful GET/PATCH
  - 201 Created: Successful POST
  - 204 No Content: Successful DELETE
  - 404 Not Found: Resource doesn't exist or user doesn't own it

**Repository Conventions:**
- Generic Repository<T> for basic CRUD
- Specialized repositories for complex queries (e.g., IServiceRepository)
- Always use async methods with CancellationToken
- Repositories call SaveChangesAsync automatically

**Current User:**
- All queries MUST filter by current user ID
- Return 404 when accessing resources not owned by current user
- Never expose resources across users

### React Frontend

**General:**
- Use TypeScript strict mode
- Use functional components with hooks
- Use React Router for navigation
- Keep components simple and focused

**Naming:**
- Components: PascalCase (ServiceListPage, RunbookEditorPage)
- Hooks: camelCase with "use" prefix (useState, useEffect)
- Props: camelCase
- CSS classes: kebab-case (page-header, btn-primary)

**API Client:**
- Centralized in `src/services/api.ts`
- All requests include `X-User-Email` header
- Base URL from `VITE_API_BASE_URL` env var
- Typed responses using types from `src/types/index.ts`

**State Management:**
- Use useState for component state
- useEffect for data fetching
- No external state library (Week 1)

**Error Handling:**
- Display errors in UI with `.error` class
- Log errors to console
- Graceful degradation

## Testing Strategy

### Backend Tests

**Unit Tests (`ShipSquire.Tests.Unit`)**
- Target: Domain entities, application services
- Framework: xUnit + FluentAssertions
- Focus: Business logic, validation, transformations
- No database dependencies

**Integration Tests (`ShipSquire.Tests.Integration`)**
- Target: API endpoints
- Framework: xUnit + WebApplicationFactory + InMemory DB
- Focus: Full request/response cycle, user scoping
- Test patterns:
  - Happy path (200, 201, 204)
  - Not found (404) when wrong user
  - Validation errors

**Running Tests:**
```bash
cd api && dotnet test
```

### Frontend Tests

**Unit/Component Tests**
- Framework: Vitest + React Testing Library
- Focus: Component rendering, user interactions
- Mock API calls

**Running Tests:**
```bash
cd web && npm test
```

## Common Development Tasks

### Adding a New Entity

1. **Create Domain Entity** in `ShipSquire.Domain/Entities/`
   - Inherit from `BaseEntity`
   - Add properties with proper types
   - Add navigation properties

2. **Add to DbContext** in `ShipSquire.Infrastructure/Persistence/ShipSquireDbContext.cs`
   - Add `DbSet<T>` property
   - Configure in `OnModelCreating` (constraints, indexes, relationships)

3. **Create Migration**
   ```bash
   dotnet ef migrations add AddEntityName --project ShipSquire.Infrastructure --startup-project ShipSquire.Api
   ```

4. **Add DTOs** in `ShipSquire.Application/DTOs/`
   - Create `EntityRequest` record
   - Create `EntityResponse` record

5. **Create Repository Interface** in `ShipSquire.Domain/Interfaces/` (if needed)
6. **Implement Repository** in `ShipSquire.Infrastructure/Repositories/`
7. **Create Application Service** in `ShipSquire.Application/Services/`
8. **Add Endpoints** in `ShipSquire.Api/Endpoints/`
9. **Register in DI** in `Program.cs`
10. **Add Tests** in `ShipSquire.Tests.Unit/` and `ShipSquire.Tests.Integration/`

### Adding a New API Endpoint

1. **Add method to endpoint class** (e.g., `ServiceEndpoints.cs`)
   ```csharp
   app.MapGet("/api/services/{id:guid}", async (Guid id, ServiceService service) => {
       var result = await service.GetByIdAsync(id);
       return result == null ? Results.NotFound() : Results.Ok(result);
   })
   .WithName("GetService")
   .WithTags("Services")
   .Produces<ServiceResponse>(200)
   .Produces(404);
   ```

2. **Ensure service method exists** in Application layer
3. **Add integration test** in `Endpoints/` directory
4. **Update Swagger tags** if needed

### Adding a Database Migration

```bash
# From project root
cd api

# Create migration
dotnet ef migrations add MigrationName --project ShipSquire.Infrastructure --startup-project ShipSquire.Api

# Apply migration (local dev)
dotnet ef database update --project ShipSquire.Infrastructure --startup-project ShipSquire.Api

# Rollback last migration
dotnet ef migrations remove --project ShipSquire.Infrastructure --startup-project ShipSquire.Api
```

### Adding a React Page

1. **Create page component** in `web/src/pages/`
2. **Add route** in `App.tsx`
3. **Add navigation link** if needed
4. **Create any required API client methods** in `api.ts`
5. **Add TypeScript types** if needed in `types/index.ts`
6. **Add basic test** in `web/src/test/`

## 12-Factor App Compliance

1. **Codebase**: Single monorepo, version-controlled
2. **Dependencies**: Explicitly declared (csproj, package.json)
3. **Config**: Environment variables (DATABASE_URL, VITE_API_BASE_URL)
4. **Backing Services**: Postgres as attached resource
5. **Build/Release/Run**: Separate stages in Docker
6. **Processes**: Stateless API
7. **Port Binding**: Self-contained (Kestrel, Vite)
8. **Concurrency**: Scale via container instances
9. **Disposability**: Fast startup/shutdown
10. **Dev/Prod Parity**: Docker Compose for both
11. **Logs**: Stdout (no log files)
12. **Admin Processes**: Migrations via startup code

## Environment Variables Reference

### API (Backend)
- `DATABASE_URL`: Postgres connection string
- `ASPNETCORE_ENVIRONMENT`: Development | Production
- `ASPNETCORE_URLS`: Binding URLs (default: http://+:5000)

### Web (Frontend)
- `VITE_API_BASE_URL`: API base URL (default: http://localhost:5000)
- `VITE_USER_EMAIL`: Default user email (default: josh@local)

## Docker Commands

```bash
# Full stack
docker compose up -d              # Start all services
docker compose down               # Stop all services
docker compose logs -f            # Follow logs
docker compose restart api        # Restart just the API

# Dev Postgres only
docker compose -f docker-compose.dev.yml up -d

# Clean up
docker compose down -v            # Remove volumes
docker volume prune               # Remove all unused volumes

# Rebuild
docker compose build              # Rebuild all images
docker compose up -d --build      # Rebuild and restart
```

## Common Pitfalls

1. **Forgot to filter by current user**
   - Always check `UserId` matches `_currentUser.UserId`
   - Return 404 for unauthorized access

2. **EF Core not tracking changes**
   - Use `UpdateAsync` from repository
   - Don't forget `UpdatedAt = DateTimeOffset.UtcNow`

3. **Migration conflicts**
   - Always pull latest before creating migrations
   - Coordinate with team on schema changes

4. **Docker port conflicts**
   - Check if ports 5432, 5000, 3000 are available
   - Use `docker compose down` to clean up

5. **CORS issues**
   - API has AllowAll CORS policy for development
   - Update for production

6. **Cascade deletes**
   - Service deletes cascade to Runbooks, Sections, Variables
   - Be careful when deleting parent resources

## Definition of Done Checklist

When completing a task, ensure:

- [ ] Code compiles without warnings
- [ ] Tests added and passing (unit + integration where applicable)
- [ ] Endpoints follow naming conventions
- [ ] DTOs use record types
- [ ] Current user filtering applied
- [ ] Migration created (if schema changed)
- [ ] Docker build succeeds
- [ ] CI passes
- [ ] Documentation updated (README, this file)
- [ ] No console errors in browser
- [ ] API returns proper status codes
- [ ] TypeScript has no errors

## Debugging Tips

**API:**
```bash
# Run with detailed EF Core logging
ASPNETCORE_ENVIRONMENT=Development dotnet run --project api/ShipSquire.Api

# Check migration status
dotnet ef migrations list --project ShipSquire.Infrastructure --startup-project ShipSquire.Api

# Generate SQL for migration
dotnet ef migrations script --project ShipSquire.Infrastructure --startup-project ShipSquire.Api
```

**Web:**
```bash
# Run with API proxy
cd web && npm run dev

# Check TypeScript errors
npm run build
```

**Database:**
```bash
# Connect to Postgres
docker exec -it shipSquire-postgres psql -U postgres -d shipSquire

# List tables
\dt

# Describe table
\d "Services"
```

## Next Steps (Week 2+)

- Implement GitHub OAuth
- Add user profile management
- Integrate with GitHub API for repo metadata
- Enhanced incident management UI
- Timeline entries for incidents
- Postmortem workflows

## Notes for AI Assistants

When working on this codebase:

1. **Always filter by current user** - This is a multi-tenant system
2. **Follow Clean Architecture** - Respect layer boundaries
3. **Use async/await** - All I/O operations should be async
4. **Return proper status codes** - 200, 201, 204, 404 as appropriate
5. **Write tests** - Both unit and integration tests
6. **Update migrations** - Schema changes require migrations
7. **Keep DTOs separate** - Never expose entities directly
8. **Validate at boundaries** - API layer validates input
9. **Use CancellationTokens** - Pass them through to EF Core
10. **Keep it simple** - Don't over-engineer Week 1 features

**Current User Rules:**
- Middleware reads `X-User-Email` header (defaults to `josh@local`)
- User is auto-created if doesn't exist
- All queries scoped to `ICurrentUser.UserId`
- Return 404 for unauthorized access (don't leak existence)

**Testing Focus:**
- Unit tests: Domain logic, business rules
- Integration tests: Full endpoint flow, user scoping
- Keep tests fast and deterministic
- Use in-memory DB for integration tests

**Code Review Checklist:**
- [ ] Follows Clean Architecture
- [ ] Current user filtering applied
- [ ] Tests added
- [ ] DTOs used (not entities)
- [ ] Async/await used correctly
- [ ] Migrations created
- [ ] Status codes correct
- [ ] Error handling appropriate
