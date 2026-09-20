// javascript-diagnostics: guarded
(() => {
    'use strict';
    const diagnostics = globalThis.localGptJavaScriptDiagnostics || {
        report(context, error) { console.error(`LocalGPT JavaScript error in ${context}.`, error); }
    };
    const states = new Map();

    const ansi16Palette = [
        '#000000', '#800000', '#008000', '#808000', '#000080', '#800080', '#008080', '#c0c0c0',
        '#808080', '#ff0000', '#00ff00', '#ffff00', '#0000ff', '#ff00ff', '#00ffff', '#ffffff'
    ];

    function indexedColor(index) {
        try {
            const value = Math.max(0, Math.min(255, Number(index) || 0));
            if (value < 16) return ansi16Palette[value];
            if (value >= 232) {
                const channel = 8 + (value - 232) * 10;
                return `rgb(${channel}, ${channel}, ${channel})`;
            }
            const cube = value - 16;
            const levels = [0, 95, 135, 175, 215, 255];
            const red = levels[Math.floor(cube / 36) % 6];
            const green = levels[Math.floor(cube / 6) % 6];
            const blue = levels[cube % 6];
            return `rgb(${red}, ${green}, ${blue})`;
        } catch (error) {
            diagnostics.report('localgpt-game-console.indexedColor', error);
            return null;
        }
    }

    function paletteColor(mode, index) {
        try {
            if (index === null || index === undefined) return null;
            const normalizedMode = String(mode || 'Indexed256').toLowerCase();
            if (normalizedMode === 'ansi16') return ansi16Palette[Math.max(0, Math.min(15, Number(index) || 0))];
            return indexedColor(index);
        } catch (error) {
            diagnostics.report('localgpt-game-console.paletteColor', error);
            return null;
        }
    }

    function normalizePresentation(mode, foreground, background, runs) {
        try {
            const name = String(mode || 'TerminalDefault');
            return {
                mode: name,
                foreground: Number.isFinite(Number(foreground)) ? Number(foreground) : (name === 'Ansi16' ? 10 : 46),
                background: Number.isFinite(Number(background)) ? Number(background) : 0,
                runs: Array.isArray(runs) ? runs.slice(0, 4096) : []
            };
        } catch (error) {
            diagnostics.report('localgpt-game-console.normalizePresentation', error);
            return { mode:'TerminalDefault', foreground:46, background:0, runs:[] };
        }
    }

    function applyTextStyle(element, style, presentation) {
        try {
            if (!(element instanceof HTMLElement) || !style) return;
            const mode = style.colorMode || (presentation.mode === 'TerminalDefault' ? 'Indexed256' : presentation.mode);
            let foreground = style.foregroundColor;
            let background = style.backgroundColor;
            if (style.invert) {
                const resolvedForeground = foreground ?? presentation.foreground;
                const resolvedBackground = background ?? presentation.background;
                foreground = resolvedBackground;
                background = resolvedForeground;
            }
            const foregroundCss = paletteColor(mode, foreground);
            const backgroundCss = paletteColor(mode, background);
            if (foregroundCss) element.style.color = foregroundCss;
            if (backgroundCss) element.style.backgroundColor = backgroundCss;
            if (style.bold) element.style.fontWeight = '700';
            if (style.dim) element.style.opacity = '0.68';
        } catch (error) {
            diagnostics.report('localgpt-game-console.applyTextStyle', error);
        }
    }

    function renderStyledAscii(screen, text, mode, foreground, background, runs) {
        try {
            if (!(screen instanceof HTMLElement)) return;
            const value = String(text || '').replace(/\r\n?/g, '\n');
            const presentation = normalizePresentation(mode, foreground, background, runs);
            screen.replaceChildren();
            screen.style.removeProperty('color');
            screen.style.removeProperty('background-color');
            if (presentation.mode !== 'TerminalDefault') {
                const foregroundCss = paletteColor(presentation.mode, presentation.foreground);
                const backgroundCss = paletteColor(presentation.mode, presentation.background);
                if (foregroundCss) screen.style.color = foregroundCss;
                if (backgroundCss) screen.style.backgroundColor = backgroundCss;
            }
            const lines = value.split('\n');
            const byLine = new Map();
            for (const run of presentation.runs) {
                const y = Number(run?.y);
                const x = Number(run?.x);
                const length = Number(run?.length);
                if (!Number.isInteger(y) || y < 0 || y >= lines.length || !Number.isFinite(x) || !Number.isFinite(length) || length <= 0 || !run?.style) continue;
                if (!byLine.has(y)) byLine.set(y, []);
                byLine.get(y).push({ x:Math.max(0, Math.trunc(x)), length:Math.max(1, Math.trunc(length)), style:run.style });
            }
            lines.forEach((line, y) => {
                const chars = Array.from(line);
                const styles = new Array(chars.length).fill(null);
                for (const run of byLine.get(y) || []) {
                    const end = Math.min(chars.length, run.x + run.length);
                    for (let x = run.x; x < end; x += 1) styles[x] = run.style;
                }
                let start = 0;
                while (start < chars.length) {
                    const style = styles[start];
                    let end = start + 1;
                    while (end < chars.length && styles[end] === style) end += 1;
                    const chunk = chars.slice(start, end).join('');
                    if (style) {
                        const span = document.createElement('span');
                        span.textContent = chunk;
                        applyTextStyle(span, style, presentation);
                        screen.appendChild(span);
                    } else {
                        screen.appendChild(document.createTextNode(chunk));
                    }
                    start = end;
                }
                if (y < lines.length - 1) screen.appendChild(document.createTextNode('\n'));
            });
        } catch (error) {
            diagnostics.report('localgpt-game-console.renderStyledAscii', error);
            if (screen instanceof HTMLElement) screen.textContent = String(text || '');
        }
    }

    function applySubtitleStyle(state, subtitle) {
        try {
            if (!(subtitle instanceof HTMLElement)) return;
            subtitle.style.removeProperty('color');
            subtitle.style.removeProperty('background-color');
            subtitle.style.removeProperty('font-weight');
            subtitle.style.removeProperty('opacity');
            const presentation = normalizePresentation(state.sequenceColorMode, state.sequenceDefaultForeground, state.sequenceDefaultBackground, []);
            applyTextStyle(subtitle, state.sequenceSubtitleStyle, presentation);
        } catch (error) {
            diagnostics.report('localgpt-game-console.applySubtitleStyle', error);
        }
    }


    function measureFrame(screen) {
        try {
            const text = String(screen.textContent || '').replace(/\r\n?/g, '\n');
            const lines = text.split('\n');
            const columns = Math.max(1, ...lines.map(line => Array.from(line).length));
            const rows = Math.max(1, lines.length);
            const computed = getComputedStyle(screen);
            const fontSize = Number.parseFloat(computed.fontSize) || 16;
            const lineHeight = Number.parseFloat(computed.lineHeight) || fontSize * 1.08;
            const canvas = document.createElement('canvas');
            const context = canvas.getContext('2d');
            if (context) context.font = computed.font;
            const glyphWidth = Math.max(1, context?.measureText('M').width || fontSize * .62);
            const paddingX = (Number.parseFloat(computed.paddingLeft) || 0) + (Number.parseFloat(computed.paddingRight) || 0);
            const paddingY = (Number.parseFloat(computed.paddingTop) || 0) + (Number.parseFloat(computed.paddingBottom) || 0);
            return { width: columns * glyphWidth + paddingX, height: rows * lineHeight + paddingY };
        } catch (error) {
            diagnostics.report('localgpt-game-console.measureFrame', error);
            return { width: Math.max(1, screen.scrollWidth), height: Math.max(1, screen.scrollHeight) };
        }
    }

    function applyScale(state) {
        try {
            if (!state?.element) return;
            const screen = state.element.querySelector('.chat-game-screen-viewport .chat-game-screen');
            const viewport = state.element.querySelector('.chat-game-screen-viewport');
            const scaleMode = state.element.dataset.scaleMode || 'fit';
            const scaledMode = scaleMode === 'fit' || scaleMode === 'width';
            if (!(screen instanceof HTMLElement) || !(viewport instanceof HTMLElement) || !scaledMode) {
                state.element.style.removeProperty('--localgpt-game-fit-font-size');
                return;
            }

            state.element.style.setProperty('--localgpt-game-fit-font-size', '16px');
            const natural = measureFrame(screen);
            const availableWidth = Math.max(1, viewport.clientWidth - 12);
            const availableHeight = Math.max(1, viewport.clientHeight - 12);
            const widthScale = availableWidth / Math.max(1, natural.width);
            const heightScale = availableHeight / Math.max(1, natural.height);
            const scale = scaleMode === 'width' ? widthScale : Math.min(widthScale, heightScale);
            const fontSize = Math.max(4, Math.min(36, 16 * scale));
            state.element.style.setProperty('--localgpt-game-fit-font-size', `${fontSize.toFixed(2)}px`);
        } catch (error) {
            diagnostics.report('localgpt-game-console.applyScale', error);
        }
    }

    function requestScale(state) {
        try {
            if (!state) return;
            if (state.scaleFrame) cancelAnimationFrame(state.scaleFrame);
            state.scaleFrame = requestAnimationFrame(() => applyScale(state));
        } catch (error) {
            diagnostics.report('localgpt-game-console.requestScale', error);
        }
    }

    function isNearBottom(region) {
        try {
            return region.scrollHeight - region.scrollTop - region.clientHeight <= 32;
        } catch (error) {
            diagnostics.report('localgpt-game-console.isNearBottom', error);
            return true;
        }
    }

    function scrollToTail(region) {
        try {
            region.scrollTop = Math.max(0, region.scrollHeight - region.clientHeight);
        } catch (error) {
            diagnostics.report('localgpt-game-console.scrollToTail', error);
        }
    }

    function attachFollowTail(state) {
        try {
            state.followTailRegions ||= new Map();
            state.element.querySelectorAll('.ascii-conversation-output, .ascii-operator-output').forEach(region => {
                if (!(region instanceof HTMLElement) || state.followTailRegions.has(region)) return;
                const entry = { enabled: true, observer: null };
                region.addEventListener('scroll', () => { entry.enabled = isNearBottom(region); }, { signal: state.abort.signal, passive: true });
                entry.observer = new MutationObserver(() => scrollToTail(region));
                entry.observer.observe(region, { childList: true, subtree: true, characterData: true });
                state.followTailRegions.set(region, entry);
                requestAnimationFrame(() => scrollToTail(region));
            });
            if (!state.followTailRootObserver) {
                state.followTailRootObserver = new MutationObserver(() => attachFollowTail(state));
                state.followTailRootObserver.observe(state.element, { childList: true, subtree: true });
            }
        } catch (error) {
            diagnostics.report('localgpt-game-console.attachFollowTail', error);
        }
    }

    function fullscreenHost(element) {
        try {
            if (!(element instanceof HTMLElement)) return null;
            const popupRoot = element.closest('dxbl-popup-root');
            return popupRoot instanceof HTMLElement ? popupRoot : element;
        } catch (error) {
            diagnostics.report('localgpt-game-console.fullscreenHost', error);
            return element instanceof HTMLElement ? element : null;
        }
    }

    function setFullscreenPopupPresentation(state, active) {
        try {
            if (!state?.element) return;
            const element = state.element;
            const host = fullscreenHost(element);
            const popupCell = element.closest('dxbl-popup-cell');
            const modalRoot = element.closest('dxbl-modal-root');
            const modalDialog = element.closest('dxbl-modal-dialog');
            if (host instanceof HTMLElement) host.classList.toggle('localgpt-game-fullscreen-host', active);
            if (popupCell instanceof HTMLElement) popupCell.classList.toggle('localgpt-game-fullscreen-cell', active);
            if (modalRoot instanceof HTMLElement) modalRoot.classList.toggle('localgpt-game-fullscreen-modal-root', active);
            if (modalDialog instanceof HTMLElement) modalDialog.classList.toggle('localgpt-game-fullscreen-dialog', active);
            element.classList.toggle('localgpt-game-fullscreen', active);
        } catch (error) {
            diagnostics.report('localgpt-game-console.setFullscreenPopupPresentation', error);
        }
    }

    function updateFullscreenPresentation(state) {
        try {
            if (!state?.element) return;
            const host = fullscreenHost(state.element);
            const active = host instanceof HTMLElement && document.fullscreenElement === host;
            setFullscreenPopupPresentation(state, active);
            requestScale(state);
        } catch (error) {
            diagnostics.report('localgpt-game-console.updateFullscreenPresentation', error);
        }
    }

    function isInteractiveTarget(target) {
        try {
            const element = target instanceof Element ? target : null;
            if (!element) return false;
            return Boolean(element.closest([
                'input', 'textarea', 'select', 'button', 'a[href]',
                '[contenteditable="true"]', '[role="combobox"]', '[role="listbox"]',
                '[role="option"]', '[role="button"]', '[role="textbox"]',
                '.dxbl-combobox', '.dxbl-dropdown', '.dxbl-popup'
            ].join(',')));
        } catch (error) {
            diagnostics.report('localgpt-game-console.isInteractiveTarget', error);
            return false;
        }
    }

    const keyActions = new Map([
        ['KeyW', 'move-forward'], ['ArrowUp', 'move-forward'],
        ['KeyS', 'move-backward'], ['ArrowDown', 'move-backward'],
        ['KeyA', 'strafe-left'], ['KeyD', 'strafe-right'],
        ['KeyQ', 'turn-left'], ['ArrowLeft', 'turn-left'],
        ['KeyR', 'turn-right'], ['ArrowRight', 'turn-right'],
        ['Space', 'shoot'], ['ControlLeft', 'duck'], ['ControlRight', 'duck'], ['KeyC', 'duck'],
        ['KeyE', 'use'], ['Enter', 'use'],
        ['Digit1', 'choice-1'], ['Digit2', 'choice-2'], ['Digit3', 'choice-3']
    ]);


    function markPressed(state, actions) {
        try {
            const active = actions instanceof Set ? actions : new Set(actions || []);
            const signature = [...active].sort().join('|');
            if (state.pressedSignature === signature) return;
            state.pressedSignature = signature;
            state.element.querySelectorAll('[data-game-action]').forEach(button => {
                const action = button.getAttribute('data-game-action');
                const mapped = action === 'left' ? ['strafe-left', 'turn-left'] : action === 'right' ? ['strafe-right', 'turn-right'] : [action];
                button.classList.toggle('is-pressed', mapped.some(item => active.has(item)));
            });
        } catch (error) {
            diagnostics.report('localgpt-game-console.markPressed', error);
        }
    }

    async function submit(state, action) {
        try {
            if (!state?.enabled || state.busy || !action) return;
            state.busy = true;
            await state.reference.invokeMethodAsync('SubmitControlFromJs', action);
        } catch (error) {
            diagnostics.report('localgpt-game-console.submit', error);
        } finally {
            if (state) window.setTimeout(() => { state.busy = false; }, 120);
        }
    }

    function connectedGamepad() {
        try {
            if (!navigator.getGamepads) return null;
            return Array.from(navigator.getGamepads()).find(Boolean) || null;
        } catch (error) {
            diagnostics.report('localgpt-game-console.connectedGamepad', error);
            return null;
        }
    }

    function cancelGamepadFrame(state) {
        try {
            if (!state?.frame) return;
            cancelAnimationFrame(state.frame);
            state.frame = 0;
        } catch (error) {
            diagnostics.report('localgpt-game-console.cancelGamepadFrame', error);
        }
    }

    function scheduleGamepadFrame(state) {
        try {
            if (!state || state.frame || !states.has(state.id) || !state.enabled || document.hidden || !state.windowFocused || !connectedGamepad()) return;
            state.frame = requestAnimationFrame(() => pollGamepad(state));
        } catch (error) {
            diagnostics.report('localgpt-game-console.scheduleGamepadFrame', error);
        }
    }

    function pollGamepad(state) {
        try {
            if (!state || !states.has(state.id)) return;
            state.frame = 0;
            if (!state.enabled || document.hidden || !state.windowFocused) return;
            const pad = connectedGamepad();
            if (!pad) {
                state.previousButtons.clear();
                markPressed(state, new Set());
                return;
            }
            const pressed = new Set();
            const buttonAction = [[0,'use'],[1,'duck'],[2,'choice-1'],[3,'choice-2'],[7,'shoot'],[12,'move-forward'],[13,'move-backward'],[14,'turn-left'],[15,'turn-right']];
            for (const [index, action] of buttonAction) if (pad.buttons[index]?.pressed) pressed.add(action);
            const x = pad.axes[0] || 0;
            const y = pad.axes[1] || 0;
            const lookX = pad.axes[2] || 0;
            if (y < -.62) pressed.add('move-forward');
            if (y > .62) pressed.add('move-backward');
            if (x < -.62) pressed.add('strafe-left');
            if (x > .62) pressed.add('strafe-right');
            if (lookX < -.62) pressed.add('turn-left');
            if (lookX > .62) pressed.add('turn-right');
            markPressed(state, pressed);
            for (const action of pressed) if (!state.previousButtons.has(action)) { submit(state, action); break; }
            state.previousButtons = pressed;
            scheduleGamepadFrame(state);
        } catch (error) {
            diagnostics.report('localgpt-game-console.pollGamepad', error);
        }
    }

    function cancelSequenceTimer(state) {
        try {
            if (!state?.sequenceTimer) return;
            clearTimeout(state.sequenceTimer);
            state.sequenceTimer = 0;
        } catch (error) {
            diagnostics.report('localgpt-game-console.cancelSequenceTimer', error);
        }
    }

    function cancelSequenceSubtitleTimer(state) {
        try {
            if (!state?.sequenceSubtitleTimer) return;
            clearTimeout(state.sequenceSubtitleTimer);
            state.sequenceSubtitleTimer = 0;
        } catch (error) {
            diagnostics.report('localgpt-game-console.cancelSequenceSubtitleTimer', error);
        }
    }

    function updateGameSequenceSubtitle(state, visible) {
        try {
            const subtitle = state?.element?.querySelector('[data-game-animation-subtitle]');
            if (!(subtitle instanceof HTMLElement)) return;
            const value = typeof state.sequenceSubtitle === 'string' ? state.sequenceSubtitle.trim() : '';
            subtitle.textContent = value;
            applySubtitleStyle(state, subtitle);
            subtitle.hidden = !visible || !value;
        } catch (error) {
            diagnostics.report('localgpt-game-console.updateGameSequenceSubtitle', error);
        }
    }

    function renderSequenceFrame(state) {
        try {
            if (!state || !states.has(state.id) || state.sequenceFrames.length < 2 || document.hidden || !state.windowFocused) return;
            const screen = state.element.querySelector(state.sequenceSelector || '[data-ascii-sequence-screen]');
            if (!(screen instanceof HTMLElement)) return;
            const index = state.sequenceOneShot
                ? Math.min(state.sequenceIndex, state.sequenceFrames.length - 1)
                : state.sequenceIndex % state.sequenceFrames.length;
            const frame = state.sequenceFrames[index] || '';
            const frameRuns = Array.isArray(state.sequenceFrameStyleRuns?.[index]) ? state.sequenceFrameStyleRuns[index] : [];
            if (state.sequenceOneShot) renderStyledAscii(screen, frame, state.sequenceColorMode, state.sequenceDefaultForeground, state.sequenceDefaultBackground, frameRuns);
            else if (screen.textContent !== frame) screen.textContent = frame;
            if (state.sequenceSelector === '[data-game-animation-screen]') requestScale(state);
            cancelSequenceTimer(state);
            if (state.sequenceOneShot && index >= state.sequenceFrames.length - 1) {
                state.sequenceIndex = state.sequenceFrames.length;
                cancelSequenceSubtitleTimer(state);
                state.sequenceSubtitleTimer = window.setTimeout(() => {
                    state.sequenceSubtitleTimer = 0;
                    updateGameSequenceSubtitle(state, false);
                }, state.sequenceSubtitleHold);
                return;
            }
            state.sequenceIndex = state.sequenceOneShot ? index + 1 : (index + 1) % state.sequenceFrames.length;
            state.sequenceTimer = window.setTimeout(() => renderSequenceFrame(state), state.sequenceDelay);
        } catch (error) {
            diagnostics.report('localgpt-game-console.renderSequenceFrame', error);
        }
    }

    function scheduleSequence(state) {
        try {
            if (!state || state.sequenceTimer || state.sequenceFrames.length < 2 || document.hidden || !state.windowFocused || (state.sequenceOneShot && state.sequenceIndex >= state.sequenceFrames.length)) return;
            state.sequenceTimer = window.setTimeout(() => {
                state.sequenceTimer = 0;
                renderSequenceFrame(state);
            }, state.sequenceDelay);
        } catch (error) {
            diagnostics.report('localgpt-game-console.scheduleSequence', error);
        }
    }

    globalThis.localGptGameConsole = {
        attach(id, reference) {
            try {
                const element = document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                this.detach(id);
                const state = { id, element, reference, enabled:false, busy:false, previousButtons:new Set(), keyboardActions:new Set(), pressedSignature:'', frame:0, scaleFrame:0, sequenceFrames:[], sequenceIndex:0, sequenceDelay:650, sequenceSelector:'[data-ascii-sequence-screen]', sequenceTimer:0, sequenceOneShot:false, sequenceSubtitle:'', sequenceSubtitleHold:1500, sequenceSubtitleTimer:0, sequenceColorMode:'TerminalDefault', sequenceDefaultForeground:46, sequenceDefaultBackground:0, sequenceFrameStyleRuns:[], sequenceSubtitleStyle:null, windowFocused:document.hasFocus(), abort:new AbortController(), followTailRegions:new Map(), followTailRootObserver:null, resizeObserver:null };
                states.set(id, state);
                attachFollowTail(state);
                requestScale(state);
                element.addEventListener('pointerdown', event => {
                    if (isInteractiveTarget(event.target)) return;
                    element.focus({ preventScroll:true });
                }, { signal:state.abort.signal });
                document.addEventListener('fullscreenchange', () => updateFullscreenPresentation(state), { signal:state.abort.signal });
                window.addEventListener('resize', () => requestScale(state), { signal:state.abort.signal });
                if (typeof ResizeObserver === 'function') {
                    state.resizeObserver = new ResizeObserver(() => requestScale(state));
                    state.resizeObserver.observe(element);
                    if (element.parentElement instanceof HTMLElement) state.resizeObserver.observe(element.parentElement);
                }
                element.addEventListener('keydown', event => {
                    if (isInteractiveTarget(event.target)) return;
                    if (event.code === 'KeyF' && !event.repeat) {
                        event.preventDefault();
                        globalThis.localGptGameConsole.fullscreen(id);
                        return;
                    }
                    const action = keyActions.get(event.code);
                    if (!action || !state.enabled || event.repeat) return;
                    event.preventDefault();
                    state.keyboardActions.add(action);
                    markPressed(state, state.keyboardActions);
                    submit(state, action);
                }, { signal:state.abort.signal });
                element.addEventListener('keyup', event => {
                    if (isInteractiveTarget(event.target)) return;
                    const action = keyActions.get(event.code);
                    if (!action) return;
                    state.keyboardActions.delete(action);
                    markPressed(state, state.keyboardActions);
                }, { signal:state.abort.signal });
                document.addEventListener('visibilitychange', () => {
                    if (document.hidden) { cancelGamepadFrame(state); cancelSequenceTimer(state); }
                    else { scheduleGamepadFrame(state); scheduleSequence(state); }
                }, { signal:state.abort.signal });
                window.addEventListener('blur', () => {
                    state.windowFocused = false;
                    cancelGamepadFrame(state);
                    cancelSequenceTimer(state);
                }, { signal:state.abort.signal });
                window.addEventListener('focus', () => {
                    state.windowFocused = true;
                    scheduleGamepadFrame(state);
                    scheduleSequence(state);
                }, { signal:state.abort.signal });
                window.addEventListener('gamepadconnected', () => scheduleGamepadFrame(state), { signal:state.abort.signal });
                window.addEventListener('gamepaddisconnected', () => {
                    if (!connectedGamepad()) {
                        cancelGamepadFrame(state);
                        state.previousButtons.clear();
                        markPressed(state, new Set());
                    }
                }, { signal:state.abort.signal });
            } catch (error) { diagnostics.report('localgpt-game-console.attach', error); }
        },
        detach(id) {
            try {
                const state = states.get(id);
                if (!state) return;
                state.abort.abort();
                for (const entry of state.followTailRegions?.values() || []) entry.observer?.disconnect();
                state.followTailRegions?.clear();
                state.followTailRootObserver?.disconnect();
                state.followTailRootObserver = null;
                state.resizeObserver?.disconnect();
                state.resizeObserver = null;
                if (state.frame) cancelAnimationFrame(state.frame);
                if (state.scaleFrame) cancelAnimationFrame(state.scaleFrame);
                cancelSequenceTimer(state);
                cancelSequenceSubtitleTimer(state);
                state.element.style.removeProperty('--localgpt-game-fit-font-size');
                states.delete(id);
            } catch (error) { diagnostics.report('localgpt-game-console.detach', error); }
        },
        refreshLayout(id) {
            try {
                const state = states.get(id);
                if (!state) return;
                attachFollowTail(state);
                requestScale(state);
            } catch (error) { diagnostics.report('localgpt-game-console.refreshLayout', error); }
        },
        setEnabled(id, enabled) {
            try {
                const state = states.get(id);
                if (state) {
                    state.enabled = Boolean(enabled);
                    if (!state.enabled) {
                        cancelGamepadFrame(state);
                        state.keyboardActions.clear();
                        state.previousButtons.clear();
                        markPressed(state, new Set());
                    } else {
                        scheduleGamepadFrame(state);
                    }
                }
            } catch (error) { diagnostics.report('localgpt-game-console.setEnabled', error); }
        },
        setFramePresentation(id, text, colorMode, defaultForegroundColor, defaultBackgroundColor, styleRuns) {
            try {
                const state = states.get(id);
                if (!state) return;
                const screen = state.element.querySelector('[data-game-animation-screen]');
                if (!(screen instanceof HTMLElement)) return;
                renderStyledAscii(screen, text, colorMode, defaultForegroundColor, defaultBackgroundColor, styleRuns);
                requestScale(state);
            } catch (error) { diagnostics.report('localgpt-game-console.setFramePresentation', error); }
        },
        setSequence(id, frames, delayMilliseconds, target, subtitle, subtitleHoldMilliseconds, colorMode, defaultForegroundColor, defaultBackgroundColor, frameStyleRuns, subtitleStyle) {
            try {
                const state = states.get(id);
                if (!state) return;
                cancelSequenceTimer(state);
                cancelSequenceSubtitleTimer(state);
                state.sequenceFrames = Array.isArray(frames) ? frames.filter(frame => typeof frame === 'string' && frame.length > 0).slice(0, 12) : [];
                state.sequenceIndex = state.sequenceFrames.length > 0 ? 1 : 0;
                const requestedDelay = Number(delayMilliseconds);
                state.sequenceDelay = Number.isFinite(requestedDelay) ? Math.max(250, Math.min(5000, requestedDelay)) : 650;
                state.sequenceSelector = target === 'game' ? '[data-game-animation-screen]' : '[data-ascii-sequence-screen]';
                state.sequenceOneShot = target === 'game';
                state.sequenceSubtitle = state.sequenceOneShot && typeof subtitle === 'string' ? subtitle.trim() : '';
                state.sequenceColorMode = String(colorMode || 'TerminalDefault');
                state.sequenceDefaultForeground = Number.isFinite(Number(defaultForegroundColor)) ? Number(defaultForegroundColor) : (state.sequenceColorMode === 'Ansi16' ? 10 : 46);
                state.sequenceDefaultBackground = Number.isFinite(Number(defaultBackgroundColor)) ? Number(defaultBackgroundColor) : 0;
                state.sequenceFrameStyleRuns = Array.isArray(frameStyleRuns) ? frameStyleRuns.slice(0, 12) : [];
                state.sequenceSubtitleStyle = subtitleStyle || null;
                const requestedHold = Number(subtitleHoldMilliseconds);
                state.sequenceSubtitleHold = Number.isFinite(requestedHold) ? Math.max(500, Math.min(10000, requestedHold)) : 1500;
                updateGameSequenceSubtitle(state, state.sequenceOneShot && state.sequenceFrames.length > 1);
                const screen = state.element.querySelector(state.sequenceSelector);
                if (screen instanceof HTMLElement && state.sequenceFrames.length > 0) {
                    if (state.sequenceOneShot) renderStyledAscii(screen, state.sequenceFrames[0], state.sequenceColorMode, state.sequenceDefaultForeground, state.sequenceDefaultBackground, state.sequenceFrameStyleRuns[0] || []);
                    else screen.textContent = state.sequenceFrames[0];
                }
                if (state.sequenceFrames.length > 1) scheduleSequence(state);
            } catch (error) { diagnostics.report('localgpt-game-console.setSequence', error); }
        },
        setScaleMode(id, mode) {
            try {
                const state = states.get(id);
                const element = state?.element || document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                const requested = String(mode || '').toLowerCase();
                element.dataset.scaleMode = requested === 'native' ? 'native' : requested === 'width' ? 'width' : 'fit';
                if (state) requestScale(state);
            } catch (error) { diagnostics.report('localgpt-game-console.setScaleMode', error); }
        },
        async fullscreen(id) {
            try {
                const element = document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                const host = fullscreenHost(element);
                if (!(host instanceof HTMLElement)) return;
                if (document.fullscreenElement === host) await document.exitFullscreen();
                else await host.requestFullscreen({ navigationUI:'hide' });
                element.focus({ preventScroll:true });
                updateFullscreenPresentation(states.get(id));
            } catch (error) { diagnostics.report('localgpt-game-console.fullscreen', error); }
        },
        async exitFullscreen(id) {
            try {
                const element = document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                const state = states.get(id) ?? { element };
                const host = fullscreenHost(element);
                if (host instanceof HTMLElement && document.fullscreenElement === host) await document.exitFullscreen();
                setFullscreenPopupPresentation(state, false);
                requestScale(state);
            } catch (error) { diagnostics.report('localgpt-game-console.exitFullscreen', error); }
        },
        followTail(id) {
            try {
                const state = states.get(id);
                if (!state) return;
                attachFollowTail(state);
                state.element.querySelectorAll('.ascii-conversation-output, .ascii-operator-output').forEach(region => {
                    if (region instanceof HTMLElement) scrollToTail(region);
                });
            } catch (error) { diagnostics.report('localgpt-game-console.followTail', error); }
        }
    };
})();
