(() => {
    const menuId = "localgpt-context-menu";
    const editableSelector = "input, textarea, select, [contenteditable='true'], [role='textbox'], .dxbl-ai-chat";
    const links = [
        ["Home", "/"],
        ["Chat", "/Chat"],
        ["AI Council", "/model-council"],
        ["Projects", "/projects"],
        ["Approvals & MFA", "/onewire-security"],
        ["Council teams", "/council-teams"]
    ];

    function getMenu() {
        let menu = document.getElementById(menuId);
        if (menu) return menu;
        menu = document.createElement("nav");
        menu.id = menuId;
        menu.className = "localgpt-context-menu";
        menu.setAttribute("aria-label", "LocalGPT context menu");
        menu.hidden = true;
        for (const [label, href] of links) {
            const anchor = document.createElement("a");
            anchor.textContent = label;
            anchor.href = href;
            menu.appendChild(anchor);
        }
        document.body.appendChild(menu);
        return menu;
    }

    function close() {
        const menu = document.getElementById(menuId);
        if (menu) menu.hidden = true;
    }

    document.addEventListener("contextmenu", event => {
        if (event.target instanceof Element && event.target.closest(editableSelector)) return;
        event.preventDefault();
        const menu = getMenu();
        menu.hidden = false;
        const maxX = Math.max(8, window.innerWidth - menu.offsetWidth - 8);
        const maxY = Math.max(8, window.innerHeight - menu.offsetHeight - 8);
        menu.style.left = `${Math.min(event.clientX, maxX)}px`;
        menu.style.top = `${Math.min(event.clientY, maxY)}px`;
    });
    document.addEventListener("click", close);
    document.addEventListener("keydown", event => { if (event.key === "Escape") close(); });
    window.addEventListener("blur", close);
    window.addEventListener("resize", close);
})();
