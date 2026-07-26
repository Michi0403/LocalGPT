"use strict";

export const ThemeController = (function () {
    let abortController;

    function getThemeLinks(attributeName) {
        return [...document.querySelectorAll(`link[${attributeName}]`)];
    }

    function waitForStylesheet(link, signal, timeoutMilliseconds = 1500) {
        if (link.sheet)
            return Promise.resolve();

        return new Promise(resolve => {
            let completed = false;
            const finish = () => {
                if (completed)
                    return;
                completed = true;
                clearTimeout(timer);
                link.removeEventListener("load", finish);
                link.removeEventListener("error", finish);
                resolve();
            };
            const timer = setTimeout(finish, timeoutMilliseconds);
            link.addEventListener("load", finish, { once: true, signal });
            link.addEventListener("error", finish, { once: true, signal });
        });
    }

    async function updateHighlightTheme(url, signal) {
        const links = getThemeLinks("hl-theme-link");
        if (!url) {
            links.forEach(link => link.remove());
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

        links.filter(link => link !== activeLink).forEach(link => link.remove());
    }

    async function applyThemeState(themeName, bootstrapThemeMode, highlightUrl, reference) {
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
    }

    return {
        applyThemeState
    };
})();
