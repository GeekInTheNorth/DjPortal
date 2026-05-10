import { useContext } from 'react';
import { AppContext } from './AppContext.jsx';
import { EventPageProvider } from './EventPageContext.jsx';
import RequestForm from './RequestForm.jsx';
import RequestList from './RequestList.jsx';

function EventPageContent() {
    const { selectedEvent } = useContext(AppContext);

    return (
        <>
            {selectedEvent.isRequestable ? <RequestForm /> : null}
            {selectedEvent.isRequestable ? <RequestList /> : null}
        </>
    );
}

function EventPageApp() {
    return (
        <EventPageProvider>
            <EventPageContent />
        </EventPageProvider>
    );
}

export default EventPageApp;
