import { useState } from 'react'
import { Button, Modal } from 'react-bootstrap';
import PropTypes from 'prop-types';
import axios from 'axios';

function DeleteRequestModal({ show, requestData, onHide, onDeleted }) {

    const [isDeleting, setIsDeleting] = useState(false);
    const [errorMessage, setErrorMessage] = useState(null);

    // Clear state once the modal has finished closing, so the next request opens clean.
    const handleExited = () => {
        setIsDeleting(false);
        setErrorMessage(null);
    };

    const handleDelete = async () => {
        setIsDeleting(true);
        setErrorMessage(null);

        try {
            await axios.delete(import.meta.env.VITE_APP_REQUESTS_DELETE, { params: { requestId: requestData.id } });
            if (onDeleted) {
                await onDeleted();
            }
            onHide();
        } catch (error) {
            console.error('Error deleting request', error);
            setErrorMessage('The request could not be deleted. Please try again.');
            setIsDeleting(false);
        }
    };

    return (
        <Modal show={show} onHide={isDeleting ? undefined : onHide} onExited={handleExited} backdrop={isDeleting ? 'static' : true}>
            <Modal.Header closeButton={!isDeleting} className='py-2'>
                <Modal.Title>Delete Request</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <p className='mb-2'>Are you sure you want to delete this request?</p>
                <p className='fw-bold mb-1 text-break'>{requestData?.trackName}</p>
                {requestData?.userName && <p className='text-muted mb-0'>Requested by {requestData.userName}</p>}
                {errorMessage && <p className='text-danger mt-3 mb-0'>{errorMessage}</p>}
            </Modal.Body>
            <Modal.Footer>
                <Button variant='danger' onClick={handleDelete} disabled={isDeleting}>{isDeleting ? 'Deleting…' : 'Delete'}</Button>
                <Button variant='secondary' onClick={onHide} disabled={isDeleting}>Cancel</Button>
            </Modal.Footer>
        </Modal>
    )
}

DeleteRequestModal.propTypes = {
    show: PropTypes.bool,
    requestData: PropTypes.shape({
        id: PropTypes.string,
        trackName: PropTypes.string,
        userName: PropTypes.string
    }),
    onHide: PropTypes.func,
    onDeleted: PropTypes.func
};

export default DeleteRequestModal;
