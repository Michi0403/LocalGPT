const consoleErrors = [];
const originalConsoleError = console.error.bind(console);

console.error = (...args) => {
    consoleErrors.push(args.map(item => {
        if (item instanceof Error) {
            return `${item.name}: ${item.message}`;
        }

        return typeof item === 'string' ? item : JSON.stringify(item);
    }).join(' '));
    originalConsoleError(...args);
};

const isVisible = element => {
    if (!element) {
        return false;
    }

    const style = window.getComputedStyle(element);
    return style.visibility !== 'hidden'
        && style.display !== 'none'
        && (element.offsetWidth > 0 || element.offsetHeight > 0 || element.getClientRects().length > 0);
};

const textOf = element => (element?.innerText || element?.textContent || '').trim();

const isDisabled = element => {
    if (!element) {
        return true;
    }

    const ariaDisabled = (element.getAttribute('aria-disabled') || '').toLowerCase();
    return !!(element.disabled
        || ariaDisabled === 'true'
        || element.classList.contains('disabled')
        || element.closest('[disabled], [aria-disabled="true"]'));
};

const markerOf = element => [
    element.getAttribute('aria-label'),
    element.getAttribute('title'),
    element.getAttribute('class'),
    textOf(element)
].filter(Boolean).join(' ');

const chatHost = () => document.querySelector('[data-testid="dxaichat-host"]');

const chatInput = () => chatHost()?.querySelector('textarea');

const findSendButton = () => {
    const host = chatHost();
    if (!host) {
        return null;
    }

    const buttons = Array.from(host.querySelectorAll('button, [role="button"]'))
        .filter(isVisible)
        .filter(button => !/attach|upload|file|paperclip|clip/i.test(markerOf(button)));

    return buttons.find(button => /send|submit|arrow|paper-plane/i.test(markerOf(button)))
        || buttons.filter(button => !isDisabled(button)).at(-1)
        || buttons.at(-1)
        || null;
};

const tagDevExpressAiChat = () => {
    const host = chatHost();
    if (!host) {
        return;
    }

    const input = chatInput();
    if (input) {
        input.setAttribute('data-testid', 'chat-input');
    }

    findSendButton()?.setAttribute('data-testid', 'send-button');
};

const tagNavigation = () => {
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
};

const refreshTags = () => {
    tagNavigation();
    tagDevExpressAiChat();
};

const wait = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

const waitFor = async (predicate, timeoutMs = 10000) => {
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
};

const query = selector => {
    refreshTags();
    return document.querySelector(selector);
};

const setNativeValue = (element, value) => {
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
};

const localGptE2e = {
    ping: () => ({
        ok: true,
        href: location.href,
        title: document.title,
        ready: document.documentElement.classList.contains('localgpt-interactive-ready')
    }),

    location: () => ({
        href: location.href,
        pathname: location.pathname,
        title: document.title
    }),

    queryExists: selector => !!query(selector),

    queryText: selector => textOf(query(selector)),

    click: selector => {
        const element = query(selector);
        if (!element) {
            return false;
        }

        element.click();
        return true;
    },

    clickSend: () => {
        const button = findSendButton();
        if (!button || isDisabled(button)) {
            return false;
        }

        button.setAttribute('data-testid', 'send-button');
        button.click();
        return true;
    },

    sendButtonRect: () => {
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
    },

    setValue: (selector, value) => {
        const element = query(selector);
        if (!element) {
            return false;
        }

        element.focus();
        setNativeValue(element, value);
        refreshTags();
        return true;
    },

    press: (selector, key) => {
        const element = query(selector);
        if (!element) {
            return false;
        }

        element.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
        element.dispatchEvent(new KeyboardEvent('keyup', { key, bubbles: true }));
        return true;
    },

    waitForSelector: async (selector, timeoutMs = 10000) => {
        const element = await waitFor(() => query(selector), timeoutMs);
        return !!element;
    },

    waitForChatInteractive: async (timeoutMs = 30000) => {
        const ready = await waitFor(() => {
            const input = chatInput();
            const sendButton = findSendButton();
            return !!input
                && isVisible(input)
                && !isDisabled(input)
                && !!sendButton
                && isVisible(sendButton);
        }, timeoutMs);

        return !!ready;
    },

    chatState: () => {
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
    },

    collectConsoleErrors: () => consoleErrors.slice(),

    collectVisibleText: () => textOf(document.body),

    refreshTags
};

window.localGptE2e = localGptE2e;
refreshTags();
new MutationObserver(refreshTags).observe(document.documentElement, {
    childList: true,
    subtree: true
});
