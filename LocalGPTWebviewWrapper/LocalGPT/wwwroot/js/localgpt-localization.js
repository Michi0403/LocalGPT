// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    'use strict';
    const excludedSelector = 'script,style,code,pre,textarea,[contenteditable="true"],.demo-chat-content,.publication-content-source,.memory-thoughts p,.memory-thoughts pre,.memory-thoughts code';
    let culture = 'en-US';
    let neutral = 'en';
    let dictionary = {};
    let exact = new Map();
    let exactFolded = new Map();
    let phrases = [];
    let words = new Map();
    let observer;
    let applying = false;
    let loadGeneration = 0;

    const normalize = value => String(value || '').replace(/\s+/g, ' ').trim();
    const currentCulture = () => normalize(document.documentElement.getAttribute('lang')) || 'en-US';

    function rebuildMaps() {
        exact = new Map();
        exactFolded = new Map();
        phrases = [];
        words = new Map();
        for (const [key, raw] of Object.entries(dictionary || {})) {
            const value = String(raw ?? '');
            if (key.startsWith('Text.')) {
                const source = normalize(key.slice(5).replaceAll('␠', ' '));
                exact.set(source, value);
                exactFolded.set(source.toLocaleLowerCase(culture), value);
            } else if (key.startsWith('Phrase.')) {
                phrases.push([key.slice(7).replaceAll('␠', ' '), value]);
            } else if (key.startsWith('Word.')) {
                words.set(key.slice(5).toLocaleLowerCase(culture), value);
            }
        }
        phrases.sort((left, right) => right[0].length - left[0].length);
    }

    function preserveCase(source, target) {
        if (!target) return target;
        if (source.toUpperCase() === source) return target.toUpperCase();
        if (source[0] && source[0].toUpperCase() === source[0]) return target[0].toUpperCase() + target.slice(1);
        return target;
    }

    function fallbackTranslate(value) {
        if (neutral !== 'de') return value;
        let result = value;
        for (const [source, replacement] of phrases) {
            const pattern = new RegExp(source.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
            result = result.replace(pattern, match => preserveCase(match, replacement));
        }

        const invariant = /^(?:AI|API|CSS|DX|DXAIChat|DIV|HTML|HTTP|HTTPS|JSON|MFA|OCR|REST|SQL|SQLite|UI|URL|GPU|CPU|LocalGPT|PublisherStudio|Blazor|DevExpress|DevExtreme|OData|Ollama|LM|Studio|Wire)$/i;
        let complete = true;
        const translated = result.replace(/\b[A-Za-z][A-Za-z'-]*\b/g, token => {
            const replacement = words.get(token.toLocaleLowerCase(culture));
            if (replacement) return preserveCase(token, replacement);
            if (invariant.test(token) || token.length <= 2) return token;
            complete = false;
            return token;
        });
        return complete ? translated : value;
    }

    function translate(value) {
        const original = normalize(value);
        if (!original) return original;
        return exact.get(original)
            || exactFolded.get(original.toLocaleLowerCase(culture))
            || fallbackTranslate(original)
            || original;
    }

    function excluded(node) {
        const element = node instanceof Element ? node : node?.parentElement;
        return !element || Boolean(element.closest(excludedSelector));
    }

    function translateTextNode(node) {
        if (!(node instanceof Text) || excluded(node)) return;
        const original = normalize(node.nodeValue);
        if (!original) return;
        const replacement = translate(original);
        if (!replacement || replacement === original) return;
        const leading = /^\s*/.exec(node.nodeValue)?.[0] || '';
        const trailing = /\s*$/.exec(node.nodeValue)?.[0] || '';
        node.nodeValue = `${leading}${replacement}${trailing}`;
    }

    function translateElement(element) {
        if (!(element instanceof Element) || excluded(element)) return;
        for (const attribute of ['title', 'aria-label', 'placeholder']) {
            const value = element.getAttribute(attribute);
            if (!value) continue;
            const replacement = translate(value);
            if (replacement && replacement !== value) element.setAttribute(attribute, replacement);
        }
        if (element.matches('input[type="button"],input[type="submit"],input[type="reset"]')) {
            const replacement = translate(element.value);
            if (replacement && replacement !== element.value) element.value = replacement;
        }
        for (const node of element.childNodes) if (node instanceof Text) translateTextNode(node);
    }

    function apply(root = document.body) {
        if (!root || applying) return;
        applying = true;
        try {
            if (root instanceof Text) {
                translateTextNode(root);
                return;
            }
            if (root instanceof Element) translateElement(root);
            root.querySelectorAll?.('button,label,option,summary,h1,h2,h3,h4,p,span,strong,small,a,input,select,[title],[aria-label],[placeholder]').forEach(translateElement);
        } finally {
            applying = false;
        }
    }

    function observe() {
        observer?.disconnect();
        observer = new MutationObserver(records => {
            if (applying) return;
            for (const record of records) {
                if (record.type === 'characterData') {
                    apply(record.target);
                } else if (record.type === 'attributes') {
                    apply(record.target);
                } else {
                    record.addedNodes.forEach(apply);
                }
            }
        });
        observer.observe(document.documentElement, {
            subtree: true,
            childList: true,
            characterData: true,
            attributes: true,
            attributeFilter: ['title', 'aria-label', 'placeholder', 'value']
        });
    }

    async function refresh() {
        const generation = ++loadGeneration;
        culture = currentCulture();
        neutral = culture.toLocaleLowerCase().split('-')[0];
        try {
            const response = await fetch(`/api/localization/${encodeURIComponent(culture)}`, {
                cache: 'no-store',
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error(`Localization request failed with HTTP ${response.status}.`);
            const loaded = await response.json();
            if (generation !== loadGeneration) return;
            dictionary = loaded || {};
        } catch (error) {
            localGptDiagnostics.report('js/localgpt-localization.js:refresh', error);
            if (generation !== loadGeneration) return;
            dictionary = {};
        }
        rebuildMaps();
        document.documentElement.dataset.localgptLocalizationCulture = culture;
        const translatedTitle = translate(document.title);
        if (translatedTitle) document.title = translatedTitle;
        apply(document.body);
        observe();
    }

    globalThis.localGptLocalization = Object.freeze({ refresh, apply });
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => void refresh(), { once: true });
    else void refresh();
 } catch (__javascriptError) { localGptDiagnostics.report('js/localgpt-localization.js:bootstrap', __javascriptError); throw __javascriptError; }})();
