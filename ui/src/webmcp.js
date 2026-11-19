// Registers WebMCP tools (https://webmachinelearning.github.io/webmcp/) so an AI agent in the visitor's
// browser can do what a dancer does on the site: find an event, search the library and request a track.
//
// The tools call the same public API as the page, in the visitor's own session, so requests made
// through them carry the visitor's requestor cookie and count against their 5-per-event limit.
//
// The homepage loads this without React, so keep it free of heavy imports.

export const REQUEST_CREATED_EVENT = 'djportal:request-created';

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const MAX_NAME_LENGTH = 50;
const MAX_TRACK_LENGTH = 200;

async function fetchJson(url, signal, options = {}) {
    const response = await fetch(url, { ...options, signal, headers: { Accept: 'application/json', ...options.headers } });
    if (!response.ok) {
        throw new Error(`The DJ Mark API returned ${response.status} for ${url}.`);
    }

    return response.json();
}

function summariseEvent(event) {
    return {
        id: event.id,
        name: event.name,
        description: event.description,
        date: event.date?.slice(0, 10),
        times: event.times,
        venue: event.locationName,
        address: event.locationAddress,
        djs: event.djs,
        isRequestable: event.isRequestable,
        eventPageUrl: event.eventPageUrl
    };
}

function resolveEventId(input, defaultEventId) {
    const eventId = (input.eventId || defaultEventId || '').trim();
    if (!GUID_PATTERN.test(eventId)) {
        throw new Error('eventId must be the id of an event from list_upcoming_events.');
    }

    return eventId;
}

function eventIdSchema(defaultEventId) {
    return {
        type: 'string',
        format: 'uuid',
        description: defaultEventId
            ? 'The event id from list_upcoming_events. Leave it out to use the event on this page.'
            : 'The event id from list_upcoming_events.'
    };
}

function buildTools(defaultEventId) {
    const eventRequired = defaultEventId ? [] : ['eventId'];

    return [
        {
            name: 'list_upcoming_events',
            title: 'List upcoming events',
            description: 'List the upcoming Ceroc dance events where DJ Mark is playing, soonest first. Use an event\'s id with request_track and list_event_requests. Only events with isRequestable true are taking song requests.',
            inputSchema: { type: 'object', properties: {}, additionalProperties: false },
            annotations: { readOnlyHint: true },
            async execute(_input, options) {
                const events = await fetchJson(import.meta.env.VITE_APP_EVENTS_LIST, options?.signal);
                return { events: events.map(summariseEvent) };
            }
        },
        {
            name: 'search_tracks',
            title: 'Search DJ Mark\'s music library',
            description: 'Search DJ Mark\'s music library by title, artist or album. Returns up to 10 tracks. Tracks that are not in the library can still be requested, but a match gives DJ Mark the exact track.',
            inputSchema: {
                type: 'object',
                properties: {
                    query: { type: 'string', minLength: 1, description: 'Words from the track title, artist or album.' },
                    lowBpm: { type: 'number', default: 100, description: 'Lowest BPM to include.' },
                    highBpm: { type: 'number', default: 150, description: 'Highest BPM to include.' }
                },
                required: ['query'],
                additionalProperties: false
            },
            annotations: { readOnlyHint: true },
            async execute(input, options) {
                const query = (input.query || '').trim();
                if (!query) {
                    throw new Error('query must not be empty.');
                }

                const params = new URLSearchParams({ query });
                if (Number.isFinite(input.lowBpm)) params.set('lowBpm', input.lowBpm);
                if (Number.isFinite(input.highBpm)) params.set('highBpm', input.highBpm);

                const tracks = await fetchJson(`${import.meta.env.VITE_APP_TRACKS_LIST}?${params}`, options?.signal);
                return {
                    tracks: tracks.map(track => ({
                        track: track.summary,
                        title: track.title,
                        artist: track.artist,
                        time: track.time,
                        bpm: track.bpm
                    }))
                };
            }
        },
        {
            name: 'list_event_requests',
            title: 'List requests for an event',
            description: 'List the song requests made so far for an event and their status (Pending, Approved, Queued, Played or AlreadyServed). The visitor\'s own requests are shown as "You". Other dancers appear under pseudonyms and their unapproved tracks read "Pending Approval".',
            inputSchema: {
                type: 'object',
                properties: { eventId: eventIdSchema(defaultEventId) },
                required: eventRequired,
                additionalProperties: false
            },
            // Request names and track text are typed by other visitors.
            annotations: { readOnlyHint: true, untrustedContentHint: true },
            async execute(input, options) {
                const eventId = resolveEventId(input, defaultEventId);
                const params = new URLSearchParams({ eventId });
                const requests = await fetchJson(`${import.meta.env.VITE_APP_REQUESTS_LIST}?${params}`, options?.signal);
                return {
                    requests: requests.map(request => ({
                        requestedBy: request.userName,
                        track: request.trackName,
                        status: request.status
                    }))
                };
            }
        },
        {
            name: 'request_track',
            title: 'Request a track',
            description: `Send DJ Mark a song request for an event on the visitor's behalf. Only use this when the visitor has asked for the song, and use the name they give. DJ Mark reviews every request and chooses what to play. Each visitor may make up to 5 requests per event. If search_tracks found the track, pass its track, bpm and time.`,
            inputSchema: {
                type: 'object',
                properties: {
                    eventId: eventIdSchema(defaultEventId),
                    track: { type: 'string', minLength: 1, maxLength: MAX_TRACK_LENGTH, description: 'The song as "Title, Artist".' },
                    requestedBy: { type: 'string', minLength: 1, maxLength: MAX_NAME_LENGTH, description: 'The visitor\'s first name, shown to DJ Mark.' },
                    bpm: { type: 'number', description: 'The track\'s BPM from search_tracks, if known.' },
                    time: { type: 'string', description: 'The track\'s length from search_tracks, if known.' }
                },
                required: [...eventRequired, 'track', 'requestedBy'],
                additionalProperties: false
            },
            async execute(input, options) {
                const signal = options?.signal;
                const eventId = resolveEventId(input, defaultEventId);
                const track = (input.track || '').trim();
                const requestedBy = (input.requestedBy || '').trim();

                if (!track || track.length > MAX_TRACK_LENGTH) {
                    throw new Error(`track must be between 1 and ${MAX_TRACK_LENGTH} characters.`);
                }
                if (!requestedBy || requestedBy.length > MAX_NAME_LENGTH) {
                    throw new Error(`requestedBy must be between 1 and ${MAX_NAME_LENGTH} characters.`);
                }

                const response = await fetch(import.meta.env.VITE_APP_REQUESTS_SUBMIT, {
                    method: 'POST',
                    signal,
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        eventId,
                        musicRequest: track,
                        requestedBy,
                        bpm: Number.isFinite(input.bpm) ? input.bpm : 0,
                        time: input.time || ''
                    })
                });

                if (response.status === 409) {
                    const body = await response.json().catch(() => ({}));
                    return { status: 'limit-reached', message: body.message || 'The visitor has already made the maximum number of requests for this event.' };
                }
                if (response.status === 400) {
                    // The API rejects events that don't exist or aren't taking requests, and says which.
                    const body = await response.json().catch(() => ({}));
                    if (body.message) {
                        return { status: 'not-open', message: body.message };
                    }
                }
                if (!response.ok) {
                    throw new Error(`The request was not accepted (HTTP ${response.status}).`);
                }

                window.dispatchEvent(new CustomEvent(REQUEST_CREATED_EVENT, { detail: { eventId } }));

                return {
                    status: 'pending',
                    message: `Requested "${track}". DJ Mark will review it and choose what to play.`
                };
            }
        }
    ];
}

/**
 * Registers the tools when the browser supports WebMCP. Pass eventId on an event page so the tools
 * default to that event. Returns an AbortController that unregisters the tools, or null when WebMCP
 * isn't available.
 */
export function registerWebMcpTools({ eventId } = {}) {
    // Older Chrome builds exposed the API on navigator rather than document.
    const modelContext = document.modelContext ?? navigator.modelContext;
    if (typeof modelContext?.registerTool !== 'function') {
        return null;
    }

    const controller = new AbortController();
    for (const tool of buildTools(eventId)) {
        // registerTool returns a promise in the current spec but not in some older builds, and can throw
        // synchronously, so wrap it to catch both.
        Promise.resolve()
            .then(() => modelContext.registerTool(tool, { signal: controller.signal }))
            .catch(error => console.warn(`WebMCP: could not register ${tool.name}.`, error));
    }

    return controller;
}
