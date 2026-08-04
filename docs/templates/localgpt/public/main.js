const floaters = ["✨", "🌟", "✦", "🐾", "🐱", "🐶", "🌸", "♡", "⋆", "🎀"];
const pawTrailIcons = ["🐾", "🐾", "🐾", "ฅ^•ﻌ•^ฅ", "🐾"];
const prefersReducedMotion = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const supportsFinePointer = () => window.matchMedia("(pointer: fine)").matches;

function createKawaiiSky() {
  if (document.querySelector(".localgpt-kawaii-sky")) return;
  const sky = document.createElement("div");
  sky.className = "localgpt-kawaii-sky";
  sky.setAttribute("aria-hidden", "true");
  const count = window.matchMedia("(max-width: 767.98px)").matches ? 14 : 30;
  for (let index = 0; index < count; index += 1) {
    const item = document.createElement("span");
    item.className = "localgpt-kawaii-floater";
    item.textContent = floaters[index % floaters.length];
    item.style.setProperty("--localgpt-left", `${(index * 37 + 7) % 98}%`);
    item.style.setProperty("--localgpt-top", `${(index * 53 + 11) % 96}%`);
    item.style.setProperty("--localgpt-size", `${0.72 + ((index * 13) % 9) / 10}rem`);
    item.style.setProperty("--localgpt-opacity", `${0.12 + ((index * 17) % 22) / 100}`);
    item.style.setProperty("--localgpt-duration", `${12 + ((index * 19) % 15)}s`);
    item.style.setProperty("--localgpt-delay", `${-((index * 7) % 17)}s`);
    item.style.setProperty("--localgpt-rotate", `${(index * 29) % 42 - 21}deg`);
    sky.appendChild(item);
  }
  document.body.prepend(sky);
}

function addKawaiiClick(event) {
  const target = event.target instanceof Element ? event.target.closest("a, button, .nav-link") : null;
  if (target && !prefersReducedMotion()) {
    const pop = document.createElement("span");
    pop.className = "localgpt-kawaii-pop";
    pop.setAttribute("aria-hidden", "true");
    pop.textContent = floaters[Math.floor(Math.random() * floaters.length)];
    pop.style.setProperty("--localgpt-pop-x", `${event.clientX}px`);
    pop.style.setProperty("--localgpt-pop-y", `${event.clientY}px`);
    document.body.appendChild(pop);
    window.setTimeout(() => pop.remove(), 1000);
  }

  if (!supportsFinePointer()) return;

  const scratch = document.createElement("span");
  scratch.className = "localgpt-cat-scratch";
  scratch.setAttribute("aria-hidden", "true");
  scratch.style.left = `${event.clientX}px`;
  scratch.style.top = `${event.clientY}px`;
  document.body.appendChild(scratch);
  window.setTimeout(() => scratch.remove(), 850);
}

function decorateBrand() {
  const brand = document.querySelector(".navbar-brand");
  if (!brand) return;

  for (const element of brand.querySelectorAll(":scope > img, :scope > svg, :scope > .logo, :scope > [class*='logo']")) {
    element.setAttribute("aria-hidden", "true");
    element.hidden = true;
  }

  for (const node of [...brand.childNodes]) {
    if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() === "D") {
      node.remove();
    }
  }

  brand.dataset.localgptCatBrand = "true";
}

function applyPreferredTheme() {
  const root = document.documentElement;
  if (!root) return;

  const storageKeys = ["theme", "docfx-theme", "localgpt-docs-theme"];
  let chosenTheme = null;
  for (const key of storageKeys) {
    try {
      const value = window.localStorage.getItem(key);
      if (value === "dark" || value === "light") {
        chosenTheme = value;
        break;
      }
    }
    catch {
      // Ignore storage access issues inside locked-down WebViews.
    }
  }

  if (!chosenTheme) {
    chosenTheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }

  root.setAttribute("data-bs-theme", chosenTheme);
}

function decorateThemeToggle() {
  const selectors = [
    "button[aria-label*='theme' i]",
    "button[title*='theme' i]",
    "button[data-bs-theme-value]",
    ".theme",
    ".theme-picker",
    ".theme-toggle",
    ".theme-switcher"
  ];

  let foundToggle = false;
  for (const selector of selectors) {
    for (const element of document.querySelectorAll(selector)) {
      if (!(element instanceof HTMLElement)) continue;
      foundToggle = true;
      element.classList.add("localgpt-theme-toggle");
      if (!element.getAttribute("title") && !element.getAttribute("aria-label")) {
        element.setAttribute("title", "Toggle light and dark mode");
      }
      element.removeAttribute("aria-hidden");
      if (element.tabIndex < 0) {
        element.tabIndex = 0;
      }
    }
  }

  const navbar = document.querySelector(".navbar .navbar-nav, .navbar .buttons, .navbar .d-flex, .navbar");
  if (navbar && !document.querySelector(".localgpt-heart-glimmer")) {
    const heart = document.createElement("span");
    heart.className = "localgpt-heart-glimmer";
    heart.setAttribute("aria-hidden", "true");
    heart.textContent = foundToggle ? "💖✨🌙" : "💖✨♡";
    navbar.appendChild(heart);
  }
}

function ensureCursorCompanion() {
  if (!supportsFinePointer() || prefersReducedMotion()) return;
  if (document.querySelector(".localgpt-cursor-paw")) return;

  const paw = document.createElement("span");
  paw.className = "localgpt-cursor-paw";
  paw.setAttribute("aria-hidden", "true");
  paw.textContent = "🐾";
  document.body.appendChild(paw);

  let lastTrailTime = 0;
  document.addEventListener("pointermove", (event) => {
    paw.style.transform = `translate(${event.clientX}px, ${event.clientY}px)`;
    const now = Date.now();
    if (now - lastTrailTime < 85) return;
    lastTrailTime = now;

    const trail = document.createElement("span");
    trail.className = "localgpt-paw-trail";
    trail.setAttribute("aria-hidden", "true");
    trail.textContent = pawTrailIcons[Math.floor(Math.random() * pawTrailIcons.length)];
    trail.style.left = `${event.clientX}px`;
    trail.style.top = `${event.clientY}px`;
    trail.style.setProperty("--localgpt-trail-rotate", `${Math.round(Math.random() * 34 - 17)}deg`);
    document.body.appendChild(trail);
    window.setTimeout(() => trail.remove(), 1200);
  }, { passive: true });
}

function decorateDocumentation() {
  document.documentElement.classList.add("localgpt-kawaii-docs");
  applyPreferredTheme();
  createKawaiiSky();
  decorateBrand();
  decorateThemeToggle();
  ensureCursorCompanion();

  if (document.documentElement.dataset.localgptKawaiiStarted !== "true") {
    document.documentElement.dataset.localgptKawaiiStarted = "true";
    document.addEventListener("click", addKawaiiClick, { passive: true });
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener?.("change", () => {
      applyPreferredTheme();
    });
  }
}

function startKawaiiDocumentation() {
  decorateDocumentation();
  window.requestAnimationFrame(decorateBrand);
  window.setTimeout(decorateBrand, 250);
}

export default {
  iconLinks: [
    {
      icon: "github",
      href: "https://github.com/Michi0403/LocalGPT",
      title: "LocalGPT on GitHub"
    }
  ],
  start: startKawaiiDocumentation
};


if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", startKawaiiDocumentation, { once: true });
}
else {
  startKawaiiDocumentation();
}
