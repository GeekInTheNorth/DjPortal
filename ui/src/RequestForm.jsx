import { useContext, useState, useCallback } from 'react'
import { AppContext } from './AppContext.jsx';
import { Form, Card, FormGroup, Button } from 'react-bootstrap';
import axios from 'axios';

function RequestForm() {

    const [requestorName, setRequestorName] = useState('');
    const [trackName, setTrackName] = useState('');
    const [showSuggestions, setShowSuggestions] = useState(true);
    const [trackSuggestions, setTrackSuggestions] = useState([]);
    const [showForm, setShowForm] = useState(true);
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

    const handleSetShowForm = () => {
        setTrackName('');
        setShowForm(true);
    }

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
            setShowForm(false);
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

    const renderThankYou = () => {
        return (
            <div className='alert alert-success my-3' role='alert'>
                <h4 className='alert-heading'>Thank you for your request!</h4>
                <p>DJ Mark will review and hope to play your request.</p>
                <hr />
                <Button variant='primary' onClick={handleSetShowForm}>Submit Another Request</Button>
            </div>
        )
    }

    const renderForm = () => {
        return (
            <Card className='my-3'>
                <Card.Header className='bg-primary text-light fw-bold'>Request a Track</Card.Header>
                <Card.Body>
                    <Form>
                        <FormGroup className='mb-3' controlId='formRequestor'>
                            <Form.Label className='fw-bold d-block'>Your Name</Form.Label>
                            <div className='form-text'>Your name will only be visible to you and the DJ.</div>
                            <Form.Control type='text' placeholder='Your Name' value={requestorName} onChange={handleRequestorNameChange} required={true} />
                        </FormGroup>
                        <FormGroup className='mb-3' controlId='formMusicRequest'>
                            <Form.Label className='fw-bold d-block'>Your Request</Form.Label>
                            <div className='form-text'>Type the song name and artist here, enter a <strong>spotify</strong> link or select one of the suggestions below as you type.</div>
                            <Form.Control type='text' placeholder='Enter a track name and artist here' value={trackName} onChange={handleTrackNameChange} onKeyDown={handleTrackNameKeyPress} required={true} />
                            { showSuggestions ? <ul className='list-group my-3'>{renderTrackSuggestions()}</ul> : null }
                        </FormGroup>
                        <Form.Group className='my-3'>
                            <Button type='submit' onClick={handleSubmitRequest}>Submit Your Track Request</Button>
                        </Form.Group>
                    </Form>
                </Card.Body>
            </Card>
        )
    };

    return(
        <>
            { showForm ? renderForm() : renderThankYou() }
        </>
    )
}

export default RequestForm
