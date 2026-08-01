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
const fusionRouteStorageKey = "LocalGPT.ThemeFusionRoute";
const maxFusionRouteSteps = 256;

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
        return readStoredValue(key) === value;
    } catch (error) {
        diagnostics.report("theme-controller.storeValue", error);
        return false;
    }
}

function removeStoredValue(key) {
    try {
        window.localStorage?.removeItem(key);
        return readStoredValue(key) === null;
    } catch (error) {
        diagnostics.report("theme-controller.removeStoredValue", error);
        return false;
    }
}

function sanitizeFusionRoute(route) {
    try {
        if (!Array.isArray(route))
            return [];

        return route
            .slice(-maxFusionRouteSteps)
            .map((step, index) => {
                const rawTarget = step?.target ?? step?.Target;
                const target = rawTarget === 0 || rawTarget === "0" || rawTarget === "Shell"
                    ? "Shell"
                    : rawTarget === 1 || rawTarget === "1" || rawTarget === "Components"
                        ? "Components"
                        : "";
                const themeName = String(step?.themeName || step?.ThemeName || "").trim();
                if (!target || !themeName)
                    return null;

                return {
                    sequence: index + 1,
                    target,
                    themeName
                };
            })
            .filter(Boolean);
    } catch (error) {
        diagnostics.report("theme-controller.sanitizeFusionRoute", error);
        return [];
    }
}

function readFusionRoute() {
    try {
        const storedRoute = readStoredValue(fusionRouteStorageKey);
        if (!storedRoute)
            return [];

        return sanitizeFusionRoute(JSON.parse(storedRoute));
    } catch (error) {
        diagnostics.report("theme-controller.readFusionRoute", error);
        return [];
    }
}

export function persistFusionRoute(route) {
    try {
        const sanitizedRoute = sanitizeFusionRoute(route);
        const serializedRoute = JSON.stringify(sanitizedRoute);
        if (!storeValue(fusionRouteStorageKey, serializedRoute))
            throw new Error("The browser rejected Theme Fusion route persistence.");

        return sanitizedRoute;
    } catch (error) {
        diagnostics.report("theme-controller.persistFusionRoute", error);
        throw error;
    }
}

function persistCookie(name, value) {
    try {
        document.cookie = `${name}=${encodeURIComponent(value)};path=/;max-age=31536000;SameSite=Lax`;
        return readCookie(name) === value;
    } catch (error) {
        diagnostics.report("theme-controller.persistCookie", error);
        return false;
    }
}

export function readThemeState() {
    try {
        const legacyThemeName = readCookie("ActiveTheme") || readStoredValue("LocalGPT.ActiveTheme");
        return {
            shellThemeName: readCookie("ActiveShellTheme")
                || readStoredValue("LocalGPT.ActiveShellTheme")
                || legacyThemeName,
            componentThemeName: readCookie("ActiveComponentTheme")
                || readStoredValue("LocalGPT.ActiveComponentTheme")
                || legacyThemeName,
            fusionRoute: readFusionRoute()
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
        const shellCookieSaved = persistCookie("ActiveShellTheme", shellThemeName);
        const componentCookieSaved = persistCookie("ActiveComponentTheme", componentThemeName);
        const legacyCookieSaved = persistCookie("ActiveTheme", componentThemeName);
        const shellStorageSaved = storeValue("LocalGPT.ActiveShellTheme", shellThemeName);
        const componentStorageSaved = storeValue("LocalGPT.ActiveComponentTheme", componentThemeName);
        const legacyStorageSaved = storeValue("LocalGPT.ActiveTheme", componentThemeName);

        const shellSaved = shellCookieSaved || shellStorageSaved;
        const componentSaved = componentCookieSaved || componentStorageSaved;
        if (!shellSaved || !componentSaved) {
            throw new Error("The browser rejected both cookie and local-storage theme persistence.");
        }

        return {
            shellSaved,
            componentSaved,
            legacySaved: legacyCookieSaved || legacyStorageSaved
        };
    } catch (error) {
        diagnostics.report("theme-controller.persistThemeState", error);
        throw error;
    }
}

export function resetFusionRoute(shellThemeName, componentThemeName) {
    try {
        persistThemeState(shellThemeName, componentThemeName);
        if (!removeStoredValue(fusionRouteStorageKey))
            throw new Error("The browser rejected Theme Fusion route removal.");

        // Reloading is deliberate: it removes runtime theme resources accumulated by the old
        // selection route while retaining the currently selected Base Theme and Style Layer.
        window.setTimeout(() => window.location.reload(), 0);
        return true;
    } catch (error) {
        diagnostics.report("theme-controller.resetFusionRoute", error);
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

        // Apply and persist first. Highlight stylesheet loading is asynchronous and must never
        // be able to cancel the selected shell/component values or make the UI look unchanged.
        document.documentElement.setAttribute("data-bs-theme", bootstrapThemeMode || "light");
        document.documentElement.setAttribute("data-localgpt-theme", shellThemeName);
        document.documentElement.setAttribute("data-localgpt-shell-theme", shellThemeName);
        document.documentElement.setAttribute("data-localgpt-component-theme", componentThemeName);
        persistThemeState(shellThemeName, componentThemeName);

        if (reference && callbackTarget)
            await reference.invokeMethodAsync("ThemeLoadedAsync", callbackTarget);

        await updateHighlightTheme(highlightUrl, signal);
    } catch (error) {
        diagnostics.report("theme-controller.applyThemeState", error);
        throw error;
    }
}

export const ThemeController = diagnostics.guardObject("ThemeController", {
    readThemeState,
    applyThemeState,
    persistFusionRoute,
    resetFusionRoute
});
