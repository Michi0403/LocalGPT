"use strict";

// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const diagnostics = window.localGptJavaScriptDiagnostics;
let abortController;

function getThemeLinks(attributeName) {
    try {
        return [...document.querySelectorAll(`link[${attributeName}]`)];
    } catch (error) {
        diagnostics.report("theme-controller.getThemeLinks", error);
        throw error;
    }
}

function waitForStylesheet(link, signal, timeoutMilliseconds = 1500) {
    try {
        if (link.sheet)
            return Promise.resolve();

        return new Promise(resolve => {
            try {
                let completed = false;
                const finish = diagnostics.guard("theme-controller.waitForStylesheet.finish", () => { try {
                    if (completed)
                        return;
                    completed = true;
                    clearTimeout(timer);
                    link.removeEventListener("load", finish);
                    link.removeEventListener("error", finish);
                    resolve();
                 } catch (__javascriptError) { localGptDiagnostics.report('switcher-resources/theme-controller.js:callback:diagnostics.guard@24', __javascriptError); throw __javascriptError; }});
                const timer = setTimeout(finish, timeoutMilliseconds);
                link.addEventListener("load", finish, { once: true, signal });
                link.addEventListener("error", finish, { once: true, signal });
            } catch (error) {
                diagnostics.report("theme-controller.waitForStylesheet.promise", error);
                throw error;
            }
        });
    } catch (error) {
        diagnostics.report("theme-controller.waitForStylesheet", error);
        throw error;
    }
}

async function updateHighlightTheme(url, signal) {
    try {
        const links = getThemeLinks("hl-theme-link");
        if (!url) {
            links.forEach(diagnostics.guard("theme-controller.removeHighlightLink", link => { try { return (link.remove()); } catch (__javascriptError) { localGptDiagnostics.report('switcher-resources/theme-controller.js:callback:diagnostics.guard@51', __javascriptError); throw __javascriptError; } }));
            return;
        }

        const absoluteUrl = new URL(url, document.baseURI).href;
        let activeLink = links.find(link => { try { return (link.href === absoluteUrl); } catch (__javascriptError) { localGptDiagnostics.report('switcher-resources/theme-controller.js:callback:links.find@56', __javascriptError); throw __javascriptError; } });
        if (!activeLink) {
            activeLink = document.createElement("link");
            activeLink.rel = "stylesheet";
            activeLink.href = url;
            activeLink.setAttribute("hl-theme-link", "");
            document.head.append(activeLink);
        }

        await waitForStylesheet(activeLink, signal);
        if (signal.aborted)
            return;

        links.filter(link => { try { return (link !== activeLink); } catch (__javascriptError) { localGptDiagnostics.report('switcher-resources/theme-controller.js:callback:links.filter@69', __javascriptError); throw __javascriptError; } })
            .forEach(diagnostics.guard("theme-controller.removeInactiveHighlightLink", link => { try { return (link.remove()); } catch (__javascriptError) { localGptDiagnostics.report('switcher-resources/theme-controller.js:callback:diagnostics.guard@70', __javascriptError); throw __javascriptError; } }));
    } catch (error) {
        diagnostics.report("theme-controller.updateHighlightTheme", error);
        throw error;
    }
}

export async function applyThemeState(themeName, bootstrapThemeMode, highlightUrl, reference) {
    try {
        abortController?.abort();
        abortController = new AbortController();
        const signal = abortController.signal;

        await updateHighlightTheme(highlightUrl, signal);
        if (signal.aborted)
            return;

        document.documentElement.setAttribute("data-bs-theme", bootstrapThemeMode || "light");
        document.documentElement.setAttribute("data-localgpt-theme", themeName);
        document.cookie = `ActiveTheme=${encodeURIComponent(themeName)};path=/;max-age=31536000;SameSite=Lax`;

        if (reference)
            await reference.invokeMethodAsync("ThemeLoadedAsync");
    } catch (error) {
        diagnostics.report("theme-controller.applyThemeState", error);
        throw error;
    }
}

export const ThemeController = diagnostics.guardObject("ThemeController", {
    applyThemeState
});
