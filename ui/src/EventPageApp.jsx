import { useContext, useState } from 'react';
import { ButtonGroup, Button } from 'react-bootstrap';
import { AppContext } from './AppContext.jsx';
import { EventPageProvider } from './EventPageContext.jsx';
import RequestForm from './RequestForm.jsx';
import AiChat from './AiChat.jsx';
import RequestList from './RequestList.jsx';

function EventPageContent() {
    const { selectedEvent } = useContext(AppContext);
    const [aiMode, setAiMode] = useState(false);

    if (!selectedEvent.isRequestable) {
        return null;
    }

    return (
        <>
            <ButtonGroup className='mt-3 w-100'>
                <Button variant={aiMode ? 'outline-primary' : 'primary'} onClick={() => setAiMode(false)}>Request Form</Button>
                <Button variant={aiMode ? 'primary' : 'outline-primary'} onClick={() => setAiMode(true)}>Ask the AI Assistant</Button>
            </ButtonGroup>
            {aiMode ? <AiChat /> : <RequestForm />}
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
