const hostStates = new WeakMap();
const boundPanels = new WeakSet();

function getHostState(host) {
    let state = hostStates.get(host);
    if (!state) {
        state = new Map();
        hostStates.set(host, state);
    }

    return state;
}

function bindPanel(host, panel, index) {
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
    summary?.addEventListener('click', () => {
        state.set(key, !panel.open);
        panel.dataset.localgptUserTogglePending = 'true';
    });

    panel.addEventListener('toggle', () => {
        // Browsers may emit a toggle event when an `open` element is first
        // inserted. Treat only a preceding user interaction as a preference.
        if (panel.dataset.localgptUserTogglePending !== 'true') {
            return;
        }

        delete panel.dataset.localgptUserTogglePending;
        state.set(key, panel.open);
    });
}

function refreshHost(host) {
    const panels = host.querySelectorAll('details[data-localgpt-panel-key]');
    panels.forEach((panel, index) => bindPanel(host, panel, index));
}

function bindHost(host) {
    if (host.dataset.localgptDetailsStateBound === 'true') {
        refreshHost(host);
        return;
    }

    host.dataset.localgptDetailsStateBound = 'true';
    const observer = new MutationObserver(() => refreshHost(host));
    observer.observe(host, { childList: true, subtree: true });
    refreshHost(host);
}

function scan(root = document) {
    if (root instanceof Element && root.matches('[data-localgpt-details-host]')) {
        bindHost(root);
    }

    root.querySelectorAll?.('[data-localgpt-details-host]').forEach(bindHost);
}

function start() {
    scan(document);
    const observer = new MutationObserver(mutations => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node instanceof Element) {
                    scan(node);
                }
            }
        }
    });
    observer.observe(document.documentElement, { childList: true, subtree: true });
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start, { once: true });
} else {
    start();
}
