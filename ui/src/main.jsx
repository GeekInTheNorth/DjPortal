import {} from 'react'
import ReactDOM from 'react-dom/client'
import { AppProvider } from './AppContext.jsx'
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css'
import DjPortal from './DjPortal/DjPortal.jsx';
import AdminTools from './DjPortal/AdminTools.jsx';
import AdminEventList from './DjPortal/AdminEventList.jsx';
import EventPageApp from './EventPageApp.jsx';

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
        <AppProvider>
            <AdminEventList />
            <AdminTools />
        </AppProvider>
    );
}

const eventPageElement = document.getElementById('eventpage');
if (eventPageElement) {
    ReactDOM.createRoot(eventPageElement).render(
        <EventPageApp />
    );
}