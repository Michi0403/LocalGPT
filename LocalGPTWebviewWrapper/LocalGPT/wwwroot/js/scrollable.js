// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
class DxDemoScrollable extends HTMLElement {
    _centerVertically = false;
    _centerHorizontally = false;
    connectedCallback() { try {
        const parent = this.parentElement;
        if (this._centerHorizontally)
            parent.scrollLeft = (this.offsetWidth - parent.offsetWidth) / 2;
        if (this._centerVertically)
            parent.scrollTop = (this.offsetHeight - parent.offsetHeight) / 2;
     } catch (__javascriptError) { localGptDiagnostics.report('js/scrollable.js:connectedCallback@5', __javascriptError); throw __javascriptError; }}

    static get observedAttributes() { try {
        return ["center-horizontally", "center-vertically"];
     } catch (__javascriptError) { localGptDiagnostics.report('js/scrollable.js:observedAttributes@13', __javascriptError); throw __javascriptError; }}
    attributeChangedCallback(name, oldValue, newValue) { try {
        switch (name) {
            case "center-horizontally":
                this._centerHorizontally = newValue === "";
                break;
            case "center-vertically":
                this._centerVertically = newValue === "";
                break;
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/scrollable.js:attributeChangedCallback@16', __javascriptError); throw __javascriptError; }}
}

localGptDiagnostics.guardClass("DxDemoScrollable", DxDemoScrollable);
customElements.define("dxbl-demo-scrollable", DxDemoScrollable);

function init(){ try {

 } catch (__javascriptError) { localGptDiagnostics.report('js/scrollable.js:init@31', __javascriptError); throw __javascriptError; }}
