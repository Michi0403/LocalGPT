window.setupPush = async function () {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return { ok: false, reason: 'unsupported' };

    // IMPORTANT: register at root so scope covers the whole app
    const reg = await navigator.serviceWorker.register('js/push-sw.js', { scope: '/js/' });

    // iOS/Safari & Chromium: permission must be triggered by user gesture
    let perm = Notification.permission;
    if (perm === 'default') perm = await Notification.requestPermission();
    if (perm !== 'granted') return { ok: false, reason: 'denied' };

    // Get public VAPID from your API
    const pub = await fetch('/api/push/publickey', { credentials: 'include' }).then(r => r.json());

    const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: b64urlToUint8(pub.key)
    });

    await fetch('/api/push/subscriptions', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' }, // drop anti-forgery (see note)
        body: JSON.stringify(sub)
    });

    return { ok: true };
}

window.b64urlToUint8 = function (b64url) {
    const pad = '='.repeat((4 - b64url.length % 4) % 4);
    const b64 = (b64url + pad).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(b64);
    const arr = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
    return arr;
}