import { useState, useEffect } from "react";
import PropTypes from 'prop-types';
import axios from 'axios';
import { AppContext } from './AppContext.jsx';

export const EventPageProvider = ({ children }) => {

    const eventData = window.__EVENT_DATA__ || {};
    const [selectedEvent, setSelectedEvent] = useState(eventData);
    const [requestCollection, setRequestCollection] = useState([]);
    const [eventCollection, setEventCollection] = useState(eventData.id ? [eventData] : []);

    useEffect(() => {
        if (eventData.id) {
            refreshEvent(eventData.id);
            getMusicRequests(eventData);
        }
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

        setTimeout(() => {
            axios.get(import.meta.env.VITE_APP_REQUESTS_LIST, { params: { eventId: event.id } })
                .then((response) => {
                    if (response.data && Array.isArray(response.data)) {
                        setRequestCollection(response.data);
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
        <AppContext.Provider value={{ eventCollection, selectedEvent, requestCollection, getEventCollection, selectEvent, deselectEvent, getMusicRequests }}>
            {children}
        </AppContext.Provider>
    )
};

EventPageProvider.propTypes = {
    children: PropTypes.node.isRequired,
};
