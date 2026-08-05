// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const consoleErrors = [];
const originalConsoleError = console.error.bind(console);

console.error = (...args) => { try {
    consoleErrors.push(args.map(item => { try {
        if (item instanceof Error) {
            return `${item.name}: ${item.message}`;
        }

        return typeof item === 'string' ? item : JSON.stringify(item);
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:callback:args.map@6', __javascriptError); throw __javascriptError; }}).join(' '));
    originalConsoleError(...args);
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:console.error@5', __javascriptError); throw __javascriptError; }};

const isVisible = element => { try {
    if (!element) {
        return false;
    }

    const style = window.getComputedStyle(element);
    return style.visibility !== 'hidden'
        && style.display !== 'none'
        && (element.offsetWidth > 0 || element.offsetHeight > 0 || element.getClientRects().length > 0);
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:isVisible@16', __javascriptError); throw __javascriptError; }};

const textOf = element => { try { return ((element?.innerText || element?.textContent || '').trim()); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:textOf@27', __javascriptError); throw __javascriptError; } };

const isDisabled = element => { try {
    if (!element) {
        return true;
    }

    const ariaDisabled = (element.getAttribute('aria-disabled') || '').toLowerCase();
    return !!(element.disabled
        || ariaDisabled === 'true'
        || element.classList.contains('disabled')
        || element.closest('[disabled], [aria-disabled="true"]'));
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:isDisabled@29', __javascriptError); throw __javascriptError; }};

const markerOf = element => { try { return ([
    element.getAttribute('aria-label'),
    element.getAttribute('title'),
    element.getAttribute('class'),
    textOf(element)
].filter(Boolean).join(' ')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:markerOf@41', __javascriptError); throw __javascriptError; } };

const chatHost = () => { try { return (document.querySelector('[data-testid="dxaichat-host"]')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:chatHost@48', __javascriptError); throw __javascriptError; } };

const chatInput = () => { try { return (chatHost()?.querySelector('textarea')); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:chatInput@50', __javascriptError); throw __javascriptError; } };

const findSendButton = () => { try {
    const host = chatHost();
    if (!host) {
        return null;
    }

    const buttons = Array.from(host.querySelectorAll('button, [role="button"]'))
        .filter(isVisible)
        .filter(button => { try { return (!/attach|upload|file|paperclip|clip/i.test(markerOf(button))); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:callback:Array.from(host.querySelectorAll(\'button, [role="button"]\')) .filter(i@60', __javascriptError); throw __javascriptError; } });

    return buttons.find(button => { try { return (/send|submit|arrow|paper-plane/i.test(markerOf(button))); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:callback:buttons.find@62', __javascriptError); throw __javascriptError; } })
        || buttons.filter(button => { try { return (!isDisabled(button)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:callback:buttons.filter@63', __javascriptError); throw __javascriptError; } }).at(-1)
        || buttons.at(-1)
        || null;
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:findSendButton@52', __javascriptError); throw __javascriptError; }};

const tagDevExpressAiChat = () => { try {
    const host = chatHost();
    if (!host) {
        return;
    }

    const input = chatInput();
    if (input) {
        input.setAttribute('data-testid', 'chat-input');
    }

    findSendButton()?.setAttribute('data-testid', 'send-button');
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:tagDevExpressAiChat@68', __javascriptError); throw __javascriptError; }};

const tagNavigation = () => { try {
    const links = Array.from(document.querySelectorAll('a[href]'));
    for (const link of links) {
        const href = (link.getAttribute('href') || '').toLowerCase();
        const label = textOf(link).toLowerCase();
        if (href.includes('/chat') || label.includes('ai chat')) {
            link.setAttribute('data-testid', 'nav-chat');
        }
        else if (href === '/' || label === 'home') {
            link.setAttribute('data-testid', 'nav-home');
        }
    }
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:tagNavigation@82', __javascriptError); throw __javascriptError; }};

const refreshTags = () => { try {
    tagNavigation();
    tagDevExpressAiChat();
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:refreshTags@96', __javascriptError); throw __javascriptError; }};

const wait = milliseconds => { try { return (new Promise(resolve => { try { return (setTimeout(resolve, milliseconds)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:ArrowFunction@101', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:wait@101', __javascriptError); throw __javascriptError; } };

const waitFor = async (predicate, timeoutMs = 10000) => { try {
    const started = Date.now();
    while (Date.now() - started < timeoutMs) {
        refreshTags();
        const value = predicate();
        if (value) {
            return value;
        }

        await wait(100);
    }

    return null;
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:waitFor@103', __javascriptError); throw __javascriptError; }};

const query = selector => { try {
    refreshTags();
    return document.querySelector(selector);
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:query@118', __javascriptError); throw __javascriptError; }};

const setNativeValue = (element, value) => { try {
    const prototype = Object.getPrototypeOf(element);
    const descriptor = Object.getOwnPropertyDescriptor(prototype, 'value');
    if (descriptor?.set) {
        descriptor.set.call(element, value);
    }
    else {
        element.value = value;
    }

    element.dispatchEvent(new InputEvent('input', {
        bubbles: true,
        inputType: 'insertText',
        data: value
    }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:setNativeValue@123', __javascriptError); throw __javascriptError; }};

const localGptE2e = {
    ping: () => { try { return (({
        ok: true,
        href: location.href,
        title: document.title,
        ready: document.documentElement.classList.contains('localgpt-interactive-ready')
    })); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:ping@142', __javascriptError); throw __javascriptError; } },

    location: () => { try { return (({
        href: location.href,
        pathname: location.pathname,
        title: document.title
    })); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:location@149', __javascriptError); throw __javascriptError; } },

    queryExists: selector => { try { return (!!query(selector)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:queryExists@155', __javascriptError); throw __javascriptError; } },

    queryText: selector => { try { return (textOf(query(selector))); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:queryText@157', __javascriptError); throw __javascriptError; } },

    click: selector => { try {
        const element = query(selector);
        if (!element) {
            return false;
        }

        element.click();
        return true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:click@159', __javascriptError); throw __javascriptError; }},

    clickSend: () => { try {
        const button = findSendButton();
        if (!button || isDisabled(button)) {
            return false;
        }

        button.setAttribute('data-testid', 'send-button');
        button.click();
        return true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:clickSend@169', __javascriptError); throw __javascriptError; }},

    sendButtonRect: () => { try {
        const button = findSendButton();
        if (!button || isDisabled(button)) {
            return null;
        }

        const rect = button.getBoundingClientRect();
        return {
            x: rect.x,
            y: rect.y,
            width: rect.width,
            height: rect.height,
            centerX: rect.x + rect.width / 2,
            centerY: rect.y + rect.height / 2
        };
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:sendButtonRect@180', __javascriptError); throw __javascriptError; }},

    setValue: (selector, value) => { try {
        const element = query(selector);
        if (!element) {
            return false;
        }

        element.focus();
        setNativeValue(element, value);
        refreshTags();
        return true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:setValue@197', __javascriptError); throw __javascriptError; }},

    press: (selector, key) => { try {
        const element = query(selector);
        if (!element) {
            return false;
        }

        element.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
        element.dispatchEvent(new KeyboardEvent('keyup', { key, bubbles: true }));
        return true;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:press@209', __javascriptError); throw __javascriptError; }},

    waitForSelector: async (selector, timeoutMs = 10000) => { try {
        const element = await waitFor(() => { try { return (query(selector)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:callback:waitFor@221', __javascriptError); throw __javascriptError; } }, timeoutMs);
        return !!element;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:waitForSelector@220', __javascriptError); throw __javascriptError; }},

    waitForChatInteractive: async (timeoutMs = 30000) => { try {
        const ready = await waitFor(() => { try {
            const input = chatInput();
            const sendButton = findSendButton();
            return !!input
                && isVisible(input)
                && !isDisabled(input)
                && !!sendButton
                && isVisible(sendButton);
         } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:callback:waitFor@226', __javascriptError); throw __javascriptError; }}, timeoutMs);

        return !!ready;
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:waitForChatInteractive@225', __javascriptError); throw __javascriptError; }},

    chatState: () => { try {
        const input = chatInput();
        const sendButton = findSendButton();
        const overlay = document.getElementById('interactive-startup-overlay');

        return {
            href: location.href,
            interactiveReady: document.documentElement.classList.contains('localgpt-interactive-ready'),
            interactiveError: document.documentElement.classList.contains('localgpt-interactive-error'),
            overlayVisible: isVisible(overlay),
            inputExists: !!input,
            inputVisible: isVisible(input),
            inputDisabled: isDisabled(input),
            inputValue: input?.value || '',
            sendExists: !!sendButton,
            sendVisible: isVisible(sendButton),
            sendDisabled: isDisabled(sendButton),
            visibleText: textOf(document.body)
        };
     } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:chatState@239', __javascriptError); throw __javascriptError; }},

    collectConsoleErrors: () => { try { return (consoleErrors.slice()); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:collectConsoleErrors@260', __javascriptError); throw __javascriptError; } },

    collectVisibleText: () => { try { return (textOf(document.body)); } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-e2e.js:collectVisibleText@262', __javascriptError); throw __javascriptError; } },

    refreshTags
};

window.localGptE2e = localGptE2e;
refreshTags();
new MutationObserver(refreshTags).observe(document.documentElement, {
    childList: true,
    subtree: true
});

// Guard browser entry points after initialization.
window.localGptE2e = localGptDiagnostics.guard("localgpt-e2e.js.localGptE2e", window.localGptE2e);
