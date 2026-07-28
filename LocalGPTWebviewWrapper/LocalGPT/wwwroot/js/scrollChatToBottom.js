// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.scrollChatToBottom = function (elementId)
{ try {
    const el = document.getElementById(elementId);
    //if (el) {
    //    el.scrollTop = el.scrollHeight;
    //}
    if (el) {
        setTimeout(() => { try {
            var bottomElement = el.lastElementChild;
            bottomElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
            el.scrollTop = el.scrollHeight;
         } catch (__javascriptError) { localGptDiagnostics.report('js/scrollChatToBottom.js:callback:setTimeout@9', __javascriptError); throw __javascriptError; }}, 150); // Give layout time to finish
    }
 } catch (__javascriptError) { localGptDiagnostics.report('js/scrollChatToBottom.js:window.scrollChatToBottom@2', __javascriptError); throw __javascriptError; }}

// Guard browser entry points after initialization.
window.scrollChatToBottom = localGptDiagnostics.guard("scrollChatToBottom.js.scrollChatToBottom", window.scrollChatToBottom);
