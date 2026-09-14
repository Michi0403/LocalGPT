// javascript-diagnostics: guarded
(() => {
    'use strict';
    const diagnostics = globalThis.localGptJavaScriptDiagnostics || {
        report(context, error) { console.error(`LocalGPT JavaScript error in ${context}.`, error); }
    };
    const states = new Map();


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
                entry.observer = new MutationObserver(() => { if (entry.enabled) scrollToTail(region); });
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

    function renderSequenceFrame(state) {
        try {
            if (!state || !states.has(state.id) || state.sequenceFrames.length < 2 || document.hidden || !state.windowFocused) return;
            const screen = state.element.querySelector(state.sequenceSelector || '[data-ascii-sequence-screen]');
            if (!(screen instanceof HTMLElement)) return;
            const frame = state.sequenceFrames[state.sequenceIndex % state.sequenceFrames.length] || '';
            if (screen.textContent !== frame) screen.textContent = frame;
            if (state.sequenceSelector === '[data-game-animation-screen]') requestScale(state);
            state.sequenceIndex = (state.sequenceIndex + 1) % state.sequenceFrames.length;
            cancelSequenceTimer(state);
            state.sequenceTimer = window.setTimeout(() => renderSequenceFrame(state), state.sequenceDelay);
        } catch (error) {
            diagnostics.report('localgpt-game-console.renderSequenceFrame', error);
        }
    }

    function scheduleSequence(state) {
        try {
            if (!state || state.sequenceTimer || state.sequenceFrames.length < 2 || document.hidden || !state.windowFocused) return;
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
                const state = { id, element, reference, enabled:false, busy:false, previousButtons:new Set(), keyboardActions:new Set(), pressedSignature:'', frame:0, scaleFrame:0, sequenceFrames:[], sequenceIndex:0, sequenceDelay:650, sequenceSelector:'[data-ascii-sequence-screen]', sequenceTimer:0, windowFocused:document.hasFocus(), abort:new AbortController(), followTailRegions:new Map(), followTailRootObserver:null };
                states.set(id, state);
                attachFollowTail(state);
                requestScale(state);
                element.addEventListener('pointerdown', () => element.focus({ preventScroll:true }), { signal:state.abort.signal });
                document.addEventListener('fullscreenchange', () => requestScale(state), { signal:state.abort.signal });
                window.addEventListener('resize', () => requestScale(state), { signal:state.abort.signal });
                element.addEventListener('keydown', event => {
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
                if (state.frame) cancelAnimationFrame(state.frame);
                if (state.scaleFrame) cancelAnimationFrame(state.scaleFrame);
                cancelSequenceTimer(state);
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
        setSequence(id, frames, delayMilliseconds, target) {
            try {
                const state = states.get(id);
                if (!state) return;
                cancelSequenceTimer(state);
                state.sequenceFrames = Array.isArray(frames) ? frames.filter(frame => typeof frame === 'string' && frame.length > 0).slice(0, 12) : [];
                state.sequenceIndex = 0;
                const requestedDelay = Number(delayMilliseconds);
                state.sequenceDelay = Number.isFinite(requestedDelay) ? Math.max(250, Math.min(5000, requestedDelay)) : 650;
                state.sequenceSelector = target === 'game' ? '[data-game-animation-screen]' : '[data-ascii-sequence-screen]';
                const screen = state.element.querySelector(state.sequenceSelector);
                if (screen instanceof HTMLElement && state.sequenceFrames.length > 0) screen.textContent = state.sequenceFrames[0];
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
                if (document.fullscreenElement === element) await document.exitFullscreen();
                else await element.requestFullscreen({ navigationUI:'hide' });
                element.focus({ preventScroll:true });
                requestScale(states.get(id));
            } catch (error) { diagnostics.report('localgpt-game-console.fullscreen', error); }
        },
        async exitFullscreen(id) {
            try {
                const element = document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                if (document.fullscreenElement === element) await document.exitFullscreen();
            } catch (error) { diagnostics.report('localgpt-game-console.exitFullscreen', error); }
        }
    };
})();
