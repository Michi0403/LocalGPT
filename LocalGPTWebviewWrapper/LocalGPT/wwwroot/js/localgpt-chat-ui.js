(() => {
    'use strict';
    const visible = element => {
        if (!(element instanceof HTMLElement)) return false;
        const style = getComputedStyle(element);
        return style.display !== 'none' && style.visibility !== 'hidden' && element.getClientRects().length > 0;
    };
    const marker = element => [
        element.getAttribute('aria-label'), element.getAttribute('title'),
        element.getAttribute('data-testid'), element.getAttribute('class'), element.textContent
    ].filter(Boolean).join(' ');
    const commonAncestor = (left, right, boundary) => {
        if (!left || !right) return left?.parentElement || right?.parentElement || null;
        const parents = new Set();
        for (let current = left; current && current !== boundary; current = current.parentElement) parents.add(current);
        for (let current = right; current && current !== boundary; current = current.parentElement) if (parents.has(current)) return current;
        return null;
    };
    function enhance(host) {
        if (!(host instanceof Element)) return;
        const editor = host.querySelector('textarea,[contenteditable="true"]');
        editor?.classList?.add('localgpt-chat-textarea');
        if (editor && !editor.getAttribute('aria-label')) editor.setAttribute('aria-label', 'Message to AI assistant');

        const buttons = [...host.querySelectorAll('button,[role="button"]')].filter(visible);
        let send = buttons.find(button => /send|submit|paper-plane|arrow-right|arrow/i.test(marker(button))
            && !/attach|upload|file|paperclip|clip/i.test(marker(button)));
        if (!send) send = buttons.filter(button => !button.matches('[disabled],[aria-disabled="true"]')).at(-1) || null;
        if (send) {
            send.classList.add('localgpt-send-button');
            if (!send.getAttribute('aria-label')) send.setAttribute('aria-label', 'Send message');
            if (!send.getAttribute('title')) send.setAttribute('title', 'Send message');
        }

        const upload = buttons.find(button => /attach|upload|paperclip|clip|choose file/i.test(marker(button)));
        if (upload) {
            upload.classList.add('localgpt-upload-button');
            if (!upload.getAttribute('aria-label')) upload.setAttribute('aria-label', 'Attach files');
            if (!upload.getAttribute('title')) upload.setAttribute('title', 'Attach files');
        }

        const composer = commonAncestor(editor, send, host)
            || editor?.closest('form')
            || editor?.parentElement?.parentElement;
        composer?.classList?.add('localgpt-chat-composer');
    }
    function apply(root = document) {
        if (root instanceof Element && root.matches('[data-testid="dxaichat-host"]')) enhance(root);
        root.querySelectorAll?.('[data-testid="dxaichat-host"]').forEach(enhance);
    }
    const observer = new MutationObserver(records => {
        for (const record of records) {
            record.addedNodes.forEach(node => { if (node instanceof Element) apply(node); });
            if (record.type === 'attributes' && record.target instanceof Element) apply(record.target.closest('[data-testid="dxaichat-host"]') || record.target);
        }
    });
    const start = () => {
        apply();
        observer.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['class','aria-label','title'] });
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
    else start();
})();
