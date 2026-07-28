// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.loginToSetCookie = async function (apiRoute, userName, password) { try {
    console.log("Sending login for:", apiRoute, userName, password);

    const body = JSON.stringify({ UserName: userName, Password: password });
    console.log("Body:", body);

    const response = await fetch(apiRoute, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        credentials: "include",
        body: body
    });

    return response.ok;
 } catch (__javascriptError) { localGptDiagnostics.report('js/loginToSetCookie.js:window.loginToSetCookie@2', __javascriptError); throw __javascriptError; }}

// Guard browser entry points after initialization.
window.loginToSetCookie = localGptDiagnostics.guard("loginToSetCookie.js.loginToSetCookie", window.loginToSetCookie);
