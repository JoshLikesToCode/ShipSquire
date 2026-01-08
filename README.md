# ShipSquire

A developer ops portal for managing Services, Runbooks, and Incidents.

## Week 1 MVP

Week 1 focuses on full-stack CRUD for Services, Runbooks, and RunbookSections with Docker Compose deployment.

## Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK (for local development)
- Node.js 20+ (for local development)
- Make (optional, for convenience commands)

### Run with Docker Compose

```bash
# Start all services (Postgres, API, Web)
docker compose up -d

# View logs
docker compose logs -f

# Stop all services
docker compose down
```

Or use Make:

```bash
make up
make logs
make down
```

### Access the Application

- **Web UI**: http://localhost:3000
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/health

### Test the Week 1 Flow

1. Navigate to http://localhost:3000
2. Create a new Service (name: "My App", slug: "my-app")
3. Click on the service to view details
4. Create a new Runbook (title: "Deployment Guide")
5. Click on the runbook to edit
6. Edit any section's markdown content
7. Click Save
8. Refresh the page - content should persist

## Local Development

### API (Backend)

```bash
cd api

# Restore dependencies
dotnet restore

# Run migrations (requires Postgres running)
dotnet ef database update --project ShipSquire.Infrastructure --startup-project ShipSquire.Api

# Run the API
dotnet run --project ShipSquire.Api

# Run tests
dotnet test
```

### Web (Frontend)

```bash
cd web

# Install dependencies
npm install

# Run dev server
npm run dev

# Run tests
npm test

# Build for production
npm run build

# Lint code
npm run lint

# Format code
npm run format
```

### Database Management

```bash
# Start just Postgres for local dev
docker compose -f docker-compose.dev.yml up -d

# Reset the database
make reset-db

# Or manually:
docker compose down postgres
docker volume rm shipSquire_postgres_data
docker compose up -d postgres
```

## Environment Variables

### API

- `DATABASE_URL`: Postgres connection string (default: `Host=localhost;Database=shipSquire;Username=postgres;Password=postgres`)
- `ASPNETCORE_ENVIRONMENT`: Environment (Development, Production)
- `ASPNETCORE_URLS`: URLs to listen on (default: `http://+:5000`)

### Web

- `VITE_API_BASE_URL`: API base URL (default: `http://localhost:5000`)
- `VITE_USER_EMAIL`: Default user email for development (default: `josh@local`)

## Architecture

ShipSquire follows Clean Architecture principles:

```
api/
  ShipSquire.Domain/        # Entities, interfaces (no dependencies)
  ShipSquire.Application/   # Use cases, DTOs, services
  ShipSquire.Infrastructure/ # EF Core, repositories, persistence
  ShipSquire.Api/           # Minimal API endpoints, middleware
  ShipSquire.Tests.Unit/    # Domain & application tests
  ShipSquire.Tests.Integration/ # API endpoint tests

web/
  src/
    pages/       # React pages
    components/  # Reusable components
    services/    # API client
    types/       # TypeScript types
```

## API Endpoints

### Health
- `GET /api/health` - Health check

### Authentication
- `GET /auth/github/login` - Initiate GitHub OAuth login
- `GET /auth/github/callback` - GitHub OAuth callback
- `POST /auth/logout` - Logout current user
- `GET /auth/me` - Get current authenticated user

### Users
- `GET /api/users/me` - Get current user

### Services
- `GET /api/services` - List all services
- `POST /api/services` - Create service
- `GET /api/services/{id}` - Get service
- `PATCH /api/services/{id}` - Update service
- `DELETE /api/services/{id}` - Delete service

### Runbooks
- `GET /api/services/{serviceId}/runbooks` - List service runbooks
- `POST /api/services/{serviceId}/runbooks` - Create runbook (auto-creates sections)
- `GET /api/runbooks/{id}` - Get runbook with sections & variables
- `PATCH /api/runbooks/{id}` - Update runbook
- `DELETE /api/runbooks/{id}` - Delete runbook

### Runbook Sections
- `POST /api/runbooks/{runbookId}/sections` - Create section
- `PATCH /api/runbooks/{runbookId}/sections/{sectionId}` - Update section
- `POST /api/runbooks/{runbookId}/sections/reorder` - Reorder sections
- `DELETE /api/runbooks/{runbookId}/sections/{sectionId}` - Delete section

### Runbook Variables
- `POST /api/runbooks/{runbookId}/variables` - Create variable
- `PATCH /api/runbooks/{runbookId}/variables/{variableId}` - Update variable
- `DELETE /api/runbooks/{runbookId}/variables/{variableId}` - Delete variable

## Authentication

ShipSquire supports two authentication mechanisms:

### GitHub OAuth (Production)

**Priority 1:** Cookie-based authentication via GitHub OAuth

1. **Register GitHub OAuth App**
   - Go to https://github.com/settings/developers
   - Click "New OAuth App"
   - Fill in:
     - **Application name:** ShipSquire (or your preferred name)
     - **Homepage URL:** `http://localhost:3000` (or your domain)
     - **Authorization callback URL:** `http://localhost:5000/auth/github/callback`
   - Click "Register application"
   - Copy the **Client ID** and generate a **Client Secret**

2. **Configure Environment Variables**

   Update `api/ShipSquire.Api/appsettings.json`:
   ```json
   {
     "GitHub": {
       "ClientId": "YOUR_GITHUB_CLIENT_ID",
       "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET",
       "RedirectUri": "http://localhost:5000/auth/github/callback"
     },
     "Encryption": {
       "Key": "generate-a-secure-32-character-key-for-production"
     }
   }
   ```

   Or use environment variables (recommended for production):
   ```bash
   export GitHub__ClientId="YOUR_GITHUB_CLIENT_ID"
   export GitHub__ClientSecret="YOUR_GITHUB_CLIENT_SECRET"
   export Encryption__Key="your-secure-encryption-key-32chars-minimum"
   ```

3. **OAuth Endpoints**
   - `GET /auth/github/login` - Initiates GitHub OAuth flow
   - `GET /auth/github/callback` - OAuth callback handler
   - `POST /auth/logout` - Logs out current user
   - `GET /auth/me` - Returns current authenticated user

4. **How It Works**
   - User clicks "Login with GitHub" button
   - Redirected to GitHub authorization page
   - GitHub redirects back with authorization code
   - API exchanges code for access token
   - User info fetched from GitHub API
   - User created/updated in database (token encrypted at rest)
   - Session cookie set (30-day expiration, HttpOnly, SameSite=Lax)

### X-User-Email Header (Development/Testing)

**Priority 2:** Header-based authentication for backward compatibility

- API reads `X-User-Email` header from requests
- If missing, defaults to `josh@local`
- User is auto-created if it doesn't exist
- All queries are scoped to the current user
- Useful for testing, API clients, and development

The web app sends `X-User-Email: josh@local` by default when not authenticated via OAuth.

## Testing

```bash
# Run all API tests
cd api && dotnet test

# Run all web tests
cd web && npm test

# Or use Make
make test
```

## CI/CD

GitHub Actions runs on every push and PR:

- API: restore, build, test
- Web: install, build, test

See `.github/workflows/ci.yml`

## Database Schema

### Core Entities

- **User**: Email, DisplayName, AuthProvider, GitHubUserId, GitHubUsername, GitHubAccessToken (encrypted)
- **Service**: Name, Slug, Description, Repo info
- **Runbook**: Title, Status, Version, Summary
- **RunbookSection**: Key, Title, Order, BodyMarkdown
- **RunbookVariable**: Name, ValueHint, IsSecret, Description

### Week 1 Schema (optional tables)

- **Incident**: Title, Severity, Status, StartedAt
- **IncidentTimelineEntry**: EntryType, OccurredAt, BodyMarkdown
- **Postmortem**: Impact, RootCause, Detection, Resolution, ActionItems

### Default Runbook Sections

When a runbook is created, these sections are auto-seeded:

1. Overview
2. Deployment
3. Rollback
4. Health Checks
5. Environment Variables
6. Troubleshooting

## Roadmap

### Week 1 ✅
- ✅ Services CRUD
- ✅ Runbooks CRUD
- ✅ Sections CRUD
- ✅ Docker Compose setup
- ✅ Basic UI
- ✅ Tests & CI

### Week 2 (Current)
- ✅ GitHub OAuth authentication
- ✅ Secure token storage (AES-256 encryption)
- ✅ Cookie-based session management
- ✅ Login/logout UI
- Service GitHub integration (in progress)

### Week 3
- Incidents UI
- Timeline entries
- Postmortem support

### Week 4
- PDF export
- Slack notifications
- Enhanced markdown editor

## Contributing

See `Claude.md` for AI-assisted development guidelines and project conventions.

## License

MIT
