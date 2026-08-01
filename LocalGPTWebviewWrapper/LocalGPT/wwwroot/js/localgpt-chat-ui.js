(() => { try {
    'use strict';

    // javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
    const diagnostics = window.localGptJavaScriptDiagnostics || localGptDiagnostics;
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


    function findScrollRegion(host, composer) {
        try {
            if (!(host instanceof HTMLElement)) return null;

            const preferred = [
                host.querySelector('.dxbl-chatui-scrollviewer .dxbl-scroll-viewer-content'),
                host.querySelector('.dxbl-scroll-viewer-content.localgpt-chat-scroll-region'),
                host.querySelector('.dxbl-scroll-viewer-content'),
                host.querySelector('[role="log"]')?.parentElement,
                host.querySelector('.localgpt-chat-scroll-region')
            ].find(element => element instanceof HTMLElement
                && !composer?.contains(element)
                && !(composer instanceof HTMLElement && element.contains(composer)));
            if (preferred instanceof HTMLElement) return preferred;

            const candidates = [...host.querySelectorAll('*')]
                .filter(element => element instanceof HTMLElement
                    && !composer?.contains(element)
                    && !(composer instanceof HTMLElement && element.contains(composer)))
                .map(element => {
                    const style = getComputedStyle(element);
                    const scrollable = /(auto|scroll)/.test(style.overflowY)
                        || element.scrollHeight > element.clientHeight + 2;
                    const score = (element.matches('[class*="scroll-viewer-content" i]') ? 10000 : 0)
                        + (element.querySelector('[role="log"]') ? 5000 : 0)
                        + (element.matches('[class*="scroll" i]') ? 1000 : 0)
                        + Math.max(0, element.scrollHeight - element.clientHeight);
                    return { element, scrollable, score };
                })
                .filter(candidate => candidate.scrollable && candidate.element.clientHeight > 0)
                .sort((left, right) => right.score - left.score);
            return candidates[0]?.element || null;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.findScrollRegion', error);
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
    const transcriptQuietDelayMilliseconds = 1000;

    function distanceToBottom(region) {
        try {
            return Math.max(0, region.scrollHeight - region.clientHeight - region.scrollTop);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.distanceToBottom', error);
            throw error;
        }
    }

    function stopSmoothFollow(state) {
        try {
            if (!state) return;
            if (state.frame) cancelAnimationFrame(state.frame);
            if (state.debounceTimer) clearTimeout(state.debounceTimer);
            state.frame = 0;
            state.debounceTimer = 0;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.stopSmoothFollow', error);
            throw error;
        }
    }

    function bindSlowScroll(host, region) {
        try {
            let state = slowScrollStates.get(host);
            if (state?.region === region) return state;
            if (state) {
                stopSmoothFollow(state);
                state.abortController?.abort();
                state.contentObserver?.disconnect();
            }

            const abortController = new AbortController();
            state = {
                region,
                frame: 0,
                debounceTimer: 0,
                follow: distanceToBottom(region) < 40,
                userInteracting: false,
                interactionTimer: 0,
                abortController,
                contentObserver: null
            };
            slowScrollStates.set(host, state);

            const releaseUserInteraction = diagnostics.guard('localgpt-chat-ui.scroll.releaseUserInput', () => {
                state.userInteracting = false;
                state.interactionTimer = 0;
                state.follow = distanceToBottom(region) < 32;
                if (state.follow) scheduleSlowFollow(host, region);
            });
            const scheduleRelease = diagnostics.guard('localgpt-chat-ui.scroll.scheduleUserInputEnd', delay => {
                if (state.interactionTimer) clearTimeout(state.interactionTimer);
                state.interactionTimer = window.setTimeout(releaseUserInteraction, delay);
            });
            const beginUserScroll = diagnostics.guard('localgpt-chat-ui.scroll.userInput', () => {
                state.userInteracting = true;
                state.follow = false;
                stopSmoothFollow(state);
            });
            const finishUserScroll = diagnostics.guard('localgpt-chat-ui.scroll.userInputEnd', () => {
                scheduleRelease(180);
            });

            const listenerOptions = { passive: true, signal: abortController.signal };
            region.addEventListener('wheel', diagnostics.guard('localgpt-chat-ui.scroll.wheel', () => {
                beginUserScroll();
                scheduleRelease(260);
            }), listenerOptions);
            region.addEventListener('touchstart', beginUserScroll, listenerOptions);
            region.addEventListener('pointerdown', beginUserScroll, listenerOptions);
            region.addEventListener('pointerup', finishUserScroll, listenerOptions);
            region.addEventListener('pointercancel', finishUserScroll, listenerOptions);
            region.addEventListener('touchend', finishUserScroll, listenerOptions);
            region.addEventListener('touchcancel', finishUserScroll, listenerOptions);
            region.addEventListener('keydown', diagnostics.guard('localgpt-chat-ui.scroll.keydown', event => {
                if (/ArrowUp|ArrowDown|PageUp|PageDown|Home|End|Space/.test(event.code)) {
                    beginUserScroll();
                    scheduleRelease(260);
                }
            }), { signal: abortController.signal });
            region.addEventListener('scroll', diagnostics.guard('localgpt-chat-ui.scroll.scroll', () => {
                if (state.userInteracting) state.follow = distanceToBottom(region) < 32;
            }), listenerOptions);

            state.contentObserver = new MutationObserver(diagnostics.guard('localgpt-chat-ui.scroll.contentChanged', () => {
                if (state.follow && !state.userInteracting) scheduleSlowFollow(host, region);
            }));
            state.contentObserver.observe(region, { childList: true, subtree: true, characterData: true });
            return state;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.bindSlowScroll', error);
            throw error;
        }
    }

    function beginSmoothFollow(host, region, state) {
        try {
            state.debounceTimer = 0;
            if (state.userInteracting || !state.follow || !state.region.isConnected) return;

            const startTop = state.region.scrollTop;
            const initialDistance = distanceToBottom(state.region);
            if (initialDistance <= 1.5) {
                state.region.scrollTop = Math.max(0, state.region.scrollHeight - state.region.clientHeight);
                return;
            }

            const startedAt = performance.now();
            const duration = Math.min(5000, Math.max(900, initialDistance * 1.8));
            const animate = diagnostics.guard('localgpt-chat-ui.scroll.followFrame', timestamp => {
                state.frame = 0;
                if (state.userInteracting || !state.follow || !state.region.isConnected) return;

                const target = Math.max(0, state.region.scrollHeight - state.region.clientHeight);
                const progress = Math.min(1, (timestamp - startedAt) / duration);
                const eased = 1 - Math.pow(1 - progress, 3);
                state.region.scrollTop = startTop + ((target - startTop) * eased);

                if (progress < 1 && distanceToBottom(state.region) > 1.5) {
                    state.frame = requestAnimationFrame(animate);
                    return;
                }

                // Snap only the final sub-pixel remainder after the quiet interval. This keeps
                // the working indicator and the last rendered line visible without chasing each token.
                state.region.scrollTop = Math.max(0, state.region.scrollHeight - state.region.clientHeight);
            });
            state.frame = requestAnimationFrame(animate);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.beginSmoothFollow', error);
            throw error;
        }
    }

    function scheduleSlowFollow(host, region) {
        try {
            const state = bindSlowScroll(host, region);
            if (!state || state.userInteracting || !state.follow || !state.region.isConnected) return;

            // Text streaming can make DevExpress scroll immediately. LocalGPT waits until no
            // transcript update has arrived for one second, then performs one smooth bottom move.
            // A new token cancels an in-flight move and restarts this trailing debounce.
            if (state.frame) cancelAnimationFrame(state.frame);
            if (state.debounceTimer) clearTimeout(state.debounceTimer);
            state.frame = 0;
            state.debounceTimer = window.setTimeout(
                diagnostics.guard('localgpt-chat-ui.scroll.quietFollow', () => beginSmoothFollow(host, region, state)),
                transcriptQuietDelayMilliseconds);
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
                    const currentEditor = host.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
                    const currentComposer = button.closest('.localgpt-chat-composer') || composer;
                    const content = editorText(currentEditor).trim();
                    if (!councilComposerActive || !councilComposerDotNet || councilComposerSubmitting) return;
                    councilComposerSubmitting = true;
                    button.disabled = true;
                    try {
                        if (content) {
                            const accepted = await councilComposerDotNet.invokeMethodAsync('QueueLiveCouncilUserMessageAsync', content);
                            if (accepted) {
                                appendLiveUserMessage(host, content);
                                clearEditor(currentEditor);
                            }
                        } else {
                            await councilComposerDotNet.invokeMethodAsync('StopActiveCouncilRunAsync');
                        }
                    } finally {
                        councilComposerSubmitting = false;
                        button.disabled = false;
                        updateCouncilComposer(host, currentEditor, currentComposer);
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
            const showLiveAction = councilComposerActive;
            const liveSend = ensureLiveSendButton(host, composer, editor);
            liveSend?.classList.toggle('localgpt-live-send-visible', showLiveAction);
            liveSend?.classList.toggle('localgpt-live-stop-mode', showLiveAction && !hasText);
            if (liveSend instanceof HTMLButtonElement) {
                liveSend.disabled = councilComposerSubmitting;
                liveSend.textContent = hasText ? '➤' : '■';
                liveSend.setAttribute('aria-label', hasText
                    ? 'Send message to running AI Council'
                    : 'Stop running AI Council');
                liveSend.setAttribute('title', hasText
                    ? 'Add this user message to the running Council without stopping generation'
                    : 'Stop the running Council');
            }

            const actionButtons = [...composer.querySelectorAll('button,[role="button"]')];
            const isUploadAction = button => /attach|upload|file|paperclip|clip/i.test(marker(button));
            const explicitStop = actionButtons.find(button => {
                if (button === liveSend) return false;
                return /stop|cancel generation|abort|square/i.test(marker(button));
            }) || null;
            const nativeSubmit = actionButtons.find(button => {
                if (button === liveSend || isUploadAction(button)) return false;
                return /send|submit|paper-plane|arrow-right/i.test(marker(button));
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
            const stopButton = explicitStop || nativeSubmit || fallbackStop;
            stopButton?.classList.toggle('localgpt-council-stop-hidden', showLiveAction);
            if (stopButton instanceof HTMLElement && stopButton.dataset.localgptCouncilStopBound !== 'true') {
                stopButton.dataset.localgptCouncilStopBound = 'true';
                stopButton.addEventListener('click', diagnostics.guard('localgpt-chat-ui.councilStop.click', () => {
                    if (councilComposerActive && councilComposerDotNet)
                        void councilComposerDotNet.invokeMethodAsync('StopActiveCouncilRunAsync');
                }), { capture: true });
            }
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
                if (editor.dataset.localgptCouncilKeyBound !== 'true') {
                    editor.dataset.localgptCouncilKeyBound = 'true';
                    editor.addEventListener('keydown', diagnostics.guard('localgpt-chat-ui.liveCouncilKeyDown', event => {
                        if (!councilComposerActive
                            || event.isComposing
                            || event.key !== 'Enter'
                            || event.shiftKey
                            || event.ctrlKey
                            || event.altKey
                            || event.metaKey) return;
                        event.preventDefault();
                        event.stopImmediatePropagation();
                        if (!editorText(editor).trim()) return;
                        ensureLiveSendButton(host, composer, editor)?.click();
                    }), { capture: true });
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
            window.localGptHumanDrafts = diagnostics.guardObject('localGptHumanDrafts', {
                read(elementId) {
                    const editor = document.getElementById(String(elementId || ''));
                    return editor instanceof HTMLTextAreaElement ? editor.value : '';
                },
                clear(elementId) {
                    const editor = document.getElementById(String(elementId || ''));
                    if (editor instanceof HTMLTextAreaElement) editor.value = '';
                }
            });
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
                readComposerDraft() {
                    try {
                        const host = document.querySelector(hostSelector);
                        const editor = host?.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
                        return editorText(editor);
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.readComposerDraft', error);
                        throw error;
                    }
                },
                restoreComposerDraft(value) {
                    try {
                        const text = String(value ?? '');
                        if (!text) return;
                        const host = document.querySelector(hostSelector);
                        const editor = host?.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
                        if (editor instanceof HTMLTextAreaElement || editor instanceof HTMLInputElement) editor.value = text;
                        else if (editor instanceof HTMLElement) editor.textContent = text;
                        editor?.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: text }));
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.restoreComposerDraft', error);
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
                },
                async copyText(value) {
                    try {
                        const text = String(value ?? '');
                        if (navigator.clipboard?.writeText) {
                            try {
                                await navigator.clipboard.writeText(text);
                                return;
                            } catch {
                                // Local browser policy can reject Clipboard API access. Use the legacy fallback below.
                            }
                        }

                        const fallback = document.createElement('textarea');
                        try {
                            fallback.value = text;
                            fallback.setAttribute('readonly', '');
                            fallback.style.position = 'fixed';
                            fallback.style.opacity = '0';
                            document.body.appendChild(fallback);
                            fallback.select();
                            document.execCommand('copy');
                        } finally {
                            fallback.remove();
                        }
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.copyText', error);
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
