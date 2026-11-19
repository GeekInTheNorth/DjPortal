import { useState, useEffect } from "react";
import PropTypes from 'prop-types';
import axios from 'axios';
import { AppContext } from './AppContext.jsx';
import { REQUEST_CREATED_EVENT } from './webmcp.js';

export const EventPageProvider = ({ children }) => {

    const eventData = window.__EVENT_DATA__ || {};
    const [selectedEvent, setSelectedEvent] = useState(eventData);
    const [requestCollection, setRequestCollection] = useState([]);
    const [requestsLoaded, setRequestsLoaded] = useState(false);
    const [eventCollection, setEventCollection] = useState(eventData.id ? [eventData] : []);

    useEffect(() => {
        if (!eventData.id) {
            return;
        }

        refreshEvent(eventData.id);
        getMusicRequests(eventData);

        // A WebMCP agent can submit a request without going through the form, so refresh the list when it does.
        const handleRequestCreated = (event) => {
            if (event.detail?.eventId === eventData.id) {
                getMusicRequests(eventData);
            }
        };

        window.addEventListener(REQUEST_CREATED_EVENT, handleRequestCreated);
        return () => window.removeEventListener(REQUEST_CREATED_EVENT, handleRequestCreated);
    }, []);

    const refreshEvent = async (eventId) => {
        try {
            const response = await axios.get(`${import.meta.env.VITE_APP_EVENTS_GET}${eventId}`);
            if (response.data) {
                setSelectedEvent(response.data);
                setEventCollection([response.data]);
            }
        } catch {
            console.error('Failed to refresh event data.');
        }
    };

    const getMusicRequests = async (event) => {
        setRequestCollection([]);
        setRequestsLoaded(false);

        setTimeout(() => {
            axios.get(import.meta.env.VITE_APP_REQUESTS_LIST, { params: { eventId: event.id } })
                .then((response) => {
                    if (response.data && Array.isArray(response.data)) {
                        setRequestCollection(response.data);
                        setRequestsLoaded(true);
                    } else {
                        console.error('Invalid music request data.');
                    }
                },
                () => {
                    console.error('Failed to retrieve requests for event.');
                });
        }, 1000);
    };

    const selectEvent = () => {};
    const deselectEvent = () => {};
    const getEventCollection = () => {};

    return (
        <AppContext.Provider value={{ eventCollection, selectedEvent, requestCollection, requestsLoaded, getEventCollection, selectEvent, deselectEvent, getMusicRequests }}>
            {children}
        </AppContext.Provider>
    )
};

EventPageProvider.propTypes = {
    children: PropTypes.node.isRequired,
};
