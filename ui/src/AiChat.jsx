import { useContext, useState, useRef, useEffect } from 'react';
import { AppContext } from './AppContext.jsx';
import { Form, Button, Spinner } from 'react-bootstrap';
import axios from 'axios';
import './AiChat.css';

const GREETING = "Hi! I'm DJ Mark's assistant. Tell me what you fancy hearing — an artist, a song, or just a vibe — and I'll help you request it.";

function AiChat() {
    const { selectedEvent, getMusicRequests } = useContext(AppContext);
    const [messages, setMessages] = useState([{ role: 'assistant', content: GREETING }]);
    const [input, setInput] = useState('');
    const [options, setOptions] = useState([]);
    const [isSending, setIsSending] = useState(false);
    const messagesEndRef = useRef(null);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, options, isSending]);

    const sendMessage = async (text) => {
        const trimmed = text.trim();
        if (!trimmed || isSending) {
            return;
        }

        const nextMessages = [...messages, { role: 'user', content: trimmed }];
        setMessages(nextMessages);
        setInput('');
        setOptions([]);
        setIsSending(true);

        try {
            const response = await axios.post(import.meta.env.VITE_APP_AI_CHAT, {
                eventId: selectedEvent.id,
                messages: nextMessages
            });
            const reply = response?.data?.reply || "Sorry, I didn't catch that. Could you try rephrasing?";
            const submitted = !!response?.data?.requestSubmitted;
            setMessages((prev) => [...prev, { role: 'assistant', content: reply, submitted }]);
            setOptions(Array.isArray(response?.data?.options) ? response.data.options : []);

            if (submitted) {
                getMusicRequests(selectedEvent);
            }
        } catch {
            setMessages((prev) => [...prev, { role: 'assistant', content: 'Sorry, something went wrong. Please try again, or use the standard request form.' }]);
        } finally {
            setIsSending(false);
        }
    };

    const handleSend = (event) => {
        event.preventDefault();
        sendMessage(input);
    };

    return (
        <>
            <div className='ai-chat-messages'>
                {messages.map((message, index) => (
                    <div key={index} className={`ai-chat-row ai-chat-row-${message.role}`}>
                        <div className={`ai-chat-bubble ai-chat-bubble-${message.role}${message.submitted ? ' ai-chat-bubble-success' : ''}`}>
                            {message.submitted && <span className='ai-chat-check' aria-hidden='true'>✓ </span>}
                            {message.content}
                        </div>
                    </div>
                ))}
                {isSending && (
                    <div className='ai-chat-row ai-chat-row-assistant'>
                        <div className='ai-chat-bubble ai-chat-bubble-assistant'>
                            <Spinner animation='border' size='sm' variant='primary' role='status' aria-label="DJ Mark's assistant is thinking" />
                            <span className='ms-2'>Thinking…</span>
                        </div>
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>
            {options.length > 0 && !isSending && (
                <div className='ai-chat-options mt-3'>
                    {options.map((option, index) => (
                        <Button key={index} variant='outline-primary' size='sm' onClick={() => sendMessage(option)}>
                            {option}
                        </Button>
                    ))}
                </div>
            )}
            <Form onSubmit={handleSend} className='ai-chat-input mt-3'>
                <Form.Control
                    type='text'
                    placeholder='Type your message…'
                    value={input}
                    onChange={(event) => setInput(event.target.value)}
                    disabled={isSending}
                    autoComplete='off'
                />
                <Button type='submit' disabled={isSending || !input.trim()}>Send</Button>
            </Form>
        </>
    );
}

export default AiChat;
