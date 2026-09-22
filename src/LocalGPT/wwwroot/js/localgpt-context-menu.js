// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    'use strict';
    const menuId = 'localgpt-context-menu';
    const editableSelector = 'input,textarea,select,[contenteditable="true"],[role="textbox"]';
    const copyableSelector = '[data-localgpt-copyable="true"],.demo-chat-content,.former-thought-content,pre,code';

    const routeLinks = [
        ['Home', '/'], ['Chat', '/Chat'], ['AI Council', '/model-council'],
        ['Projects', '/projects'], ['Approvals & MFA', '/onewire-security'], ['Council teams', '/council-teams']
    ];


    const controllerStorageKey = 'localgpt.controllerMode';
    const controllerCursorId = 'localgpt-controller-cursor';
    const controllerBadgeId = 'localgpt-controller-mode-badge';
    const controllerState = {
        mode: localStorage.getItem(controllerStorageKey) === 'cursor' ? 'cursor' : 'control',
        frame: 0,
        buttons: [],
        x: Math.max(24, globalThis.innerWidth / 2),
        y: Math.max(24, globalThis.innerHeight / 2),
        lastFrameTime: 0,
        badgeTimer: 0
    };

    function controllerGamepad() { try {
        if (typeof navigator.getGamepads !== 'function') return null;
        return [...(navigator.getGamepads?.() || [])].find(Boolean) || null;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerGamepad', __javascriptError); return null; }}

    function controllerButtonPressed(gamepad, index) { try {
        return Boolean(gamepad?.buttons?.[index]?.pressed || Number(gamepad?.buttons?.[index]?.value || 0) > .55);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerButtonPressed', __javascriptError); return false; }}

    function controllerButtonEdge(gamepad, index) { try {
        const pressed = controllerButtonPressed(gamepad, index);
        const previous = Boolean(controllerState.buttons[index]);
        controllerState.buttons[index] = pressed;
        return pressed && !previous;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerButtonEdge', __javascriptError); return false; }}

    function controllerAxis(value, deadzone = .18) { try {
        const numeric = Number(value) || 0;
        const magnitude = Math.abs(numeric);
        if (magnitude <= deadzone) return 0;
        const normalized = Math.min(1, (magnitude - deadzone) / Math.max(.01, 1 - deadzone));
        return Math.sign(numeric) * Math.pow(normalized, 1.35);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerAxis', __javascriptError); return 0; }}

    function ensureControllerCursor() { try {
        let cursor = document.getElementById(controllerCursorId);
        if (!cursor) {
            cursor = document.createElement('div');
            cursor.id = controllerCursorId;
            cursor.className = 'localgpt-controller-cursor';
            cursor.setAttribute('aria-hidden', 'true');
            document.body.appendChild(cursor);
        }
        return cursor;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:ensureControllerCursor', __javascriptError); throw __javascriptError; }}

    function updateControllerCursor() { try {
        const cursor = ensureControllerCursor();
        const visible = controllerState.mode === 'cursor' && Boolean(controllerGamepad());
        cursor.hidden = !visible;
        if (visible) cursor.style.transform = `translate3d(${Math.round(controllerState.x)}px, ${Math.round(controllerState.y)}px, 0)`;
        document.documentElement.dataset.localgptControllerMode = controllerState.mode;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:updateControllerCursor', __javascriptError); throw __javascriptError; }}

    function showControllerModeBadge() { try {
        let badge = document.getElementById(controllerBadgeId);
        if (!badge) {
            badge = document.createElement('div');
            badge.id = controllerBadgeId;
            badge.className = 'localgpt-controller-mode-badge';
            badge.setAttribute('role', 'status');
            badge.setAttribute('aria-live', 'polite');
            document.body.appendChild(badge);
        }
        badge.textContent = controllerState.mode === 'cursor' ? 'Controller cursor mode · native mouse/touchpad stays active' : 'Controller control mode · keyboard and pointer stay active';
        badge.hidden = false;
        if (controllerState.badgeTimer) clearTimeout(controllerState.badgeTimer);
        controllerState.badgeTimer = setTimeout(() => { try { badge.hidden = true; } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerBadgeTimer', __javascriptError); } }, 1300);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:showControllerModeBadge', __javascriptError); throw __javascriptError; }}

    function setControllerMode(mode, announce = true) { try {
        controllerState.mode = mode === 'cursor' ? 'cursor' : 'control';
        localStorage.setItem(controllerStorageKey, controllerState.mode);
        updateControllerCursor();
        document.dispatchEvent(new CustomEvent('localgpt:controller-mode-changed', { detail: { mode: controllerState.mode } }));
        if (announce) showControllerModeBadge();
        return controllerState.mode;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:setControllerMode', __javascriptError); throw __javascriptError; }}

    function controllerTarget() { try {
        return document.elementFromPoint(controllerState.x, controllerState.y) || document.body;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerTarget', __javascriptError); return document.body; }}

    function controllerPrimaryAction() { try {
        const target = controllerTarget();
        const clickable = target?.closest?.('button,a,input,select,textarea,[role="button"],[role="menuitem"],[tabindex]');
        if (clickable instanceof HTMLElement && !clickable.hasAttribute('disabled')) {
            try { clickable.focus({ preventScroll: true }); } catch (__caughtJavaScriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerPrimaryAction-focus', __caughtJavaScriptError); }
            clickable.click();
            return true;
        }
        if (target instanceof HTMLElement) {
            target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, clientX: controllerState.x, clientY: controllerState.y, view: globalThis }));
            return true;
        }
        return false;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerPrimaryAction', __javascriptError); throw __javascriptError; }}

    function controllerContextAction(target = controllerTarget(), x = controllerState.x, y = controllerState.y) { try {
        if (!(target instanceof Element)) return false;
        const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true, button: 2, buttons: 2, clientX: x, clientY: y, view: globalThis });
        Object.defineProperty(event, 'localGptControllerContext', { value: true });
        target.dispatchEvent(event);
        return true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerContextAction', __javascriptError); throw __javascriptError; }}

    function controllerContextForControlMode() { try {
        const target = document.activeElement instanceof Element ? document.activeElement : document.body;
        const bounds = target.getBoundingClientRect?.();
        const x = bounds && bounds.width > 0 ? Math.min(globalThis.innerWidth - 8, Math.max(8, bounds.left + bounds.width / 2)) : globalThis.innerWidth / 2;
        const y = bounds && bounds.height > 0 ? Math.min(globalThis.innerHeight - 8, Math.max(8, bounds.top + bounds.height / 2)) : globalThis.innerHeight / 2;
        return controllerContextAction(target, x, y);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerContextForControlMode', __javascriptError); throw __javascriptError; }}

    function controllerScroll(gamepad, elapsedSeconds) { try {
        const vertical = controllerAxis(gamepad?.axes?.[3]);
        const horizontal = controllerAxis(gamepad?.axes?.[2]);
        if (!vertical && !horizontal) return;
        const target = controllerTarget();
        const scrollable = target?.closest?.('[data-controller-scroll],.dxbl-scroll-view,.blazor-scroll-view,[style*="overflow"]');
        const amount = Math.max(1, 760 * elapsedSeconds);
        if (scrollable instanceof HTMLElement) scrollable.scrollBy({ left: horizontal * amount, top: vertical * amount, behavior: 'auto' });
        else globalThis.scrollBy({ left: horizontal * amount, top: vertical * amount, behavior: 'auto' });
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerScroll', __javascriptError); throw __javascriptError; }}

    function pollController(time) { try {
        controllerState.frame = 0;
        if (document.hidden) return;
        const gamepad = controllerGamepad();
        if (!gamepad) {
            controllerState.buttons = [];
            controllerState.lastFrameTime = 0;
            updateControllerCursor();
            return;
        }
        const elapsedSeconds = controllerState.lastFrameTime ? Math.min(.05, Math.max(.001, (time - controllerState.lastFrameTime) / 1000)) : 1 / 60;
        controllerState.lastFrameTime = time;

        if (controllerButtonEdge(gamepad, 8)) setControllerMode(controllerState.mode === 'cursor' ? 'control' : 'cursor');
        if (controllerButtonEdge(gamepad, 9)) controllerState.mode === 'cursor' ? controllerContextAction() : controllerContextForControlMode();

        if (controllerState.mode === 'cursor') {
            const dpadX = controllerButtonPressed(gamepad, 14) ? -1 : controllerButtonPressed(gamepad, 15) ? 1 : 0;
            const dpadY = controllerButtonPressed(gamepad, 12) ? -1 : controllerButtonPressed(gamepad, 13) ? 1 : 0;
            const x = dpadX || controllerAxis(gamepad.axes?.[0]);
            const y = dpadY || controllerAxis(gamepad.axes?.[1]);
            const speed = 980;
            controllerState.x = Math.min(globalThis.innerWidth - 8, Math.max(8, controllerState.x + x * speed * elapsedSeconds));
            controllerState.y = Math.min(globalThis.innerHeight - 8, Math.max(8, controllerState.y + y * speed * elapsedSeconds));
            updateControllerCursor();
            controllerScroll(gamepad, elapsedSeconds);
            if (controllerButtonEdge(gamepad, 0)) controllerPrimaryAction();
            if (controllerButtonEdge(gamepad, 4)) controllerContextAction();
            if (controllerButtonEdge(gamepad, 1)) {
                close();
                document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', bubbles: true, cancelable: true }));
            }
        } else {
            // Keep button edge state current without intercepting native mouse/touchpad input.
            for (const index of [0, 1, 4, 12, 13, 14, 15]) controllerButtonEdge(gamepad, index);
            updateControllerCursor();
        }
        scheduleController();
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:pollController', __javascriptError); controllerState.frame = 0; scheduleController(); }}

    function scheduleController() { try {
        if (controllerState.frame || document.hidden || !controllerGamepad()) return;
        controllerState.frame = requestAnimationFrame(pollController);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:scheduleController', __javascriptError); throw __javascriptError; }}

    globalThis.localGptControllerInput = localGptDiagnostics.guardObject('localGptControllerInput', {
        getMode: () => controllerState.mode,
        setMode: mode => setControllerMode(mode),
        shouldConsumeGameInput: () => controllerState.mode === 'control',
        openContextAtCursor: () => controllerContextAction(),
        schedule: () => scheduleController()
    });

    function close() { try {
        const menu = document.getElementById(menuId);
        if (menu) menu.hidden = true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:close@12', __javascriptError); throw __javascriptError; }}


    function hasTextSelection() { try {
        const selection = globalThis.getSelection?.();
        return Boolean(selection && !selection.isCollapsed && selection.toString().trim().length > 0);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:hasTextSelection', __javascriptError); throw __javascriptError; }}

    function shouldUseNativeContextMenu(target, event) { try {
        if (event?.localGptControllerContext === true) return false;
        if (event?.shiftKey || event?.ctrlKey || event?.metaKey) return true;
        if (!(target instanceof Element)) return true;
        if (target.closest(editableSelector) || target.closest(copyableSelector) || hasTextSelection()) return true;
        const optedIn = localStorage.getItem('localgpt.customContextMenu') === 'true';
        const explicitlyEnabled = Boolean(target.closest('[data-localgpt-custom-context-menu="true"]'));
        return !(event?.altKey || optedIn || explicitlyEnabled);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:shouldUseNativeContextMenu', __javascriptError); throw __javascriptError; }}

    function addLink(menu, label, href) { try {
        const anchor = document.createElement('a');
        anchor.textContent = label;
        anchor.href = href;
        anchor.dataset.enhanceNav = "false";
        menu.appendChild(anchor);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:addLink@17', __javascriptError); throw __javascriptError; }}

    function addAction(menu, label, action, disabled = false) { try {
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = label;
        button.disabled = disabled;
        button.addEventListener('click', event => { try {
            event.stopPropagation();
            close();
            action();
         } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:button.addEventListener@30', __javascriptError); throw __javascriptError; }});
        menu.appendChild(button);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:addAction@25', __javascriptError); throw __javascriptError; }}

    function click(selector) { try {
        const target = document.querySelector(selector);
        if (target instanceof HTMLElement) {
            target.click();
            return true;
        }
        return false;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:click@38', __javascriptError); throw __javascriptError; }}

    function clickByText(rootSelector, pattern) { try {
        const root = document.querySelector(rootSelector) || document;
        const candidates = root.querySelectorAll('button,[role="button"],a,[role="menuitem"]');
        for (const candidate of candidates) {
            const text = `${candidate.textContent || ''} ${candidate.getAttribute('title') || ''} ${candidate.getAttribute('aria-label') || ''}`.replace(/\s+/g, ' ').trim();
            if (pattern.test(text) && candidate instanceof HTMLElement && !candidate.hasAttribute('disabled')) {
                candidate.click();
                return true;
            }
        }
        return false;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:clickByText@47', __javascriptError); throw __javascriptError; }}

    function toggleDetails(selector) { try {
        const details = document.querySelector(selector);
        if (details instanceof HTMLDetailsElement) {
            details.open = !details.open;
            details.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:toggleDetails@60', __javascriptError); throw __javascriptError; }}

    function buildMenu(target) { try {
        let menu = document.getElementById(menuId);
        if (!menu) {
            menu = document.createElement('nav');
            menu.id = menuId;
            menu.className = 'localgpt-context-menu';
            menu.setAttribute('aria-label', 'LocalGPT context menu');
            document.body.appendChild(menu);
        }
        menu.replaceChildren();

        const path = location.pathname.toLowerCase();
        if (path === '/chat') {
            addAction(menu, 'Focus message input', () => { try { return (document.dispatchEvent(new CustomEvent('localgpt:focus-chat'))); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@81', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Start new chat', () => { try { return (clickByText('[data-testid="chat-session-actions"]', /start new chat|new chat/i)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@82', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Refresh local models', () => { try { return (clickByText('[data-testid="chat-page"]', /refresh (ollama|local models)/i)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@83', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Show / hide Council controls', () => { try { return (toggleDetails('#localgpt-council-controls')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@84', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Show / hide memory and projects', () => { try { return (toggleDetails('#localgpt-memory-controls')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@85', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Open approvals & team', () => { try { return (click('[data-localgpt-command="open-approvals"], .human-inbox-launcher')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@86', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Open Council spooler', () => { try { return (click('[data-localgpt-command="open-spooler"], .council-spooler-launcher')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@87', __javascriptError); throw __javascriptError; } });
            menu.appendChild(document.createElement('hr'));
        } else if (path === '/database') {
            addAction(menu, 'Fit database workspace', () => { try { return (document.documentElement.classList.toggle('localgpt-database-compact')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@90', __javascriptError); throw __javascriptError; } });
            addAction(menu, 'Wrap grid text', () => { try { return (document.documentElement.classList.toggle('localgpt-grid-wrap')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@91', __javascriptError); throw __javascriptError; } });
            menu.appendChild(document.createElement('hr'));
        }

        addAction(menu, 'Use native browser context menu by default', () => { try {
            localStorage.setItem('localgpt.customContextMenu', 'false');
         } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:native-default', __javascriptError); throw __javascriptError; }});
        addAction(menu, 'Open browser developer tools', () => { try {
            if (globalThis.chrome?.webview?.postMessage) globalThis.chrome.webview.postMessage('localgpt-open-devtools');
            else console.info('Use the browser native context menu or F12 to open developer tools.');
         } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:open-devtools', __javascriptError); throw __javascriptError; }});
        addAction(menu, document.documentElement.classList.contains('localgpt-overlays-hidden') ? 'Show helper bars' : 'Hide helper bars', () => { try {
            document.documentElement.classList.toggle('localgpt-overlays-hidden');
         } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@95', __javascriptError); throw __javascriptError; }});
        addAction(menu, 'Review pending approvals', () => { try { return (click('[data-localgpt-command="open-approvals"], .human-inbox-launcher')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@98', __javascriptError); throw __javascriptError; } });
        addAction(menu, 'Open Council spooler', () => { try { return (click('[data-localgpt-command="open-spooler"], .council-spooler-launcher')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:addAction@99', __javascriptError); throw __javascriptError; } });
        for (const [label, href] of routeLinks) addLink(menu, label, href);
        return menu;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:buildMenu@68', __javascriptError); throw __javascriptError; }}

    document.addEventListener('contextmenu', event => { try {
        const target = event.target instanceof Element ? event.target : null;
        if (shouldUseNativeContextMenu(target, event)) { close(); return; }
        event.preventDefault();
        const menu = buildMenu(target);
        menu.hidden = false;
        const maxX = Math.max(8, window.innerWidth - menu.offsetWidth - 8);
        const maxY = Math.max(8, window.innerHeight - menu.offsetHeight - 8);
        menu.style.left = `${Math.min(event.clientX, maxX)}px`;
        menu.style.top = `${Math.min(event.clientY, maxY)}px`;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:document.addEventListener@104', __javascriptError); throw __javascriptError; }});
    document.addEventListener('click', event => { try {
        if (!(event.target instanceof Element) || !event.target.closest(`#${menuId}`)) close();
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:document.addEventListener@115', __javascriptError); throw __javascriptError; }});
    document.addEventListener('keydown', event => { try { if (event.key === 'Escape') close();  } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:callback:document.addEventListener@118', __javascriptError); throw __javascriptError; }});
    window.addEventListener('blur', close);
    window.addEventListener('resize', () => { try {
        close();
        controllerState.x = Math.min(globalThis.innerWidth - 8, Math.max(8, controllerState.x));
        controllerState.y = Math.min(globalThis.innerHeight - 8, Math.max(8, controllerState.y));
        updateControllerCursor();
    } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:resize-controller', __javascriptError); throw __javascriptError; }});
    document.addEventListener('pointermove', event => { try {
        if (controllerState.mode !== 'cursor' || event.pointerType === 'touch') return;
        controllerState.x = Math.min(globalThis.innerWidth - 8, Math.max(8, Number(event.clientX) || controllerState.x));
        controllerState.y = Math.min(globalThis.innerHeight - 8, Math.max(8, Number(event.clientY) || controllerState.y));
        updateControllerCursor();
    } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:pointer-sync-controller', __javascriptError); throw __javascriptError; }}, { passive: true });
    window.addEventListener('gamepadconnected', scheduleController);
    window.addEventListener('gamepaddisconnected', () => { try { controllerState.buttons = []; updateControllerCursor(); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:gamepadDisconnected', __javascriptError); throw __javascriptError; }});
    document.addEventListener('visibilitychange', () => { try {
        if (document.hidden && controllerState.frame) cancelAnimationFrame(controllerState.frame);
        controllerState.frame = 0;
        if (!document.hidden) scheduleController();
    } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:controllerVisibility', __javascriptError); throw __javascriptError; }});
    updateControllerCursor();
    scheduleController();
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();
