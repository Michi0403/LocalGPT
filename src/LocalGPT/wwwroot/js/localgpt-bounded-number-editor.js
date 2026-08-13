(() => {
    "use strict";

    const viewportMargin = 8;

    window.localGptBoundedNumberEditor = {
        clamp(element) {
            if (!(element instanceof HTMLElement))
                return;

            element.style.setProperty("--bounded-number-shift-x", "0px");
            const rect = element.getBoundingClientRect();
            const viewportWidth = document.documentElement.clientWidth || window.innerWidth;
            let shift = 0;

            if (rect.right > viewportWidth - viewportMargin)
                shift -= rect.right - (viewportWidth - viewportMargin);
            if (rect.left + shift < viewportMargin)
                shift += viewportMargin - (rect.left + shift);

            element.style.setProperty("--bounded-number-shift-x", `${Math.round(shift)}px`);
        }
    };
})();
