import { useContext } from 'react'
import { AppContext } from './../AppContext.jsx';
import axios from 'axios';
import { Button } from 'react-bootstrap';

function DeleteExpiredEvents() {

    const { getEventCollection } = useContext(AppContext);

    const purgeEvents = async () => {
        await axios.delete(import.meta.env.VITE_APP_EVENTS_DELETEEXPIRED);
        await getEventCollection();
    };

    return (
        <div className='mb-3 border-start border-danger border-3 px-3'>
            <label className='form-label'>Delete expired events.</label><br/>
            <Button variant='danger' onClick={purgeEvents}>Delete Expired Events</Button>
        </div>
    );
}

export default DeleteExpiredEvents;