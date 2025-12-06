import { useState } from 'react';

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
        <>
            <h2>Upload Track List</h2>
            <form onSubmit={handleSubmit}>
                <div style={{ marginBottom: '1rem' }}>
                    <input
                        type="file"
                        accept=".csv"
                        onChange={handleFileChange}
                        disabled={isUploading}
                    />
                </div>
                <div style={{ marginBottom: '1rem' }}>
                    <button type="submit" disabled={isUploading || !selectedFile}>
                        {isUploading ? 'Uploading...' : 'Upload CSV'}
                    </button>
                </div>
                {message && (
                    <div style={{ 
                        padding: '0.5rem', 
                        marginTop: '1rem',
                        color: message.includes('success') ? 'green' : 'red'
                    }}>
                        {message}
                    </div>
                )}
            </form>
        </>
    );
}

export default UploadTrackList;