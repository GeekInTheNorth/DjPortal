import { createContext, useState, useEffect } from "react";
import PropTypes from 'prop-types';
import axios from 'axios';

export const AppContext = createContext();

export const AppProvider = ({ children }) => {

    const [eventCollection, setEventCollection] = useState([]);
    const [requestCollection, setRequestCollection] = useState([]);
    const [selectedEvent, setSelectedEvent] = useState({});
    
    useEffect(() => { getEventCollection() }, []);
    
    const getEventCollection = async () => {
        setEventCollection([]);
        
        await axios.get(import.meta.env.VITE_APP_EVENTS_LIST)
            .then((response) => {
                if (response.data && Array.isArray(response.data)){
                    setEventCollection(response.data);
                    if (response.data.length > 0) {
                        setSelectedEvent(response.data[0]);
                        getMusicRequests(response.data[0]);
                    }
                } else {
                    console.error('Invalid event list data');
                }
            },
            () => {
                console.error('Error fetching event list');
            });
        };
    
    const getMusicRequests = async (event) => {
        setTimeout(() => {
            axios.get(import.meta.env.VITE_APP_REQUESTS_LIST, { params: { eventId: event.id } })
                .then((response) => {
                    if (response.data && Array.isArray(response.data))
                    {
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

    const selectEvent = (event) => {
        setSelectedEvent(event);
        getMusicRequests(event);
    };

    const deselectEvent = () => {
        setSelectedEvent({});
        setRequestCollection([]);
    };

    return (
        <AppContext.Provider value={{ eventCollection, selectedEvent, requestCollection, getEventCollection, selectEvent, deselectEvent, getMusicRequests }}>
            {children}
        </AppContext.Provider>
    )
};

AppProvider.propTypes = {
    children: PropTypes.node.isRequired,
};