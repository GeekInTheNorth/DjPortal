import { useContext, useState, useRef, useEffect } from 'react';
import { AppContext } from './AppContext.jsx';
import { Form, Button, Spinner } from 'react-bootstrap';
import axios from 'axios';
import PropTypes from 'prop-types';
import './AiChat.css';

const DANCER_GREETING = "Hi! I'm DJ Mark's AI Assistant. Tell me what you fancy hearing — an artist, a song, or just a vibe — and I'll help you request it.";

const DANCER_OPTIONS = ["I want a specific song", "I want anything by a specific artist", "I want a specific genre or decade", "I don't know the name, but the lyrics go..."];

// Prefixing the next message tells the assistant it is being given a half-remembered line rather than a
// title, and gives the content filter enough context not to read the lyric as something it isn't.
const LYRICS_PREFIX = 'It has the lyrics ';

const isLyricsOption = (option) => /lyric/i.test(option);

// Excluded messages stay on screen but are never resent, so a phrase that tripped the content filter
// cannot go on blocking every subsequent turn.
const toWireMessages = (list) => list
    .filter((message) => !message.excluded)
    .map(({ role, content }) => ({ role, content }));

const excludeLastUserMessage = (list) => {
    const lastUser = list.map((message) => message.role).lastIndexOf('user');

    return lastUser === -1
        ? list
        : list.map((message, index) => (index >= lastUser ? { ...message, excluded: true } : message));
};

function AiChat({ greeting = DANCER_GREETING, initialOptions = DANCER_OPTIONS, mode = 'dancer' }) {
    const { selectedEvent, getMusicRequests } = useContext(AppContext);
    const [messages, setMessages] = useState([{ role: 'assistant', content: greeting }]);
    const [input, setInput] = useState('');
    const [options, setOptions] = useState(initialOptions);
    const [isSending, setIsSending] = useState(false);
    const [awaitingLyrics, setAwaitingLyrics] = useState(false);
    const messagesEndRef = useRef(null);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, options, isSending]);

    const sendMessage = async (text, fromOption = false) => {
        const trimmed = text.trim();
        if (!trimmed || isSending) {
            return;
        }

        // They tapped the lyrics option last time, so this is the line they remember.
        const needsLyricsPrefix = awaitingLyrics
            && !fromOption
            && !trimmed.toLowerCase().startsWith(LYRICS_PREFIX.trim().toLowerCase());
        const content = needsLyricsPrefix ? `${LYRICS_PREFIX}${trimmed}` : trimmed;
        setAwaitingLyrics(fromOption && isLyricsOption(trimmed));

        const nextMessages = [...messages, { role: 'user', content }];
        setMessages(nextMessages);
        setInput('');
        setOptions([]);
        setIsSending(true);

        try {
            const response = await axios.post(import.meta.env.VITE_APP_AI_CHAT, {
                eventId: selectedEvent.id,
                mode: mode,
                messages: toWireMessages(nextMessages)
            });
            const reply = response?.data?.reply || "Sorry, I didn't catch that. Could you try rephrasing?";
            const submitted = !!response?.data?.requestSubmitted;
            const contentFiltered = !!response?.data?.contentFiltered;
            setMessages((prev) => [
                ...(contentFiltered ? excludeLastUserMessage(prev) : prev),
                { role: 'assistant', content: reply, submitted }
            ]);
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
                        <div className={`ai-chat-bubble ai-chat-bubble-${message.role}${message.submitted ? ' ai-chat-bubble-success' : ''}${message.excluded ? ' ai-chat-bubble-excluded' : ''}`}>
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
                        <Button key={index} variant='outline-primary' size='sm' onClick={() => sendMessage(option, true)}>
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

AiChat.propTypes = {
    greeting: PropTypes.string,
    initialOptions: PropTypes.arrayOf(PropTypes.string),
    mode: PropTypes.oneOf(['dancer', 'dj'])
};

export default AiChat;
