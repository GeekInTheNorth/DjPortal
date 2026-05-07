# DJ Portal

[https://dj.stott.pro](https://dj.stott.pro)

DJ Portal is the public-facing event listing and music request site for **DJ Mark Stott**, who DJs at Ceroc Modern Jive dance events across Yorkshire (primarily Leeds and Tadcaster). The site lets dancers see upcoming events, add them to their calendar, and submit song requests directly to the DJ. A private DJ Portal lets Mark triage incoming requests during a live event, and an Admin area is used to manage events and reference data.

The solution is deployed as a single [Azure Static Web App](https://learn.microsoft.com/azure/static-web-apps/overview) consisting of a React frontend and a .NET Azure Functions API.

## Features

### Public site (`/`)
- Lists upcoming events where DJ Mark is performing, generated as static HTML pages at build time for SEO and fast first paint.
- Each event has a dedicated URL ([`/events/yyyy-MM-dd`](https://dj.stott.pro/)) with full event metadata.
- One-click `.ics` calendar download for any event.
- Optional Facebook event link per event.
- Schema.org event data emitted for events that opt in.
- FAQ and tag-based event categorisation (e.g. *DJ Mark*, *Valentine's Ball*).

### Music requests
- Dancers can submit song requests for any event marked as **Requestable**.
- Requests can be either a free-text song name or a pasted Spotify track URL — Spotify URLs are automatically resolved to *Track Name - Artist(s)* via the Spotify API.
- Anonymous users are capped at **3 requests per event** ([AppConstants.cs](api/Features/AppConstants.cs)) and tracked via a `cydjr.requestor` cookie.
- Other users' names are obfuscated when viewing the request list anonymously; the dancer sees their own as "You".
- Requests can be shared into the currently requestable event via a "Share Link" endpoint (used by the PWA share target).

### DJ Portal (`/djportal.html`, authenticated)
- Live view of incoming requests for the current event, sorted by status (Approved → Queued → Pending → Played → AlreadyServed).
- DJ-side track search backed by Azure Search across Mark's catalogue (CSV-imported track list with title, artist, album, BPM, key, time).
- Status updates per request (Approved / Queued / Played / Already Served, etc.).

### Admin (`/admin.html`, authenticated)
- Create / update / delete events ([CreateEventModal.jsx](ui/src/DjPortal/CreateEventModal.jsx), [UpdateEventModal.jsx](ui/src/DjPortal/UpdateEventModal.jsx)).
- Bulk delete: all events, all requests, expired events, and Azure Search index purge/recreate.
- Upload the DJ track catalogue from CSV ([UploadTrackList.jsx](ui/src/DjPortal/UploadTrackList.jsx)) — recreates the `tracks` index.
- Trigger a GitHub Actions rebuild of the site ([TriggerRebuild.jsx](ui/src/DjPortal/TriggerRebuild.jsx)) via `workflow_dispatch` — useful after editing event data so the static event pages regenerate.
- Search term insights ([SearchTermInsights.jsx](ui/src/DjPortal/SearchTermInsights.jsx)) — pulls track-search queries from Application Insights to highlight songs dancers are looking for.

### Authentication
- GitHub OAuth via Azure Static Web Apps (`/.auth/login/github`).
- Protected routes and admin endpoints require the `authenticated` role; configured in [staticwebapp.config.json](ui/public/staticwebapp.config.json).
- Anonymous endpoints (event list, public request submission) do their own authorisation in code where needed.

## Technology Stack

### Backend ([api/](api/))
- **.NET 8.0** with **Azure Functions V4** (isolated worker model)
- **Azure AI Search** for all data persistence — three indexes: `events`, `requests`, `tracks` (plus a `ceroc-dj-synonyms` synonym map)
- **Spotify Web API** for track metadata enrichment
- **Application Insights** for telemetry and the search-term insights query
- **CsvHelper** for CSV ingestion of the track catalogue
- DI configured in [Program.cs](api/Program.cs); repositories inherit a shared [BaseRepository](api/Features/Common/BaseRepository.cs) that builds the Azure Search client from configuration.

### Frontend ([ui/](ui/))
- **React 19** with **Vite 7** in a Multi-Page App configuration
- **Bootstrap 5** + **React-Bootstrap**
- **FontAwesome**, **date-fns**
- Three HTML entry points: [index.html](ui/index.html) (public), [djportal.html](ui/djportal.html), [admin.html](ui/admin.html)
- A [Vite middleware plugin](ui/vite.config.js) and a [post-build script](ui/scripts/generate-event-pages.js) generate static `/events/yyyy-MM-dd.html` pages from the live API.

### Hosting
- Azure Static Web Apps (frontend + API gateway)
- GitHub Actions for CI/CD on push to `main`
- Production domain: `dj.stott.pro`

## Repository Layout

```
DjPortal/
├── api/                         .NET Azure Functions API
│   ├── Features/
│   │   ├── Common/              BaseRepository (Azure Search client factory)
│   │   ├── Deployment/          GitHub Actions workflow_dispatch trigger
│   │   ├── Events/              Event CRUD, .ics generation, tag rendering
│   │   ├── Insights/            Application Insights query for track searches
│   │   ├── Requests/            Music request domain (status, comparer, models)
│   │   ├── Spotify/             Spotify track lookup
│   │   ├── Tracks/              Azure Search-backed DJ track catalogue + CSV import
│   │   └── Extensions/          Shared helpers (string obfuscation, etc.)
│   ├── EventsFunction.cs        HTTP triggers — events
│   ├── RequestsFunction.cs      HTTP triggers — music requests
│   ├── TracksFunction.cs        HTTP triggers — track search & CSV upload
│   ├── InsightsFunction.cs      HTTP triggers — analytics
│   ├── DeploymentFunction.cs    HTTP triggers — rebuild
│   ├── Program.cs               DI registration & Functions host setup
│   └── host.json                CORS for local dev
│
├── ui/                          React + Vite frontend
│   ├── src/
│   │   ├── App.jsx              Public event list root
│   │   ├── EventList.jsx        Public event list
│   │   ├── EventPageApp.jsx     Single-event request form root
│   │   ├── RequestForm.jsx      Public request submission form
│   │   ├── RequestList.jsx      Public request list (obfuscated names)
│   │   ├── AppContext.jsx       Global state (events, user, auth)
│   │   └── DjPortal/            Authenticated DJ + Admin views
│   ├── public/                  Static assets, manifest, staticwebapp.config.json
│   ├── scripts/
│   │   └── generate-event-pages.js   Static page generator (post-build)
│   └── vite.config.js           MPA build + dev middleware that pre-renders event pages
│
├── ApiTests.http                REST Client request collection for the API
├── DjPortal.sln                 Visual Studio / Rider solution
└── package.json                 Root scripts (SWA CLI, Azurite, build)
```

## API Surface

All routes are exposed under `/api/*` via the Static Web Apps proxy.

| Route | Methods | Auth | Purpose |
|---|---|---|---|
| `/api/events/list` | GET | Anonymous | Upcoming, non-cancelled events |
| `/api/events/byid/{id}` | GET | Anonymous | Single event |
| `/api/events/getinvite/{id}/dance-event.ics` | GET | Anonymous | Calendar invite |
| `/api/events/create` | POST | Authenticated | Create event |
| `/api/events/update` | POST | Authenticated | Update event |
| `/api/events/delete` | DELETE | Authenticated | Delete event by id |
| `/api/events/deleteall` | DELETE | Authenticated | Drop & recreate events index |
| `/api/events/deleteexpired` | DELETE | Authenticated | Remove past events |
| `/api/events/deletecache` | DELETE | Authenticated | Purge in-memory event cache |
| `/api/musicrequest/list` | GET | Anonymous | List requests for an event (names obfuscated for non-DJ users) |
| `/api/musicrequest/create` | POST | Anonymous | Submit a request (rate-limited by cookie) |
| `/api/musicrequest/share` | POST | Anonymous | PWA share-target ingestion |
| `/api/musicrequest/updatestatus` | POST | Authenticated | DJ updates request status |
| `/api/musicrequest/delete` | DELETE | Authenticated | Delete a request |
| `/api/musicrequest/deleteall` | DELETE | Authenticated | Drop & recreate requests index |
| `/api/tracks/search` | GET | Anonymous | Search the DJ track catalogue |
| `/api/tracks/csvupload` | POST | Authenticated | Replace tracks index from CSV |
| `/api/insights/searchterms` | GET | Authenticated | Recent track-search analytics |
| `/api/deployment/rebuild` | POST | Authenticated | Fire GitHub Actions workflow_dispatch |

## Configuration

### API (`api/local.settings.json`)
| Key | Description |
|---|---|
| `SearchServiceUri` | Azure AI Search endpoint URL |
| `SearchServiceAdminApiKey` | Azure AI Search admin key |
| `SPOTIFY_CLIENT_ID` / `SPOTIFY_CLIENT_SECRET` | Spotify Web API client credentials |
| `APPINSIGHTS_INSTRUMENTATIONKEY` | Application Insights instrumentation key |
| `ApplicationInsights__AppId` / `ApplicationInsights__ApiKey` | App Insights API access for the search-term insights query |
| `GitHubToken` / `GitHubOwner` / `GitHubRepo` / `GitHubWorkflowFileName` | Required by `/api/deployment/rebuild` to call `workflow_dispatch` |

### Frontend
- [ui/.env](ui/.env) — local API URLs
- [ui/.env.production](ui/.env.production) — relative `/api` paths

## Development

Prerequisites:
- Node.js 20+
- .NET 8 SDK
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (`func`)
- An accessible Azure AI Search instance (or substitute test data)

### Run the full stack
From the repository root:
```bash
npm install
npm run dev
```
This launches the Static Web Apps CLI, which proxies the Vite dev server (port 4280) and the Functions host (port 7071) under a single origin.

### Run the frontend only
```bash
cd ui
npm install
npm run dev
```
The Vite dev server's middleware will fetch live event data from `https://dj.stott.pro/api/events/list/` to render the static event pages on the fly.

### Run the API only
```bash
npm run start:api
# or
cd api
dotnet build
func start
```

### Local storage emulation (optional)
```bash
npm run start:azurite
```

### Lint
```bash
cd ui
npm run lint
```

### Build for deployment
```bash
cd ui
npm run build
```
This produces `ui/dist/` containing:
- The three hashed-asset HTML entry points (`index.html`, `djportal.html`, `admin.html`)
- A pre-rendered `events/yyyy-MM-dd.html` for every upcoming event (generated by [generate-event-pages.js](ui/scripts/generate-event-pages.js))
- Static assets copied from [ui/public/](ui/public/)

## Testing

There is no automated test suite. Manual API testing is done via [ApiTests.http](ApiTests.http) (VS Code REST Client extension).

## Deployment

Pushes to `main` trigger the GitHub Actions workflow in [.github/workflows/](.github/workflows/), which:
1. Builds the React app from `ui/`
2. Deploys `ui/dist/` to Azure Static Web Apps
3. Deploys the `api/` project to the linked Azure Functions app

Mark can also trigger a rebuild from the Admin page (e.g. after editing event data) — this hits `/api/deployment/rebuild`, which fires the same workflow via the GitHub REST API and regenerates the static event pages.
