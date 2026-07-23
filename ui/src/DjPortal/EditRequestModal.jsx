import { useState, useCallback, useEffect } from 'react'
import { Button, Form, Modal, ModalBody } from 'react-bootstrap';
import PropTypes from 'prop-types';
import axios from 'axios';

function EditRequestModal({ show, requestData, onHide, onSaved }) {

    const [trackName, setTrackName] = useState('');
    const [trackBpm, setTrackBpm] = useState(0);
    const [trackTime, setTrackTime] = useState(null);
    const [showSuggestions, setShowSuggestions] = useState(false);
    const [trackSuggestions, setTrackSuggestions] = useState([]);

    // Seed the edit box with the current values whenever a new request is opened.
    useEffect(() => {
        if (show && requestData) {
            setTrackName(requestData.trackName ?? '');
            setTrackBpm(requestData.bpm ?? 0);
            setTrackTime(requestData.time ?? null);
            setShowSuggestions(false);
            setTrackSuggestions([]);
        }
    }, [show, requestData]);

    const handleTrackNameChange = (event) => {
        const value = event.target.value;
        setTrackName(value);
        handleTrackSearch(value);
    };

    const handleTrackNameKeyPress = () => {
        setShowSuggestions(true);
    };

    const handleTrackNameKeyUp = (event) => {
        if (!/^[a-z0-9]$/i.test(event.key)) {
            return;
        }

        setTrackBpm(0);
        setTrackTime(null);
    };

    const handleSuggestionClick = (event) => {
        const buttonText = event.target.innerText;
        setTrackName(buttonText);
        setShowSuggestions(false);
        setTrackTime(event.target.dataset.time ?? null);
        setTrackBpm(event.target.dataset.bpm ?? 0);
    };

    const handleSave = async (event) => {
        event.preventDefault();

        const payload = {
            requestId: requestData.id,
            trackName: trackName,
            bpm: trackBpm ?? 0,
            time: trackTime ?? ''
        };

        try {
            await axios.post(import.meta.env.VITE_APP_REQUESTS_UPDATETRACK, payload);
            if (onSaved) {
                await onSaved();
            }
            onHide();
        } catch (error) {
            console.error('Error updating request', error);
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
            <li key={index} className='list-group-item'><Button variant='link' onClick={handleSuggestionClick} data-time={track.time} data-bpm={track.bpm}>{track.title}, {track.artist}</Button></li>
          )}
        )};

    return (
        <Modal size='lg' show={show} onHide={onHide}>
            <Form>
                <Modal.Header closeButton className='py-2'>
                    <Modal.Title>Correct Track Request</Modal.Title>
                </Modal.Header>
                <ModalBody>
                    <Form.Group className='mb-3'>
                        <Form.Label className='fw-bold d-block'>Current Value</Form.Label>
                        <p className='text-muted mb-0'>{requestData?.trackName}</p>
                    </Form.Group>
                    <Form.Group className='mb-3' controlId='formEditMusicRequest'>
                        <Form.Label className='fw-bold d-block'>Corrected Value</Form.Label>
                        <Form.Control type='text' placeholder='Enter a track name and artist here' value={trackName} onChange={handleTrackNameChange} onKeyDown={handleTrackNameKeyPress} onKeyUp={handleTrackNameKeyUp} required={true} />
                        { showSuggestions ? <ul className='list-group my-3'>{renderTrackSuggestions()}</ul> : null }
                    </Form.Group>
                </ModalBody>
                <Modal.Footer>
                    <Button variant='primary' type='submit' onClick={handleSave}>Save</Button>
                    <Button variant='secondary' onClick={onHide}>Cancel</Button>
                </Modal.Footer>
            </Form>
        </Modal>
    )
}

EditRequestModal.propTypes = {
    show: PropTypes.bool,
    requestData: PropTypes.shape({
        id: PropTypes.string,
        trackName: PropTypes.string,
        bpm: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
        time: PropTypes.string
    }),
    onHide: PropTypes.func,
    onSaved: PropTypes.func
};

export default EditRequestModal;
