import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve, dirname } from 'path'
import { readFileSync } from 'fs'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

// Helper functions for generating event HTML (duplicated from generate-event-pages.js for dev mode)
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

function escapeHtml(text) {
  if (!text) return '';
  return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
}

function renderTags(event) {
  if (!event.tagList || event.tagList.length === 0) return '';
  const badges = event.tagList.map(tag =>
      `<span class="badge rounded-pill bg-${tag.colour} mx-1">${escapeHtml(tag.name)}</span>`
  ).join('');
  return `<div>${badges}</div>`;
}

function generateDevEventListHtml(events) {
  if (!Array.isArray(events) || events.length === 0) {
    return '<div class="alert alert-info">No events found.</div>';
  }

  return events.map(event => {
    const formattedDate = formatDate(event.date);
    const dateFilename = formatDateForFilename(event.date);

    let footerContent = '';
    if (event.isRequestable) {
      footerContent += `<a href="/events/${dateFilename}.html" class="btn btn-primary">Request a Track</a>`;
    }
    if (event.calendarInviteUrl) {
      footerContent += `<a href="${escapeHtml(event.calendarInviteUrl)}" class="btn btn-success mx-2" download rel="nofollow" title="Add to Calendar">📅</a>`;
    }
    if (event.facebookEventUrl) {
      footerContent += `<a href="${escapeHtml(event.facebookEventUrl)}" class="btn btn-facebook mx-2" target="_blank" rel="nofollow noreferrer" title="View on Facebook">📘</a>`;
    }

    return `
    <div class="card mb-3">
        <div class="card-header fw-bold">${escapeHtml(event.name)}</div>
        <div class="card-body">
            <p class="card-text">${escapeHtml(event.description)}</p>
            <p class="card-text">${escapeHtml(formattedDate)}, ${escapeHtml(event.times)}</p>
            <p class="card-text">${escapeHtml(event.locationName)}<br/>${escapeHtml(event.locationAddress)}</p>
            ${!event.isRequestable ? '<small>Requests are not currently open for this event.</small>' : ''}
            ${renderTags(event)}
        </div>
        ${footerContent ? `<div class="card-footer">\n            ${footerContent}\n        </div>` : ''}
    </div>`;
  }).join('\n');
}

function eventPagesDevPlugin() {
  return {
    name: 'event-pages-dev',
    configureServer(server) {
      server.middlewares.use(async (req, res, next) => {
        // Handle index page
        if (req.url === '/' || req.url === '/index.html') {
          try {
            const apiRes = await fetch('https://dj.stott.pro/api/events/list/');
            const events = await apiRes.json();

            // Read the source index.html
            const indexHtml = readFileSync(resolve(__dirname, 'index.html'), 'utf-8');

            // Generate event list HTML
            const eventListHtml = generateDevEventListHtml(events);

            // Inject event list into the placeholder
            let modifiedHtml = indexHtml.replace(
              /<div class="container pt-3" id="event-list">\s*<!-- Static event list and FAQ will be injected here by generate-event-pages\.js during build -->\s*<\/div>/,
              `<div class="container pt-3" id="event-list">\n${eventListHtml}\n</div>`
            );

            // Add script tag for dev mode so Vite can load CSS
            modifiedHtml = modifiedHtml.replace(
              /<\/body>/,
              '<script type="module" src="/src/main.jsx"></script>\n</body>'
            );

            const transformed = await server.transformIndexHtml(req.url, modifiedHtml);
            res.setHeader('Content-Type', 'text/html; charset=utf-8');
            res.end(transformed);
            return;
          } catch (err) {
            console.error('Index page dev plugin error:', err.message);
            // Fall through to next() to let Vite handle it normally
          }
        }

        // Handle individual event pages
        const match = req.url?.match(/^\/events\/(\d{4}-\d{2}-\d{2})/);
        if (!match) return next();

        const dateStr = match[1];
        try {
          const apiRes = await fetch('https://dj.stott.pro/api/events/list/');
          const events = await apiRes.json();
          const event = events.find(e => e.date && e.date.startsWith(dateStr));

          if (!event) {
            res.statusCode = 404;
            res.end('Event not found');
            return;
          }

          const eventJson = JSON.stringify(event).replace(/<\//g, '<\\/');
          const html = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>${event.name} - DJ Mark</title>
    <meta name="viewport" content="maximum-scale=5.0, initial-scale=1.0, width=device-width">
    <meta name="theme-color" content="#212529">
    <link rel="icon" type="image/png" sizes="32x32" href="/images/favicon.png">
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
        <div class="card-header fw-bold">${event.name}</div>
        <div class="card-body">
            <p class="card-text">${event.description || ''}</p>
            <p class="card-text">${event.times || ''}</p>
            <p class="card-text">${event.locationName || ''}<br/>${event.locationAddress || ''}</p>
        </div>
    </div>
    <div id="eventpage"></div>
</div>
</main>
<script>window.__EVENT_DATA__ = ${eventJson};</script>
<script type="module" src="/src/main.jsx"></script>
</body>
</html>`;

          const transformed = await server.transformIndexHtml(req.url, html);
          res.setHeader('Content-Type', 'text/html; charset=utf-8');
          res.end(transformed);
        } catch (err) {
          console.error('Event page dev plugin error:', err.message);
          res.statusCode = 502;
          res.end('Failed to fetch event data. Is the API running on port 7071?');
        }
      });
    }
  };
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), eventPagesDevPlugin()],
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        djportal: resolve(__dirname, 'djportal.html'),
        admin: resolve(__dirname, 'admin.html'),
      },
      output: {
        assetFileNames: 'static/[name]-[hash][extname]',
        entryFileNames: 'static/[name]-[hash].js',
        chunkFileNames: 'static/[name]-[hash].js',
      }
    }
  }
})
