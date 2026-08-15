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
    let layoutPulseTimer = 0;
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

    function clickPromptSuggestion(title) {
        try {
            const normalizedTitle = String(title ?? '').replace(/\s+/g, ' ').trim().toLowerCase();
            if (!normalizedTitle) return false;
            const host = document.querySelector(hostSelector);
            if (!(host instanceof HTMLElement)) return false;
            const editor = host.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
            const composer = findComposer(host, editor, null);
            const candidates = [...host.querySelectorAll('button,[role="button"],a,[class*="suggest" i]')]
                .filter(element => element instanceof HTMLElement && visible(element))
                .filter(element => !composer?.contains(element))
                .filter(element => (element.textContent || '').replace(/\s+/g, ' ').trim().toLowerCase().includes(normalizedTitle));
            const suggestion = candidates.find(element => suggestionRegion(element, host, composer)) || candidates[0];
            if (!(suggestion instanceof HTMLElement)) return false;
            suggestion.focus({ preventScroll: true });
            suggestion.click();
            return true;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.clickPromptSuggestion', error);
            return false;
        }
    }


    async function waitForComposerSubmission(editor, expectedText) {
        try {
            const expected = String(expectedText ?? '').trim();
            for (let attempt = 0; attempt < 16; attempt++) {
                await new Promise(resolve => setTimeout(resolve, 75));
                if (!(editor instanceof HTMLElement) || !editor.isConnected) return true;
                const current = editorText(editor).trim();
                if (!current || (expected && current !== expected)) return true;
            }
            return false;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.waitForComposerSubmission', error);
            return false;
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
    const interactionQuietDelayMilliseconds = 3000;
    const smoothFollowDurationMilliseconds = 950;
    const followResumeDistancePixels = 120;

    function takeScrollOwnership(region) {
        try {
            if (!(region instanceof HTMLElement)) return null;
            region.style.scrollBehavior = 'auto';
            const viewer = region.closest('dxbl-scroll-viewer,[class*="scroll-viewer" i]');
            const roots = [region, viewer].filter(element => element instanceof HTMLElement);
            for (const root of roots) {
                root.removeAttribute('request-make-element-visible');
                root.querySelectorAll?.('[request-make-element-visible]').forEach(element =>
                    element.removeAttribute('request-make-element-visible'));
            }
            if (viewer instanceof HTMLElement) viewer.dataset.localgptScrollOwner = 'true';
            return viewer instanceof HTMLElement ? viewer : region;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.takeScrollOwnership', error);
            throw error;
        }
    }

    function distanceToBottom(region) {
        try {
            return Math.max(0, region.scrollHeight - region.clientHeight - region.scrollTop);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.distanceToBottom', error);
            throw error;
        }
    }

    function setRegionScrollTop(state, value) {
        try {
            if (!state?.region?.isConnected) return;
            const pageX = window.scrollX;
            const pageY = window.scrollY;
            const targetTop = Math.max(0, value);
            state.lastProgrammaticAt = performance.now();
            state.lastProgrammaticScrollTop = targetTop;
            state.programmaticUntil = state.lastProgrammaticAt + 260;
            state.region.scrollTop = targetTop;
            if (window.scrollX !== pageX || window.scrollY !== pageY)
                window.scrollTo({ left: pageX, top: pageY, behavior: 'auto' });
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.setRegionScrollTop', error);
            throw error;
        }
    }

    function stopSmoothFollow(state) {
        try {
            if (!state) return;
            if (state.frame) cancelAnimationFrame(state.frame);
            if (state.debounceTimer) clearTimeout(state.debounceTimer);
            if (state.measureFrame) cancelAnimationFrame(state.measureFrame);
            state.frame = 0;
            state.debounceTimer = 0;
            state.measureFrame = 0;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.stopSmoothFollow', error);
            throw error;
        }
    }

    function queueTranscriptHeightCheck(host, state) {
        try {
            if (!state || state.measureFrame || !state.region.isConnected) return;
            state.measureFrame = requestAnimationFrame(diagnostics.guard('localgpt-chat-ui.scroll.measureTranscript', () => {
                state.measureFrame = 0;
                if (!state.region.isConnected) return;
                takeScrollOwnership(state.region);
                const nextScrollHeight = state.region.scrollHeight;
                const nextClientHeight = state.region.clientHeight;
                const changed = nextScrollHeight !== state.lastScrollHeight
                    || nextClientHeight !== state.lastClientHeight;
                state.lastScrollHeight = nextScrollHeight;
                state.lastClientHeight = nextClientHeight;
                if (changed && state.follow && !state.userInteracting)
                    scheduleSlowFollow(host, state.region);
            }));
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.queueTranscriptHeightCheck', error);
            throw error;
        }
    }

    function bindSlowScroll(host, region) {
        try {
            let state = slowScrollStates.get(host);
            if (state?.region === region) {
                takeScrollOwnership(region);
                return state;
            }
            if (state) {
                stopSmoothFollow(state);
                if (state.interactionTimer) clearTimeout(state.interactionTimer);
                state.abortController?.abort();
                state.contentObserver?.disconnect();
                state.ownerObserver?.disconnect();
                state.resizeObserver?.disconnect();
            }

            const owner = takeScrollOwnership(region);
            const abortController = new AbortController();
            state = {
                region,
                frame: 0,
                debounceTimer: 0,
                measureFrame: 0,
                follow: distanceToBottom(region) <= followResumeDistancePixels,
                userInteracting: false,
                interactionTimer: 0,
                userPauseUntil: 0,
                pendingFollowRequest: false,
                programmaticUntil: 0,
                lastProgrammaticAt: 0,
                lastProgrammaticScrollTop: region.scrollTop,
                lastScrollHeight: region.scrollHeight,
                lastClientHeight: region.clientHeight,
                initialized: false,
                abortController,
                contentObserver: null,
                ownerObserver: null,
                resizeObserver: null
            };
            slowScrollStates.set(host, state);

            const releaseUserInteraction = diagnostics.guard('localgpt-chat-ui.scroll.releaseUserInput', () => {
                const remaining = state.userPauseUntil - performance.now();
                if (remaining > 1) {
                    if (state.interactionTimer) clearTimeout(state.interactionTimer);
                    state.interactionTimer = window.setTimeout(releaseUserInteraction, remaining);
                    return;
                }

                state.userInteracting = false;
                state.interactionTimer = 0;
                const closeEnoughToFollow = distanceToBottom(region) <= followResumeDistancePixels;
                state.follow = closeEnoughToFollow;
                host.dataset.localgptAutoFollow = closeEnoughToFollow ? 'following' : 'manual';
                if (closeEnoughToFollow && state.pendingFollowRequest) {
                    state.pendingFollowRequest = false;
                    scheduleSlowFollow(host, region);
                }
            });
            const scheduleRelease = diagnostics.guard('localgpt-chat-ui.scroll.scheduleUserInputEnd', () => {
                if (state.interactionTimer) clearTimeout(state.interactionTimer);
                const delay = Math.max(1, state.userPauseUntil - performance.now());
                state.interactionTimer = window.setTimeout(releaseUserInteraction, delay);
            });
            const beginUserScroll = diagnostics.guard('localgpt-chat-ui.scroll.userInput', () => {
                state.userInteracting = true;
                state.userPauseUntil = performance.now() + interactionQuietDelayMilliseconds;
                state.follow = false;
                host.dataset.localgptAutoFollow = 'paused';
                state.pendingFollowRequest = true;
                if (state.interactionTimer) clearTimeout(state.interactionTimer);
                state.interactionTimer = 0;
                stopSmoothFollow(state);
            });
            const finishUserScroll = diagnostics.guard('localgpt-chat-ui.scroll.userInputEnd', () => {
                scheduleRelease();
            });

            const listenerOptions = { passive: true, signal: abortController.signal };
            region.addEventListener('wheel', diagnostics.guard('localgpt-chat-ui.scroll.wheel', () => {
                beginUserScroll();
                scheduleRelease();
            }), listenerOptions);
            region.addEventListener('touchstart', beginUserScroll, listenerOptions);
            region.addEventListener('touchend', finishUserScroll, listenerOptions);
            region.addEventListener('touchcancel', finishUserScroll, listenerOptions);
            region.addEventListener('keydown', diagnostics.guard('localgpt-chat-ui.scroll.keydown', event => {
                if (/ArrowUp|ArrowDown|PageUp|PageDown|Home|End|Space/.test(event.code)) {
                    beginUserScroll();
                    scheduleRelease();
                }
            }), { signal: abortController.signal });
            region.addEventListener('scroll', diagnostics.guard('localgpt-chat-ui.scroll.scroll', () => {
                const now = performance.now();
                const delayedProgrammaticEvent = now - state.lastProgrammaticAt < 400
                    && Math.abs(region.scrollTop - state.lastProgrammaticScrollTop) <= 2;
                if (now < state.programmaticUntil || delayedProgrammaticEvent) return;
                beginUserScroll();
                scheduleRelease();
            }), listenerOptions);

            state.contentObserver = new MutationObserver(diagnostics.guard('localgpt-chat-ui.scroll.contentChanged', () =>
                queueTranscriptHeightCheck(host, state)));
            state.contentObserver.observe(region, { childList: true, subtree: true, characterData: true });

            if (owner instanceof HTMLElement) {
                state.ownerObserver = new MutationObserver(diagnostics.guard('localgpt-chat-ui.scroll.ownerChanged', () =>
                    takeScrollOwnership(region)));
                state.ownerObserver.observe(owner, {
                    attributes: true,
                    subtree: true,
                    attributeFilter: ['request-make-element-visible']
                });
            }

            if (typeof ResizeObserver === 'function') {
                state.resizeObserver = new ResizeObserver(diagnostics.guard('localgpt-chat-ui.scroll.resized', () =>
                    queueTranscriptHeightCheck(host, state)));
                state.resizeObserver.observe(region);
            }
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
            takeScrollOwnership(state.region);

            const startTop = state.region.scrollTop;
            const initialTargetTop = Math.max(0, state.region.scrollHeight - state.region.clientHeight);
            const initialDistance = Math.max(0, initialTargetTop - startTop);
            if (initialDistance <= 1.5) {
                setRegionScrollTop(state, Math.max(0, state.region.scrollHeight - state.region.clientHeight));
                return;
            }

            const startedAt = performance.now();
            const animate = diagnostics.guard('localgpt-chat-ui.scroll.followFrame', timestamp => {
                state.frame = 0;
                if (state.userInteracting || !state.follow || !state.region.isConnected) return;

                const progress = Math.min(1, (timestamp - startedAt) / smoothFollowDurationMilliseconds);
                const eased = progress < .5
                    ? 4 * progress * progress * progress
                    : 1 - Math.pow(-2 * progress + 2, 3) / 2;
                // Streaming Council content can grow while the follow animation is already running.
                // Recalculate the true inner-container bottom on every frame instead of chasing a
                // stale target captured before the latest model/tool/status content arrived.
                const liveTargetTop = Math.max(0, state.region.scrollHeight - state.region.clientHeight);
                setRegionScrollTop(state, startTop + ((liveTargetTop - startTop) * eased));

                if (progress < 1) {
                    state.frame = requestAnimationFrame(animate);
                    return;
                }
                setRegionScrollTop(state, Math.max(0, state.region.scrollHeight - state.region.clientHeight));
                state.measureFrame = requestAnimationFrame(diagnostics.guard('localgpt-chat-ui.scroll.settleAtBottom', () => {
                    state.measureFrame = 0;
                    if (!state.userInteracting && state.follow && state.region.isConnected)
                        setRegionScrollTop(state, Math.max(0, state.region.scrollHeight - state.region.clientHeight));
                }));
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

            // Wait for one quiet second after the latest transcript/layout update. The actual
            // movement then uses one fixed target and a roughly one-second animation, so LocalGPT
            // never chases streaming tokens or fights DevExpress' own make-visible behavior.
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

    const liveUploadFileCache = new WeakMap();

    function cachePendingUploadFiles(composer) {
        try {
            if (!(composer instanceof HTMLElement)) return [];
            const input = pendingUploadInput(composer);
            const current = input instanceof HTMLInputElement && input.files ? [...input.files] : [];
            if (current.length > 0) {
                // DevExpress may materialize its attachment chips and then clear the browser input. Keep the
                // real File objects for the custom live-Council send path until LocalGPT accepts or cancels them.
                liveUploadFileCache.set(composer, current);
                return current;
            }
            return liveUploadFileCache.get(composer) || [];
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.cachePendingUploadFiles', error);
            throw error;
        }
    }

    function pendingUploadInput(composer) {
        try {
            return composer instanceof HTMLElement
                ? composer.querySelector('input[type="file"]')
                : null;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.pendingUploadInput', error);
            throw error;
        }
    }

    function pendingUploadFiles(composer) {
        try {
            return cachePendingUploadFiles(composer);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.pendingUploadFiles', error);
            throw error;
        }
    }

    async function readPendingUploadFiles(composer) {
        try {
            const files = pendingUploadFiles(composer);
            const payloads = [];
            for (const file of files) {
                payloads.push({
                    name: file.name || 'attachment',
                    contentType: file.type || 'application/octet-stream',
                    sizeBytes: file.size || 0,
                    data: new Uint8Array(await file.arrayBuffer())
                });
            }
            return payloads;
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.readPendingUploadFiles', error);
            throw error;
        }
    }

    function clearPendingUploadFiles(composer) {
        try {
            const input = pendingUploadInput(composer);
            if (!(input instanceof HTMLInputElement)) return;

            // The live-Council send path intentionally bypasses DxAIChat's normal send handler, so clear both
            // the native input and any DevExpress upload-list entries it already materialized. Clicking only
            // bounded remove/delete controls avoids touching unrelated composer buttons.
            const uploadList = composer.querySelector('.dxbl-upload-file-list-view');
            const removalButtons = uploadList
                ? [...uploadList.querySelectorAll('button,[role="button"]')].filter(button => {
                    const label = `${button.getAttribute('aria-label') || ''} ${button.getAttribute('title') || ''} ${button.textContent || ''}`.toLowerCase();
                    return label.includes('remove') || label.includes('delete') || label.includes('clear') || label.includes('cancel');
                })
                : [];
            for (const button of removalButtons) {
                if (button instanceof HTMLElement && !button.hasAttribute('disabled')) button.click();
            }

            liveUploadFileCache.delete(composer);
            input.value = '';
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
            scheduleApply();
            scheduleLayoutStabilization();
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.clearPendingUploadFiles', error);
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
                if (Array.isArray(message.fileNames) && message.fileNames.length > 0) {
                    const attachments = document.createElement('div');
                    attachments.className = 'localgpt-live-user-attachments';
                    for (const fileName of message.fileNames) {
                        const chip = document.createElement('span');
                        chip.className = 'localgpt-live-user-attachment';
                        chip.textContent = `📎 ${fileName}`;
                        attachments.appendChild(chip);
                    }
                    bubble.appendChild(attachments);
                }
                row.appendChild(bubble);
                messageList.appendChild(row);
            }
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.renderLiveUserMessages', error);
            throw error;
        }
    }

    function appendLiveUserMessage(host, content, fileNames = []) {
        try {
            const messages = liveUserMessages.get(host) || [];
            messages.push({ id: `live-user-${++liveUserMessageSequence}`, content, fileNames });
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
                    const pendingFiles = pendingUploadFiles(currentComposer);
                    if (!councilComposerActive || !councilComposerDotNet || councilComposerSubmitting) return;
                    councilComposerSubmitting = true;
                    button.disabled = true;
                    try {
                        if (content || pendingFiles.length > 0) {
                            const filePayloads = await readPendingUploadFiles(currentComposer);
                            const accepted = await councilComposerDotNet.invokeMethodAsync('QueueLiveCouncilUserMessageAsync', content, filePayloads);
                            if (accepted) {
                                appendLiveUserMessage(
                                    host,
                                    content || 'Attached files',
                                    filePayloads.map(file => file.name));
                                clearEditor(currentEditor);
                                clearPendingUploadFiles(currentComposer);
                            }
                        } else {
                            const stopped = await councilComposerDotNet.invokeMethodAsync('StopActiveCouncilRunAsync');
                            if (stopped) councilComposerActive = false;
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
            const hasPendingFiles = pendingUploadFiles(composer).length > 0;
            const hasMessage = hasText || hasPendingFiles;
            const liveAction = ensureLiveSendButton(host, composer, editor);
            const actionButtons = [...composer.querySelectorAll('button,[role="button"]')];
            const nativeStop = actionButtons.find(button => {
                if (button === liveAction || !visible(button)) return false;
                return /stop|cancel generation|abort|square/i.test(marker(button));
            }) || null;
            const nativeSend = actionButtons.find(button => {
                if (button === liveAction || button === nativeStop) return false;
                const buttonMarker = marker(button);
                return /send|submit|paper-plane|arrow-right/i.test(buttonMarker)
                    && !/attach|upload|file|paperclip|clip|stop|cancel generation|abort|square/i.test(buttonMarker);
            }) || null;
            nativeSend?.classList.toggle('localgpt-council-native-send-hidden', councilComposerActive && hasMessage);
            const showLiveAction = councilComposerActive && (hasMessage || nativeStop === null);
            liveAction?.classList.toggle('localgpt-live-send-visible', showLiveAction);
            if (liveAction instanceof HTMLButtonElement) {
                liveAction.disabled = councilComposerSubmitting;
                liveAction.textContent = hasMessage ? '➤' : '■';
                liveAction.setAttribute(
                    'aria-label',
                    hasMessage ? 'Send message and attachments to running AI Council' : 'Stop running AI Council');
                liveAction.setAttribute(
                    'title',
                    hasMessage
                        ? 'Add this user message and attached files to the running Council without stopping generation'
                        : 'Stop the running Council');
            }
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.updateCouncilComposer', error);
            throw error;
        }
    }

    function markConfigurationSurfaces(host) {
        try {
            if (!(host instanceof HTMLElement)) return;

            document.querySelectorAll('.localgpt-chat-dialog-surface').forEach(element => {
                if (!visible(element)) element.classList.remove('localgpt-chat-dialog-surface');
            });
            document.querySelectorAll('[role="dialog"]').forEach(element => {
                if (visible(element) && element.querySelectorAll('input,textarea,select,button,[role="button"]').length > 0) {
                    addClass(element, 'localgpt-chat-dialog-surface');
                }
            });

            host.querySelectorAll('.localgpt-chat-settings-surface').forEach(element => {
                if (!visible(element)) element.classList.remove('localgpt-chat-settings-surface');
            });

            const titlePattern = /^(chat\s+settings|chat\s+configuration|settings|configuration|chat-einstellungen|chatkonfiguration|einstellungen|konfiguration)$/i;
            const hostRect = host.getBoundingClientRect();
            const titleCandidates = [...host.querySelectorAll('h1,h2,h3,h4,h5,[role="heading"],legend,summary')];
            for (const title of titleCandidates) {
                if (!visible(title)) continue;
                const text = String(title.textContent || '').replace(/\s+/g, ' ').trim();
                if (!titlePattern.test(text)) continue;

                let candidate = null;
                let ancestor = title.parentElement;
                for (let depth = 0; ancestor && ancestor !== host && depth < 8; depth++, ancestor = ancestor.parentElement) {
                    if (!visible(ancestor)) continue;
                    const controls = ancestor.querySelectorAll('input,textarea,select,button,[role="button"]').length;
                    const rect = ancestor.getBoundingClientRect();
                    if (controls < 2 || rect.width < 220 || rect.height < 120) continue;
                    if (hostRect.width > 0 && rect.width >= hostRect.width * .9) break;
                    candidate = ancestor;
                }
                if (candidate) addClass(candidate, 'localgpt-chat-settings-surface');
            }
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.markConfigurationSurfaces', error);
            throw error;
        }
    }

    function enhance(host) {
        try {
            if (!(host instanceof HTMLElement)) return;
            markChatRoots(host);
            markConfigurationSurfaces(host);

            const editor = host.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
            addClass(editor, 'localgpt-chat-textarea');
            setAttributeIfMissing(editor, 'aria-label', 'Message to AI assistant');
            if (editor instanceof HTMLElement) editor.dataset.localgptChatInput = 'true';

            const allButtons = [...host.querySelectorAll('button,[role="button"]')];
            const buttons = allButtons.filter(visible);
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

            for (const button of allButtons) {
                const copyMarker = [marker(button), ...[...button.querySelectorAll('*')].map(marker)].join(' ');
                if (/copy|clipboard/i.test(copyMarker)) {
                    addClass(button, 'localgpt-native-copy-always-visible');
                    setAttributeIfMissing(button, 'title', 'Copy message');
                }
            }

            const composer = findComposer(host, editor, send);
            addClass(composer, 'localgpt-chat-composer');
            if (composer instanceof HTMLElement) composer.dataset.localgptComposer = 'true';
            if (composer instanceof HTMLElement) {
                const uploadInput = pendingUploadInput(composer);
                if (uploadInput instanceof HTMLInputElement && uploadInput.dataset.localgptCouncilUploadBound !== 'true') {
                    uploadInput.dataset.localgptCouncilUploadBound = 'true';
                    uploadInput.addEventListener('change', diagnostics.guard('localgpt-chat-ui.liveCouncilUpload.change', () => {
                        cachePendingUploadFiles(composer);
                        updateCouncilComposer(host, editor, composer);
                    }));
                }
            }
            if (editor instanceof HTMLElement && composer instanceof HTMLElement) {
                if (editor.dataset.localgptCouncilInputBound !== 'true') {
                    editor.dataset.localgptCouncilInputBound = 'true';
                    editor.addEventListener('input', diagnostics.guard('localgpt-chat-ui.liveCouncilInput', () =>
                        updateCouncilComposer(host, editor, composer)));
                    editor.addEventListener('keydown', diagnostics.guard('localgpt-chat-ui.liveCouncilInput.keydown', event => {
                        if (!councilComposerActive
                            || event.key !== 'Enter'
                            || event.shiftKey
                            || event.isComposing
                            || (editorText(editor).trim().length === 0 && pendingUploadFiles(composer).length === 0)) return;
                        event.preventDefault();
                        event.stopImmediatePropagation();
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
                const scrollState = bindSlowScroll(host, scrollRegion);
                if (!scrollState.initialized) {
                    scrollState.initialized = true;
                    if (scrollState.follow) scheduleSlowFollow(host, scrollRegion);
                }
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

    function scheduleLayoutStabilization() {
        try {
            if (layoutPulseTimer) clearTimeout(layoutPulseTimer);
            layoutPulseTimer = window.setTimeout(diagnostics.guard('localgpt-chat-ui.layoutStabilization', () => {
                layoutPulseTimer = 0;
                const host = document.querySelector(hostSelector);
                if (!(host instanceof HTMLElement)) return;
                // Expanding a DevExpress details block naturally causes the control to remeasure itself.
                // Reproduce that benign layout notification after a loaded/rejoined transcript settles so
                // the initial chat viewport is correct without requiring the user to toggle a details block.
                requestAnimationFrame(diagnostics.guard('localgpt-chat-ui.layoutStabilization.firstFrame', () =>
                    requestAnimationFrame(diagnostics.guard('localgpt-chat-ui.layoutStabilization.secondFrame', () => {
                        window.dispatchEvent(new Event('resize'));
                        const composer = host.querySelector('.localgpt-chat-composer');
                        const region = findScrollRegion(host, composer);
                        if (region instanceof HTMLElement) {
                            const state = bindSlowScroll(host, region);
                            queueTranscriptHeightCheck(host, state);
                        }
                    }))));
            }), 240);
        } catch (error) {
            diagnostics.report('localgpt-chat-ui.scheduleLayoutStabilization', error);
            throw error;
        }
    }

    const observer = new MutationObserver(diagnostics.guard('localgpt-chat-ui.mutationObserver', records => { try {
        const changed = records.some(record => { try { return (record.type === 'attributes' || record.addedNodes.length > 0 || record.removedNodes.length > 0); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:records.some@159', __javascriptError); throw __javascriptError; } });
        if (changed) {
            scheduleApply();
            if (records.some(record => record.target instanceof Element && (record.target.matches?.(hostSelector) || record.target.closest?.(hostSelector))))
                scheduleLayoutStabilization();
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-chat-ui.js:callback:diagnostics.guard@158', __javascriptError); throw __javascriptError; }}));

    function start() {
        try {
            scheduleApply();
            scheduleLayoutStabilization();
            observer.observe(document.body, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['class', 'style', 'aria-hidden', 'open']
            });
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
                stabilizeLayout() {
                    try {
                        scheduleLayoutStabilization();
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.stabilizeLayout', error);
                    }
                },
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
                prepareDirectCouncilStarter() {
                    try {
                        document.querySelectorAll('.chat-configuration-ribbon[open], .chat-session-tools-ribbon[open]')
                            .forEach(element => element.removeAttribute('open'));
                        document.documentElement.style.removeProperty('overflow');
                        document.body?.style.removeProperty('overflow');
                        return true;
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.prepareDirectCouncilStarter', error);
                        return false;
                    }
                },
                async submitSuggestionOrPrompt(title, value) {
                    try {
                        // Prompt-suggestion clicks are intentionally not used for direct Council starts.
                        // DevExpress highlights a suggestion before it submits and can therefore report a
                        // successful click while no message was sent. Direct starters must populate the
                        // composer and activate its native send action so the Council pipeline really runs.
                        void title;
                        return await this.submitPrompt(value);
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.submitSuggestionOrPrompt', error);
                        return false;
                    }
                },
                async submitPrompt(value) {
                    try {
                        const text = String(value ?? '').trim();
                        if (!text) return false;
                        for (let attempt = 0; attempt < 24; attempt++) {
                            const host = document.querySelector(hostSelector);
                            const editor = host?.querySelector('textarea,[contenteditable="true"],[role="textbox"]');
                            if (host instanceof HTMLElement && editor instanceof HTMLElement) {
                                if (editor instanceof HTMLTextAreaElement || editor instanceof HTMLInputElement) {
                                    const prototype = editor instanceof HTMLTextAreaElement
                                        ? HTMLTextAreaElement.prototype
                                        : HTMLInputElement.prototype;
                                    const setter = Object.getOwnPropertyDescriptor(prototype, 'value')?.set;
                                    if (setter) setter.call(editor, text); else editor.value = text;
                                } else {
                                    editor.textContent = text;
                                }
                                editor.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: text }));
                                editor.dispatchEvent(new Event('change', { bubbles: true }));
                                await new Promise(resolve => setTimeout(resolve, 60));
                                const composer = findComposer(host, editor, null);
                                const editorRect = editor.getBoundingClientRect();
                                const localCandidates = composer instanceof HTMLElement
                                    ? [...composer.querySelectorAll('button,[role="button"]')]
                                    : [];
                                const hostCandidates = [...host.querySelectorAll('button,[role="button"]')]
                                    .filter(button => {
                                        if (!(button instanceof HTMLElement)) return false;
                                        const rect = button.getBoundingClientRect();
                                        const verticallyNearComposer = rect.top <= editorRect.bottom + 80 && rect.bottom >= editorRect.top - 80;
                                        const horizontallyNearComposer = rect.left >= editorRect.left - 80 && rect.right <= editorRect.right + 240;
                                        return verticallyNearComposer && horizontallyNearComposer;
                                    });
                                const candidates = [...new Set([...localCandidates, ...hostCandidates])]
                                    .filter(button => button instanceof HTMLElement && visible(button))
                                    .filter(button => !(button instanceof HTMLButtonElement) || !button.disabled)
                                    .filter(button => button.getAttribute('aria-disabled') !== 'true')
                                    .filter(button => !/attach|upload|file|paperclip|clip|stop|cancel|abort|square|microphone/i.test(marker(button)));
                                let send = candidates.find(button =>
                                    /send|submit|paper-plane|arrow-right|arrow-up|dxbl-image-submit/i.test(marker(button) + ' ' + button.innerHTML));
                                if (!(send instanceof HTMLElement) && candidates.length > 0) {
                                    // DevExpress may render an icon-only submit button outside the editor's immediate parent.
                                    // Prefer the right-most candidate close to the composer rather than any page-level action.
                                    send = candidates
                                        .map(button => ({ button, rect: button.getBoundingClientRect() }))
                                        .sort((left, right) => right.rect.right - left.rect.right)[0]?.button || null;
                                }
                                if (send instanceof HTMLElement) {
                                    send.focus({ preventScroll: true });
                                    send.click();
                                    if (await waitForComposerSubmission(editor, text)) return true;
                                }

                                // Keyboard submission is the final DevExpress-compatible fallback when the icon button
                                // is rendered in a shadowed or dynamically replaced action container.
                                editor.focus({ preventScroll: true });
                                const keyOptions = { key: 'Enter', code: 'Enter', bubbles: true, cancelable: true };
                                editor.dispatchEvent(new KeyboardEvent('keydown', keyOptions));
                                editor.dispatchEvent(new KeyboardEvent('keypress', keyOptions));
                                editor.dispatchEvent(new KeyboardEvent('keyup', keyOptions));
                                if (await waitForComposerSubmission(editor, text)) return true;
                            }
                            await new Promise(resolve => setTimeout(resolve, 125));
                        }
                        return false;
                    } catch (error) {
                        diagnostics.report('localgpt-chat-ui.submitPrompt', error);
                        return false;
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
                state.pendingFollowRequest = true;
                if (state.userInteracting || performance.now() < state.userPauseUntil) return;
                state.follow = distanceToBottom(region) <= followResumeDistancePixels;
                if (state.follow) {
                    state.pendingFollowRequest = false;
                    scheduleSlowFollow(host, region);
                }
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
