import { useState } from 'react';
import axios from 'axios';
import { Button, Alert } from 'react-bootstrap';

function UpdateEventIndex() {
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState(null);
    const [messageType, setMessageType] = useState('success');

    const handleUpdateIndex = async () => {
        setIsLoading(true);
        setMessage(null);

        try {
            await axios.post(import.meta.env.VITE_APP_EVENTS_UPDATEINDEX);

            setMessageType('success');
            setMessage('Event index updated successfully. New fields are now available without affecting existing events.');
        } catch (error) {
            setMessageType('danger');
            setMessage(error.response?.data?.error || 'Failed to update the event index. Please try again.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className='mb-3 border-start border-primary border-3 px-3'>
            <label className='form-label'>
                Update the event search index to add any new fields. This is non-destructive and preserves all existing events.
            </label>
            <br/>
            <Button
                variant='primary'
                onClick={handleUpdateIndex}
                disabled={isLoading}
            >
                {isLoading ? 'Updating Index...' : 'Update Event Index'}
            </Button>

            {message && (
                <Alert variant={messageType} className="mt-3 mb-0">
                    {message}
                </Alert>
            )}
        </div>
    );
}

export default UpdateEventIndex;
