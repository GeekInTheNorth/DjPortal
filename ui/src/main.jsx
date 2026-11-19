import {} from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import { AppProvider } from './AppContext.jsx'
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css'
import DjPortal from './DjPortal/DjPortal.jsx';
import AdminTools from './DjPortal/AdminTools.jsx';
import AdminEventList from './DjPortal/AdminEventList.jsx';
import ImportEvents from './DjPortal/ImportEvents.jsx';
import EventPageApp from './EventPageApp.jsx';
import Contact from './Contact.jsx';
import { registerWebMcpTools } from './webmcp.js';

const rootElement = document.getElementById('root');
if (rootElement) {
    ReactDOM.createRoot(rootElement).render(
        <AppProvider>
            <App />
        </AppProvider>
    );
}

const djPortalElement = document.getElementById('djportal');
if (djPortalElement) {
    ReactDOM.createRoot(djPortalElement).render(
        <AppProvider>
            <DjPortal />
        </AppProvider>
    );
}

const djAdminElement = document.getElementById('djadmin');
if (djAdminElement) {
    ReactDOM.createRoot(djAdminElement).render(
        <AppProvider includeExpired>
            <AdminEventList />
            <AdminTools />
        </AppProvider>
    );
}

const djImportElement = document.getElementById('djimport');
if (djImportElement) {
    ReactDOM.createRoot(djImportElement).render(
        <AppProvider>
            <ImportEvents />
        </AppProvider>
    );
}

const eventPageElement = document.getElementById('eventpage');
if (eventPageElement) {
    ReactDOM.createRoot(eventPageElement).render(
        <EventPageApp />
    );

    registerWebMcpTools({ eventId: window.__EVENT_DATA__?.id });
}

const contactElement = document.getElementById('contact');
if (contactElement) {
    ReactDOM.createRoot(contactElement).render(
        <AppProvider>
            <Contact />
        </AppProvider>
    );
}