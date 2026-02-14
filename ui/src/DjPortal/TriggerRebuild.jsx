import { useState } from 'react';
import axios from 'axios';
import { Button, Alert } from 'react-bootstrap';

function TriggerRebuild() {
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState(null);
    const [messageType, setMessageType] = useState('success');

    const handleRebuild = async () => {
        setIsLoading(true);
        setMessage(null);

        try {
            await axios.post(import.meta.env.VITE_APP_DEPLOYMENT_REBUILD);

            setMessageType('success');
            setMessage('Rebuild triggered successfully! The deployment will start shortly. Check GitHub Actions for progress.');
        } catch (error) {
            setMessageType('danger');

            if (error.response) {
                const status = error.response.status;
                const errorData = error.response.data;

                if (status === 401) {
                    setMessage('Authentication failed. Please check the GitHub token configuration.');
                } else if (status === 404) {
                    setMessage('Workflow not found. Please verify the workflow configuration.');
                } else if (status === 422) {
                    setMessage('Workflow does not support manual triggering. Please update the workflow file.');
                } else if (status === 429) {
                    setMessage('GitHub API rate limit exceeded. Please try again later.');
                } else {
                    setMessage(errorData?.error || 'Failed to trigger rebuild. Please try again.');
                }
            } else if (error.request) {
                setMessage('No response from server. Please check your connection and try again.');
            } else {
                setMessage('An unexpected error occurred. Please try again.');
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className='mb-3 border-start border-primary border-3 px-3'>
            <label className='form-label'>
                Trigger a rebuild and redeployment of the application. This will rebuild the frontend and API from the latest main branch code.
            </label>
            <br/>
            <Button
                variant='primary'
                onClick={handleRebuild}
                disabled={isLoading}
            >
                {isLoading ? 'Triggering Rebuild...' : 'Trigger Rebuild'}
            </Button>

            {message && (
                <Alert variant={messageType} className="mt-3 mb-0">
                    {message}
                </Alert>
            )}
        </div>
    );
}

export default TriggerRebuild;
