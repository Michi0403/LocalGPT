// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const hostStates = new WeakMap();
const boundPanels = new WeakSet();

function getHostState(host) { try {
    let state = hostStates.get(host);
    if (!state) {
        state = new Map();
        hostStates.set(host, state);
    }

    return state;
 } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:getHostState@5', __javascriptError); throw __javascriptError; }}

function bindPanel(host, panel, index) { try {
    const key = panel.dataset.localgptPanelKey || `panel-${index}`;
    const state = getHostState(host);

    // The renderer supplies the default state: unfinished panels are open and
    // completed panels are closed. A user decision takes precedence over that
    // default for the rest of this message's lifetime.
    if (state.has(key)) {
        panel.open = state.get(key);
    }

    if (boundPanels.has(panel)) {
        return;
    }

    boundPanels.add(panel);

    // Capture the intended value immediately. This closes the tiny race where
    // another streamed token replaces the element before the native `toggle`
    // event is delivered. The toggle handler remains the authoritative fallback
    // for keyboard activation and programmatic changes.
    const summary = panel.querySelector(':scope > summary');
    summary?.addEventListener('click', () => { try {
        state.set(key, !panel.open);
        panel.dataset.localgptUserTogglePending = 'true';
     } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:callback:summary?.addEventListener@37', __javascriptError); throw __javascriptError; }});

    panel.addEventListener('toggle', () => { try {
        // Browsers may emit a toggle event when an `open` element is first
        // inserted. Treat only a preceding user interaction as a preference.
        if (panel.dataset.localgptUserTogglePending !== 'true') {
            return;
        }

        delete panel.dataset.localgptUserTogglePending;
        state.set(key, panel.open);
     } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:callback:panel.addEventListener@42', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:bindPanel@15', __javascriptError); throw __javascriptError; }}

function refreshHost(host) { try {
    const panels = host.querySelectorAll('details[data-localgpt-panel-key]');
    panels.forEach((panel, index) => { try { return (bindPanel(host, panel, index)); } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:callback:panels.forEach@56', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:refreshHost@54', __javascriptError); throw __javascriptError; }}

function bindHost(host) { try {
    if (host.dataset.localgptDetailsStateBound === 'true') {
        refreshHost(host);
        return;
    }

    host.dataset.localgptDetailsStateBound = 'true';
    const observer = new MutationObserver(() => { try { return (refreshHost(host)); } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:ArrowFunction@66', __javascriptError); throw __javascriptError; } });
    observer.observe(host, { childList: true, subtree: true });
    refreshHost(host);
 } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:bindHost@59', __javascriptError); throw __javascriptError; }}

function scan(root = document) { try {
    if (root instanceof Element && root.matches('[data-localgpt-details-host]')) {
        bindHost(root);
    }

    root.querySelectorAll?.('[data-localgpt-details-host]').forEach(bindHost);
 } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:scan@71', __javascriptError); throw __javascriptError; }}

function start() { try {
    scan(document);
    const observer = new MutationObserver(mutations => { try {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node instanceof Element) {
                    scan(node);
                }
            }
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:ArrowFunction@81', __javascriptError); throw __javascriptError; }});
    observer.observe(document.documentElement, { childList: true, subtree: true });
 } catch (__javascriptError) { localGptDiagnostics.report('js/chat-details-state.js:start@79', __javascriptError); throw __javascriptError; }}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start, { once: true });
} else {
    start();
}
