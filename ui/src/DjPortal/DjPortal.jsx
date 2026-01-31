import { useContext, useEffect, useState } from 'react'
import { AppContext } from './../AppContext.jsx';
import { Card, Form } from 'react-bootstrap'
import { format } from 'date-fns';
import axios from 'axios';
import PropTypes from 'prop-types';
import SearchTermInsights from './SearchTermInsights.jsx';
import DjRequestForm from './DjRequestForm.jsx';

function DjPortal() {

    const { eventCollection, selectedEvent, requestCollection, selectEvent, getMusicRequests } = useContext(AppContext);
    const [allowDelete, setAllowDelete] = useState(false);

    useEffect(() => {
        const intervalId = setInterval(() => {
            reloadRequests();
        }, 60000);

        return () => clearInterval(intervalId);
    });

    const renderRequestCollection = () => {
        return requestCollection && requestCollection.map((requestData) => {
        const isSpotifyUrl = typeof requestData.spotifyUrl === 'string' && requestData.spotifyUrl.startsWith('https://open.spotify.com/track/');
          return (
            <tr key={requestData.id}>
                <td className='text-break'>
                    {requestData.trackName}
                    {isSpotifyUrl ? (<a href={requestData.spotifyUrl} className='spotify-link' target="_blank" rel="noopener noreferrer">Open in Spotify</a>) : ''}
                </td>
                <td>{requestData.userName}</td>
                <td>{requestData.status}</td>
                <td>
                    {requestData.status === 'Pending' && <button className='btn btn-primary' onClick={() => handleRequestStateChange(requestData.id, 'Queued')}>Queued</button>}
                    {requestData.status === 'Queued' && <button className='btn btn-primary' onClick={() => handleRequestStateChange(requestData.id, 'Played')}>Played</button>}
                    {requestData.status !== 'Pending' && <button className='btn btn-danger mx-3' onClick={() => handleRequestStateChange(requestData.id, 'Pending')}>Reset</button>}
                    {allowDelete && <button className='btn btn-danger mx-3' onClick={() => handleDeleteRequest(requestData.id)}>Delete</button>}
                </td>
            </tr>
          )}
        )};

    const renderEventCollection = () => {
        return eventCollection && eventCollection.map((eventData) => {
          return (
            <option key={eventData.id} value={eventData.id}>{getDateString(eventData.date)}, {eventData.name}</option>
          )}
        )};

    const handleEventChange = (event) => {
        const selectedEventId = event.target.value;
        const selectedEvent = eventCollection.find(x => x.id === selectedEventId);
        if (selectedEvent) {
            selectEvent(selectedEvent);
        }
    };

    const getDateString = (date) => {
        return format(new Date(date), 'yyyy-MM-dd');
    };

    const reloadRequests = () => {
        if (selectedEvent && selectedEvent.id) {
            getMusicRequests(selectedEvent);
        }
        else if (eventCollection && eventCollection.length > 0) {
            getMusicRequests(eventCollection[0]);
        }
    }

    const handleRequestStateChange = async (requestId, status) =>
    {
        const requestData = {
            requestId: requestId,
            status: status
        };

        try {
            await axios.post(import.meta.env.VITE_APP_REQUESTS_STATUS, requestData);
            await getMusicRequests(selectedEvent);
        } catch (error) {
            console.error('Error submitting request', error);
        }
    }

    const handleDeleteRequest = async (requestId) => {
        try {
            await axios.delete(import.meta.env.VITE_APP_REQUESTS_DELETE, { params: { requestId: requestId } });
            await getMusicRequests(selectedEvent);
        } catch (error) {
            console.error('Error deleting request', error);
        }
    }

    return (
        <>
        <Card className='mb-3'>
            <Card.Header>Music Requests</Card.Header>
            <Card.Body>
                <Form.Group className='mb-3'>
                    <Form.Label>Select Event</Form.Label>
                    <Form.Select aria-label="Default select example" onChange={handleEventChange}>
                        {renderEventCollection()}
                    </Form.Select>
                </Form.Group>
                <div className='my-3'>
                    <Form.Check 
                        type='checkbox'
                        label='Allow Deleting Requests'
                        checked={allowDelete}
                        onChange={(e) => setAllowDelete(e.target.checked)}
                    />
                </div>
                <table className='table table-striped'>
                    <thead>
                        <tr>
                            <th>Request</th>
                            <th>Requested By</th>
                            <th>Status</th>
                            <th>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {renderRequestCollection()}
                    </tbody>
                </table>
            </Card.Body>
        </Card>
        <DjRequestForm/>
        <SearchTermInsights />
        </>
    )
}

DjPortal.propTypes = {
    eventData: PropTypes.shape({
        id: PropTypes.string,
        date: PropTypes.string,
        name: PropTypes.string,
        times: PropTypes.string,
        locationName: PropTypes.string,
        locationAddress: PropTypes.string,
        isRequestable: PropTypes.bool,
        calendarInviteUrl: PropTypes.string,
        facebookEventUrl: PropTypes.string
    }),
    requestData: PropTypes.shape({
        id: PropTypes.string,
        userName: PropTypes.string,
        trackName: PropTypes.string,
        status: PropTypes.string
    })
};

export default DjPortal
