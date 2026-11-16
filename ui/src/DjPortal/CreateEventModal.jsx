import { useState, useContext, useEffect, useRef } from 'react'
import { AppContext } from './../AppContext.jsx';
import { Button, Form, Modal, ModalBody, Spinner } from 'react-bootstrap';
import PropTypes from 'prop-types';
import axios from 'axios';

const DEFAULT_TIMES = '20:00 - 23:30';
const DEFAULT_ORGANIZER = 'DJ Mark';

// Regular venues, so their house name and address are used whenever one of them is picked.
const KNOWN_VENUES = [
    { match: 'Tadcaster', locationName: 'Tadcaster Riley Smith Hall', locationAddress: 'Westgate, Tadcaster, LS24 9AB' },
    { match: 'Queens Hall', locationName: 'Queens Hall, Leeds', locationAddress: 'Parish Lane, Morley, Leeds, LS27 8DW' },
    { match: 'Bramhope', locationName: 'West Park Rugby Club', locationAddress: 'Bramhope, Leeds, LS16 9JR' },
    { match: 'Energise', locationName: 'Collingham Memorial Hall', locationAddress: 'Main Street, Collingham, LS22 5AS', times: '15:00 - 18:30' }
];

const postcodeOf = (address) => (address ?? '').split(',').pop().replace(/\s+/g, '').toUpperCase();

// Typing an event name picks a venue by its name.
const findVenueByName = (name) => KNOWN_VENUES.find((venue) => name.includes(venue.match));

// A name-based guess only fills fields that are empty or still hold an earlier guess, so a venue or times
// that were imported or typed by hand are never overwritten by editing the name.
const isGuessableLocation = (locationName, locationAddress) =>
    (!locationName.trim() && !locationAddress.trim())
    || KNOWN_VENUES.some((venue) => venue.locationName === locationName && venue.locationAddress === locationAddress);

const isGuessableTimes = (times) =>
    !times.trim() || times === DEFAULT_TIMES || KNOWN_VENUES.some((venue) => venue.times === times);

// Imported events are matched on postcode instead, so a "Tadcaster" event at a different hall keeps its own address.
const findVenueByAddress = (address) => {
    const postcode = postcodeOf(address);
    return postcode ? KNOWN_VENUES.find((venue) => postcodeOf(venue.locationAddress) === postcode) : undefined;
};

/**
 * With no props it renders its own "Create Event" button and opens empty.
 * Pass `show` (with `onHide`) to control it from outside, and `prefill` to seed the form each time it opens.
 */
function CreateEventModal({ show, prefill, onHide })
{
    const isControlled = show !== undefined;
    const [internalShow, setInternalShow] = useState(false);
    const showModal = isControlled ? show : internalShow;

    const [eventName, setEventName] = useState('');
    const [eventDescription, setEventDescription] = useState('');
    const [locationName, setLocationName] = useState('');
    const [locationAddress, setLocationAddress] = useState('');
    const [eventDate, setEventDate] = useState('');
    const [eventTimes, setEventTimes] = useState(DEFAULT_TIMES);
    const [facebookEventId, setFacebookEventId] = useState('');
    const [tags, setTags] = useState('');
    const [organizer, setOrganizer] = useState(DEFAULT_ORGANIZER);
    const [isRequestable, setIsRequestable] = useState(false);

    const [isGenerating, setIsGenerating] = useState(false);
    const [generateError, setGenerateError] = useState('');
    // The description from before the AI replaced it, so it can be restored. Null when there's nothing to undo.
    const [previousDescription, setPreviousDescription] = useState(null);
    // Bumped whenever the form is reset, so a reply that lands after Cancel can't refill the description.
    const generationId = useRef(0);

    const { getEventCollection } = useContext(AppContext);

    const resetAiState = () =>
    {
        generationId.current += 1;
        setIsGenerating(false);
        setGenerateError('');
        setPreviousDescription(null);
    }

    // Seed the form whenever it opens with new pre-filled details.
    useEffect(() => {
        if (show && prefill) {
            resetAiState();
            const venue = findVenueByAddress(prefill.locationAddress);
            setEventName(prefill.name ?? '');
            setEventDescription(prefill.description ?? '');
            setLocationName(venue?.locationName ?? prefill.locationName ?? '');
            setLocationAddress(venue?.locationAddress ?? prefill.locationAddress ?? '');
            setEventDate(prefill.date ?? '');
            setEventTimes(prefill.times ?? DEFAULT_TIMES);
            setFacebookEventId('');
            setTags(prefill.tags ?? '');
            setOrganizer(prefill.organizer ?? DEFAULT_ORGANIZER);
            setIsRequestable(false);
        }
    }, [show, prefill]);

    const closeModal = () =>
    {
        resetAiState();
        if (isControlled)
        {
            onHide?.();
        }
        else
        {
            setInternalShow(false);
        }
    }

    const handleEventNameChange = (event) =>
    {
        var value = event.target.value;
        setEventName(value);

        const venue = findVenueByName(value);
        if (!venue)
        {
            return;
        }

        if (isGuessableLocation(locationName, locationAddress))
        {
            setLocationName(venue.locationName);
            setLocationAddress(venue.locationAddress);
        }

        if (venue.times && isGuessableTimes(eventTimes))
        {
            setEventTimes(venue.times);
        }
    }

    const handleDiscardFormData = () =>
    {
        setEventName('');
        setEventDescription('');
        setLocationName('');
        setLocationAddress('');
        setEventDate('');
        setEventTimes(DEFAULT_TIMES);
        setFacebookEventId('');
        setTags('');
        setOrganizer(DEFAULT_ORGANIZER);
        setIsRequestable(false);
        resetAiState();
        closeModal();
    }

    const buildPayload = () => ({
        name: eventName,
        description: eventDescription,
        locationName: locationName,
        locationAddress: locationAddress,
        date: eventDate,
        times: eventTimes,
        facebookEventId: facebookEventId,
        tags: tags,
        organizer: organizer,
        isRequestable: isRequestable
    });

    // Sends everything currently in the form, so the new description reflects any edits made so far.
    const handleRegenerateDescription = async () =>
    {
        const thisGeneration = ++generationId.current;
        setIsGenerating(true);
        setGenerateError('');

        try
        {
            const response = await axios.post(import.meta.env.VITE_APP_IMPORT_DESCRIPTION, buildPayload());
            if (thisGeneration !== generationId.current) return;

            setPreviousDescription(eventDescription);
            setEventDescription(response.data.description);
        }
        catch (error)
        {
            if (thisGeneration !== generationId.current) return;

            setGenerateError(error.response?.status === 401
                ? 'You need to be signed in to use the AI.'
                : error.response?.data?.message ?? 'Couldn\'t write a description just now. Please try again.');
        }
        finally
        {
            if (thisGeneration === generationId.current)
            {
                setIsGenerating(false);
            }
        }
    }

    const handleUndoDescription = () =>
    {
        setEventDescription(previousDescription ?? '');
        setPreviousDescription(null);
    }

    const handleSubmitEvent = async (event) =>
    {
        event.preventDefault();
        const payload = buildPayload();

        await axios.post(import.meta.env.VITE_APP_EVENTS_CREATE, payload, { headers: { 'Content-Type': 'application/json' } })
            .then(() => setTimeout(() => { getEventCollection(); }, 1000))
            .catch((error) => {
                console.error('Error creating event: ' + error);
            });

        closeModal();
    }

    return (
        <>
            {!isControlled && (
                <div className='mb-3 border-start border-primary border-3 px-3'>
                    <label className='form-label'>Create a New Event</label><br/>
                    <Button variant='primary' onClick={() => setInternalShow(true)}>Create Event</Button>
                </div>
            )}
            <Modal size='lg' show={showModal} onHide={closeModal}>
                <Form>
                    <Modal.Header closeButton className='py-2'>
                        <Modal.Title>Create Event</Modal.Title>
                    </Modal.Header>
                    <ModalBody>
                        <Form.Group className='mb-3'>
                            <Form.Label>Event Name</Form.Label>
                            <Form.Control type='text' placeholder='Name the event' list='standardEventNames' value={eventName} onChange={handleEventNameChange} />
                            <datalist id='standardEventNames'>
                                <option value='Tadcaster Friday Freestyle'>Tadcaster Friday Freestyle</option>
                                <option value='Queens Hall Friday Freestyle'>Queens Hall Friday Freestyle</option>
                                <option value='Bramhope Friday Freestyle'>Bramhope Friday Freestyle</option>
                            </datalist>
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <div className='d-flex justify-content-between align-items-end mb-2'>
                                <Form.Label className='mb-0'>Event Description</Form.Label>
                                <div>
                                    {previousDescription !== null && !isGenerating && (
                                        <Button variant='link' size='sm' className='p-0 me-3' onClick={handleUndoDescription}>Undo</Button>
                                    )}
                                    <Button variant='outline-primary' size='sm' disabled={isGenerating || !eventName.trim()} onClick={handleRegenerateDescription}
                                        title={eventName.trim() ? 'Write a new description from the details in this form' : 'Enter an event name first'}>
                                        {isGenerating
                                            ? <><Spinner as='span' animation='border' size='sm' aria-hidden='true' className='me-1' />Writing…</>
                                            : eventDescription.trim() ? 'Regenerate with AI' : 'Write with AI'}
                                    </Button>
                                </div>
                            </div>
                            <Form.Control as='textarea' rows={5} placeholder='Describe the event' value={eventDescription} disabled={isGenerating}
                                onChange={(event) => { setEventDescription(event.target.value); setPreviousDescription(null); }} />
                            {generateError && <div className='form-text text-danger'>{generateError}</div>}
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Location Name</Form.Label>
                            <Form.Control type='text' placeholder='Name of the location' value={locationName} onChange={(event) => setLocationName(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Location Address</Form.Label>
                            <Form.Control type='text' placeholder='Address of the location' value={locationAddress} onChange={(event) => setLocationAddress(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Event Date</Form.Label>
                            <Form.Control type='date' value={eventDate} onChange={(event) => setEventDate(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Event Times</Form.Label>
                            <Form.Control type='text' placeholder='Start and end times' value={eventTimes} onChange={(event) => setEventTimes(event.target.value)} />
                            <div className='form-text'>Enter the times in <em>20:00 - 23:30</em> format.</div>
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Facebook Event URL</Form.Label>
                            <Form.Control type='text' placeholder='e.g. https://www.facebook.com/events/...' value={facebookEventId} onChange={(event) => setFacebookEventId(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Tags</Form.Label>
                            <Form.Control type='text' placeholder='e.g. tag-one, tag-two' value={tags} onChange={(event) => setTags(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Organizer</Form.Label>
                            <Form.Select value={organizer} onChange={(event) => setOrganizer(event.target.value)}>
                                <option value='DJ Mark'>DJ Mark</option>
                                <option value='Ceroc Yorkshire'>Ceroc Yorkshire</option>
                            </Form.Select>
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Check type='switch' label='Is this event requestable?' checked={isRequestable} onChange={(event) => setIsRequestable(event.target.checked) } />
                        </Form.Group>
                    </ModalBody>
                    <Modal.Footer>
                        <Button variant='primary' type='submit' onClick={handleSubmitEvent} disabled={isGenerating}>Create</Button>
                        <Button variant='secondary' onClick={handleDiscardFormData}>Cancel</Button>
                    </Modal.Footer>
                </Form>
            </Modal>
        </>
    )
}

CreateEventModal.propTypes = {
    show: PropTypes.bool,
    onHide: PropTypes.func,
    prefill: PropTypes.shape({
        name: PropTypes.string,
        description: PropTypes.string,
        locationName: PropTypes.string,
        locationAddress: PropTypes.string,
        date: PropTypes.string,
        times: PropTypes.string,
        tags: PropTypes.string,
        organizer: PropTypes.string
    })
};

export default CreateEventModal;
