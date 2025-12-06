import { useContext } from 'react'
import { AppContext } from './../AppContext.jsx';
import axios from 'axios';
import { Button } from 'react-bootstrap';

function DeleteAllRequests() {

    const { selectedEvent, getMusicRequests } = useContext(AppContext);

    const purgeRequests = async () => {
        await axios.delete(import.meta.env.VITE_APP_REQUESTS_DELETEALL);
        await getMusicRequests(selectedEvent);
    };

    return (
        <div className='mb-3 border-start border-danger border-3 px-3'>
            <label className='form-label'>Delete all requests for all events.</label><br/>
            <Button variant='danger' onClick={purgeRequests}>Delete All Requests</Button>
        </div>
    );
}

export default DeleteAllRequests;