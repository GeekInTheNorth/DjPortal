import { useContext } from 'react'
import { AppContext } from './../AppContext.jsx';
import axios from 'axios';
import { Button } from 'react-bootstrap';

function DeleteAllEvents() {

    const { getEventCollection } = useContext(AppContext);

    const purgeEvents = async () => {
        await axios.delete(import.meta.env.VITE_APP_EVENTS_DELETEALL);
        await getEventCollection();
    };

    return (
        <div className='mb-3'>
            <label className='form-label'>Delete all events.</label><br/>
            <Button variant='danger' onClick={purgeEvents}>Delete All Events</Button>
        </div>
    );
}

export default DeleteAllEvents;