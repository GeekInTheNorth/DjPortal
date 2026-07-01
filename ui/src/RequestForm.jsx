import { useContext, useState, useCallback } from 'react'
import { AppContext } from './AppContext.jsx';
import { Form, Card, FormGroup, Button, Spinner } from 'react-bootstrap';
import axios from 'axios';
import './RequestForm.css';

const escapeRegExp = (value) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

function RequestForm() {

    const [requestorName, setRequestorName] = useState('');
    const [trackName, setTrackName] = useState('');
    const [trackBpm, setTrackBpm] = useState(0);
    const [trackTime, setTrackTime] = useState(null);
    const [showSuggestions, setShowSuggestions] = useState(true);
    const [trackSuggestions, setTrackSuggestions] = useState([]);
    const [isSearching, setIsSearching] = useState(false);
    const [hasSearched, setHasSearched] = useState(false);
    const [activeIndex, setActiveIndex] = useState(-1);
    const [showForm, setShowForm] = useState(true);
    const [errorMessage, setErrorMessage] = useState('');
    const { selectedEvent, getMusicRequests } = useContext(AppContext);

    const handleRequestorNameChange = (event) => {
        setRequestorName(event.target.value);
    };

    const handleTrackNameChange = (event) => {
        const value = event.target.value;
        setTrackName(value);
        // Typing invalidates any track metadata picked from a previous suggestion.
        setTrackBpm(0);
        setTrackTime(null);
        setShowSuggestions(true);
        setActiveIndex(-1);

        if (value.trim().length < 3) {
            setTrackSuggestions([]);
            setHasSearched(false);
            setIsSearching(false);
            return;
        }

        setIsSearching(true);
        handleTrackSearch(value);
    };

    const selectTrack = (track) => {
        setTrackName(`${track.title}, ${track.artist}`);
        setTrackBpm(track.bpm ?? 0);
        setTrackTime(track.time ?? null);
        setShowSuggestions(false);
        setActiveIndex(-1);
        setTrackSuggestions([]);
        setHasSearched(false);
    };

    const handleTrackNameKeyDown = (event) => {
        if (!showSuggestions || trackSuggestions.length === 0) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                setActiveIndex((index) => (index + 1) % trackSuggestions.length);
                break;
            case 'ArrowUp':
                event.preventDefault();
                setActiveIndex((index) => (index <= 0 ? trackSuggestions.length - 1 : index - 1));
                break;
            case 'Enter':
                if (activeIndex >= 0 && trackSuggestions[activeIndex]) {
                    event.preventDefault();
                    selectTrack(trackSuggestions[activeIndex]);
                }
                break;
            case 'Escape':
                setShowSuggestions(false);
                setActiveIndex(-1);
                break;
            default:
                break;
        }
    };

    const handleTrackNameFocus = () => {
        // Only reshow on focus if we actually have results — never resurface the "no matches" row.
        if (trackName.trim().length >= 3 && trackSuggestions.length > 0) {
            setShowSuggestions(true);
        }
    };

    const handleTrackNameBlur = () => {
        // Delay so a click/tap on a suggestion still registers before we hide it.
        setTimeout(() => setShowSuggestions(false), 150);
    };

    const handleSetShowForm = () => {
        setTrackName('');
        setTrackSuggestions([]);
        setHasSearched(false);
        setShowForm(true);
    }

    const handleSubmitRequest = async (event) => {
        event.preventDefault();

        const requestData = {
            eventId: selectedEvent.id,
            musicRequest: trackName,
            requestedBy: requestorName,
            bpm: trackBpm ?? 0,
            time: trackTime ?? ''
        };

        try {
            setErrorMessage('');
            await axios.post(import.meta.env.VITE_APP_REQUESTS_SUBMIT, requestData);
            await getMusicRequests(selectedEvent);
            setShowForm(false);
            setTrackName('');
            setTrackBpm(0);
            setTrackTime('');
            setTrackSuggestions([]);
            setHasSearched(false);
        } catch (error) {
            if (error.response && error.response.status === 409) {
                setErrorMessage(error.response.data?.message || 'You have reached the maximum number of requests for this event.');
            } else {
                console.error('Error submitting request', error);
            }
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
        debounce(async (query) => {
            if (query.trim().length < 3) {
                setIsSearching(false);
                return;
            }

            try {
                const response = await axios.get(import.meta.env.VITE_APP_TRACKS_LIST, { params: { query } });
                setTrackSuggestions(response?.data && Array.isArray(response.data) ? response.data : []);
            } catch {
                console.error('Error fetching track suggestions');
                setTrackSuggestions([]);
            } finally {
                setIsSearching(false);
                setHasSearched(true);
            }
        }, 300),
        []
    );

    const highlightMatch = (text, query) => {
        const terms = query.trim().split(/\s+/).filter((term) => term.length > 1).map(escapeRegExp);
        if (!text || terms.length === 0) {
            return text;
        }

        const lowerTerms = terms.map((term) => term.toLowerCase());
        const parts = String(text).split(new RegExp(`(${terms.join('|')})`, 'ig'));
        return parts.map((part, index) =>
            lowerTerms.includes(part.toLowerCase()) ? <strong key={index}>{part}</strong> : part
        );
    };

    const showDropdown = showSuggestions
        && trackName.trim().length >= 3
        && (trackSuggestions.length > 0 || (hasSearched && !isSearching));

    const renderDropdown = () => {
        if (!showDropdown) {
            return null;
        }

        return (
            <ul className='list-group request-suggestions'>
                { trackSuggestions.length > 0
                    ? trackSuggestions.map((track, index) => (
                        <li key={index} className={`list-group-item${index === activeIndex ? ' active' : ''}`}>
                            <Button
                                variant='link'
                                onMouseDown={(event) => { event.preventDefault(); selectTrack(track); }}
                                onMouseEnter={() => setActiveIndex(index)}
                            >
                                {highlightMatch(track.title, trackName)}, {highlightMatch(track.artist, trackName)}
                            </Button>
                        </li>
                    ))
                    : (
                        <li className='list-group-item'>
                            <div className='request-suggestions-empty'>No match in DJ Mark&apos;s library — you can still submit your request.</div>
                        </li>
                    )
                }
            </ul>
        );
    };

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
                        { errorMessage && <div className='alert alert-warning' role='alert'>{errorMessage}</div> }
                        <FormGroup className='mb-3' controlId='formRequestor'>
                            <Form.Label className='fw-bold d-block'>Your Name</Form.Label>
                            <div className='form-text'>Your name will only be visible to you and the DJ.</div>
                            <Form.Control type='text' placeholder='Your Name' value={requestorName} onChange={handleRequestorNameChange} required={true} />
                        </FormGroup>
                        <FormGroup className='mb-3' controlId='formMusicRequest'>
                            <Form.Label className='fw-bold d-block'>Your Request</Form.Label>
                            <div className='form-text'>Enter an <strong>artist</strong>, <strong>song name</strong> or <strong>spotify</strong> link here.  Optionally you can you click or tap on a suggestion as they appear and it will complete this field ready to submit.</div>
                            <div className='request-track-field'>
                                <Form.Control
                                    type='text'
                                    placeholder='Enter a track name and artist here'
                                    value={trackName}
                                    onChange={handleTrackNameChange}
                                    onKeyDown={handleTrackNameKeyDown}
                                    onFocus={handleTrackNameFocus}
                                    onBlur={handleTrackNameBlur}
                                    autoComplete='off'
                                    required={true}
                                />
                                { isSearching && <span className='request-track-spinner'><Spinner animation='border' size='sm' variant='primary' role='status' aria-label='Searching tracks' /></span> }
                                { renderDropdown() }
                            </div>
                        </FormGroup>
                        <Form.Group className='my-3'>
                            <Button type='submit' onClick={handleSubmitRequest}>Submit Your Request</Button>
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
