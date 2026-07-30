// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.scrollChatToBottom = function (elementId)
{ try {
    if (typeof window.localGptSlowScrollToBottom === 'function') {
        window.localGptSlowScrollToBottom(elementId);
        return;
    }
    const host = document.getElementById(elementId);
    const region = host?.querySelector('[class*="overflow"], [style*="overflow"]') || host;
    if (region instanceof HTMLElement) region.scrollTop = region.scrollHeight;
 } catch (__javascriptError) { localGptDiagnostics.report('js/scrollChatToBottom.js:window.scrollChatToBottom@2', __javascriptError); throw __javascriptError; }}

// Guard browser entry points after initialization.
window.scrollChatToBottom = localGptDiagnostics.guard("scrollChatToBottom.js.scrollChatToBottom", window.scrollChatToBottom);
