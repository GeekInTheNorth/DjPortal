import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DIST_DIR = path.resolve(__dirname, '..', 'dist');
const EVENTS_DIR = path.resolve(DIST_DIR, 'events');
const API_URL = 'https://dj.stott.pro/api/events/list/';
const SITE_BASE_URL = 'https://dj.stott.pro';

async function fetchEvents() {
    const response = await fetch(API_URL);
    if (!response.ok) {
        throw new Error(`Failed to fetch events: ${response.status} ${response.statusText}`);
    }
    return response.json();
}

function extractAssetRefs(indexHtml) {
    const scriptMatch = indexHtml.match(/<script type="module" crossorigin src="([^"]+)"/);
    const cssMatch = indexHtml.match(/<link rel="stylesheet" crossorigin href="([^"]+)"/);

    return {
        jsPath: scriptMatch ? scriptMatch[1] : null,
        cssPath: cssMatch ? cssMatch[1] : null,
    };
}

function formatDate(dateString) {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const months = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];
    const month = months[date.getMonth()];
    const year = date.getFullYear();
    return `${day} ${month} ${year}`;
}

function formatDateForFilename(dateString) {
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function formatDateISO(dateString) {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
}

function getTagColour(tag) {
    const lowerTag = tag.toLowerCase();
    if (lowerTag === 'dj mark') return 'success';
    if (lowerTag.startsWith('valentine') || lowerTag.endsWith('ball')) return 'danger';
    return 'primary';
}

function renderTags(event) {
    if (!event.tagList || event.tagList.length === 0) return '';

    const badges = event.tagList.map(tag =>
        `<span class="badge rounded-pill bg-${tag.colour} mx-1">${escapeHtml(tag.name)}</span>`
    ).join('');

    return `<div>${badges}</div>`;
}

function escapeHtml(text) {
    if (!text) return '';
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function escapeJsonForScript(obj) {
    return JSON.stringify(obj).replace(/<\//g, '<\\/');
}

function renderCalendarButton(event) {
    if (!event.calendarInviteUrl) return '';
    return `<a href="${escapeHtml(event.calendarInviteUrl)}" class="btn btn-success mx-2" download rel="nofollow" title="Add to Calendar">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 448 512" width="16" height="16" fill="currentColor"><path d="M152 24c0-13.3-10.7-24-24-24s-24 10.7-24 24V64H64C28.7 64 0 92.7 0 128v16 48V448c0 35.3 28.7 64 64 64H384c35.3 0 64-28.7 64-64V192 144 128c0-35.3-28.7-64-64-64H344V24c0-13.3-10.7-24-24-24s-24 10.7-24 24V64H152V24zM48 192H400V448c0 8.8-7.2 16-16 16H64c-8.8 0-16-7.2-16-16V192z"/></svg>
                </a>`;
}

function renderFacebookButton(event) {
    if (!event.facebookEventUrl) return '';
    return `<a href="${escapeHtml(event.facebookEventUrl)}" class="btn btn-facebook mx-2" target="_blank" rel="nofollow noreferrer" title="View on Facebook">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 512" width="16" height="16" fill="currentColor"><path d="M80 299.3V512H196V299.3h86.5l18-97.8H196V166.9c0-51.7 20.3-71.5 72.7-71.5c16.3 0 29.4 .4 37 1.2V7.9C291.4 4 256.4 0 236.2 0C129.3 0 80 50.5 80 159.4v42.1H14v97.8H80z"/></svg>
                </a>`;
}

function renderRequestStatus(event) {
    if (!event.isRequestable) {
        return '<small>Requests are not currently open for this event.</small>';
    }
    return '';
}

function buildSchemaJsonLd(event) {
    const dateStr = formatDateISO(event.date);

    const schema = {
        "@context": "https://schema.org",
        "@type": "DanceEvent",
        "name": event.name,
        "description": event.description,
        "startDate": event.startTime || `${dateStr}T20:00:00`,
        "endDate": event.endTime || `${dateStr}T23:00:00`,
        "eventStatus": "https://schema.org/EventScheduled",
        "eventAttendanceMode": "https://schema.org/OfflineEventAttendanceMode",
        "location": {
            "@type": "Place",
            "name": event.locationName,
            "address": {
                "@type": "PostalAddress",
                "streetAddress": event.locationAddress
            }
        },
        "organizer": {
            "@type": "Person",
            "name": "DJ Mark",
            "url": SITE_BASE_URL
        },
        "performer": {
            "@type": "Person",
            "name": "DJ Mark"
        },
        "url": `${SITE_BASE_URL}/events/${formatDateForFilename(event.date)}`
    };

    return JSON.stringify(schema, null, 2);
}

function stripNewlines(text) {
    if (!text) return '';
    return text.replace(/[\r\n]+/g, ' ').replace(/\s+/g, ' ').trim();
}

function generateEventPageHtml(event, assetRefs) {
    const formattedDate = formatDate(event.date);
    const dateFilename = formatDateForFilename(event.date);
    const pageTitle = `${event.name} - DJ Mark`;
    const pageDescription = stripNewlines(`${event.description || event.name} at ${event.locationName} on ${formattedDate}`);

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>${escapeHtml(pageTitle)}</title>
    <meta name="description" content="${escapeHtml(pageDescription)}" />
    <meta name="keywords" content="Modern Jive, Ceroc, Partner Dancing, Ceroc Yorkshire, ${escapeHtml(event.locationName)}, music requests, song requests, DJ requests" />
    <meta property="og:url" content="${SITE_BASE_URL}/events/${dateFilename}" />
    <meta property="og:title" content="${escapeHtml(pageTitle)}" />
    <meta property="og:description" content="${escapeHtml(pageDescription)}" />
    <meta property="og:type" content="website" />
    <meta property="og:locale" content="en_GB" />
    <meta property="og:image" content="${SITE_BASE_URL}/images/social.png" />
    <meta name="twitter:creator" content="@GeekInTheNorth" />
    <meta name="twitter:site" content="@GeekInTheNorth" />
    <meta name="twitter:title" content="${escapeHtml(pageTitle)}" />
    <meta name="twitter:description" content="${escapeHtml(pageDescription)}" />
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:image" content="${SITE_BASE_URL}/images/social.png" />
    <link rel="canonical" href="${SITE_BASE_URL}/events/${dateFilename}" />
    <meta name="viewport" content="maximum-scale=5.0, initial-scale=1.0, width=device-width">
    <meta name="theme-color" content="#212529">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
    <meta name="apple-mobile-web-app-title" content="DJ Mark">
    <link rel="icon" type="image/png" sizes="32x32" href="/images/favicon.png">
    <link rel="apple-touch-icon" href="/images/apple-touch-icon.png">
    <link rel="manifest" href="/manifest.json">
    ${assetRefs.cssPath ? `<link rel="stylesheet" crossorigin href="${assetRefs.cssPath}">` : ''}
    <script type="application/ld+json">
${buildSchemaJsonLd(event)}
    </script>
</head>
<body>

<header>
    <nav class="navbar fixed-top navbar-dark bg-dark" id="mainNav">
        <div class="container-fluid justify-content-start">
            <a class="navbar-brand fw-bold" href="/">DJ Mark</a>
            <a class="nav-link text-light fw-bold mx-2" href="/">Events</a>
        </div>
    </nav>
</header>
<main class="mt-5">

<div class="container pt-3">
    <div class="card mb-3">
        <div class="card-header fw-bold">${escapeHtml(event.name)}</div>
        <div class="card-body">
            <p class="card-text">${escapeHtml(event.description)}</p>
            <p class="card-text">${escapeHtml(formattedDate)}, ${escapeHtml(event.times)}</p>
            <p class="card-text">${escapeHtml(event.locationName)}<br/>${escapeHtml(event.locationAddress)}</p>
            ${renderRequestStatus(event)}
            ${renderTags(event)}
        </div>
        <div class="card-footer">
            ${renderCalendarButton(event)}
            ${renderFacebookButton(event)}
        </div>
    </div>

    <div id="eventpage"></div>
</div>

</main>
<script>window.__EVENT_DATA__ = ${escapeJsonForScript(event)};</script>
${assetRefs.jsPath ? `<script type="module" crossorigin src="${assetRefs.jsPath}"></script>` : ''}
</body>
</html>`;
}

function generateEventListHtml(events) {
    if (!Array.isArray(events) || events.length === 0) {
        return '';
    }

    return events.map(event => {
        const formattedDate = formatDate(event.date);
        const dateFilename = formatDateForFilename(event.date);

        // Build footer buttons
        let footerContent = '';
        if (event.isRequestable) {
            footerContent += `<a href="/events/${dateFilename}" class="btn btn-primary">Request a Track</a>`;
        }
        footerContent += renderCalendarButton(event);
        footerContent += renderFacebookButton(event);

        return `
    <div class="card mb-3">
        <div class="card-header fw-bold">${escapeHtml(event.name)}</div>
        <div class="card-body">
            <p class="card-text">${escapeHtml(event.description)}</p>
            <p class="card-text">${escapeHtml(formattedDate)}, ${escapeHtml(event.times)}</p>
            <p class="card-text">${escapeHtml(event.locationName)}<br/>${escapeHtml(event.locationAddress)}</p>
            ${renderRequestStatus(event)}
            ${renderTags(event)}
        </div>
        ${footerContent ? `<div class="card-footer">\n            ${footerContent}\n        </div>` : ''}
    </div>`;
    }).join('\n');
}

function generateFaqHtml() {
    return `
    <div class="card mb-3">
        <div class="card-header">FAQ</div>
        <div class="card-body">
            <div class="accordion" id="faqAccordion">
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#faq0" aria-expanded="true" aria-controls="faq0">
                            Will My Request Get Played?
                        </button>
                    </h2>
                    <div id="faq0" class="accordion-collapse collapse show" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>Requests will be played on a best endevours basis. Just because a request is made, I can not promise that it will be played.</p>
                            <p>In order to get played, a track must have a relatively stable beat between 110 and 150 beats per minute and must not be excessively offensive. Over the course of a 4 hour freestyle, I will planning to play between 8 and 16 requests and work to fit them into one of 4 journeys. If there are too many requests of a similar speed, then a selection will be made.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq1" aria-expanded="false" aria-controls="faq1">
                            What if you don't have my song?
                        </button>
                    </h2>
                    <div id="faq1" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>If I do not have the song you have requested, I will preview that song online and if it matches our criteria, then I will attempt to purchase that song so I can play it.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq2" aria-expanded="false" aria-controls="faq2">
                            Can I request more than one track?
                        </button>
                    </h2>
                    <div id="faq2" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>Yes, but in order to be fair to other dancers, your requests will be treated as options and I will attempt to honour just one of them.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq3" aria-expanded="false" aria-controls="faq3">
                            Why are you buying tracks instead of streaming them?
                        </button>
                    </h2>
                    <div id="faq3" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>Artists make very little from streaming services and content on streaming services is not guaranteed to always be on that service. By purchasing a track, I support the artist and ensure that I have access to that track indefinitely.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq4" aria-expanded="false" aria-controls="faq4">
                            Who Are These Events For?
                        </button>
                    </h2>
                    <div id="faq4" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>These Events are aimed at people who are into social partner dancing. Our specific style is known as Ceroc, but also commonly as Modern Jive.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq5" aria-expanded="false" aria-controls="faq5">
                            Where Can I Learn To Dance?
                        </button>
                    </h2>
                    <div id="faq5" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p><a href="https://www.facebook.com/CerocYorkshire" target="_blank" rel="nofollow">Ceroc Yorkshire</a> host 3 regular class nights every week across the West Yorkshire region at venues in Tadcaster, Leeds and Ilkley. Doors open at 7:30pm, the beginners class starts at 7:45pm and the intermediate class starts at 9:00pm.</p>
                            <p>For more information, do checkout the <a href="https://www.facebook.com/CerocYorkshire" target="_blank" rel="nofollow">Ceroc Yorkshire</a> facebook page as well as the national <a href='https://www.ceroc.com/' rel='nofollow'>Ceroc</a> website.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq6" aria-expanded="false" aria-controls="faq6">
                            How Do I Install This App on iPhone/iPad?
                        </button>
                    </h2>
                    <div id="faq6" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>To install this app on your iOS device:</p>
                            <ol>
                                <li>Open this website in <strong>Safari</strong> (not Chrome or other browsers)</li>
                                <li>Tap the <strong>Share button</strong> (square with arrow pointing up) at the bottom</li>
                                <li>Scroll down and tap <strong>"Add to Home Screen"</strong></li>
                                <li>Tap <strong>"Add"</strong> in the top-right corner</li>
                            </ol>
                            <p>The app icon will appear on your home screen like a regular app. You can then open it directly without needing to use Safari.</p>
                        </div>
                    </div>
                </div>
                <div class="accordion-item">
                    <h2 class="accordion-header">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#faq7" aria-expanded="false" aria-controls="faq7">
                            Does This Website Use Cookies
                        </button>
                    </h2>
                    <div id="faq7" class="accordion-collapse collapse" data-bs-parent="#faqAccordion">
                        <div class="accordion-body">
                            <p>This website uses a single essential cookie to ensure it functions properly. This cookie does not collect personal data and is not used for advertising or analytics.</p>
                            <p>By continuing to use this site, you agree to the use of this essential cookie.</p>
                            <p>The cookie is called <em>cydjr.requestor</em>. It is used to link you to requests you have made and contains only a randomly generated identifier. The cookie expires after 365 days.</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>`;
}

async function main() {
    console.log('Generating static event pages...');

    // Check that dist directory exists
    const indexHtmlPath = path.join(DIST_DIR, 'index.html');
    if (!fs.existsSync(indexHtmlPath)) {
        console.error('Error: dist/index.html not found. Run "vite build" first.');
        process.exit(1);
    }

    // Extract asset references from djportal.html (which still has React)
    // since index.html is now static-only
    const djportalHtmlPath = path.join(DIST_DIR, 'djportal.html');
    const djportalHtml = fs.readFileSync(djportalHtmlPath, 'utf-8');
    const assetRefs = extractAssetRefs(djportalHtml);

    if (!assetRefs.jsPath) {
        console.warn('Warning: Could not find JS asset reference in djportal.html');
    }
    if (!assetRefs.cssPath) {
        console.warn('Warning: Could not find CSS asset reference in djportal.html');
    }

    console.log(`Assets: JS=${assetRefs.jsPath}, CSS=${assetRefs.cssPath}`);

    // Fetch events from local API
    let events;
    try {
        events = await fetchEvents();
    } catch (error) {
        console.error(`Error fetching events: ${error.message}`);
        console.error('Set EVENTS_API_URL env var or ensure the API is accessible.');
        process.exit(1);
    }

    if (!Array.isArray(events) || events.length === 0) {
        console.log('No events found. No pages generated.');
        return;
    }

    console.log(`Found ${events.length} event(s).`);

    // Create events directory
    if (!fs.existsSync(EVENTS_DIR)) {
        fs.mkdirSync(EVENTS_DIR, { recursive: true });
    }

    // Generate a page for each event
    let generated = 0;
    for (const event of events) {
        const dateFilename = formatDateForFilename(event.date);
        const outputPath = path.join(EVENTS_DIR, `${dateFilename}.html`);
        const html = generateEventPageHtml(event, assetRefs);

        fs.writeFileSync(outputPath, html, 'utf-8');
        console.log(`  Generated: events/${dateFilename} - ${event.name}`);
        generated++;
    }

    console.log(`Done. Generated ${generated} event page(s).`);

    // Generate sitemap.xml
    const today = new Date().toISOString().split('T')[0];
    const sitemapEntries = [
        `  <url>\n    <loc>${SITE_BASE_URL}/</loc>\n    <lastmod>${today}</lastmod>\n  </url>`
    ];

    for (const event of events) {
        const dateFilename = formatDateForFilename(event.date);
        sitemapEntries.push(`  <url>\n    <loc>${SITE_BASE_URL}/events/${dateFilename}</loc>\n    <lastmod>${today}</lastmod>\n  </url>`);
    }

    const sitemap = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${sitemapEntries.join('\n')}\n</urlset>\n`;

    fs.writeFileSync(path.join(DIST_DIR, 'sitemap.xml'), sitemap, 'utf-8');
    console.log(`Generated sitemap.xml with ${sitemapEntries.length} URL(s).`);

    // Inject static event listings and CSS into index.html
    console.log('Injecting static content into index.html...');

    const eventListHtml = generateEventListHtml(events);
    const faqHtml = generateFaqHtml();

    // Read the built index.html
    let indexHtmlContent = fs.readFileSync(indexHtmlPath, 'utf-8');

    // Inject CSS link in the head (since we removed React from index.html)
    if (assetRefs.cssPath) {
        const headEndPattern = /<\/head>/;
        indexHtmlContent = indexHtmlContent.replace(
            headEndPattern,
            `    <link rel="stylesheet" crossorigin href="${assetRefs.cssPath}">\n</head>`
        );
        console.log(`  Injected CSS: ${assetRefs.cssPath}`);
    }

    // Find and replace the event-list div with static content
    const eventListDivPattern = /<div class="container pt-3" id="event-list">\s*<!-- Static event list and FAQ will be injected here by generate-event-pages\.js during build -->\s*<\/div>/;
    if (eventListDivPattern.test(indexHtmlContent)) {
        indexHtmlContent = indexHtmlContent.replace(
            eventListDivPattern,
            `<div class="container pt-3" id="event-list">\n${eventListHtml}${faqHtml}\n</div>`
        );

        // Inject Bootstrap JS before closing body tag for accordion functionality
        const bodyEndPattern = /<\/body>/;
        indexHtmlContent = indexHtmlContent.replace(
            bodyEndPattern,
            `<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js" integrity="sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz" crossorigin="anonymous"></script>\n</body>`
        );
        console.log('  Injected Bootstrap JS for accordion functionality');

        // Write the modified HTML back
        fs.writeFileSync(indexHtmlPath, indexHtmlContent, 'utf-8');
        console.log('Successfully injected static event listings into index.html');
    } else {
        console.warn('Warning: Could not find event-list div pattern in index.html. Static content not injected.');
    }
}

main().catch(error => {
    console.error('Unexpected error:', error);
    process.exit(1);
});
