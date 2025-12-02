import { useContext, useState, useCallback } from 'react'
import { AppContext } from './../AppContext.jsx';
import { Form, Card, FormGroup, Button } from 'react-bootstrap';
import axios from 'axios';

function DjRequestForm() {

    const [requestorName, setRequestorName] = useState('');
    const [trackName, setTrackName] = useState('');
    const [showSuggestions, setShowSuggestions] = useState(true);
    const [trackSuggestions, setTrackSuggestions] = useState([]);
    const { selectedEvent, getMusicRequests } = useContext(AppContext);

    const handleRequestorNameChange = (event) => {
        setRequestorName(event.target.value);
    };

    const handleTrackNameChange = (event) => {
        const value = event.target.value;
        setTrackName(value);
        handleTrackSearch(value);
    };

    const handleTrackNameKeyPress = () => {
        setShowSuggestions(true);
    };

    const handleSuggestionClick = (event) => {
        const buttonText = event.target.innerText;
        setTrackName(buttonText);
        setShowSuggestions(false);
    };

    const handleSubmitRequest = async (event) => {
        event.preventDefault();
        
        const requestData = {
            eventId: selectedEvent.id,
            musicRequest: trackName,
            requestedBy: requestorName
        };

        try {
            await axios.post(import.meta.env.VITE_APP_REQUESTS_SUBMIT, requestData);
            await getMusicRequests(selectedEvent);
        } catch (error) {
            console.error('Error submitting request', error);
        }
    };

    // Debounce function
    const debounce = (func, delay) => {
        let debounceTimer;
        return function(...args) {
            const context = this;
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => func.apply(context, args), delay);
        };
    };

    // Handle track search with debounce
    const handleTrackSearch = useCallback(
        debounce(async (trackName) => {
            if (trackName.length < 3) return;

            await axios.get(import.meta.env.VITE_APP_TRACKS_LIST, { params: { query: trackName } })
                .then((response) => {
                    if (response && response.data && Array.isArray(response.data)) {
                        setTrackSuggestions(response.data);
                    }
                    else {
                        console.error('Invalid response data');
                    }
                },
            () => {
                console.error('Error fetching track suggestions');
            });

        }, 1000),
        []
    );

    const renderTrackSuggestions = () => {
        return trackSuggestions && trackSuggestions.map((track, index) => {
          return (
            <li key={index} className='list-group-item'><Button variant='link' onClick={handleSuggestionClick}>{track.title}, {track.artist}</Button></li>
          )}
        )};

    return(
        <Card className='my-3'>
            <Card.Header>Request a Track</Card.Header>
            <Card.Body>
                <Form>
                    <FormGroup className='mb-3' controlId='formRequestor'>
                        <Form.Label className='fw-bold d-block'>Dancer</Form.Label>
                        <Form.Control type='text' placeholder='Your Name' value={requestorName} onChange={handleRequestorNameChange} required={true} />
                    </FormGroup>
                    <FormGroup className='mb-3' controlId='formMusicRequest'>
                        <Form.Label className='fw-bold d-block'>Track Request</Form.Label>
                        <Form.Control type='text' placeholder='Enter a track name and artist here' value={trackName} onChange={handleTrackNameChange} onKeyDown={handleTrackNameKeyPress} required={true} />
                        { showSuggestions ? <ul className='list-group my-3'>{renderTrackSuggestions()}</ul> : null }
                    </FormGroup>
                    <Form.Group className='my-3'>
                        <Button type='submit' onClick={handleSubmitRequest}>Submit</Button>
                    </Form.Group>
                </Form>
            </Card.Body>
        </Card>
    )
}

export default DjRequestForm
