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
    let councilComposerDotNet = null;
    let councilComposerActive = false;
    let councilComposerSubmitting = false;
    let liveUserMessageSequence = 0;
    const liveUserMessages = new WeakMap();

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
            const chatRoot = host.querySelector('.demo-chat') || host;
            const candidates = [...chatRoot.querySelectorAll('*')]
                .filter(element => {
                    if (!(element instanceof HTMLElement)
                        || composer?.contains(element)
                        || (composer instanceof HTMLElement && element.contains(composer))) return false;
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

            state.targetTop = Math.max(0, region.scrollHeight - region.clientHeight);
            if (state.frame) return;

            let lastFrame = performance.now();
            const step = diagnostics.guard('localgpt-chat-ui.slowScroll.step', now => {
                if (state.userInteracting || !state.follow) {
                    state.frame = 0;
                    return;
                }

                state.targetTop = Math.max(0, region.scrollHeight - region.clientHeight);
                const current = region.scrollTop;
                const remaining = state.targetTop - current;
                if (Math.abs(remaining) <= 1) {
                    region.scrollTop = state.targetTop;
                    state.displayTop = state.targetTop;
                    state.frame = 0;
                    return;
                }

                const elapsed = Math.max(1, now - lastFrame);
                lastFrame = now;
                // Move toward the changing bottom target with an approximately six-second
                // time constant. Never rewind the viewport to an older stored position.
                const fraction = 1 - Math.exp(-elapsed / 6000);
                const next = current + remaining * Math.max(.0025, fraction);
                region.scrollTop = next;
                state.displayTop = next;
                state.frame = requestAnimationFrame(step);
            });
            state.frame = requestAnimationFrame(step);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.scheduleSlowFollow', error);
            throw error;
        }
    }


    function editorText(editor) {
        try {
            if (editor instanceof HTMLTextAreaElement || editor instanceof HTMLInputElement) return editor.value || '';
            return editor instanceof HTMLElement ? (editor.innerText || editor.textContent || '') : '';
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.editorText', error);
            throw error;
        }
    }

    function findMessageList(region, composer) {
        try {
            if (!(region instanceof HTMLElement)) return null;
            const candidates = [...region.querySelectorAll(
                '[role="log"],[role="list"],[class*="message-list" i],[class*="messages" i],[class*="chat-history" i],[class*="conversation" i]')]
                .filter(element => element instanceof HTMLElement
                    && !composer?.contains(element)
                    && !(composer instanceof HTMLElement && element.contains(composer)));
            const scored = candidates.map(element => ({
                element,
                score: element.querySelectorAll(
                    '[role="listitem"],[class*="message" i],[data-message-role],[data-role]').length * 100
                    + element.children.length * 10
                    + (element.parentElement?.querySelectorAll('[class*="message" i]').length ?? 0)
            })).sort((left, right) => right.score - left.score);
            return scored[0]?.element || region;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.findMessageList', error);
            throw error;
        }
    }

    function clearEditor(editor) {
        try {
            if (editor instanceof HTMLTextAreaElement || editor instanceof HTMLInputElement) editor.value = '';
            else if (editor instanceof HTMLElement) editor.textContent = '';
            editor?.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'deleteContentBackward' }));
            editor?.dispatchEvent(new Event('change', { bubbles: true }));
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.clearEditor', error);
            throw error;
        }
    }

    function renderLiveUserMessages(host, region) {
        try {
            if (!(host instanceof HTMLElement) || !(region instanceof HTMLElement)) return;
            const messages = liveUserMessages.get(host) || [];
            const composer = host.querySelector('.localgpt-chat-composer');
            const messageList = findMessageList(region, composer);
            if (!(messageList instanceof HTMLElement)) return;
            const existing = new Set(
                [...messageList.querySelectorAll('[data-localgpt-live-user-message-id]')]
                    .map(element => element.getAttribute('data-localgpt-live-user-message-id'))
                    .filter(Boolean));
            for (const message of messages) {
                const actualMessageExists = [...messageList.querySelectorAll('[role="listitem"],[class*="message" i],[data-message-role],[data-role]')]
                    .some(element => !element.hasAttribute('data-localgpt-live-user-message-id')
                        && (element.textContent || '').trim() === message.content.trim());
                if (actualMessageExists) {
                    messageList.querySelector(`[data-localgpt-live-user-message-id="${CSS.escape(message.id)}"]`)?.remove();
                    continue;
                }
                if (existing.has(message.id)) continue;
                const row = document.createElement('div');
                row.className = 'localgpt-live-user-message-row';
                row.dataset.localgptLiveUserMessage = 'true';
                row.dataset.localgptLiveUserMessageId = message.id;
                row.dataset.messageRole = 'user';
                row.setAttribute('role', 'listitem');
                row.setAttribute('aria-label', 'User message');
                const bubble = document.createElement('div');
                bubble.className = 'localgpt-live-user-message';
                bubble.textContent = message.content;
                row.appendChild(bubble);
                messageList.appendChild(row);
            }
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.renderLiveUserMessages', error);
            throw error;
        }
    }

    function appendLiveUserMessage(host, content) {
        try {
            const messages = liveUserMessages.get(host) || [];
            messages.push({ id: `live-user-${++liveUserMessageSequence}`, content });
            liveUserMessages.set(host, messages);
            const composer = host.querySelector('.localgpt-chat-composer');
            const region = findScrollRegion(host, composer);
            if (!(region instanceof HTMLElement)) return;
            renderLiveUserMessages(host, region);
            const state = bindSlowScroll(host, region);
            if (state.follow && !state.userInteracting) scheduleSlowFollow(host, region);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.appendLiveUserMessage', error);
            throw error;
        }
    }

    function ensureLiveSendButton(host, composer, editor) {
        try {
            if (!(composer instanceof HTMLElement)) return null;
            let button = composer.querySelector('.localgpt-live-send-button');
            if (!(button instanceof HTMLButtonElement)) {
                button = document.createElement('button');
                button.type = 'button';
                button.className = 'localgpt-live-send-button localgpt-send-button';
                button.setAttribute('aria-label', 'Send message to running AI Council');
                button.setAttribute('title', 'Add this user message to the running Council without stopping generation');
                button.textContent = '➤';
                button.addEventListener('click', diagnostics.guard('localgpt-chat-ui.liveCouncilSend.click', async event => {
                    event.preventDefault();
                    event.stopPropagation();
                    const content = editorText(editor).trim();
                    if (!content || !councilComposerActive || !councilComposerDotNet || councilComposerSubmitting) return;
                    councilComposerSubmitting = true;
                    button.disabled = true;
                    try {
                        const accepted = await councilComposerDotNet.invokeMethodAsync('QueueLiveCouncilUserMessageAsync', content);
                        if (accepted) {
                            appendLiveUserMessage(host, content);
                            clearEditor(editor);
                        }
                    } finally {
                        councilComposerSubmitting = false;
                        button.disabled = false;
                        updateCouncilComposer(host, editor, composer);
                    }
                }));
                composer.appendChild(button);
            }
            return button;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.ensureLiveSendButton', error);
            throw error;
        }
    }

    function updateCouncilComposer(host, editor, composer) {
        try {
            if (!(host instanceof HTMLElement) || !(editor instanceof HTMLElement) || !(composer instanceof HTMLElement)) return;
            const hasText = editorText(editor).trim().length > 0;
            const showLiveSend = councilComposerActive && hasText;
            const liveSend = ensureLiveSendButton(host, composer, editor);
            liveSend?.classList.toggle('localgpt-live-send-visible', showLiveSend);
            if (liveSend instanceof HTMLButtonElement) liveSend.disabled = councilComposerSubmitting;

            const actionButtons = [...composer.querySelectorAll('button,[role="button"]')];
            const isUploadAction = button => /attach|upload|file|paperclip|clip/i.test(marker(button));
            const explicitStop = actionButtons.find(button => {
                if (button === liveSend) return false;
                return /stop|cancel generation|abort|square/i.test(marker(button));
            }) || null;
            const composerRect = composer.getBoundingClientRect();
            const fallbackStop = actionButtons.find(button => {
                if (button === liveSend || isUploadAction(button)) return false;
                const rect = button.getBoundingClientRect();
                return rect.width > 0
                    && rect.width <= 96
                    && rect.height > 0
                    && rect.height <= 96
                    && rect.right >= composerRect.right - 120;
            }) || null;
            (explicitStop || fallbackStop)?.classList.toggle('localgpt-council-stop-hidden', showLiveSend);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.updateCouncilComposer', error);
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
            const send = buttons.find(button => { try { return (!button.classList.contains('localgpt-live-send-button')
                && /send|submit|paper-plane|arrow-right/i.test(marker(button))
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
            if (editor instanceof HTMLElement && composer instanceof HTMLElement) {
                if (editor.dataset.localgptCouncilInputBound !== 'true') {
                    editor.dataset.localgptCouncilInputBound = 'true';
                    editor.addEventListener('input', diagnostics.guard('localgpt-chat-ui.liveCouncilInput', () =>
                        updateCouncilComposer(host, editor, composer)));
                }
                updateCouncilComposer(host, editor, composer);
            }

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
                renderLiveUserMessages(host, scrollRegion);
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
            window.localGptChatUi = {
                registerCouncilComposer(dotNetReference, isActive) {
                    try {
                        councilComposerDotNet = dotNetReference || councilComposerDotNet;
                        councilComposerActive = Boolean(isActive);
                        scheduleApply();
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.registerCouncilComposer', error);
                        throw error;
                    }
                },
                refreshCouncilComposer(isActive) {
                    try {
                        councilComposerActive = Boolean(isActive);
                        scheduleApply();
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.refreshCouncilComposer', error);
                        throw error;
                    }
                },
                clearLiveUserMessages() {
                    try {
                        document.querySelectorAll(hostSelector).forEach(host => {
                            liveUserMessages.delete(host);
                            host.querySelectorAll('[data-localgpt-live-user-message-id]').forEach(element => element.remove());
                        });
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.clearLiveUserMessages', error);
                        throw error;
                    }
                }
            };
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
