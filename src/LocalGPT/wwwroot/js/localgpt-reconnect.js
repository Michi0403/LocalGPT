// javascript-diagnostics: guarded
(() => {
    'use strict';

    const storageKey = 'localgpt.activeCouncilRunId';
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    let observedModal = null;
    let classObserver = null;

    function report(context, error) {
        try { globalThis.localGptJavaScriptDiagnostics?.report?.(`localgpt-reconnect.${context}`, error); }
        catch (reportError) { console.error('LocalGPT reconnect diagnostics failed.', reportError); }
    }

    function readRunId() {
        try {
            const value = sessionStorage.getItem(storageKey) || localStorage.getItem(storageKey) || '';
            return guidPattern.test(value) ? value : '';
        } catch (error) {
            report('readRunId', error);
            return '';
        }
    }

    function writeRunId(value) {
        try {
            const normalized = String(value || '').trim();
            if (!guidPattern.test(normalized)) {
                sessionStorage.removeItem(storageKey);
                localStorage.removeItem(storageKey);
                return;
            }
            sessionStorage.setItem(storageKey, normalized);
            localStorage.setItem(storageKey, normalized);
        } catch (error) {
            report('writeRunId', error);
        }
    }

    function buildReloadTarget() {
      try {
                const runId = readRunId();
                if (!runId) return location.href;
                const url = new URL('Chat', document.baseURI);
                url.searchParams.set('rejoinCouncilRunId', runId);
                return url.href;
    
      } catch (error) {
        report('buildReloadTarget', error);
        throw error;
      }
    }

    function findModal() {
      try {
                const modal = document.getElementById('components-reconnect-modal');
                return modal instanceof HTMLElement ? modal : null;
    
      } catch (error) {
        report('findModal', error);
        throw error;
      }
    }

    function setStatus(modal, value) {
      try {
                const status = modal?.querySelector('[data-localgpt-reconnect-status]');
                if (status instanceof HTMLElement) status.textContent = value;
    
      } catch (error) {
        report('setStatus', error);
        throw error;
      }
    }

    function setBusy(modal, busy) {
      try {
                const reconnectButton = modal?.querySelector('[data-localgpt-reconnect]');
                if (reconnectButton instanceof HTMLButtonElement) reconnectButton.disabled = busy;
    
      } catch (error) {
        report('setBusy', error);
        throw error;
      }
    }

    function updateFromClass(modal = findModal()) {
        if (!(modal instanceof HTMLElement)) return;
        const runId = readRunId();
        const suffix = runId
            ? ' Reloading will reopen this running Council directly.'
            : ' Reloading restores the UI; a running Council remains available through the Council spooler.';

        if (modal.classList.contains('components-reconnect-failed')) {
            setStatus(modal, `Automatic reconnection failed.${suffix}`);
            setBusy(modal, false);
        } else if (modal.classList.contains('components-reconnect-rejected')) {
            setStatus(modal, `The previous interactive circuit can no longer be restored.${suffix}`);
            setBusy(modal, false);
        } else if (modal.classList.contains('components-reconnect-show')) {
            setStatus(modal, `The interactive UI is disconnected while server-side Council work may still continue.${suffix}`);
            setBusy(modal, false);
        } else if (modal.classList.contains('components-reconnect-hide')) {
            setBusy(modal, false);
        }
    }

    function observeModal() {
      try {
                const modal = findModal();
                if (!(modal instanceof HTMLElement) || observedModal === modal) return;
                classObserver?.disconnect();
                observedModal = modal;
                classObserver = new MutationObserver(() => updateFromClass(modal));
                classObserver.observe(modal, { attributes: true, attributeFilter: ['class'] });
                updateFromClass(modal);
    
      } catch (error) {
        report('observeModal', error);
        throw error;
      }
    }

    async function reconnectNow() {
        const modal = findModal();
        if (!(modal instanceof HTMLElement)) return;
        setBusy(modal, true);
        setStatus(modal, 'Reconnecting the interactive UI…');
        try {
            const reconnect = globalThis.Blazor?.reconnect;
            if (typeof reconnect !== 'function') {
                setStatus(modal, 'The reconnect runtime is unavailable. Use Reload & rejoin instead.');
                setBusy(modal, false);
                return;
            }
            const restored = await reconnect();
            if (!restored) {
                setStatus(modal, 'The old circuit was rejected. Use Reload & rejoin to restore the UI and reopen the running Council.');
                setBusy(modal, false);
            }
        } catch (error) {
            report('reconnectNow', error);
            setStatus(modal, 'Reconnect failed. Use Reload & rejoin to restore the UI and reopen the running Council.');
            setBusy(modal, false);
        }
    }

    function reloadAndRejoin() {
      try {
                const target = buildReloadTarget();
                if (target === location.href) location.reload();
                else location.assign(target);
    
      } catch (error) {
        report('reloadAndRejoin', error);
        throw error;
      }
    }

    function start() {
      try {
                observeModal();
                new MutationObserver(observeModal).observe(document.documentElement, { childList: true, subtree: true });

                // Capture-phase delegation keeps the recovery controls browser-owned and clickable even when
                // the Blazor circuit is dead or a reconnect render replaced the modal's descendants.
                document.addEventListener('click', event => {
                    const target = event.target instanceof Element ? event.target.closest('button,a') : null;
                    if (!(target instanceof HTMLElement)) return;
                    if (target.matches('[data-localgpt-reconnect]')) {
                        event.preventDefault();
                        event.stopImmediatePropagation();
                        void reconnectNow();
                    } else if (target.matches('[data-localgpt-reload]')) {
                        event.preventDefault();
                        event.stopImmediatePropagation();
                        reloadAndRejoin();
                    }
                }, true);

                window.addEventListener('online', () => updateFromClass());
                globalThis.localGptReconnect = Object.freeze({
                    setCouncilRun(runId) {
                        writeRunId(runId);
                        updateFromClass();
                    },
                    clearCouncilRun() {
                        writeRunId('');
                        updateFromClass();
                    },
                    reloadAndRejoin
                });
    
      } catch (error) {
        report('start', error);
        throw error;
      }
    }

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', start, { once: true });
    else
        start();
})();
