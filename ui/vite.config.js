import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'path'

function eventPagesDevPlugin() {
  return {
    name: 'event-pages-dev',
    configureServer(server) {
      server.middlewares.use(async (req, res, next) => {
        const match = req.url?.match(/^\/events\/(\d{4}-\d{2}-\d{2})\.html/);
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
