// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.logoutToDeleteCookies = async function () { try {
    document.cookie.split(";").forEach(cookie => { try {
        const name = cookie.split("=")[0].trim();
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
     } catch (__javascriptError) { localGptDiagnostics.report('js/logoutToDeleteCookies.js:callback:document.cookie.split(";").forEach@3', __javascriptError); throw __javascriptError; }});

    return true;
 } catch (__javascriptError) { localGptDiagnostics.report('js/logoutToDeleteCookies.js:window.logoutToDeleteCookies@2', __javascriptError); throw __javascriptError; }}

// Guard browser entry points after initialization.
window.logoutToDeleteCookies = localGptDiagnostics.guard("logoutToDeleteCookies.js.logoutToDeleteCookies", window.logoutToDeleteCookies);
