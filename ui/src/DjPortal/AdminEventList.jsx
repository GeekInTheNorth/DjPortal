import { useContext } from 'react'
import { AppContext } from './../AppContext.jsx';
import { format } from 'date-fns';
import axios from 'axios';
import { Button, Table } from 'react-bootstrap';
import UpdateEventModal from './UpdateEventModal.jsx';

function AdminEventList() {

    const { eventCollection, getEventCollection } = useContext(AppContext);

    const handleDeleteEvent = async (eventId) => {
        await axios.delete(import.meta.env.VITE_APP_EVENTS_DELETE, { params: { id: eventId } })
                   .then(() => setTimeout(() => { getEventCollection(); }, 1000));
    }

    return (
        <Table hover className='my-3'>
            <thead>
                <tr>
                    <th>Event Name</th>
                    <th>Event Date</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                {eventCollection.map((event) => (
                    <tr key={event.id}>
                        <td>{event.name}</td>
                        <td>{format(new Date(event.date), 'dd MMMM yyyy')}</td>
                        <td>
                            <UpdateEventModal eventData={event} />
                            <Button variant='danger' size='sm' className='ms-2' onClick={() => handleDeleteEvent(event.id)}>Delete</Button>
                        </td>
                    </tr>
                ))}
            </tbody>
        </Table>
    );
}

export default AdminEventList;