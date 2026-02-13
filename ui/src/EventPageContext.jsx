import { useState, useEffect } from "react";
import PropTypes from 'prop-types';
import axios from 'axios';
import { AppContext } from './AppContext.jsx';

export const EventPageProvider = ({ children }) => {

    const eventData = window.__EVENT_DATA__ || {};
    const [selectedEvent] = useState(eventData);
    const [requestCollection, setRequestCollection] = useState([]);
    const [eventCollection] = useState(eventData.id ? [eventData] : []);
    const [selectedView] = useState('details');

    useEffect(() => {
        if (selectedEvent && selectedEvent.id) {
            getMusicRequests(selectedEvent);
        }
    }, []);

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
        <AppContext.Provider value={{ eventCollection, selectedEvent, selectedView, requestCollection, getEventCollection, selectEvent, deselectEvent, getMusicRequests }}>
            {children}
        </AppContext.Provider>
    )
};

EventPageProvider.propTypes = {
    children: PropTypes.node.isRequired,
};
