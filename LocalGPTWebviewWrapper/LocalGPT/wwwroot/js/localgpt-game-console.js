// javascript-diagnostics: guarded
(() => {
    'use strict';
    const diagnostics = globalThis.localGptJavaScriptDiagnostics || {
        report(context, error) { console.error(`LocalGPT JavaScript error in ${context}.`, error); }
    };
    const states = new Map();

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

    function pollGamepad(state) {
        try {
            if (!state || !states.has(state.id)) return;
            if (state.enabled && navigator.getGamepads) {
                const pad = Array.from(navigator.getGamepads()).find(Boolean);
                if (pad) {
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
                }
            }
        } catch (error) {
            diagnostics.report('localgpt-game-console.pollGamepad', error);
        }
        state.frame = requestAnimationFrame(() => pollGamepad(state));
    }

    globalThis.localGptGameConsole = {
        attach(id, reference) {
            try {
                const element = document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                this.detach(id);
                const state = { id, element, reference, enabled:false, busy:false, previousButtons:new Set(), keyboardActions:new Set(), frame:0, abort:new AbortController() };
                states.set(id, state);
                element.addEventListener('pointerdown', () => element.focus({ preventScroll:true }), { signal:state.abort.signal });
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
                state.frame = requestAnimationFrame(() => pollGamepad(state));
            } catch (error) { diagnostics.report('localgpt-game-console.attach', error); }
        },
        detach(id) {
            try {
                const state = states.get(id);
                if (!state) return;
                state.abort.abort();
                if (state.frame) cancelAnimationFrame(state.frame);
                states.delete(id);
            } catch (error) { diagnostics.report('localgpt-game-console.detach', error); }
        },
        setEnabled(id, enabled) {
            try {
                const state = states.get(id);
                if (state) {
                    state.enabled = Boolean(enabled);
                    if (!state.enabled) {
                        state.keyboardActions.clear();
                        state.previousButtons.clear();
                        markPressed(state, new Set());
                    }
                }
            } catch (error) { diagnostics.report('localgpt-game-console.setEnabled', error); }
        },
        async fullscreen(id) {
            try {
                const element = document.getElementById(id);
                if (!(element instanceof HTMLElement)) return;
                if (document.fullscreenElement === element) await document.exitFullscreen();
                else await element.requestFullscreen({ navigationUI:'hide' });
                element.focus({ preventScroll:true });
            } catch (error) { diagnostics.report('localgpt-game-console.fullscreen', error); }
        }
    };
})();
