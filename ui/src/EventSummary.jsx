import { useContext } from 'react'
import { AppContext } from './AppContext.jsx';
import { format } from 'date-fns';
import { useState, useEffect } from 'react'
import { Card, Button, Badge, Alert } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCalendar } from '@fortawesome/free-solid-svg-icons'
import { faFacebookF } from "@fortawesome/free-brands-svg-icons"
import PropTypes from 'prop-types';

function EventSummary(props) {

    const { selectEvent, deselectEvent } = useContext(AppContext);

    const [eventName, setEventName] = useState('');
    const [eventDescription, setEventDescription] = useState('');
    const [eventDate, setEventDate] = useState('');
    const [eventTime, setEventTime] = useState('');
    const [eventCalendarUrl, setEventCalendarUrl] = useState('');
    const [eventLocationName, setEventLocationName] = useState('');
    const [eventLocationAddress, setEventLocationAddress] = useState('');
    const [eventFacebookUrl, setEventFacebookUrl] = useState('');
    const [tagList, setTagList] = useState([]);

    useEffect(() => {
        transformEventData()
      })

    const transformEventData = () => {
        if (props.eventData) {
            setEventDate(format(new Date(props.eventData.date), 'dd MMMM yyyy'));
            setEventName(props.eventData.name);
            setEventDescription(props.eventData.description);
            setEventTime(props.eventData.times);
            setEventLocationName(props.eventData.locationName);
            setEventLocationAddress(props.eventData.locationAddress);
            setEventCalendarUrl(props.eventData.calendarInviteUrl);
            setEventFacebookUrl(props.eventData.facebookEventUrl);
            setTagList(props.eventData.tagList ?? []);
        }
    }

    const renderFooter = () => {
        const couldShowCta = props.eventData.isRequestable ?? false;
        const shouldShowCta = props.showRequestCta ?? false;

        if (couldShowCta && shouldShowCta) {
            return renderFooterCta();
        } else if (shouldShowCta) {
            return renderDefaultFooter();
        } else { 
            return renderBackToEventsCta();
        } 
    }

    const renderFooterCta = () => {
        return(
            <Card.Footer>
                <Button variant='primary' onClick={handleCtaClick}>Request a Track</Button>
                {renderCalendarCta()}
                {renderFacebookCta()}
            </Card.Footer>
        )
    }

    const renderBackToEventsCta = () => {
        return(
            <Card.Footer className='text-end'>
                {renderCalendarCta()}
                {renderFacebookCta()}
                <Button variant='info' onClick={deselectEvent}>See More Events</Button>
            </Card.Footer>
        )
    }

    const renderDefaultFooter = () => {
        return(
            <Card.Footer>
                {renderCalendarCta()}
                {renderFacebookCta()}
            </Card.Footer>
        )
    }

    const renderTags = () => {
        if (tagList && tagList.length > 0) {
            return (
                <div>
                    {tagList.map((tag, index) => (
                        <Badge key={index} pill bg={tag.colour} className='mx-1'>{tag.name}</Badge>
                    ))}
                </div>
            )
        }

        return null;
    }

    const renderRequestsNotOpen = () => {
        const couldShowCta = props.eventData.isRequestable ?? false;
        if (!couldShowCta && props.showRequestCta) {
            return (
                <small>Requests are not currently open for this event.</small>
            )
        } else if (!couldShowCta) {
            return (
                <Alert variant='warning'>Requests are not currently open for this event.</Alert>
            )
        }

        return null;
    }

    const renderCalendarCta = () => {
        if (eventCalendarUrl && eventCalendarUrl !== '') {
            return (
                <a href={eventCalendarUrl} className='btn btn-success mx-2' download rel='nofollow' title='Add to Calendar'>
                    <FontAwesomeIcon icon={faCalendar} />
                </a>
            )
        }

        return null;
    }

    const renderFacebookCta = () => {
        if (eventFacebookUrl && eventFacebookUrl !== '') {
            return (
                <a href={eventFacebookUrl} className='btn btn-facebook mx-2' target='_blank' rel='nofollow noreferrer' title='View on Facebook'>
                    <FontAwesomeIcon icon={faFacebookF} />
                </a>
            )
        }

        return null;
    }

    const handleCtaClick = () => {
        selectEvent(props.eventData);
    }

    return (
        <Card className='mb-3'>
            <Card.Header className='fw-bold'>{eventName}</Card.Header>
            <Card.Body>
                <p className='card-text'>{eventDescription}</p>
                <p className='card-text'>{eventDate}, {eventTime}</p>
                <p className='card-text'>{eventLocationName}<br/>{eventLocationAddress}</p>
                {renderRequestsNotOpen()}
                {renderTags()}
            </Card.Body>
            {renderFooter()}
        </Card>
    )
}

EventSummary.propTypes = {
    eventData: PropTypes.shape({
        date: PropTypes.string,
        name: PropTypes.string,
        description: PropTypes.string,
        times: PropTypes.string,
        locationName: PropTypes.string,
        locationAddress: PropTypes.string,
        isRequestable: PropTypes.bool,
        calendarInviteUrl: PropTypes.string,
        facebookEventUrl: PropTypes.string,
        tagList: PropTypes.arrayOf(PropTypes.shape({
            name: PropTypes.string,
            colour: PropTypes.string,
        })),
        tags: PropTypes.string,
    }),
    showRequestCta: PropTypes.bool,
};

export default EventSummary