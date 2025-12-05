const CACHE_NAME = 'dj-mark-v1';

self.addEventListener('install', () => {
  self.skipWaiting();
});

self.addEventListener('activate', () => {
  self.clients.claim();
});

// Intercept all fetch requests
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);
  
  // Check if this is a share target request
  if (url.pathname === '/share-target' && event.request.method === 'POST') {
    event.respondWith(handleShareTarget(event.request));
    return;
  }
  
  // Let all other requests pass through normally
  event.respondWith(fetch(event.request));
});

async function handleShareTarget(request) {
  try {
    // Parse the form data from the share
    const formData = await request.formData();
    const sharedUrl = formData.get('url') || '';
    const sharedTitle = formData.get('title') || '';
    const sharedText = formData.get('text') || '';

    // Encode the shared data as URL parameters
    const params = new URLSearchParams();
    if (sharedUrl) params.append('url', sharedUrl);
    if (sharedTitle) params.append('title', sharedTitle);
    if (sharedText) params.append('text', sharedText);

    // Redirect to main app with shared data in URL
    return Response.redirect(`/?shared=true&${params.toString()}`, 303);
  } catch (error) {
    console.error('Share handling error:', error);
    return Response.redirect('/?shared=error', 303);
  }
}