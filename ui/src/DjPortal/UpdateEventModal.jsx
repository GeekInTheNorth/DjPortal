import { useState, useContext } from 'react'
import { AppContext } from './../AppContext.jsx';
import { Button, Form, Modal, ModalBody } from 'react-bootstrap';
import { format } from 'date-fns';
import PropTypes from 'prop-types';
import axios from 'axios';

function UpdateEventModal(props) 
{
    const [showModal, setShowModal] = useState(false);
    const [eventName, setEventName] = useState(props.eventData?.name ?? '');
    const [eventDescription, setEventDescription] = useState(props.eventData?.description ?? '');
    const [locationName, setLocationName] = useState(props.eventData?.locationName ?? '');
    const [locationAddress, setLocationAddress] = useState(props.eventData?.locationAddress ?? '');
    const [eventDate, setEventDate] = useState(format(new Date(props.eventData?.date), 'yyyy-MM-dd'));
    const [eventTimes, setEventTimes] = useState(props.eventData?.times ?? '20:00 - 23:30');
    const [facebookEventId, setFacebookEventId] = useState(props.eventData?.facebookEventId ?? '');
    const [tags, setTags] = useState(props.eventData?.tags ?? '');
    const [isRequestable, setIsRequestable] = useState(props.eventData?.isRequestable ?? false);
    const [generateSchemaData, setGenerateSchemaData] = useState(props.eventData?.generateSchemaData ?? false);
    const [isCancelled, setIsCancelled] = useState(props.eventData?.isCancelled ?? false);

    const { getEventCollection } = useContext(AppContext);

    const handleEventNameChange = (event) =>
    {
        var value = event.target.value;
        setEventName(value);
        if (value.includes('Tadcaster'))
        {
            setLocationName('Tadcaster Riley Smith Hall');
            setLocationAddress('Westgate, Tadcaster, LS24 9AB');
        } else if (value.includes('Queens Hall'))
        {
            setLocationName('Queens Hall, Leeds');
            setLocationAddress('Parish Lane, Morley, Leeds, LS27 8DW');
        } else if (value.includes('Bramhope'))
        {
            setLocationName('West Park Rugby Club');
            setLocationAddress('Bramhope, Leeds, LS16 9JR');
        }
    }

    const handleSubmitEvent = async (event) =>
    {
        event.preventDefault();

        let params = new URLSearchParams();
        params.append('id', props.eventData?.id ?? '');
        params.append('name', eventName);
        params.append('description', eventDescription);
        params.append('locationName', locationName);
        params.append('locationAddress', locationAddress);
        params.append('date', eventDate);
        params.append('times', eventTimes);
        params.append('facebookEventId', facebookEventId);
        params.append('tags', tags);
        params.append('isRequestable', isRequestable);
        params.append('generateSchemaData', generateSchemaData);
        params.append('isCancelled', isCancelled);

        await axios.post(import.meta.env.VITE_APP_EVENTS_UPDATE, params)
            .then(() => setTimeout(() => { getEventCollection(); }, 1000))
            .catch((error) => {
                console.error('Error creating event: ' + error);
            });

        setShowModal(false);
    }

    const handleModalClose = () =>
    {
        setShowModal(false);
        getEventCollection();
    }

    return (
        <>
            <Button variant='primary' size='sm' onClick={() => setShowModal(true)}>Update</Button>
            <Modal show={showModal} onHide={() => { setShowModal(false); }}>
                <Form>
                    <Modal.Header closeButton className='py-2'>
                        <Modal.Title>Update Event</Modal.Title>
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
                            <Form.Label>Event Description</Form.Label>
                            <Form.Control as='textarea' placeholder='Describe the event' value={eventDescription} onChange={(event) => setEventDescription(event.target.value)} />
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
                            <Form.Label>Facebook Event ID</Form.Label>
                            <Form.Control type='text' placeholder='e.g. 000000000000000' value={facebookEventId} onChange={(event) => setFacebookEventId(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Label>Tags</Form.Label>
                            <Form.Control type='text' placeholder='e.g. tag-one, tag-two' value={tags} onChange={(event) => setTags(event.target.value)} />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Check type='switch' label='Is this event requestable?' checked={isRequestable} onChange={(event) => setIsRequestable(event.target.checked) } />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Check type='switch' label='Generate Schema.org Data?' checked={generateSchemaData} onChange={(event) => setGenerateSchemaData(event.target.checked) } />
                        </Form.Group>
                        <Form.Group className='mb-3'>
                            <Form.Check type='switch' label='Is this event cancelled?' checked={isCancelled} onChange={(event) => setIsCancelled(event.target.checked) } />
                        </Form.Group>
                    </ModalBody>
                    <Modal.Footer>
                        <Button variant='primary' type='submit' onClick={handleSubmitEvent}>Update</Button>
                        <Button variant='secondary' onClick={handleModalClose}>Cancel</Button>
                    </Modal.Footer>
                </Form>
            </Modal>
        </>
    )
}

UpdateEventModal.propTypes = {
    eventData: PropTypes.shape({
        id: PropTypes.string,
        date: PropTypes.string,
        name: PropTypes.string,
        description: PropTypes.string,
        times: PropTypes.string,
        locationName: PropTypes.string,
        locationAddress: PropTypes.string,
        facebookEventId: PropTypes.string,
        tagList: PropTypes.arrayOf(PropTypes.arrayOf(PropTypes.string)),
        tags: PropTypes.string,
        isRequestable: PropTypes.bool,
        generateSchemaData: PropTypes.bool,
        isCancelled: PropTypes.bool
    })
};

export default UpdateEventModal;