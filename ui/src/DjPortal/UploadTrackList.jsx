import { useState } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';

function UploadTrackList()
{
    const [selectedFile, setSelectedFile] = useState(null);
    const [isUploading, setIsUploading] = useState(false);
    const [message, setMessage] = useState('');

    const handleFileChange = (event) => {
        const file = event.target.files[0];
        if (file) {
            setSelectedFile(file);
            setMessage('');
        }
    };

    const handleSubmit = async (event) => {
        event.preventDefault();
        
        if (!selectedFile) {
            setMessage('Please select a CSV file to upload.');
            return;
        }

        setIsUploading(true);
        setMessage('');

        const formData = new FormData();
        formData.append('file', selectedFile);

        try {
            const response = await fetch(import.meta.env.VITE_APP_TRACKS_CSVUPLOAD, {
                method: 'POST',
                body: formData,
            });

            if (response.ok) {
                setMessage('Track list uploaded successfully!');
                setSelectedFile(null);
                // Reset the file input
                event.target.reset();
            } else {
                const errorData = await response.json().catch(() => ({}));
                setMessage(`Upload failed: ${errorData.message || response.statusText}`);
            }
        } catch (error) {
            setMessage(`Upload error: ${error.message}`);
        } finally {
            setIsUploading(false);
        }
    };

    return (
        <Form onSubmit={handleSubmit} className='mb-3 border-start border-primary border-3 px-3'>
            <Form.Group controlId="formFile" className="mb-3">
                <Form.Label>Import Playlist CSV File</Form.Label>
                <Form.Control type="file" accept=".csv" onChange={handleFileChange} disabled={isUploading} />
            </Form.Group>
            <Button variant="primary" type="submit" disabled={isUploading || !selectedFile}>
                {isUploading ? 'Uploading...' : 'Upload CSV'}
            </Button>
            {message && (
                <Alert variant={message.includes('success') ? 'success' : 'danger'} className="mt-3">
                    {message}
                </Alert>
            )}
        </Form>
    );
}

export default UploadTrackList;