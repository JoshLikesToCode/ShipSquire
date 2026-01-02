# Week 1 Execution Plan

## Objectives

Deliver a working Week 1 MVP with:
- Full-stack CRUD for Services, Runbooks, RunbookSections
- Docker Compose deployment (Postgres + API + Web)
- Swagger API documentation at `/swagger`
- Health endpoint at `/api/health`
- UI flow: create service → create runbook → edit sections → save → persist
- Database migrations working from scratch
- Minimal current-user mechanism (X-User-Email header)
- Tests (unit + integration)
- GitHub Actions CI

## Day-by-Day Breakdown

### Day 1: Foundation & Infrastructure

**Goals:**
- Repository structure created
- Docker Compose with Postgres running
- .NET solution building
- React app rendering
- Health endpoint working
- CI skeleton in place

**Deliverables:**
- [x] Monorepo structure (`api/`, `web/`, `infra/`, `.github/workflows/`)
- [x] `.gitignore`, `.editorconfig`, `.dockerignore`
- [x] `docker-compose.yml` with Postgres service
- [x] .NET solution with all projects
- [x] React + Vite + TypeScript scaffolding
- [x] Health endpoint: `GET /api/health → {status: "ok"}`
- [x] Basic React app rendering "ShipSquire"
- [x] GitHub Actions workflow (skeleton)
- [x] README.md with quick start
- [x] Claude.md with project conventions

**Tests:**
- [x] Health endpoint integration test
- [x] React app renders test

**Validation:**
```bash
make up
curl http://localhost:5000/api/health  # Should return {"status":"ok"}
open http://localhost:3000              # Should show "ShipSquire"
```

---

### Day 2: Database & Current User

**Goals:**
- EF Core models created
- Database migrations working
- Current user mechanism functional
- User seeded via header

**Deliverables:**
- [x] Domain entities: User, Service, Runbook, RunbookSection, RunbookVariable, Incident
- [x] EF Core DbContext with entity configurations
- [x] Initial migration created
- [x] CurrentUserMiddleware reading X-User-Email header
- [x] ICurrentUser service exposing UserId, Email
- [x] Default user `josh@local` seeded on first request
- [x] GET /api/users/me endpoint

**Tests:**
- [x] BaseEntity initialization test
- [x] User auto-creation test
- [x] Current user header test

**Validation:**
```bash
# Migration should run on startup
make up
curl -H "X-User-Email: test@example.com" http://localhost:5000/api/users/me
# Should create and return user
```

---

### Day 3: Services CRUD

**Goals:**
- Services endpoints fully functional
- User-scoped queries working
- Integration tests covering CRUD

**Deliverables:**
- [x] ServiceRepository with user filtering
- [x] ServiceService with CRUD operations
- [x] Service endpoints (GET, POST, PATCH, DELETE)
- [x] Service DTOs (ServiceRequest, ServiceResponse)
- [x] Services UI page (list + create)
- [x] Service detail UI page

**Tests:**
- [x] Create service integration test
- [x] Get services integration test
- [x] User scoping test (404 for wrong user)
- [x] Service page renders test

**Validation:**
```bash
# Create service via API
curl -X POST http://localhost:5000/api/services \
  -H "Content-Type: application/json" \
  -H "X-User-Email: josh@local" \
  -d '{"name":"Test Service","slug":"test-service","description":"Test"}'

# List services
curl -H "X-User-Email: josh@local" http://localhost:5000/api/services

# Verify in UI
open http://localhost:3000/services
```

---

### Day 4: Runbooks & Sections

**Goals:**
- Runbooks endpoints functional
- Sections auto-seeded on runbook creation
- Section editing working

**Deliverables:**
- [x] RunbookRepository with user filtering
- [x] RunbookService with default sections seeding
- [x] Runbook endpoints (GET, POST, PATCH, DELETE)
- [x] RunbookSection endpoints (CREATE, UPDATE, REORDER, DELETE)
- [x] RunbookVariable endpoints (CREATE, UPDATE, DELETE)
- [x] Runbook DTOs (RunbookRequest, RunbookResponse, SectionRequest, etc.)
- [x] Runbook editor UI page

**Tests:**
- [x] Create runbook with sections test
- [x] Update section test
- [x] Section ordering test

**Default Sections:**
1. Overview
2. Deployment
3. Rollback
4. Health Checks
5. Environment Variables
6. Troubleshooting

**Validation:**
```bash
# Create service
SERVICE_ID=$(curl -X POST http://localhost:5000/api/services \
  -H "Content-Type: application/json" \
  -H "X-User-Email: josh@local" \
  -d '{"name":"App","slug":"app"}' | jq -r '.id')

# Create runbook (should auto-create 6 sections)
RUNBOOK_ID=$(curl -X POST http://localhost:5000/api/services/$SERVICE_ID/runbooks \
  -H "Content-Type: application/json" \
  -H "X-User-Email: josh@local" \
  -d '{"title":"Deploy Guide"}' | jq -r '.id')

# Get runbook (should include sections)
curl -H "X-User-Email: josh@local" http://localhost:5000/api/runbooks/$RUNBOOK_ID

# Verify in UI
open http://localhost:3000/runbooks/$RUNBOOK_ID
```

---

### Day 5: End-to-End Flow & Polish

**Goals:**
- Full Week 1 acceptance flow working
- Frontend tests passing
- CI passing
- Documentation complete

**Deliverables:**
- [x] Service → Runbook → Edit → Save → Persist flow working
- [x] All tests passing (unit + integration)
- [x] CI green on GitHub Actions
- [x] Swagger documentation complete
- [x] README.md polished
- [x] Claude.md complete
- [x] Week 1 checklist verified

**Tests:**
- All existing tests green
- Frontend component tests

**Acceptance Criteria:**
1. `docker compose up` runs all services
2. Swagger available at http://localhost:5000/swagger
3. Health check returns 200
4. UI flow:
   - Open http://localhost:3000
   - Create service "My App" with slug "my-app"
   - Click service to view details
   - Create runbook "Deployment Guide"
   - Click runbook to edit
   - Edit "Overview" section markdown
   - Click Save
   - Refresh page
   - Content persists ✓
5. Database migrations work from scratch
6. Tests pass: `make test`
7. CI passes on push

**Final Validation:**
```bash
# Full clean start
make clean
make up

# Wait for services to start (30 seconds)
sleep 30

# Health check
curl http://localhost:5000/api/health

# Swagger
open http://localhost:5000/swagger

# UI flow (manual)
open http://localhost:3000

# Run tests
make test

# Check CI
git push origin main
# Verify GitHub Actions green
```

---

## Success Metrics

- [x] All code compiles without errors
- [x] All tests passing (unit + integration)
- [x] Docker Compose brings up all services
- [x] API health endpoint returns 200
- [x] Swagger accessible
- [x] End-to-end UI flow works
- [x] Database migrations run automatically
- [x] Current user mechanism works
- [x] User-scoped queries return 404 for wrong user
- [x] CI pipeline green
- [x] Documentation complete (README + Claude.md)

## Post-Week 1 Cleanup

Before moving to Week 2:

1. Tag release: `git tag v1.0-week1`
2. Review Claude.md for accuracy
3. Update README with any missing details
4. Document known issues (if any)
5. Celebrate! 🎉

## Known Issues / Technical Debt

- [ ] Integration test isolation issue with in-memory DB (GetServices test)
- [ ] No markdown preview in UI (Week 1 optional)
- [ ] No pagination on list endpoints (Week 1 acceptable)
- [ ] CurrentUser stored in HttpContext.Items (temporary, good enough for Week 1)

## Week 2 Preview

- GitHub OAuth integration
- Real user sessions
- Service GitHub metadata sync
- Enhanced error handling
- Request validation middleware
