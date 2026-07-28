// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.localGptOneWireSecurity = {
    renderQr(elementId, value, label) { try {
        const host = document.getElementById(elementId);
        if (!host) return;
        host.replaceChildren();
        if (!value) return;
        try {
            const qr = qrcode(0, 'M');
            qr.addData(String(value), 'Byte');
            qr.make();
            host.innerHTML = qr.createSvgTag({
                cellSize: 4,
                margin: 2,
                scalable: true,
                alt: { text: label || 'LocalGPT 1-Wire security QR code' }
            });
        } catch (error) {
            const message = document.createElement('span');
            message.className = 'onewire-security-error';
            message.textContent = `QR generation failed: ${error?.message || error}`;
            host.appendChild(message);
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/oneWireSecurityQr.js:renderQr@3', __javascriptError); throw __javascriptError; }}
};

// Guard browser entry points after initialization.
window.localGptOneWireSecurity = localGptDiagnostics.guard("oneWireSecurityQr.js.localGptOneWireSecurity", window.localGptOneWireSecurity);
