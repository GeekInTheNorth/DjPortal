# DjPortal - GitHub Context Guide

## Project Overview
DjPortal is a full-stack web application that manages music requests and events for DJ operations. It includes an Azure Functions-based API backend and a modern React UI.

## Technology Stack

### Backend
- **Runtime**: .NET 8.0 (C#)
- **Framework**: Azure Functions
- **Cloud Services**: Azure Storage, Application Insights
- **Database**: Azure Table Storage
- **API Style**: RESTful HTTP triggers

### Frontend
- **Framework**: React 18+ with Vite
- **Styling**: CSS modules
- **Build Tool**: Vite
- **State Management**: React Context API
- **HTTP Client**: Fetch API

### External Integrations
- **Spotify API**: For track search and metadata
- **Azure Static Web Apps**: Hosting and authentication

## Project Structure

```
DjPortal/
├── api/                          # .NET 8.0 Azure Functions backend
│   ├── Features/
│   │   ├── Events/              # Event management (CRUD operations)
│   │   ├── Requests/            # Music request handling
│   │   ├── Tracks/              # Track data management
│   │   ├── Insights/            # Analytics via Application Insights
│   │   ├── Spotify/             # Spotify API integration
│   │   └── Common/              # Shared utilities and base classes
│   ├── EventsFunction.cs         # Events endpoint
│   ├── RequestsFunction.cs       # Requests endpoint
│   ├── TracksFunction.cs         # Tracks endpoint
│   ├── InsightsFunction.cs       # Analytics endpoint
│   └── Program.cs                # Function app configuration
│
├── ui/                           # React + Vite frontend
│   ├── src/
│   │   ├── App.jsx              # Main application component
│   │   ├── AppContext.jsx       # Global state/context provider
│   │   ├── EventList.jsx        # Event listing component
│   │   ├── EventDetails.jsx     # Event details view
│   │   ├── EventSummary.jsx     # Event summary display
│   │   ├── RequestForm.jsx      # Music request form
│   │   ├── RequestList.jsx      # Music requests list
│   │   ├── Faq.jsx              # FAQ page
│   │   ├── DjPortal/            # DJ admin dashboard
│   │   │   ├── AdminEventList.jsx
│   │   │   ├── AdminTools.jsx
│   │   │   ├── CreateEventModal.jsx
│   │   │   ├── DeleteAllEvents.jsx
│   │   │   ├── DeleteAllRequests.jsx
│   │   │   └── DeleteCache.jsx
│   │   └── main.jsx             # Entry point
│   ├── vite.config.js           # Vite configuration
│   └── package.json             # Dependencies
│
├── src/                          # Static web assets
│   ├── index.html               # Main HTML
│   ├── djportal.html            # DJ portal HTML
│   ├── admin.html               # Admin panel HTML
│   └── staticwebapp.config.json  # Azure SWA configuration
│
├── DjPortal.sln                 # Visual Studio solution file
├── package.json                 # Root npm configuration
├── ApiTests.http                # HTTP test file (REST Client format)
└── README.md                    # Project README

```

## Deployment

In order to create the fully deployable app, the following must be done:

- In DjPortal/ui run `npm run build-dotnet`
  - This will build the react component as JS and CSS and place the files in DjPortal/src/static
- Update DjPortal/src/*.html so that index*.js and index*.css reference the new files
- GitHub actions will deploy the following folders into an Azure Static WebApp:
  - DjPortal/api will become the functions app
  - DjPortal/src will become the static web app
