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

    function close() { try {
        const menu = document.getElementById(menuId);
        if (menu) menu.hidden = true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:close@12', __javascriptError); throw __javascriptError; }}


    function hasTextSelection() { try {
        const selection = globalThis.getSelection?.();
        return Boolean(selection && !selection.isCollapsed && selection.toString().trim().length > 0);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:hasTextSelection', __javascriptError); throw __javascriptError; }}

    function shouldUseNativeContextMenu(target) { try {
        if (!(target instanceof Element)) return hasTextSelection();
        return Boolean(target.closest(editableSelector) || target.closest(copyableSelector) || hasTextSelection());
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
        if (shouldUseNativeContextMenu(target)) { close(); return; }
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
    window.addEventListener('resize', close);
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-context-menu.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();
