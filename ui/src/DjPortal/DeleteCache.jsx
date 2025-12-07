import { useContext } from 'react'
import { AppContext } from './../AppContext.jsx';
import axios from 'axios';
import { Button } from 'react-bootstrap';

function DeleteCache() {

    const { getEventCollection } = useContext(AppContext);

    const purgeEvents = async () => {
        await axios.delete(import.meta.env.VITE_APP_EVENTS_DELETECACHE);
        await getEventCollection();
    };

    return (
        <div className='mb-3 border-start border-danger border-3 px-3'>
            <label className='form-label'>Delete event cache, use this if you have added or removed an event and you cannot see the changes yet.</label><br/>
            <Button variant='danger' onClick={purgeEvents}>Delete Event Cache</Button>
        </div>
    );
}

export default DeleteCache;