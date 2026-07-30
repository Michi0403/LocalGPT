(() => { try {
    'use strict';

    // javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
    const diagnostics = window.localGptJavaScriptDiagnostics;
    const hostSelector = '[data-testid="dxaichat-host"]';
    let scheduled = false;

    function visible(element) {
        try {
            if (!(element instanceof HTMLElement)) return false;
            const style = getComputedStyle(element);
            return style.display !== 'none' && style.visibility !== 'hidden' && element.getClientRects().length > 0;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.visible', error);
            throw error;
        }
    }

    function marker(element) {
        try {
            return [
                element.getAttribute('aria-label'),
                element.getAttribute('title'),
                element.getAttribute('data-testid'),
                element.getAttribute('class'),
                element.textContent
            ].filter(Boolean).join(' ');
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.marker', error);
            throw error;
        }
    }

    function addClass(element, className) {
        try {
            if (element instanceof HTMLElement && !element.classList.contains(className)) element.classList.add(className);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.addClass', error);
            throw error;
        }
    }

    function setAttributeIfMissing(element, name, value) {
        try {
            if (element instanceof HTMLElement && !element.hasAttribute(name)) element.setAttribute(name, value);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.setAttributeIfMissing', error);
            throw error;
        }
    }

    function findComposer(host, editor, send) {
        try {
            if (!(editor instanceof HTMLElement)) return null;
            const marked = editor.closest('[class*="composer"],[class*="input-container"],[class*="prompt-input"],form');
            if (marked instanceof HTMLElement && host.contains(marked) && (!send || marked.contains(send))) return marked;

            let current = editor.parentElement;
            for (let depth = 0; current && current !== host && depth < 7; depth++, current = current.parentElement) {
                const rect = current.getBoundingClientRect();
                if ((!send || current.contains(send)) && rect.height > 0 && rect.height <= Math.max(260, innerHeight * .34)) return current;
            }
            return editor.parentElement;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.findComposer', error);
            throw error;
        }
    }

    function suggestionRegion(button, host, composer) {
        try {
            if (!(button instanceof HTMLElement) || composer?.contains(button)) return false;
            if (button.closest('[role="toolbar"],.dxbl-toolbar,.chat-session-toolbar,.chat-provider-row')) return false;
            const text = (button.textContent || '').replace(/\s+/g, ' ').trim();
            if (text.length < 4 || text.length > 260) return false;
            if (/close|collapse|expand|menu|copy|retry|regenerate|send|attach|upload|file|refresh|approve|hide/i.test(marker(button))) return false;
            return Boolean(button.closest('[class*="suggest"],[class*="welcome"],[class*="empty"],ul,ol') || host.querySelectorAll('button').length <= 16);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.suggestionRegion', error);
            throw error;
        }
    }

    function markChatRoots(host) {
        try {
            for (const child of host.children) addClass(child, 'localgpt-chat-root');
            const chat = host.querySelector('.demo-chat,[class*="ai-chat" i],[class*="aichat" i]');
            addClass(chat, 'localgpt-chat-root');
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.markChatRoots', error);
            throw error;
        }
    }

    const slowScrollStates = new WeakMap();

    function findScrollRegion(host, composer) {
        try {
            const candidates = [...host.querySelectorAll('*')]
                .filter(element => {
                    if (!(element instanceof HTMLElement) || composer?.contains(element)) return false;
                    const style = getComputedStyle(element);
                    return /(auto|scroll)/i.test(style.overflowY)
                        && element.clientHeight >= 120
                        && element.scrollHeight > element.clientHeight + 8;
                })
                .sort((left, right) => (right.clientHeight * right.clientWidth) - (left.clientHeight * left.clientWidth));
            return candidates[0] || null;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.findScrollRegion', error);
            throw error;
        }
    }

    function cancelSlowFollow(state) {
        try {
            state.follow = false;
            state.userInteracting = true;
            state.displayTop = state.region.scrollTop;
            if (state.frame) cancelAnimationFrame(state.frame);
            state.frame = 0;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.cancelSlowFollow', error);
            throw error;
        }
    }

    function bindSlowScroll(host, region) {
        try {
            let state = slowScrollStates.get(host);
            if (state?.region === region) return state;
            if (state?.frame) cancelAnimationFrame(state.frame);

            state = {
                region,
                displayTop: region.scrollTop,
                targetTop: Math.max(0, region.scrollHeight - region.clientHeight),
                frame: 0,
                follow: region.scrollHeight - region.clientHeight - region.scrollTop < 96,
                userInteracting: false
            };
            slowScrollStates.set(host, state);

            const beginUserScroll = diagnostics.guard('localgpt-chat-ui.slowScroll.userInput', () => cancelSlowFollow(state));
            region.addEventListener('wheel', beginUserScroll, { passive: true });
            region.addEventListener('touchstart', beginUserScroll, { passive: true });
            region.addEventListener('pointerdown', beginUserScroll, { passive: true });
            region.addEventListener('keydown', diagnostics.guard('localgpt-chat-ui.slowScroll.keydown', event => {
                if (/ArrowUp|ArrowDown|PageUp|PageDown|Home|End|Space/.test(event.code)) cancelSlowFollow(state);
            }));
            region.addEventListener('scroll', diagnostics.guard('localgpt-chat-ui.slowScroll.scroll', () => {
                if (!state.userInteracting) return;
                state.displayTop = region.scrollTop;
                state.follow = region.scrollHeight - region.clientHeight - region.scrollTop < 96;
            }), { passive: true });
            region.addEventListener('pointerup', diagnostics.guard('localgpt-chat-ui.slowScroll.pointerup', () => {
                state.userInteracting = false;
                state.follow = region.scrollHeight - region.clientHeight - region.scrollTop < 96;
            }), { passive: true });
            region.addEventListener('touchend', diagnostics.guard('localgpt-chat-ui.slowScroll.touchend', () => {
                state.userInteracting = false;
                state.follow = region.scrollHeight - region.clientHeight - region.scrollTop < 96;
            }), { passive: true });
            return state;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.bindSlowScroll', error);
            throw error;
        }
    }

    function scheduleSlowFollow(host, region) {
        try {
            const state = bindSlowScroll(host, region);
            if (!state || state.userInteracting || !state.follow) return;

            const target = Math.max(0, region.scrollHeight - region.clientHeight);
            if (target <= state.displayTop + 1) {
                state.displayTop = target;
                return;
            }

            // DevExpress may jump the message viewport immediately while streaming.
            // Restore the last rendered position, then travel to the moving target over
            // roughly seven seconds. New tokens update the target without restarting.
            region.scrollTop = Math.min(state.displayTop, target);
            state.targetTop = target;
            if (state.frame) return;

            const started = performance.now();
            const startTop = state.displayTop;
            const duration = 7000;
            const step = diagnostics.guard('localgpt-chat-ui.slowScroll.step', now => {
                if (state.userInteracting || !state.follow) {
                    state.frame = 0;
                    return;
                }
                state.targetTop = Math.max(0, region.scrollHeight - region.clientHeight);
                const progress = Math.min(1, (now - started) / duration);
                const eased = progress < .5
                    ? 2 * progress * progress
                    : 1 - Math.pow(-2 * progress + 2, 2) / 2;
                state.displayTop = startTop + (state.targetTop - startTop) * eased;
                region.scrollTop = state.displayTop;
                if (progress < 1) state.frame = requestAnimationFrame(step);
                else {
                    state.displayTop = state.targetTop;
                    region.scrollTop = state.targetTop;
                    state.frame = 0;
                }
            });
            state.frame = requestAnimationFrame(step);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.scheduleSlowFollow', error);
            throw error;
        }
    }

    function enhance(host) {
        try {
            if (!(host instanceof HTMLElement)) return;
            markChatRoots(host);

            const editor = host.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
            addClass(editor, 'localgpt-chat-textarea');
            setAttributeIfMissing(editor, 'aria-label', 'Message to AI assistant');
            if (editor instanceof HTMLElement) editor.dataset.localgptChatInput = 'true';

            const buttons = [...host.querySelectorAll('button,[role="button"]')].filter(visible);
            const send = buttons.find(button => { try { return (/send|submit|paper-plane|arrow-right/i.test(marker(button))
                && !/attach|upload|file|paperclip|clip/i.test(marker(button))); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:buttons.find@107', __javascriptError); throw __javascriptError; } }) || null;
            if (send) {
                addClass(send, 'localgpt-send-button');
                setAttributeIfMissing(send, 'aria-label', 'Send message');
                setAttributeIfMissing(send, 'title', 'Send message');
            }

            const upload = buttons.find(button => { try { return (/attach|upload|paperclip|choose file/i.test(marker(button))); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:buttons.find@115', __javascriptError); throw __javascriptError; } }) || null;
            if (upload) {
                addClass(upload, 'localgpt-upload-button');
                setAttributeIfMissing(upload, 'aria-label', 'Attach files');
                setAttributeIfMissing(upload, 'title', 'Attach files');
            }

            const composer = findComposer(host, editor, send);
            addClass(composer, 'localgpt-chat-composer');
            if (composer instanceof HTMLElement) composer.dataset.localgptComposer = 'true';

            for (const button of buttons) {
                if (button === send || button === upload) continue;
                if (suggestionRegion(button, host, composer)) {
                    addClass(button, 'localgpt-prompt-suggestion');
                    addClass(button.parentElement, 'localgpt-prompt-suggestions');
                }
            }

            const scrollRegion = findScrollRegion(host, composer);
            if (scrollRegion) {
                addClass(scrollRegion, 'localgpt-chat-scroll-region');
                scheduleSlowFollow(host, scrollRegion);
            }
            host.dataset.localgptChatEnhanced = 'true';
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.enhance', error);
            throw error;
        }
    }

    function apply() {
        try {
            scheduled = false;
            document.querySelectorAll(hostSelector).forEach(diagnostics.guard('localgpt-chat-ui.apply.enhance', enhance));
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.apply', error);
            throw error;
        }
    }

    function scheduleApply() {
        try {
            if (scheduled) return;
            scheduled = true;
            requestAnimationFrame(diagnostics.guard('localgpt-chat-ui.scheduleApply.frame', apply));
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.scheduleApply', error);
            throw error;
        }
    }

    const observer = new MutationObserver(diagnostics.guard('localgpt-chat-ui.mutationObserver', records => { try {
        if (records.some(record => { try { return (record.addedNodes.length > 0 || record.removedNodes.length > 0); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:records.some@159', __javascriptError); throw __javascriptError; } })) scheduleApply();
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:diagnostics.guard@158', __javascriptError); throw __javascriptError; }}));

    function start() {
        try {
            scheduleApply();
            observer.observe(document.body, { childList: true, subtree: true });
            window.localGptSlowScrollToBottom = diagnostics.guard('localgpt-chat-ui.externalSlowScroll', elementId => {
                const host = document.getElementById(elementId) || document.querySelector(hostSelector);
                if (!(host instanceof HTMLElement)) return;
                const composer = host.querySelector('.localgpt-chat-composer');
                const region = findScrollRegion(host, composer);
                if (!region) return;
                const state = bindSlowScroll(host, region);
                state.follow = true;
                state.userInteracting = false;
                scheduleSlowFollow(host, region);
            });
            document.addEventListener('focusin', diagnostics.guard('localgpt-chat-ui.focusin', event => { try {
                if (event.target instanceof Element && event.target.closest(hostSelector)) scheduleApply();
             } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:diagnostics.guard@166', __javascriptError); throw __javascriptError; }}), true);
            document.addEventListener('localgpt:focus-chat', diagnostics.guard('localgpt-chat-ui.focusChat', () => { try {
                const editor = document.querySelector(`${hostSelector} textarea,${hostSelector} [contenteditable="true"],${hostSelector} [role="textbox"]`);
                if (editor instanceof HTMLElement) editor.focus({ preventScroll: false });
             } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:diagnostics.guard@169', __javascriptError); throw __javascriptError; }}));
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.start', error);
            throw error;
        }
    }

    try {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', diagnostics.guard('localgpt-chat-ui.domContentLoaded', start), { once: true });
        else start();
    } catch (error) {
        diagnostics.report('localgpt-chat-ui.bootstrap', error);
        throw error;
    }
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:ArrowFunction@1', __javascriptError); throw __javascriptError; }})();
