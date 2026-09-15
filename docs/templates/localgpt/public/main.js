// javascript-diagnostics: guarded

function reportDocumentationError(context, error) {
  try {
    const bridge = globalThis.localGptJavaScriptDiagnostics;
    if (bridge?.report) bridge.report('localgpt-docs.' + context, error);
    else console.error('LocalGPT documentation JavaScript error in ' + context + '.', error);
  } catch (reportError) {
    console.error('LocalGPT documentation diagnostics failed.', reportError);
  }
}
const THEME_COOKIE = "localgpt-docs-theme";
const THEME_STORAGE = "localgpt-docs-theme";
const DOCFX_THEME_STORAGE = "theme";
const VALID_THEMES = new Set(["light", "dark", "auto"]);
const THEME_LABELS = {
  light: { label: "Light", icon: "☀️" },
  dark: { label: "Dark", icon: "🌙" },
  auto: { label: "Auto", icon: "◐" }
};
const floaters = ["✨", "✦", "🐾", "🌸", "♡", "⋆", "🎀"];
const pawTrailIcons = ["🐾", "🐾", "🐾", "ฅ^•ﻌ•^ฅ"];

const prefersReducedMotion = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const supportsFinePointer = () => window.matchMedia("(pointer: fine)").matches;

function readCookie(name) {
  try {
      const prefix = `${encodeURIComponent(name)}=`;
      for (const part of document.cookie.split(";")) {
        const value = part.trim();
        if (value.startsWith(prefix)) return decodeURIComponent(value.slice(prefix.length));
      }
      return null;
  } catch (error) {
    reportDocumentationError('readCookie', error);
    throw error;
  }
}

function readStoredTheme() {
  try {
      const cookieTheme = readCookie(THEME_COOKIE);
      if (VALID_THEMES.has(cookieTheme)) return cookieTheme;

      for (const key of [THEME_STORAGE, DOCFX_THEME_STORAGE]) {
        try {
          const value = window.localStorage.getItem(key);
          if (VALID_THEMES.has(value)) return value;
        }
        catch (error) {
        reportDocumentationError('recovered-operation', error);
          // Locked-down WebViews may deny storage. The cookie/system fallback still works.
        }
      }
      return "auto";
  } catch (error) {
    reportDocumentationError('readStoredTheme', error);
    throw error;
  }
}

function resolveTheme(preference) {
  try {
      if (preference === "dark" || preference === "light") return preference;
      return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  } catch (error) {
    reportDocumentationError('resolveTheme', error);
    throw error;
  }
}

function persistTheme(preference) {
  try {
      if (!VALID_THEMES.has(preference)) return;
      try {
        window.localStorage.setItem(THEME_STORAGE, preference);
        window.localStorage.setItem(DOCFX_THEME_STORAGE, preference);
      }
      catch (error) {
        reportDocumentationError('recovered-operation', error);
        // Cookie persistence is still attempted below.
      }

      const secure = window.location.protocol === "https:" ? "; Secure" : "";
      document.cookie = `${encodeURIComponent(THEME_COOKIE)}=${encodeURIComponent(preference)}; Max-Age=31536000; Path=/; SameSite=Lax${secure}`;
  } catch (error) {
    reportDocumentationError('persistTheme', error);
    throw error;
  }
}

function updateThemeControl(preference) {
  try {
      const normalized = VALID_THEMES.has(preference) ? preference : "auto";
      const current = THEME_LABELS[normalized];
      document.querySelectorAll("[data-localgpt-theme-control]").forEach(control => {
        if (!(control instanceof HTMLElement)) return;
        control.dataset.themePreference = normalized;
        const icon = control.querySelector("[data-localgpt-theme-current-icon]");
        const text = control.querySelector("[data-localgpt-theme-current-label]");
        if (icon) icon.textContent = current.icon;
        if (text) text.textContent = current.label;
        control.querySelectorAll("[data-localgpt-theme]").forEach(button => {
          const active = button.getAttribute("data-localgpt-theme") === normalized;
          button.classList.toggle("active", active);
          button.setAttribute("aria-checked", String(active));
        });
      });
  } catch (error) {
    reportDocumentationError('updateThemeControl', error);
    throw error;
  }
}

function applyTheme(preference, persist = false) {
  try {
      const normalized = VALID_THEMES.has(preference) ? preference : "auto";
      document.documentElement.dataset.localgptThemePreference = normalized;
      document.documentElement.setAttribute("data-bs-theme", resolveTheme(normalized));
      updateThemeControl(normalized);
      if (persist) persistTheme(normalized);
  } catch (error) {
    reportDocumentationError('applyTheme', error);
    throw error;
  }
}

function createThemeControl() {
  try {
      const details = document.createElement("details");
      details.className = "localgpt-theme-control";
      details.setAttribute("data-localgpt-theme-control", "true");

      const summary = document.createElement("summary");
      summary.className = "btn border-0 localgpt-theme-control-toggle";
      summary.title = "Choose documentation theme";
      summary.setAttribute("aria-label", "Choose documentation theme");
      summary.innerHTML = '<span data-localgpt-theme-current-icon aria-hidden="true">◐</span><span class="visually-hidden">Theme: <span data-localgpt-theme-current-label>Auto</span></span>';
      details.appendChild(summary);

      const menu = document.createElement("div");
      menu.className = "localgpt-theme-control-menu";
      menu.setAttribute("role", "radiogroup");
      menu.setAttribute("aria-label", "Documentation theme");

      for (const preference of ["light", "dark", "auto"]) {
        const option = THEME_LABELS[preference];
        const button = document.createElement("button");
        button.type = "button";
        button.className = "localgpt-theme-control-option";
        button.setAttribute("data-localgpt-theme", preference);
        button.setAttribute("role", "radio");
        button.setAttribute("aria-checked", "false");
        button.innerHTML = `<span aria-hidden="true">${option.icon}</span><span>${option.label}</span>`;
        menu.appendChild(button);
      }
      details.appendChild(menu);
      return details;
  } catch (error) {
    reportDocumentationError('createThemeControl', error);
    throw error;
  }
}

function mountThemeControl() {
  try {
      const nativeToggle = document.querySelector(
        ".navbar .dropdown > a[title*='theme' i], .navbar .dropdown > button[title*='theme' i], .navbar .dropdown > [aria-label*='theme' i]"
      );
      const nativePicker = nativeToggle?.closest(".dropdown");
      let control = document.querySelector("[data-localgpt-theme-control]");

      if (nativePicker) {
        nativePicker.classList.add("localgpt-native-theme-picker");
        nativePicker.setAttribute("aria-hidden", "true");
        nativePicker.setAttribute("inert", "");
        if (control && control.nextElementSibling !== nativePicker) nativePicker.before(control);
      }

      if (!control) {
        const insertionParent = nativePicker?.parentElement || document.querySelector(".navbar .navbar-collapse, .navbar .container-xxl, .navbar");
        if (!insertionParent) return false;
        control = createThemeControl();
        if (nativePicker) nativePicker.before(control);
        else insertionParent.appendChild(control);
      }

      updateThemeControl(readStoredTheme());
      return true;
  } catch (error) {
    reportDocumentationError('mountThemeControl', error);
    throw error;
  }
}

function installThemePersistence() {
  try {
      if (document.documentElement.dataset.localgptThemePersistence === "true") return;
      document.documentElement.dataset.localgptThemePersistence = "true";

      const initial = readStoredTheme();
      applyTheme(initial, true);

      document.addEventListener("click", event => {
        const option = event.target instanceof Element
          ? event.target.closest("[data-localgpt-theme]")
          : null;
        const requested = option?.getAttribute("data-localgpt-theme");
        if (!VALID_THEMES.has(requested)) return;

        event.preventDefault();
        event.stopPropagation();
        applyTheme(requested, true);
        option.closest("details")?.removeAttribute("open");
      });

      document.addEventListener("click", event => {
        document.querySelectorAll("details[data-localgpt-theme-control][open]").forEach(control => {
          if (event.target instanceof Node && !control.contains(event.target)) control.removeAttribute("open");
        });
      });

      document.addEventListener("keydown", event => {
        if (event.key !== "Escape") return;
        document.querySelectorAll("details[data-localgpt-theme-control][open]").forEach(control => {
          control.removeAttribute("open");
          control.querySelector("summary")?.focus();
        });
      });

      window.addEventListener("storage", event => {
        if (event.key !== THEME_STORAGE && event.key !== DOCFX_THEME_STORAGE) return;
        const value = VALID_THEMES.has(event.newValue) ? event.newValue : readStoredTheme();
        applyTheme(value, true);
      });

      window.matchMedia("(prefers-color-scheme: dark)").addEventListener?.("change", () => {
        if (readStoredTheme() === "auto") applyTheme("auto", false);
      });
  } catch (error) {
    reportDocumentationError('installThemePersistence', error);
    throw error;
  }
}

function watchThemeControl() {
  try {
      if (document.documentElement.dataset.localgptThemeControlWatch === "true") return;
      document.documentElement.dataset.localgptThemeControlWatch = "true";
      let scheduled = false;
      const observer = new MutationObserver(() => {
        if (scheduled) return;
        scheduled = true;
        window.requestAnimationFrame(() => {
          scheduled = false;
          mountThemeControl();
        });
      });
      observer.observe(document.body, { childList: true, subtree: true });
      window.setTimeout(() => observer.disconnect(), 10000);
  } catch (error) {
    reportDocumentationError('watchThemeControl', error);
    throw error;
  }
}

function createKawaiiSky() {
  try {
      if (document.querySelector(".localgpt-kawaii-sky")) return;
      const sky = document.createElement("div");
      sky.className = "localgpt-kawaii-sky";
      sky.setAttribute("aria-hidden", "true");

      // The old sky repeated the same emoji pattern. Build a fresh, bounded field
      // for every page load instead: mostly tiny points, some colored stars and a
      // couple of slow satellites. Each object receives independent timing so the
      // whole background never brightens, dims or drifts in lock-step.
      const compact = window.matchMedia("(max-width: 767.98px)").matches;
      const starCount = compact ? 48 : 112;
      const palette = ["white", "white", "white", "lavender", "pink", "blue", "warm"];
      const randomBetween = (minimum, maximum) => minimum + (Math.random() * (maximum - minimum));

      const nebulaPalette = ["pink", "violet", "blue", "warm"];
      const nebulaCount = compact ? 3 : 5;
      for (let index = 0; index < nebulaCount; index += 1) {
        const nebula = document.createElement("span");
        const tone = nebulaPalette[Math.floor(Math.random() * nebulaPalette.length)];
        nebula.className = `localgpt-kawaii-nebula localgpt-kawaii-nebula-${tone}`;
        nebula.style.setProperty("--localgpt-nebula-left", `${randomBetween(-10, 88).toFixed(2)}%`);
        nebula.style.setProperty("--localgpt-nebula-top", `${randomBetween(-8, 88).toFixed(2)}%`);
        nebula.style.setProperty("--localgpt-nebula-size", `${randomBetween(compact ? 16 : 22, compact ? 34 : 48).toFixed(2)}rem`);
        nebula.style.setProperty("--localgpt-nebula-opacity", randomBetween(0.10, compact ? 0.19 : 0.24).toFixed(3));
        nebula.style.setProperty("--localgpt-nebula-duration", `${randomBetween(38, 82).toFixed(2)}s`);
        nebula.style.setProperty("--localgpt-nebula-delay", `${-randomBetween(0, 42).toFixed(2)}s`);
        nebula.style.setProperty("--localgpt-nebula-dx", `${randomBetween(-42, 46).toFixed(1)}px`);
        nebula.style.setProperty("--localgpt-nebula-dy", `${randomBetween(-30, 34).toFixed(1)}px`);
        sky.appendChild(nebula);
      }

      for (let index = 0; index < starCount; index += 1) {
        const star = document.createElement("span");
        const tone = palette[Math.floor(Math.random() * palette.length)];
        const sparkle = Math.random() < 0.22;
        star.className = `localgpt-kawaii-star localgpt-kawaii-star-${tone}${sparkle ? ` localgpt-kawaii-star-sparkle` : ""}`;

        const maximumOpacity = randomBetween(0.44, sparkle ? 1 : 0.88);
        const minimumOpacity = Math.max(0.10, maximumOpacity * randomBetween(0.20, 0.52));
        star.style.setProperty("--localgpt-star-left", `${randomBetween(1, 99).toFixed(2)}%`);
        star.style.setProperty("--localgpt-star-top", `${randomBetween(2, 98).toFixed(2)}%`);
        star.style.setProperty("--localgpt-star-size", `${randomBetween(sparkle ? 0.18 : 0.08, sparkle ? 0.34 : 0.22).toFixed(3)}rem`);
        star.style.setProperty("--localgpt-star-min-opacity", minimumOpacity.toFixed(3));
        star.style.setProperty("--localgpt-star-max-opacity", maximumOpacity.toFixed(3));
        star.style.setProperty("--localgpt-star-twinkle-duration", `${randomBetween(3.2, 11.5).toFixed(2)}s`);
        star.style.setProperty("--localgpt-star-drift-duration", `${randomBetween(22, 61).toFixed(2)}s`);
        star.style.setProperty("--localgpt-star-delay", `${-randomBetween(0, 28).toFixed(2)}s`);
        star.style.setProperty("--localgpt-star-dx", `${randomBetween(-16, 16).toFixed(1)}px`);
        star.style.setProperty("--localgpt-star-dy", `${randomBetween(-24, 12).toFixed(1)}px`);
        sky.appendChild(star);
      }

      if (!compact && Math.random() < 0.68) {
        const planet = document.createElement("span");
        planet.className = "localgpt-kawaii-planet";
        planet.style.setProperty("--localgpt-planet-left", `${randomBetween(3, 94).toFixed(2)}%`);
        planet.style.setProperty("--localgpt-planet-top", `${randomBetween(8, 88).toFixed(2)}%`);
        planet.style.setProperty("--localgpt-planet-duration", `${randomBetween(44, 78).toFixed(2)}s`);
        planet.style.setProperty("--localgpt-planet-delay", `${-randomBetween(0, 31).toFixed(2)}s`);
        sky.appendChild(planet);
      }

      const satelliteCount = compact ? 1 : 2 + (Math.random() < 0.52 ? 1 : 0);
      for (let index = 0; index < satelliteCount; index += 1) {
        const satellite = document.createElement("span");
        satellite.className = "localgpt-kawaii-satellite";
        satellite.style.setProperty("--localgpt-satellite-left", `${randomBetween(8, 90).toFixed(2)}%`);
        satellite.style.setProperty("--localgpt-satellite-top", `${randomBetween(12, 84).toFixed(2)}%`);
        satellite.style.setProperty("--localgpt-satellite-scale", randomBetween(0.72, 1.08).toFixed(3));
        satellite.style.setProperty("--localgpt-satellite-duration", `${randomBetween(38, 68).toFixed(2)}s`);
        satellite.style.setProperty("--localgpt-satellite-delay", `${-randomBetween(0, 34).toFixed(2)}s`);
        satellite.style.setProperty("--localgpt-satellite-dx", `${randomBetween(-44, 52).toFixed(1)}px`);
        satellite.style.setProperty("--localgpt-satellite-dy", `${randomBetween(-26, 30).toFixed(1)}px`);
        satellite.style.setProperty("--localgpt-satellite-rotate", `${randomBetween(-16, 16).toFixed(1)}deg`);
        satellite.innerHTML = '<span class="localgpt-kawaii-satellite-panel localgpt-kawaii-satellite-panel-left"></span><span class="localgpt-kawaii-satellite-body"></span><span class="localgpt-kawaii-satellite-panel localgpt-kawaii-satellite-panel-right"></span>';
        sky.appendChild(satellite);
      }

      document.body.prepend(sky);
  } catch (error) {
    reportDocumentationError('createKawaiiSky', error);
    throw error;
  }
}

function decorateBrand() {
  try {
      const brand = document.querySelector(".navbar-brand");
      if (!brand) return;
      brand.dataset.localgptCatBrand = "true";
      const logo = brand.querySelector("img#logo, img[src*='logo.svg']");
      if (logo) {
        logo.hidden = false;
        logo.removeAttribute("aria-hidden");
        logo.setAttribute("alt", "LocalGPT cat paw");
      }
      for (const node of [...brand.childNodes]) {
        if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() === "D") node.remove();
      }
  } catch (error) {
    reportDocumentationError('decorateBrand', error);
    throw error;
  }
}

let lastKawaiiHoverTarget = null;
let lastKawaiiHoverAt = 0;

function addKawaiiHoverSprinkle(event) {
  try {
      if (!supportsFinePointer() || prefersReducedMotion()) return;
      const target = event.target instanceof Element ? event.target.closest("a, button, summary, .nav-link") : null;
      if (!target) return;
      if (event.relatedTarget instanceof Node && target.contains(event.relatedTarget)) return;

      const now = Date.now();
      if (target === lastKawaiiHoverTarget && now - lastKawaiiHoverAt < 650) return;
      lastKawaiiHoverTarget = target;
      lastKawaiiHoverAt = now;

      const rect = target.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0) return;
      const icons = ["✦", "⋆", "🐾"];
      const placements = [
        [0.18, 0.16, -5, -10],
        [0.82, 0.18, 6, -13],
        [0.68, 0.84, 4, -8]
      ];
      placements.forEach((placement, index) => {
        const sparkle = document.createElement("span");
        sparkle.className = "localgpt-hover-sparkle";
        sparkle.setAttribute("aria-hidden", "true");
        sparkle.textContent = icons[index % icons.length];
        sparkle.style.left = `${rect.left + rect.width * placement[0]}px`;
        sparkle.style.top = `${rect.top + rect.height * placement[1]}px`;
        sparkle.style.setProperty("--localgpt-hover-dx", `${placement[2]}px`);
        sparkle.style.setProperty("--localgpt-hover-dy", `${placement[3]}px`);
        sparkle.style.setProperty("--localgpt-hover-size", `${0.62 + index * 0.08}rem`);
        document.body.appendChild(sparkle);
        window.setTimeout(() => sparkle.remove(), 900);
      });
  } catch (error) {
    reportDocumentationError('addKawaiiHoverSprinkle', error);
    throw error;
  }
}

function addKawaiiClick(event) {
  try {
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

      if (!supportsFinePointer() || prefersReducedMotion()) return;
      const scratch = document.createElement("span");
      scratch.className = "localgpt-cat-scratch";
      scratch.setAttribute("aria-hidden", "true");
      scratch.style.left = `${event.clientX}px`;
      scratch.style.top = `${event.clientY}px`;
      document.body.appendChild(scratch);
      window.setTimeout(() => scratch.remove(), 850);
  } catch (error) {
    reportDocumentationError('addKawaiiClick', error);
    throw error;
  }
}

function ensureCursorCompanion() {
  try {
      if (!supportsFinePointer() || prefersReducedMotion() || document.querySelector(".localgpt-cursor-paw")) return;
      const paw = document.createElement("span");
      paw.className = "localgpt-cursor-paw";
      paw.setAttribute("aria-hidden", "true");
      paw.textContent = "🐾";
      document.body.appendChild(paw);

      let lastTrailTime = 0;
      document.addEventListener("pointermove", event => {
        paw.style.transform = `translate(${event.clientX}px, ${event.clientY}px)`;
        const now = Date.now();
        if (now - lastTrailTime < 95) return;
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
  } catch (error) {
    reportDocumentationError('ensureCursorCompanion', error);
    throw error;
  }
}

let localgptMermaidModulePromise = null;

function getMermaidTheme() {
  return document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "default";
}

async function recoverMermaidDiagrams() {
  try {
      const rawBlocks = [...document.querySelectorAll("pre > code.lang-mermaid, pre > code.language-mermaid")]
        .filter(code => code.parentElement && !code.parentElement.querySelector("svg"));
      const pendingBlocks = [...document.querySelectorAll(".mermaid[data-mermaid]")]
        .filter(block => !block.querySelector("svg"));
      if (rawBlocks.length === 0 && pendingBlocks.length === 0) return true;
      const visible = rawBlocks.some(code => code.parentElement?.offsetParent !== null) || pendingBlocks.some(block => block.offsetParent !== null);
      if (!visible) return false;
      const themeScript = document.querySelector('script[data-localgpt-kawaii-script]');
      const moduleUrl = new URL("../public/mermaid.core-PFJTYFYY.min.js", themeScript?.src || import.meta.url);
      localgptMermaidModulePromise ??= import(moduleUrl.href);
      const mermaid = (await localgptMermaidModulePromise).default;
      if (!mermaid?.initialize || !mermaid?.run) throw new Error("DocFX Mermaid renderer is unavailable.");
      mermaid.initialize({ startOnLoad: false, theme: getMermaidTheme() });
      const nodes = [];
      for (const code of rawBlocks) {
        const block = code.parentElement;
        if (!block || block.offsetParent === null) continue;
        const source = code.textContent?.trim();
        if (!source) continue;
        block.classList.add("mermaid");
        block.setAttribute("data-mermaid", source);
        block.removeAttribute("data-processed");
        block.textContent = source;
        nodes.push(block);
      }
      for (const block of pendingBlocks) {
        if (block.offsetParent === null || nodes.includes(block)) continue;
        const source = block.getAttribute("data-mermaid")?.trim();
        if (!source) continue;
        block.removeAttribute("data-processed");
        block.textContent = source;
        nodes.push(block);
      }
      if (nodes.length > 0) await mermaid.run({ nodes });
      return ![...document.querySelectorAll("pre > code.lang-mermaid, pre > code.language-mermaid")]
        .some(code => code.parentElement?.offsetParent !== null);
  } catch (error) {
    console.warn("LocalGPT documentation Mermaid recovery could not render yet.", error);
    return false;
  }
}

function scheduleMermaidRecovery() {
  if (document.documentElement.dataset.localgptMermaidRecoveryScheduled === "true") return;
  document.documentElement.dataset.localgptMermaidRecoveryScheduled = "true";
  let attempts = 0;
  let running = false;
  const retry = async () => {
    if (running) return;
    running = true;
    try {
      attempts += 1;
      const complete = await recoverMermaidDiagrams();
      if (complete || attempts >= 60) window.clearInterval(timer);
    } finally { running = false; }
  };
  const timer = window.setInterval(() => void retry(), 500);
  void retry();
  window.addEventListener("pageshow", () => void retry(), { passive: true });
  window.addEventListener("focus", () => void retry(), { passive: true });
  window.addEventListener("resize", () => void retry(), { passive: true });
  document.addEventListener("visibilitychange", () => { if (!document.hidden) void retry(); }, { passive: true });
}

function startKawaiiDocumentation() {
  try {
      document.documentElement.classList.add("localgpt-kawaii-docs");
      installThemePersistence();
      createKawaiiSky();
      scheduleMermaidRecovery();
      decorateBrand();
      ensureCursorCompanion();

      if (document.documentElement.dataset.localgptKawaiiStarted !== "true") {
        document.documentElement.dataset.localgptKawaiiStarted = "true";
        document.addEventListener("click", addKawaiiClick, { passive: true });
        document.addEventListener("pointerover", addKawaiiHoverSprinkle, { passive: true });
      }

      mountThemeControl();
      watchThemeControl();
      window.requestAnimationFrame(() => {
        decorateBrand();
        mountThemeControl();
      });
      window.setTimeout(mountThemeControl, 250);
      window.setTimeout(mountThemeControl, 900);
      window.setTimeout(mountThemeControl, 2200);
  } catch (error) {
    reportDocumentationError('startKawaiiDocumentation', error);
    throw error;
  }
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
