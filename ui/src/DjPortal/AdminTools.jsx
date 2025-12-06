import {} from 'react'
import { Card } from 'react-bootstrap';
import CreateEventModal from './CreateEventModal.jsx';
import DeleteAllRequests from './DeleteAllRequests.jsx';
import DeleteExpiredEvents from './DeleteExpiredEvents.jsx';
import DeleteCache from './DeleteCache.jsx';
import UploadTrackList from './UploadTrackList.jsx';

function AdminTools() {

    return (
        <Card>
            <Card.Header>Admin Tools</Card.Header>
            <Card.Body>
                <CreateEventModal />
                <DeleteCache />
                <DeleteExpiredEvents />
                <DeleteAllRequests />
                <UploadTrackList />
            </Card.Body>
        </Card>
    );
}

export default AdminTools;