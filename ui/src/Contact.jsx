import { useState, useRef, useEffect } from 'react'
import { Form, Card, FormGroup, Button } from 'react-bootstrap';
import axios from 'axios';

function Contact() {

    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [subject, setSubject] = useState('');
    const [message, setMessage] = useState('');
    const [company, setCompany] = useState(''); // honeypot - real users never fill this
    const [submitted, setSubmitted] = useState(false);
    const [errorMessage, setErrorMessage] = useState('');
    const renderedAt = useRef(0);

    useEffect(() => {
        renderedAt.current = Date.now();
    }, []);

    const handleSubmit = async (event) => {
        event.preventDefault();

        const contactData = {
            name,
            email,
            subject,
            message,
            company,
            elapsedMs: Date.now() - renderedAt.current
        };

        try {
            setErrorMessage('');
            await axios.post(import.meta.env.VITE_APP_CONTACT_SUBMIT, contactData);
            setSubmitted(true);
        } catch (error) {
            if (error.response && error.response.status === 429) {
                setErrorMessage('Please wait a moment before sending another message.');
            } else if (error.response && error.response.status === 400) {
                setErrorMessage('Please check your details and try again.');
            } else {
                setErrorMessage('Sorry, something went wrong sending your message. Please try again later.');
                console.error('Error submitting contact form', error);
            }
        }
    };

    const handleSendAnother = () => {
        setName('');
        setEmail('');
        setSubject('');
        setMessage('');
        setCompany('');
        renderedAt.current = Date.now();
        setSubmitted(false);
    };

    const renderThankYou = () => {
        return (
            <div className='alert alert-success my-3' role='alert'>
                <h4 className='alert-heading'>Thank you for getting in touch!</h4>
                <p>Your message has been sent. DJ Mark will get back to you as soon as possible.</p>
                <hr />
                <Button variant='primary' onClick={handleSendAnother}>Send Another Message</Button>
            </div>
        )
    };

    const renderForm = () => {
        return (
            <Card className='my-3'>
                <Card.Header className='bg-primary text-light fw-bold'>Contact DJ Mark</Card.Header>
                <Card.Body>
                    <Form onSubmit={handleSubmit}>
                        { errorMessage && <div className='alert alert-warning' role='alert'>{errorMessage}</div> }
                        <FormGroup className='mb-3' controlId='formContactName'>
                            <Form.Label className='fw-bold d-block'>Your Name</Form.Label>
                            <Form.Control type='text' placeholder='Your Name' value={name} onChange={(e) => setName(e.target.value)} required={true} />
                        </FormGroup>
                        <FormGroup className='mb-3' controlId='formContactEmail'>
                            <Form.Label className='fw-bold d-block'>Your Email</Form.Label>
                            <div className='form-text'>So DJ Mark can reply to you.</div>
                            <Form.Control type='email' placeholder='you@example.com' value={email} onChange={(e) => setEmail(e.target.value)} required={true} />
                        </FormGroup>
                        <FormGroup className='mb-3' controlId='formContactSubject'>
                            <Form.Label className='fw-bold d-block'>Subject</Form.Label>
                            <Form.Control type='text' placeholder='What is your message about?' value={subject} onChange={(e) => setSubject(e.target.value)} required={true} />
                        </FormGroup>
                        <FormGroup className='mb-3' controlId='formContactMessage'>
                            <Form.Label className='fw-bold d-block'>Message</Form.Label>
                            <Form.Control as='textarea' rows={5} placeholder='Your message' value={message} onChange={(e) => setMessage(e.target.value)} required={true} />
                        </FormGroup>
                        {/* Honeypot: hidden from people, tempting to bots. Leave empty. */}
                        <div aria-hidden='true' style={{ position: 'absolute', left: '-5000px', height: 0, overflow: 'hidden' }}>
                            <label htmlFor='formContactCompany'>Company</label>
                            <input type='text' id='formContactCompany' name='company' tabIndex={-1} autoComplete='off' value={company} onChange={(e) => setCompany(e.target.value)} />
                        </div>
                        <Form.Group className='my-3'>
                            <Button type='submit'>Send Message</Button>
                        </Form.Group>
                    </Form>
                </Card.Body>
            </Card>
        )
    };

    return(
        <>
            { submitted ? renderThankYou() : renderForm() }
        </>
    )
}

export default Contact
