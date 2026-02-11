# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DjPortal is a full-stack music request management system for DJ operations, built as an Azure Static Web App with a .NET Azure Functions API backend and React frontend.

## Technology Stack

### Backend
- .NET 8.0 with Azure Functions V4 (isolated worker model)
- Azure Search Service for data persistence (events, requests, tracks)
- Application Insights for telemetry and analytics
- Spotify API integration for track metadata
- Dependency injection configured in [Program.cs](api/Program.cs)

### Frontend
- React 18+ with Vite build tool
- Bootstrap 5 and React-Bootstrap for UI components
- React Context API for state management (see [AppContext.jsx](ui/src/AppContext.jsx))
- FontAwesome icons
- date-fns for date formatting

### Deployment
- Azure Static Web Apps with GitHub Actions
- GitHub OAuth authentication with role-based access control

## Development Commands

### Running the Full Stack
```bash
# From root directory - runs both frontend and API via SWA CLI
npm run dev
```

### Frontend Only
```bash
cd ui
npm run dev          # Vite dev server on port 4280
npm run build        # Production build
npm run lint         # ESLint
```

### API Only
```bash
npm run start:api    # From root: builds and runs Azure Functions on port 7071
# or from api directory:
cd api
dotnet build
func start
```

### Local Storage Emulation
```bash
npm run start:azurite  # From root: starts Azurite for Azure Storage emulation
```

### Building for Deployment
```bash
cd ui
npm run build        # Builds React app to dist/ with automatic hash injection
```

The build process automatically:
- Compiles all three HTML entry points (index.html, djportal.html, admin.html)
- Bundles React app and outputs to `ui/dist/static/` with hashed filenames
- Injects correct hashed filenames into all HTML files
- Copies static assets from `ui/public/` to `ui/dist/`

## Architecture

### API Structure (Feature-Based Organization)

The API follows a feature-based architecture where each domain area is self-contained:

```
api/
├── Features/
│   ├── Common/
│   │   └── BaseRepository.cs         # Base class for Azure Search operations
│   ├── Events/                        # Event CRUD operations
│   │   ├── IEventRepository.cs
│   │   ├── EventRepository.cs        # Azure Search persistence
│   │   ├── IEventService.cs
│   │   └── EventService.cs           # Business logic
│   ├── Requests/                      # Music request handling
│   ├── Tracks/                        # Track data management
│   ├── Spotify/                       # Spotify API integration
│   └── Insights/                      # Analytics queries
├── EventsFunction.cs                  # HTTP trigger endpoints
├── RequestsFunction.cs
├── TracksFunction.cs
└── Program.cs                         # DI configuration
```

**Key Patterns:**
- **Repository Pattern**: Each feature has a repository for data access (inherits from `BaseRepository`)
- **Service Layer**: Business logic separated from HTTP concerns
- **Dependency Injection**: Services registered in [Program.cs](api/Program.cs)
- **BaseRepository**: All repositories inherit from [BaseRepository](api/Features/Common/BaseRepository.cs) which provides Azure Search Client creation from configuration

**Azure Search Usage:**
- All data is stored in Azure Search indexes (not Azure Table Storage)
- Configuration keys: `SearchServiceUri` and `SearchServiceAdminApiKey`
- Each entity type has its own index (events, requests, tracks)

### Frontend Structure

The frontend uses Vite's **Multi-Page App (MPA)** configuration with three HTML entry points:

```
ui/
├── index.html                 # Public event listing page
├── djportal.html              # DJ portal page (authenticated)
├── admin.html                 # Admin page (authenticated)
├── src/
│   ├── App.jsx                # Main app component with routing logic
│   ├── AppContext.jsx         # Global state provider (events, requests, user)
│   ├── main.jsx               # Entry point (loaded by all HTML files)
│   ├── EventList.jsx          # Public event listing
│   ├── EventDetails.jsx       # Event detail view with request form
│   ├── RequestForm.jsx        # Music request submission
│   ├── RequestList.jsx        # Display of requests for an event
│   └── DjPortal/              # Admin dashboard (authenticated only)
│       ├── AdminEventList.jsx
│       ├── AdminTools.jsx
│       ├── CreateEventModal.jsx
│       └── Delete*.jsx        # Admin deletion tools
├── public/                    # Static assets (copied to build output)
│   ├── images/
│   ├── manifest.json
│   ├── robots.txt
│   ├── staticwebapp.config.json
│   └── .well-known/
└── vite.config.js             # Multi-page build configuration
```

**Key Patterns:**
- **Context API**: `AppContext` manages global state (events, requests, authentication)
- **Fetch API**: Direct fetch calls to `/api/*` endpoints (no axios wrapper)
- **CSS Modules**: Component-specific styling
- **Bootstrap Components**: React-Bootstrap for modals, forms, buttons

### Authentication & Authorization

Configured in [src/staticwebapp.config.json](src/staticwebapp.config.json):
- GitHub OAuth authentication via `/.auth/login/github`
- Protected routes require `authenticated` role
- Admin operations (create/update/delete events, insights) are restricted
- Unauthenticated requests to protected routes redirect to login

### Configuration

**API Configuration** ([api/local.settings.json](api/local.settings.json)):
- `SearchServiceUri`: Azure Search endpoint
- `SearchServiceAdminApiKey`: Azure Search admin key
- `SPOTIFY_CLIENT_ID` and `SPOTIFY_CLIENT_SECRET`: Spotify API credentials
- `APPINSIGHTS_INSTRUMENTATIONKEY`: Application Insights key

**Frontend Environment** ([ui/.env](ui/.env)):
- Development: Local API URLs
- Production ([ui/.env.production](ui/.env.production)): Relative `/api` paths

### Security & CORS

**CORS Configuration** ([api/host.json](api/host.json)):
- CORS settings allow `http://localhost:4280` and `http://127.0.0.1:4280` for local development only
- In production, Azure Static Web Apps acts as a reverse proxy, making all requests same-origin
- The Functions API is not directly exposed in production - all requests go through the Static Web Apps gateway
- CORS headers are only needed for local development when frontend (port 4280) and API (port 7071) run on different origins

**Production Security Model:**
- Frontend and API both served from `https://yourapp.azurestaticapps.net`
- No cross-origin requests = no CORS needed
- Authentication/authorization handled by [staticwebapp.config.json](ui/public/staticwebapp.config.json)
- Functions API accessed only through Static Web Apps proxy at `/api/*`

## Common Tasks

### Adding a New API Endpoint

1. Add method to appropriate Function class (e.g., [EventsFunction.cs](api/EventsFunction.cs))
2. If needed, add method to corresponding Repository interface and implementation
3. If needed, add method to corresponding Service interface and implementation
4. Update [staticwebapp.config.json](src/staticwebapp.config.json) if authentication is required
5. Test with [ApiTests.http](ApiTests.http)

### Adding a New Feature Domain

1. Create folder under `api/Features/`
2. Create models, interfaces, repository, and service classes
3. Register services in [Program.cs](api/Program.cs)
4. Create Function class at `api/` root level for HTTP triggers
5. Update [staticwebapp.config.json](src/staticwebapp.config.json) for routing/auth

### Modifying the Frontend

1. Update React components in `ui/src/`
2. Test with `npm run dev` from `ui/` directory
3. For deployment: run `npm run build` from `ui/` directory (no manual steps needed)

## Testing

**API Testing:**
- Use [ApiTests.http](ApiTests.http) file with VS Code REST Client extension
- Manual testing with Azure Functions Core Tools

**No automated test suite currently exists.**

## Deployment

The [GitHub Actions workflow](.github/workflows/azure-static-web-apps-thankful-plant-0e4d7031e.yml) automatically deploys on push to `main`:
- Builds the React app from `ui/` directory
- Deploys `ui/dist/` → Static web app
- Deploys `api/` → Azure Functions app

The deployment process:
1. GitHub Actions checks out the code
2. Runs `npm install && npm run build` in the `ui/` directory
3. Deploys the `ui/dist/` output to Azure Static Web Apps
4. Deploys the `api/` directory to Azure Functions

The `ui/dist/` folder contains:
- HTML files with automatic hash injection (index.html, djportal.html, admin.html)
- Bundled JS/CSS in `static/` subdirectory
- Static assets (images, manifest, config files)

## Solution File

[DjPortal.sln](DjPortal.sln) contains the API project and can be opened in Visual Studio or Rider.
