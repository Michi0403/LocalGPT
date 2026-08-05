// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.setupPush = async function () { try {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return { ok: false, reason: 'unsupported' };

    // IMPORTANT: register at root so scope covers the whole app
    const reg = await navigator.serviceWorker.register('js/push-sw.js', { scope: '/js/' });

    // iOS/Safari & Chromium: permission must be triggered by user gesture
    let perm = Notification.permission;
    if (perm === 'default') perm = await Notification.requestPermission();
    if (perm !== 'granted') return { ok: false, reason: 'denied' };

    // Get public VAPID from your API
    const pub = await fetch('/api/push/publickey', { credentials: 'include' }).then(r => { try { return (r.json()); } catch (__javascriptError) { localGptDiagnostics.report('js/push.js:callback:fetch(\'/api/push/publickey\', { credentials: \'include\' }).then@14', __javascriptError); throw __javascriptError; } });

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
 } catch (__javascriptError) { localGptDiagnostics.report('js/push.js:window.setupPush@2', __javascriptError); throw __javascriptError; }}

window.b64urlToUint8 = function (b64url) { try {
    const pad = '='.repeat((4 - b64url.length % 4) % 4);
    const b64 = (b64url + pad).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(b64);
    const arr = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
    return arr;
 } catch (__javascriptError) { localGptDiagnostics.report('js/push.js:window.b64urlToUint8@31', __javascriptError); throw __javascriptError; }}

// Guard browser entry points after initialization.
window.setupPush = localGptDiagnostics.guard("push.js.setupPush", window.setupPush);
window.b64urlToUint8 = localGptDiagnostics.guard("push.js.b64urlToUint8", window.b64urlToUint8);
