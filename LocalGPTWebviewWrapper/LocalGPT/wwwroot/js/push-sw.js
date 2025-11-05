self.addEventListener('install', e => self.skipWaiting());
self.addEventListener('activate', e => e.waitUntil(self.clients.claim()));

// Show notification from JSON payload
self.addEventListener('push', e => {
    let p = {};
    try { p = e.data?.json() ?? {}; } catch { }
    e.waitUntil(self.registration.showNotification(p.title || '🔔', {
        body: p.body || '',
        icon: p.icon || '/android-chrome-192x192.png',
        badge: p.badge || '/favicon-32x32.png',
        image: p.image,                        // shown on Chrome/Android; ignored on iOS
        tag: p.tag,
        renotify: !!p.renotify,
        requireInteraction: !!p.requireInteraction,
        actions: p.actions || [],              // up to 2 buttons
        data: p.data || {}                    // your custom round-trip data
    }));
});

// Handle body or button clicks
self.addEventListener('notificationclick', event => {
    event.notification.close();
    const action = event.action || 'default';
    const data = event.notification.data || {};
    const map = data.actions || {};
    const spec = map[action] || {};
    const navUrl = spec.navigate || data.defaultUrl || '/';

    event.waitUntil((async () => {
        // 1) record reaction (for auditing/workflow)
        if (data.eventId) {
            try {
                await fetch('/api/push/reaction', {
                    method: 'POST', credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ eventId: data.eventId, action })
                });
            } catch { }
        }

        // 2) optional: perform the domain action immediately
        if (spec.api?.url) {
            try {
                await fetch(spec.api.url, {
                    method: spec.api.method || 'POST',
                    credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: spec.api.body ? JSON.stringify(spec.api.body) : undefined
                });
            } catch { }
        }

        // 3) focus or open the app
        const wins = await clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const w of wins) { try { await w.navigate(navUrl); return w.focus(); } catch { } }
        if (clients.openWindow) return clients.openWindow(navUrl);
    })());
});

// (optional) track dismissals
self.addEventListener('notificationclose', event => {
    const { eventId } = event.notification.data || {};
    if (!eventId) return;
    event.waitUntil(fetch('/api/push/reaction', {
        method: 'POST', credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ eventId, action: 'dismiss' })
    }).catch(() => { }));
});