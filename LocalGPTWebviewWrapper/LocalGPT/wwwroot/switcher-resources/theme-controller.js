"use strict";

// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const diagnostics = window.localGptJavaScriptDiagnostics || localGptDiagnostics;
let abortController;

function getThemeLinks(attributeName) {
    try {
        return [...document.querySelectorAll(`link[${attributeName}]`)];
    } catch (error) {
        diagnostics.report("theme-controller.getThemeLinks", error);
        throw error;
    }
}

function readCookie(name) {
    try {
        const prefix = `${name}=`;
        for (const entry of document.cookie.split(";")) {
            const cookie = entry.trim();
            if (cookie.startsWith(prefix))
                return decodeURIComponent(cookie.substring(prefix.length));
        }
        return null;
    } catch (error) {
        diagnostics.report("theme-controller.readCookie", error);
        return null;
    }
}

function readStoredValue(key) {
    try {
        return window.localStorage?.getItem(key) || null;
    } catch (error) {
        diagnostics.report("theme-controller.readStoredValue", error);
        return null;
    }
}

function storeValue(key, value) {
    try {
        window.localStorage?.setItem(key, value);
    } catch (error) {
        diagnostics.report("theme-controller.storeValue", error);
    }
}

function persistCookie(name, value) {
    try {
        document.cookie = `${name}=${encodeURIComponent(value)};path=/;max-age=31536000;SameSite=Lax`;
    } catch (error) {
        diagnostics.report("theme-controller.persistCookie", error);
        throw error;
    }
}

export function readThemeState() {
    try {
        const legacyThemeName = readStoredValue("LocalGPT.ActiveTheme") || readCookie("ActiveTheme");
        return {
            shellThemeName: readStoredValue("LocalGPT.ActiveShellTheme")
                || readCookie("ActiveShellTheme")
                || legacyThemeName,
            componentThemeName: readStoredValue("LocalGPT.ActiveComponentTheme")
                || readCookie("ActiveComponentTheme")
                || legacyThemeName
        };
    } catch (error) {
        diagnostics.report("theme-controller.readThemeState", error);
        return { shellThemeName: null, componentThemeName: null };
    }
}

function waitForStylesheet(link, signal, timeoutMilliseconds = 1500) {
    try {
        if (link.sheet)
            return Promise.resolve();

        return new Promise(resolve => {
            try {
                let completed = false;
                const finish = diagnostics.guard("theme-controller.waitForStylesheet.finish", () => {
                    try {
                        if (completed)
                            return;
                        completed = true;
                        clearTimeout(timer);
                        link.removeEventListener("load", finish);
                        link.removeEventListener("error", finish);
                        resolve();
                    } catch (javascriptError) {
                        localGptDiagnostics.report("theme-controller.waitForStylesheet.finish", javascriptError);
                        resolve();
                    }
                });
                const timer = setTimeout(finish, timeoutMilliseconds);
                link.addEventListener("load", finish, { once: true, signal });
                link.addEventListener("error", finish, { once: true, signal });
            } catch (error) {
                diagnostics.report("theme-controller.waitForStylesheet.promise", error);
                resolve();
            }
        });
    } catch (error) {
        diagnostics.report("theme-controller.waitForStylesheet", error);
        return Promise.resolve();
    }
}

async function updateHighlightTheme(url, signal) {
    try {
        const links = getThemeLinks("hl-theme-link");
        if (!url) {
            links.forEach(diagnostics.guard("theme-controller.removeHighlightLink", link => link.remove()));
            return;
        }

        const absoluteUrl = new URL(url, document.baseURI).href;
        let activeLink = links.find(link => link.href === absoluteUrl);
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

        links.filter(link => link !== activeLink)
            .forEach(diagnostics.guard("theme-controller.removeInactiveHighlightLink", link => link.remove()));
    } catch (error) {
        diagnostics.report("theme-controller.updateHighlightTheme", error);
        throw error;
    }
}

function persistThemeState(shellThemeName, componentThemeName) {
    try {
        persistCookie("ActiveShellTheme", shellThemeName);
        persistCookie("ActiveComponentTheme", componentThemeName);
        persistCookie("ActiveTheme", componentThemeName);
        storeValue("LocalGPT.ActiveShellTheme", shellThemeName);
        storeValue("LocalGPT.ActiveComponentTheme", componentThemeName);
        storeValue("LocalGPT.ActiveTheme", componentThemeName);
    } catch (error) {
        diagnostics.report("theme-controller.persistThemeState", error);
        throw error;
    }
}

export async function applyThemeState(
    shellThemeName,
    bootstrapThemeMode,
    highlightUrl,
    componentThemeName,
    callbackTarget,
    reference) {
    try {
        abortController?.abort();
        abortController = new AbortController();
        const signal = abortController.signal;

        await updateHighlightTheme(highlightUrl, signal);
        if (signal.aborted)
            return;

        document.documentElement.setAttribute("data-bs-theme", bootstrapThemeMode || "light");
        document.documentElement.setAttribute("data-localgpt-theme", shellThemeName);
        document.documentElement.setAttribute("data-localgpt-shell-theme", shellThemeName);
        document.documentElement.setAttribute("data-localgpt-component-theme", componentThemeName);
        persistThemeState(shellThemeName, componentThemeName);

        if (reference && callbackTarget)
            await reference.invokeMethodAsync("ThemeLoadedAsync", callbackTarget);
    } catch (error) {
        diagnostics.report("theme-controller.applyThemeState", error);
        throw error;
    }
}

export const ThemeController = diagnostics.guardObject("ThemeController", {
    readThemeState,
    applyThemeState
});
