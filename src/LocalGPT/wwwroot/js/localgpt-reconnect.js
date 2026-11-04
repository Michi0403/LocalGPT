// javascript-diagnostics: guarded
(() => {
    'use strict';

    const storageKey = 'localgpt.activeCouncilRunId';
    const conversationStorageKey = 'localgpt.activeChatConversationId';
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    let observedModal = null;
    let classObserver = null;
    let resumeCheckTimer = 0;
    let hiddenAfterInteractiveReady = false;
    let autoRecoveryTimer = 0;
    let autoRecoveryInProgress = false;
    let confirmedRunId = '';
    let confirmedConversationId = '';
    const automaticReloadKey = 'localgpt.reconnect.autoReloadBudget';

    function report(context, error) {
        try { globalThis.localGptJavaScriptDiagnostics?.report?.(`localgpt-reconnect.${context}`, error); }
        catch (reportError) { console.error('LocalGPT reconnect diagnostics failed.', reportError); }
    }

    function readRunId() {
        try {
            const fromUrl = new URL(location.href).searchParams.get('rejoinCouncilRunId') || '';
            const value = confirmedRunId || (guidPattern.test(fromUrl) ? fromUrl : '') || sessionStorage.getItem(storageKey) || '';
            return guidPattern.test(value) ? value : '';
        } catch (error) {
            report('readRunId', error);
            return '';
        }
    }

    // A Council run and a Chat conversation are distinct identities. Preserve both on a
    // full circuit reload so joining a running Council never creates an empty Chat.
    function readConversationId() {
        try {
            const fromUrl = new URL(location.href).searchParams.get('rejoinConversationId') || '';
            const value = confirmedConversationId || (guidPattern.test(fromUrl) ? fromUrl : '') ||
                sessionStorage.getItem(conversationStorageKey) || '';
            return guidPattern.test(value) ? value : '';
        } catch (error) {
            report('readConversationId', error);
            return '';
        }
    }

    function writeConversationId(value) {
        try {
            const normalized = String(value || '').trim();
            if (guidPattern.test(normalized)) sessionStorage.setItem(conversationStorageKey, normalized);
            else sessionStorage.removeItem(conversationStorageKey);
        } catch (error) { report('writeConversationId', error); }
    }

    function writeRunId(value) {
        try {
            const normalized = String(value || '').trim();
            if (!guidPattern.test(normalized)) {
                sessionStorage.removeItem(storageKey);
                return;
            }
            sessionStorage.setItem(storageKey, normalized);
        } catch (error) {
            report('writeRunId', error);
        }
    }

    function buildReloadTarget() {
      try {
                const runId = readRunId();
                const conversationId = readConversationId();
                if (!runId && !conversationId) return location.href;
                const url = location.pathname.toLowerCase().endsWith('/chat') ? new URL(location.href) : new URL('Chat', document.baseURI);
                if (runId) url.searchParams.set('rejoinCouncilRunId', runId);
                else url.searchParams.delete('rejoinCouncilRunId');
                if (conversationId) url.searchParams.set('rejoinConversationId', conversationId);
                else url.searchParams.delete('rejoinConversationId');
                return url.href;
    
      } catch (error) {
        report('buildReloadTarget', error);
        throw error;
      }
    }

    function claimAutomaticReload() {
        try {
            const now = Date.now();
            const saved = JSON.parse(sessionStorage.getItem(automaticReloadKey) || 'null');
            const withinWindow = saved && Number.isFinite(saved.started) &&
                now >= saved.started && now - saved.started < 120000;
            const started = withinWindow ? saved.started : now;
            const attempts = withinWindow ? Number(saved.attempts) || 0 : 0;
            if (attempts >= 2) return false;
            sessionStorage.setItem(automaticReloadKey, JSON.stringify({ started, attempts: attempts + 1 }));
            return true;
        } catch (error) {
            report('claimAutomaticReload', error);
            return false;
        }
    }

    function cancelAutomaticRecovery() {
        if (autoRecoveryTimer) clearTimeout(autoRecoveryTimer);
        autoRecoveryTimer = 0;
        autoRecoveryInProgress = false;
    }

    function scheduleAutomaticRecovery(delayMilliseconds) {
        try {
            if (autoRecoveryTimer || autoRecoveryInProgress) return;
            autoRecoveryTimer = window.setTimeout(async () => {
                autoRecoveryTimer = 0;
                const modal = findModal();
                if (!(modal instanceof HTMLElement)) return;
                if (modal.classList.contains('components-reconnect-hide')) return;
                if (navigator.onLine === false || document.visibilityState === 'hidden') return;
                if (!modal.classList.contains('components-reconnect-show') &&
                    !modal.classList.contains('components-reconnect-failed') &&
                    !modal.classList.contains('components-reconnect-rejected')) return;
                autoRecoveryInProgress = true;
                try {
                    // A rejected circuit is gone; Blazor.reconnect() cannot revive it.
                    // For a transient failure, give the framework one last reconnect.
                    if (!modal.classList.contains('components-reconnect-rejected') &&
                        typeof globalThis.Blazor?.reconnect === 'function') {
                        try {
                            const restored = await globalThis.Blazor.reconnect();
                            if (restored) return;
                        } catch (error) {
                            report('automaticReconnect', error);
                        }
                    }
                    if (!modal.classList.contains('components-reconnect-hide') && claimAutomaticReload()) {
                        reloadAndRejoin();
                    } else if (!modal.classList.contains('components-reconnect-hide')) {
                        setStatus(modal, 'Automatic recovery paused to avoid a reload loop. Use Reload & rejoin to retry manually.');
                    }
                } finally {
                    autoRecoveryInProgress = false;
                }
            }, delayMilliseconds);
        } catch (error) {
            report('scheduleAutomaticRecovery', error);
        }
    }

    function recoverFromBackForwardCache(event) {
        try {
            if (!event?.persisted) return;
            event.stopImmediatePropagation?.();
            const target = new URL(buildReloadTarget(), document.baseURI);
            target.searchParams.set('_localgptBfcacheReload', Date.now().toString());
            location.replace(target.href);
        } catch (error) {
            report('recoverFromBackForwardCache', error);
            location.reload();
        }
    }

    function clearBackForwardCacheMarker() {
        try {
            const url = new URL(location.href);
            if (!url.searchParams.has('_localgptBfcacheReload')) return;
            url.searchParams.delete('_localgptBfcacheReload');
            history.replaceState(history.state, '', url.href);
        } catch (error) {
            report('clearBackForwardCacheMarker', error);
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

    function interactiveShellLooksUsable() {
        try {
            const main = document.getElementById('current-main-window');
            if (!(main instanceof HTMLElement)) return false;
            const bounds = main.getBoundingClientRect();
            return bounds.width > 1 && bounds.height > 1 && main.childElementCount > 0;
        } catch (error) {
            report('interactiveShellLooksUsable', error);
            return false;
        }
    }

    function revealRecoverySurface(message) {
        try {
            const modal = findModal();
            if (!(modal instanceof HTMLElement)) return null;
            modal.classList.remove('components-reconnect-hide');
            if (!modal.classList.contains('components-reconnect-failed') &&
                !modal.classList.contains('components-reconnect-rejected'))
                modal.classList.add('components-reconnect-show');
            setStatus(modal, message);
            setBusy(modal, false);
            return modal;
        } catch (error) {
            report('revealRecoverySurface', error);
            return null;
        }
    }

    function scheduleResumeHealthCheck(reason) {
        try {
            if (resumeCheckTimer) clearTimeout(resumeCheckTimer);
            resumeCheckTimer = window.setTimeout(() => {
                resumeCheckTimer = 0;
                try {
                    if (document.visibilityState === 'hidden') return;
                    const modal = findModal();
                    const reconnectVisible = modal instanceof HTMLElement &&
                        (modal.classList.contains('components-reconnect-show') ||
                         modal.classList.contains('components-reconnect-failed') ||
                         modal.classList.contains('components-reconnect-rejected'));
                    const startupFailed = document.documentElement.classList.contains('localgpt-interactive-error');
                    const ready = document.documentElement.classList.contains('localgpt-interactive-ready');
                    const shellUsable = interactiveShellLooksUsable();

                    if (!reconnectVisible && !startupFailed && ready && shellUsable) return;

                    revealRecoverySurface(
                        `The browser resumed after ${reason}, but the interactive surface is not healthy yet. ` +
                        'LocalGPT will try to reconnect without stopping server-side Council or benchmark work.');
                    if (typeof globalThis.Blazor?.reconnect === 'function')
                        void reconnectNow();
                } catch (error) {
                    report('scheduleResumeHealthCheck.callback', error);
                }
            }, 1250);
        } catch (error) {
            report('scheduleResumeHealthCheck', error);
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
            scheduleAutomaticRecovery(1200);
        } else if (modal.classList.contains('components-reconnect-rejected')) {
            setStatus(modal, `The previous interactive circuit can no longer be restored.${suffix}`);
            setBusy(modal, false);
            scheduleAutomaticRecovery(600);
        } else if (modal.classList.contains('components-reconnect-show')) {
            setStatus(modal, `The interactive UI is disconnected while server-side Council work may still continue.${suffix}`);
            setBusy(modal, false);
            scheduleAutomaticRecovery(15000);
        } else if (modal.classList.contains('components-reconnect-hide')) {
            cancelAutomaticRecovery();
            setBusy(modal, false);
            globalThis.localGptJavaScriptDiagnostics?.resumeTransport?.();
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
                reloadAndRejoin();
                return;
            }
            const restored = await reconnect();
            if (!restored) {
                reloadAndRejoin();
            }
        } catch (error) {
            report('reconnectNow', error);
            reloadAndRejoin();
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
                clearBackForwardCacheMarker();
                observeModal();
                // The reconnect modal is a direct body child, and its buttons use delegated
                // clicks. Observing the entire document subtree here inspected every live
                // Council token/DevExpress render and amplified main-thread contention.
                new MutationObserver(observeModal).observe(document.body, { childList: true });

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

                window.addEventListener('online', () => {
                    updateFromClass();
                    if (hiddenAfterInteractiveReady) scheduleResumeHealthCheck('network recovery');
                });
                document.addEventListener('visibilitychange', () => {
                    try {
                        if (document.visibilityState === 'hidden') {
                            hiddenAfterInteractiveReady = document.documentElement.classList.contains('localgpt-interactive-ready');
                            return;
                        }
                        if (hiddenAfterInteractiveReady) {
                            hiddenAfterInteractiveReady = false;
                            scheduleResumeHealthCheck('a hidden/background tab');
                        }
                    } catch (error) {
                        report('visibilitychange', error);
                    }
                });
                window.addEventListener('focus', () => {
                    if (hiddenAfterInteractiveReady) {
                        hiddenAfterInteractiveReady = false;
                        scheduleResumeHealthCheck('browser focus restoration');
                    }
                });
                globalThis.localGptReconnect = Object.freeze({
                    showDisconnected() {
                        revealRecoverySurface('The browser lost its Blazor connection. LocalGPT will recover and rejoin the Council automatically if necessary.');
                        scheduleAutomaticRecovery(15000);
                    },
                    setConversation(conversationId) {
                        const normalized = String(conversationId || '').trim();
                        if (!guidPattern.test(normalized)) return;
                        confirmedConversationId = normalized;
                        writeConversationId(normalized);
                        // A previously rejoined URL must not keep targeting a different chat
                        // if the user explicitly loads another saved conversation.
                        try {
                            const url = new URL(location.href);
                            if (url.searchParams.has('rejoinConversationId') &&
                                url.searchParams.get('rejoinConversationId') !== normalized) {
                                url.searchParams.set('rejoinConversationId', normalized);
                                history.replaceState(history.state, '', url.href);
                            }
                        } catch (error) { report('setConversation', error); }
                    },
                    clearConversation() {
                        confirmedConversationId = '';
                        writeConversationId('');
                        try {
                            const url = new URL(location.href);
                            if (url.searchParams.has('rejoinConversationId')) {
                                url.searchParams.delete('rejoinConversationId');
                                history.replaceState(history.state, '', url.href);
                            }
                        } catch (error) { report('clearConversation', error); }
                    },
                    setCouncilRun(runId) {
                        // A fresh circuit may render before the server run is recovered.
                        if (guidPattern.test(String(runId || ''))) {
                            confirmedRunId = String(runId);
                            writeRunId(runId);
                            try {
                                const url = new URL(location.href);
                                if (url.searchParams.has('rejoinCouncilRunId') &&
                                    url.searchParams.get('rejoinCouncilRunId') !== String(runId)) {
                                    url.searchParams.set('rejoinCouncilRunId', String(runId));
                                    history.replaceState(history.state, '', url.href);
                                }
                            } catch (error) { report('setCouncilRun', error); }
                        }
                        updateFromClass();
                    },
                    clearCouncilRun() {
                        confirmedRunId = '';
                        writeRunId('');
                        try {
                            const url = new URL(location.href);
                            if (url.searchParams.has('rejoinCouncilRunId')) {
                                url.searchParams.delete('rejoinCouncilRunId');
                                history.replaceState(history.state, '', url.href);
                            }
                        } catch (error) { report('clearCouncilRun', error); }
                        updateFromClass();
                    },
                    reloadAndRejoin
                });
    
      } catch (error) {
        report('start', error);
        throw error;
      }
    }

    // This script is loaded before blazor.web.js. Register first so a BFCache restore cannot
    // hand Blazor/DevExpress a preserved DOM containing stale event-tracker registrations.
    window.addEventListener('pageshow', recoverFromBackForwardCache, { capture: true });

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', start, { once: true });
    else
        start();
})();
