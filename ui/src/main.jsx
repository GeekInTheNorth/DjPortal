import {} from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import { AppProvider } from './AppContext.jsx'
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css'
import DjPortal from './DjPortal/DjPortal.jsx';
import AdminTools from './DjPortal/AdminTools.jsx';
import AdminEventList from './DjPortal/AdminEventList.jsx';

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
        <AppProvider>
            <AdminEventList />
            <AdminTools />
        </AppProvider>
    );
}