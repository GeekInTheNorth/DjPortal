import { useContext } from 'react';
import { Tabs, Tab } from 'react-bootstrap';
import { AppContext } from './AppContext.jsx';
import { EventPageProvider } from './EventPageContext.jsx';
import RequestForm from './RequestForm.jsx';
import AiChat from './AiChat.jsx';
import RequestList from './RequestList.jsx';
import './EventPageApp.css';

function EventPageContent() {
    const { selectedEvent } = useContext(AppContext);

    if (!selectedEvent.isRequestable) {
        return null;
    }

    return (
        <>
            <Tabs defaultActiveKey='form' className='mt-3 request-tabs' fill>
                <Tab eventKey='form' title='I know what I want'>
                    <div className='border border-top-0 rounded-bottom p-3 bg-white'>
                        <RequestForm />
                    </div>
                </Tab>
                <Tab eventKey='ai' title='Ask DJ Assistant'>
                    <div className='border border-top-0 rounded-bottom p-3 bg-white'>
                        <AiChat />
                    </div>
                </Tab>
            </Tabs>
            <RequestList />
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
