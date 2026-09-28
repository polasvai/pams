self.addEventListener('install', (event) => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(clients.claim());
});

self.addEventListener('fetch', (event) => {
    // We do not want to cache dynamic auction data, 
    // so we let the network handle all requests by default.
    event.respondWith(fetch(event.request).catch(error => {
        // Optional: return offline page here if caching was set up for it
        throw error;
    }));
});
