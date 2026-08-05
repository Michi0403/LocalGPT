// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.getAllAsync = async function getAllAsync(typeName, additionalQuery = null) { try {
    let url = `api/odata/${typeName}`;
    if (additionalQuery) {
        url += `?${additionalQuery}`;
    }

    try {
        const response = await fetch(url, {
            method: 'GET',
            credentials: 'include', // send auth cookie
            headers: {
                'Accept': 'application/json, application/octet-stream',
                'Content-MessageType': 'application/json; odata.metadata=full'
            }
        });

        if (!response.ok) {
            console.warn(`Fetch failed: ${response.status}`);
            return [];
        }

        const data = await response.json();
        return data.value || [];
    } catch (err) {
        console.error('Fetch error:', err);
        return [];
    }
 } catch (__javascriptError) { localGptDiagnostics.report('js/webApiCallsInBrowser.js:getAllAsync@2', __javascriptError); throw __javascriptError; }}

//window.startBlazorRefresh = function () {
//    setInterval(() => {
//        DotNet.invokeMethodAsync('TacosPortal', 'TriggerRefreshFromJs')
//            .catch(err => console.warn('Blazor refresh failed:', err));
//    }, 10000); // every 10 seconds
//};

// Guard browser entry points after initialization.
window.getAllAsync = localGptDiagnostics.guard("webApiCallsInBrowser.js.getAllAsync", window.getAllAsync);
