import { useContext, useEffect, useMemo, useState } from 'react'
import { AppContext } from './../AppContext.jsx';
import { Alert, Badge, Button, Card, Spinner, Table } from 'react-bootstrap';
import { format, parseISO } from 'date-fns';
import axios from 'axios';
import CreateEventModal from './CreateEventModal.jsx';

function ImportEvents() {

    const { eventCollection } = useContext(AppContext);

    const [listings, setListings] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [loadError, setLoadError] = useState('');
    const [importingUrl, setImportingUrl] = useState(null);
    const [importError, setImportError] = useState('');
    const [draft, setDraft] = useState(null);
    const [showModal, setShowModal] = useState(false);

    useEffect(() => {
        axios.get(import.meta.env.VITE_APP_IMPORT_CEROC_LIST)
            .then((response) => setListings(response.data ?? []))
            .catch(() => setLoadError('Couldn\'t load the Ceroc events. Please try again later.'))
            .finally(() => setIsLoading(false));
    }, []);

    // Names of the events already in the portal, keyed by yyyy-MM-dd, so clashing Ceroc rows can be flagged.
    const eventNamesByDate = useMemo(() => {
        const byDate = new Map();
        (eventCollection ?? []).forEach((event) => {
            const key = format(new Date(event.date), 'yyyy-MM-dd');
            byDate.set(key, [...(byDate.get(key) ?? []), event.name]);
        });
        return byDate;
    }, [eventCollection]);

    const handleImport = async (listing) => {
        setImportingUrl(listing.url);
        setImportError('');

        try {
            const response = await axios.post(import.meta.env.VITE_APP_IMPORT_CEROC_EVENT, { url: listing.url });
            setDraft({ ...response.data, date: response.data?.date ?? listing.date });
        } catch (error) {
            // Still open the form with what the listing gave us, so the DJ isn't blocked.
            setImportError(error.response?.status === 401
                ? 'You need to be signed in to import events, so only the basic details have been filled in.'
                : error.response?.data?.message ?? 'Couldn\'t import that event, so only the basic details have been filled in.');
            setDraft({
                name: listing.name,
                date: listing.date,
                locationAddress: [listing.location, listing.postcode].filter(Boolean).join(', '),
                organizer: 'Ceroc Yorkshire'
            });
        } finally {
            setImportingUrl(null);
            setShowModal(true);
        }
    };

    const renderListings = () => {
        if (isLoading) {
            return (
                <div className='text-center my-4'>
                    <Spinner animation='border' role='status' />
                    <div className='mt-2'>Loading Ceroc events…</div>
                </div>
            );
        }

        if (loadError) {
            return null;
        }

        if (listings.length === 0) {
            return <p className='mb-0'>No Freestyle events found on Ceroc right now.</p>;
        }

        return (
            <Table hover responsive className='mb-0 align-middle'>
                <thead>
                    <tr>
                        <th>Event Name</th>
                        <th>Location</th>
                        <th>Date</th>
                        <th className='text-end'>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {listings.map((listing) => {
                        const existingNames = eventNamesByDate.get(listing.date);
                        const isImporting = importingUrl === listing.url;

                        return (
                            <tr key={listing.url}>
                                <td>
                                    <a href={listing.url} target='_blank' rel='noopener noreferrer'>{listing.name}</a>
                                    {existingNames && (
                                        <div>
                                            <Badge bg='warning' text='dark' className='mt-1 text-wrap'>Already have: {existingNames.join(', ')}</Badge>
                                        </div>
                                    )}
                                </td>
                                <td>{[listing.location, listing.postcode].filter(Boolean).join(', ')}</td>
                                <td className='text-nowrap'>{format(parseISO(listing.date), 'EEE dd/MM/yyyy')}</td>
                                <td className='text-end'>
                                    <Button
                                        size='sm'
                                        variant={existingNames ? 'outline-secondary' : 'primary'}
                                        disabled={importingUrl !== null}
                                        onClick={() => handleImport(listing)}>
                                        {isImporting
                                            ? <><Spinner as='span' animation='border' size='sm' aria-hidden='true' className='me-1' />Importing…</>
                                            : 'Import'}
                                    </Button>
                                </td>
                            </tr>
                        );
                    })}
                </tbody>
            </Table>
        );
    };

    return (
        <>
            <Card className='mb-3'>
                <Card.Header>Import from Ceroc</Card.Header>
                <Card.Body>
                    <p className='text-muted small'>
                        Upcoming Freestyle events from ceroc.com. Importing one writes a description with AI and opens
                        the Create Event form so you can check and adjust the details before saving.
                    </p>
                    {loadError && <Alert variant='danger'>{loadError}</Alert>}
                    {importError && <Alert variant='warning' dismissible onClose={() => setImportError('')}>{importError}</Alert>}
                    {renderListings()}
                </Card.Body>
            </Card>
            <CreateEventModal show={showModal} prefill={draft} onHide={() => setShowModal(false)} />
        </>
    );
}

export default ImportEvents;
