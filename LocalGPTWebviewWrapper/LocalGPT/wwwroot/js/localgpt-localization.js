(() => {
    'use strict';
    const culture = String(document.documentElement.lang || 'en-US');
    const neutral = culture.toLowerCase().split('-')[0];
    const excludedSelector = 'script,style,code,pre,textarea,[contenteditable="true"],.demo-chat-content,.publication-content-source,.memory-thoughts p,.memory-thoughts pre,.memory-thoughts code';
    let dictionary = {};
    let exact = new Map();
    let phrases = [];
    let words = new Map();
    let observer;
    let applying = false;

    const normalize = value => String(value || '').replace(/\s+/g, ' ').trim();
    function rebuildMaps() {
        exact = new Map(); phrases = []; words = new Map();
        for (const [key, raw] of Object.entries(dictionary || {})) {
            const value = String(raw ?? '');
            if (key.startsWith('Text.')) exact.set(normalize(key.slice(5).replaceAll('␠', ' ')), value);
            else if (key.startsWith('Phrase.')) phrases.push([key.slice(7).replaceAll('␠', ' '), value]);
            else if (key.startsWith('Word.')) words.set(key.slice(5).toLowerCase(), value);
        }
        phrases.sort((a, b) => b[0].length - a[0].length);
    }
    function preserveCase(source, target) {
        if (source.toUpperCase() === source) return target.toUpperCase();
        if (source[0] && source[0].toUpperCase() === source[0]) return target[0]?.toUpperCase() + target.slice(1);
        return target;
    }
    function fallbackTranslate(value) {
        if (neutral !== 'de') return value;
        let result = value;
        for (const [source, replacement] of phrases) {
            const pattern = new RegExp(source.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
            result = result.replace(pattern, match => preserveCase(match, replacement));
        }

        // Never create a half German / half English label. Word fallback is accepted only
        // when every ordinary word is known. Product names, acronyms and protocol tokens
        // intentionally remain invariant.
        const invariant = /^(?:AI|API|CSS|DX|DXAIChat|DIV|HTML|HTTP|HTTPS|JSON|MFA|OCR|REST|SQL|SQLite|UI|URL|GPU|CPU|LocalGPT|PublisherStudio|Blazor|DevExpress|DevExtreme|OData|Ollama|LM|Studio|Wire)$/i;
        let complete = true;
        const translated = result.replace(/\b[A-Za-z][A-Za-z'-]*\b/g, token => {
            const replacement = words.get(token.toLowerCase());
            if (replacement) return preserveCase(token, replacement);
            if (invariant.test(token) || token.length <= 2) return token;
            complete = false;
            return token;
        });
        return complete ? translated : value;
    }
    function translate(value) {
        const original = normalize(value);
        return exact.get(original) || fallbackTranslate(original) || original;
    }
    function excluded(node) {
        const element = node instanceof Element ? node : node?.parentElement;
        return !element || Boolean(element.closest(excludedSelector));
    }
    function textNode(node) {
        if (!(node instanceof Text) || excluded(node)) return;
        const original = normalize(node.nodeValue);
        if (!original) return;
        const replacement = translate(original);
        if (!replacement || replacement === original) return;
        const leading = /^\s*/.exec(node.nodeValue)?.[0] || '';
        const trailing = /\s*$/.exec(node.nodeValue)?.[0] || '';
        node.nodeValue = `${leading}${replacement}${trailing}`;
    }
    function element(el) {
        if (!(el instanceof Element) || excluded(el)) return;
        for (const attribute of ['title','aria-label','placeholder']) {
            const value = el.getAttribute(attribute);
            if (value) el.setAttribute(attribute, translate(value));
        }
        if (el.matches('input[type="button"],input[type="submit"],input[type="reset"]')) el.value = translate(el.value);
        for (const node of el.childNodes) if (node instanceof Text) textNode(node);
    }
    function apply(root = document.body) {
        if (!root || applying) return;
        applying = true;
        try {
            if (root instanceof Text) textNode(root);
            else {
                if (root instanceof Element) element(root);
                root.querySelectorAll?.('button,label,option,summary,h1,h2,h3,h4,p,span,strong,small,a,input,select,[title],[aria-label],[placeholder]').forEach(element);
            }
        } finally { applying = false; }
    }
    async function load() {
        try {
            const response = await fetch(`/api/localization/${encodeURIComponent(culture)}`, { cache: 'no-store' });
            if (response.ok) dictionary = await response.json();
        } catch (error) { console.warn('LocalGPT localization dictionary could not be loaded.', error); }
        rebuildMaps();
        document.title = translate(document.title);
        apply(document.body);
        observer?.disconnect();
        observer = new MutationObserver(records => {
            if (applying) return;
            for (const record of records) record.addedNodes.forEach(apply);
        });
        observer.observe(document.documentElement, { subtree: true, childList: true });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', load, { once: true });
    else void load();
})();
