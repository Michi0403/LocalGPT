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

let localgptKawaiiMeteorTimer = 0;

function createKawaiiShootingStar(sky) {
  try {
      if (!(sky instanceof HTMLElement) || prefersReducedMotion()) return;
      const compact = window.matchMedia("(max-width: 767.98px)").matches;
      const randomBetween = (minimum, maximum) => minimum + (Math.random() * (maximum - minimum));
      const meteor = document.createElement("span");
      meteor.className = "localgpt-kawaii-shooting-star";
      meteor.setAttribute("aria-hidden", "true");
      meteor.style.setProperty("--localgpt-meteor-left", `${randomBetween(compact ? -6 : -10, compact ? 70 : 82).toFixed(2)}%`);
      meteor.style.setProperty("--localgpt-meteor-top", `${randomBetween(4, compact ? 52 : 58).toFixed(2)}%`);
      meteor.style.setProperty("--localgpt-meteor-length", `${randomBetween(compact ? 3.8 : 4.8, compact ? 6.2 : 8.8).toFixed(2)}rem`);
      meteor.style.setProperty("--localgpt-meteor-thickness", `${randomBetween(0.10, 0.18).toFixed(3)}rem`);
      meteor.style.setProperty("--localgpt-meteor-duration", `${randomBetween(0.95, 1.75).toFixed(2)}s`);
      meteor.style.setProperty("--localgpt-meteor-delay", `${randomBetween(0, 0.24).toFixed(2)}s`);
      meteor.style.setProperty("--localgpt-meteor-dx", `${randomBetween(compact ? 180 : 240, compact ? 320 : 460).toFixed(1)}px`);
      meteor.style.setProperty("--localgpt-meteor-dy", `${randomBetween(compact ? 120 : 170, compact ? 240 : 310).toFixed(1)}px`);
      meteor.style.setProperty("--localgpt-meteor-rotate", `${randomBetween(24, 34).toFixed(1)}deg`);
      meteor.style.setProperty("--localgpt-meteor-scale", randomBetween(0.88, 1.16).toFixed(3));
      meteor.style.setProperty("--localgpt-meteor-peak-opacity", `${randomBetween(0.78, 0.98).toFixed(3)}`);
      sky.appendChild(meteor);
      meteor.addEventListener("animationend", () => meteor.remove(), { once: true });
  } catch (error) {
    reportDocumentationError('createKawaiiShootingStar', error);
    throw error;
  }
}

function scheduleKawaiiShootingStars(sky) {
  try {
      if (!(sky instanceof HTMLElement) || prefersReducedMotion()) return;
      if (sky.dataset.localgptMeteorSchedule === "true") return;
      sky.dataset.localgptMeteorSchedule = "true";
      const randomBetween = (minimum, maximum) => minimum + (Math.random() * (maximum - minimum));
      const compact = () => window.matchMedia("(max-width: 767.98px)").matches;

      const planNext = (minimumDelay, maximumDelay) => {
        window.clearTimeout(localgptKawaiiMeteorTimer);
        localgptKawaiiMeteorTimer = window.setTimeout(() => {
          localgptKawaiiMeteorTimer = 0;
          if (!document.body?.contains(sky)) {
            sky.dataset.localgptMeteorSchedule = "";
            return;
          }
          if (!document.hidden && !prefersReducedMotion()) {
            createKawaiiShootingStar(sky);
            if (!compact() && Math.random() < 0.08) {
              window.setTimeout(() => {
                if (document.body?.contains(sky) && !document.hidden && !prefersReducedMotion()) createKawaiiShootingStar(sky);
              }, randomBetween(420, 960));
            }
          }
          planNext(compact() ? 36000 : 30000, compact() ? 84000 : 72000);
        }, randomBetween(minimumDelay, maximumDelay));
      };

      planNext(compact() ? 18000 : 12000, compact() ? 42000 : 30000);
      document.addEventListener("visibilitychange", () => {
        if (document.hidden) return;
        if (document.body?.contains(sky) && !prefersReducedMotion() && localgptKawaiiMeteorTimer === 0) {
          planNext(12000, 28000);
        }
      }, { passive: true });
  } catch (error) {
    reportDocumentationError('scheduleKawaiiShootingStars', error);
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

      // Nebula depth is painted by compositor-safe CSS radial gradients. Avoid huge
      // blur-filter DOM layers here: Chromium can rasterize those as visible square tiles.

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
        star.style.setProperty("--localgpt-star-drift-duration", `${randomBetween(12, 34).toFixed(2)}s`);
        star.style.setProperty("--localgpt-star-delay", `${-randomBetween(0, 24).toFixed(2)}s`);
        star.style.setProperty("--localgpt-star-dx", `${randomBetween(-38, 38).toFixed(1)}px`);
        star.style.setProperty("--localgpt-star-dy", `${randomBetween(-48, 28).toFixed(1)}px`);
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

      // Satellites are guaranteed rather than probabilistic. On wide screens they live
      // primarily in the side gutters so the central documentation cards do not hide them.
      const satelliteCount = compact ? 1 : 2;
      for (let index = 0; index < satelliteCount; index += 1) {
        const satellite = document.createElement("span");
        satellite.className = "localgpt-kawaii-satellite";
        const satelliteLeft = compact
          ? randomBetween(5, 88)
          : index % 2 === 0 ? randomBetween(3, 15) : randomBetween(85, 97);
        satellite.style.setProperty("--localgpt-satellite-left", `${satelliteLeft.toFixed(2)}%`);
        satellite.style.setProperty("--localgpt-satellite-top", `${randomBetween(14, 78).toFixed(2)}%`);
        satellite.style.setProperty("--localgpt-satellite-scale", randomBetween(0.92, 1.32).toFixed(3));
        satellite.style.setProperty("--localgpt-satellite-duration", `${randomBetween(24, 42).toFixed(2)}s`);
        satellite.style.setProperty("--localgpt-satellite-delay", `${-randomBetween(0, 24).toFixed(2)}s`);
        const satelliteDx = index % 2 === 0 ? randomBetween(120, 250) : randomBetween(-250, -120);
        satellite.style.setProperty("--localgpt-satellite-dx", `${satelliteDx.toFixed(1)}px`);
        satellite.style.setProperty("--localgpt-satellite-dy", `${randomBetween(-84, 84).toFixed(1)}px`);
        satellite.style.setProperty("--localgpt-satellite-rotate", `${randomBetween(-16, 16).toFixed(1)}deg`);
        satellite.innerHTML = '<span class="localgpt-kawaii-satellite-panel localgpt-kawaii-satellite-panel-left"></span><span class="localgpt-kawaii-satellite-body"></span><span class="localgpt-kawaii-satellite-panel localgpt-kawaii-satellite-panel-right"></span>';
        sky.appendChild(satellite);
      }

      document.body.prepend(sky);
      scheduleKawaiiShootingStars(sky);
      document.documentElement.dataset.localgptDynamicSky = "ready";
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

function ensureKawaiiPointerOverlay() {
  try {
      let overlay = document.querySelector(".localgpt-pointer-overlay");
      if (overlay instanceof HTMLElement) return overlay;
      if (!document.body) return null;
      overlay = document.createElement("div");
      overlay.className = "localgpt-pointer-overlay";
      overlay.setAttribute("aria-hidden", "true");
      document.body.prepend(overlay);
      return overlay;
  } catch (error) {
    reportDocumentationError('ensureKawaiiPointerOverlay', error);
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

      const overlay = ensureKawaiiPointerOverlay();
      if (!overlay) return;
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
        overlay.appendChild(sparkle);
        window.setTimeout(() => sparkle.remove(), 900);
      });
  } catch (error) {
    reportDocumentationError('addKawaiiHoverSprinkle', error);
    throw error;
  }
}

function addKawaiiClick(event) {
  try {
      const overlay = ensureKawaiiPointerOverlay();
      if (!overlay) return;
      const target = event.target instanceof Element ? event.target.closest("a, button, .nav-link") : null;
      if (target && !prefersReducedMotion()) {
        const pop = document.createElement("span");
        pop.className = "localgpt-kawaii-pop";
        pop.setAttribute("aria-hidden", "true");
        pop.textContent = floaters[Math.floor(Math.random() * floaters.length)];
        pop.style.setProperty("--localgpt-pop-x", `${event.clientX}px`);
        pop.style.setProperty("--localgpt-pop-y", `${event.clientY}px`);
        overlay.appendChild(pop);
        window.setTimeout(() => pop.remove(), 1000);
      }

      if (!supportsFinePointer() || prefersReducedMotion()) return;
      const scratch = document.createElement("span");
      scratch.className = "localgpt-cat-scratch";
      scratch.setAttribute("aria-hidden", "true");
      scratch.style.left = `${event.clientX}px`;
      scratch.style.top = `${event.clientY}px`;
      overlay.appendChild(scratch);
      window.setTimeout(() => scratch.remove(), 850);
  } catch (error) {
    reportDocumentationError('addKawaiiClick', error);
    throw error;
  }
}

function ensureCursorCompanion() {
  try {
      if (!supportsFinePointer() || prefersReducedMotion()) return;
      const overlay = ensureKawaiiPointerOverlay();
      if (!overlay || overlay.querySelector(".localgpt-cursor-paw")) return;
      const paw = document.createElement("span");
      paw.className = "localgpt-cursor-paw";
      paw.setAttribute("aria-hidden", "true");
      paw.innerHTML = makeKawaiiGalaxyPaw();
      overlay.appendChild(paw);

      let lastTrailTime = 0;
      document.addEventListener("pointermove", event => {
        const x = Math.max(0, Math.min(window.innerWidth - 1, event.clientX));
        const y = Math.max(0, Math.min(window.innerHeight - 1, event.clientY));
        paw.style.left = `${x}px`;
        paw.style.top = `${y}px`;
        paw.style.opacity = "1";
        const now = Date.now();
        if (now - lastTrailTime < 95) return;
        lastTrailTime = now;
        const trail = document.createElement("span");
        trail.className = "localgpt-paw-trail";
        trail.setAttribute("aria-hidden", "true");
        trail.textContent = pawTrailIcons[Math.floor(Math.random() * pawTrailIcons.length)];
        trail.style.left = `${x}px`;
        trail.style.top = `${y}px`;
        trail.style.setProperty("--localgpt-trail-rotate", `${Math.round(Math.random() * 34 - 17)}deg`);
        overlay.appendChild(trail);
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

function clippedMermaidNodeLabels(block) {
  // Actual rendered label extents, never guesses from character count.
  // Supports text/tspan and foreignObject Mermaid node renderers.
  const svg = block.querySelector('svg');
  if (!svg) return [];
  return [...svg.querySelectorAll('.node')].flatMap(node => {
    const shape = node.querySelector('rect, polygon, ellipse, path');
    const label = node.querySelector('.label, .nodeLabel, foreignObject, text');
    if (!shape || !label) return [];
    const outline = shape.getBoundingClientRect();
    const measured = label.getBoundingClientRect();
    if (!outline.width || !outline.height || !measured.width || !measured.height) return [];
    const allowance = 3;
    const clipped = measured.left < outline.left - allowance || measured.right > outline.right + allowance
      || measured.top < outline.top - allowance || measured.bottom > outline.bottom + allowance;
    return clipped ? [{ node, shape, label, outline, measured }] : [];
  });
}

function hasClippedMermaidLabels(block) {
  return clippedMermaidNodeLabels(block).length > 0;
}

function fitAlreadyRenderedMermaidLabels(block) {
  // DocFX may have rendered Mermaid before our recovery hook, consuming the
  // original source. Instead of resetting the diagram with missing source,
  // scale only overflowing text inside its existing SVG node boxes.
  for (const entry of clippedMermaidNodeLabels(block)) {
    const text = entry.node.querySelector('text')
      || entry.node.querySelector('foreignObject .nodeLabel')
      || entry.node.querySelector('foreignObject div');
    if (!text) continue;
    const ratio = Math.min(1, (entry.outline.width - 8) / entry.measured.width,
      (entry.outline.height - 7) / entry.measured.height);
    const size = Number.parseFloat(window.getComputedStyle(text).fontSize);
    if (!Number.isFinite(size) || ratio <= 0 || ratio >= 1) continue;
    // At least 9px remains readable; a warning still reports more severe overflow.
    text.style.fontSize = `${Math.max(9, size * ratio)}px`;
  }
  return !hasClippedMermaidLabels(block);
}

async function recoverMermaidDiagrams() {
  try {
      // Convert any legacy DocFX code fences into attached Mermaid nodes before Mermaid
      // measures labels. Rendering detached SVG/HTML labels caused getBoundingClientRect
      // failures inside embedded documentation iframes.
      const rawBlocks = [...document.querySelectorAll("pre > code.lang-mermaid, pre > code.language-mermaid")];
      for (const code of rawBlocks) {
        const pre = code.parentElement;
        const source = code.textContent?.trim();
        if (!pre || !source) continue;
        const wrapper = document.createElement("div");
        wrapper.className = `mermaid localgpt-mermaid-diagram`;
        wrapper.setAttribute("data-localgpt-mermaid-source", source);
        wrapper.textContent = source;
        pre.replaceWith(wrapper);
      }

      const pendingBlocks = [...document.querySelectorAll(".mermaid")]
        .filter(block => !block.querySelector("svg") && block.getAttribute("data-processed") !== "true");
      for (const block of pendingBlocks) {
        // Preserve the author-owned Mermaid source for a one-time layout recovery.
        if (!block.getAttribute('data-localgpt-mermaid-source'))
          block.setAttribute('data-localgpt-mermaid-source', block.textContent || '');
      }
      const renderedBlocks = [...document.querySelectorAll('.mermaid')]
        .filter(block => block.querySelector('svg'));
      const desktop = window.matchMedia('(min-width: 768px)').matches;
      if (pendingBlocks.length === 0 && (!desktop || !renderedBlocks.some(hasClippedMermaidLabels))) return true;

      const themeScript = document.querySelector('script[data-localgpt-kawaii-script]');
      const moduleUrl = new URL("../public/mermaid.core-PFJTYFYY.min.js", themeScript?.src || import.meta.url);
      localgptMermaidModulePromise ??= import(moduleUrl.href);
      const mermaid = (await localgptMermaidModulePromise).default;
      if (!mermaid?.initialize || !mermaid?.run) throw new Error("DocFX Mermaid renderer is unavailable.");
      if (document.fonts?.ready) {
        try { await document.fonts.ready; } catch { /* use the browser fallback font metrics */ }
      }
      mermaid.initialize({
        startOnLoad: false,
        theme: getMermaidTheme(),
        securityLevel: "strict",
        flowchart: { htmlLabels: false, useMaxWidth: true, wrappingWidth: 180 }
      });

      if (pendingBlocks.length) await mermaid.run({ nodes: pendingBlocks, suppressErrors: false });
      // Inspect even pre-rendered DocFX diagrams; otherwise a clipped SVG can
      // mistakenly be marked complete before this recovery hook runs.
      if (desktop) {
        const clipped = [...document.querySelectorAll('.mermaid')].filter(hasClippedMermaidLabels);
        if (clipped.length) {
          mermaid.initialize({
            startOnLoad: false,
            theme: getMermaidTheme(),
            securityLevel: 'strict',
            flowchart: { htmlLabels: true, useMaxWidth: false, wrappingWidth: 230 }
          });
          for (const block of clipped) {
            const oldMarkup = block.innerHTML;
            const source = block.getAttribute('data-localgpt-mermaid-source');
            if (!source) {
              // An already-rendered DocFX diagram can have no recoverable source.
              // Never wipe its SVG; correct its measured labels in place.
              if (!fitAlreadyRenderedMermaidLabels(block))
                console.warn('localgpt diagram has unrecoverable label overflow.');
              continue;
            }
            try {
              block.textContent = source;
              block.removeAttribute('data-processed');
              await mermaid.run({ nodes: [block], suppressErrors: false });
              if (!block.querySelector('svg') || hasClippedMermaidLabels(block)) {
                // Do not sacrifice a usable diagram for an unsuccessful recovery.
                block.innerHTML = oldMarkup;
                block.setAttribute('data-processed', 'true');
                if (!fitAlreadyRenderedMermaidLabels(block))
                  console.warn('localgpt Mermaid diagram label repair was inconclusive.');
              }
            } catch (error) {
              block.innerHTML = oldMarkup;
              block.setAttribute('data-processed', 'true');
              reportDocumentationError('mermaidLabelRecovery', error);
            }
          }
        }
      }
      for (const block of pendingBlocks) {
        block.classList.add("localgpt-mermaid-diagram");
        if (block.querySelector("svg")) block.setAttribute("data-localgpt-mermaid-rendered", "true");
      }
      return pendingBlocks.every(block => block.querySelector("svg"));
  } catch (error) {
    console.warn("LocalGPT documentation Mermaid recovery could not render yet.", error);
    return false;
  }
}

function scheduleMermaidRecovery() {
  if (document.documentElement.dataset.localgptMermaidRecoveryScheduled === "true") return;
  document.documentElement.dataset.localgptMermaidRecoveryScheduled = "true";
  const retryDelays = [0, 300, 900, 1800];
  let attempt = 0;
  let running = false;

  const retry = async () => {
    if (running || attempt >= retryDelays.length) return;
    running = true;
    try {
      const complete = await recoverMermaidDiagrams();
      attempt += 1;
      if (!complete && attempt < retryDelays.length)
        window.setTimeout(() => void retry(), retryDelays[attempt]);
    } finally {
      running = false;
    }
  };

  window.setTimeout(() => void retry(), retryDelays[0]);
  // DocFX's own Mermaid renderer may finish after the first recovery check.
  // Bounded later audits cover that race without observing/rebuilding the DOM.
  const lateAudit = async () => {
    if (running) return;
    running = true;
    try { await recoverMermaidDiagrams(); }
    finally { running = false; }
  };
  window.setTimeout(() => void lateAudit(), 750);
  window.setTimeout(() => void lateAudit(), 2200);
  let wasDesktop = window.matchMedia('(min-width: 768px)').matches;
  window.addEventListener('resize', () => {
    const desktop = window.matchMedia('(min-width: 768px)').matches;
    if (wasDesktop !== desktop) {
      wasDesktop = desktop;
      window.setTimeout(() => void lateAudit(), 100);
    }
  }, { passive: true });
}

// The documentation tear is ephemeral presentation state. It never edits DocFX nodes or sources.
let kawaiiTearSession = null;
let kawaiiScratchAudio = null;

function makeKawaiiGalaxyPaw() {
  // Theme-aware, inline SVG: the paw remains part of the pointer overlay, not the DocFX content mask.
  // The fur silhouette gives the pastel toe beans contrast on light surfaces without changing the scratch renderer.
  return `<svg viewBox="0 0 92 92" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" focusable="false">
    <defs>
      <linearGradient id="localgpt-paw-gradient" x1=".15" y1=".1" x2=".88" y2=".92"><stop stop-color="#ffd0ef"/><stop offset=".38" stop-color="#f48ecf"/><stop offset=".7" stop-color="#bd83ff"/><stop offset="1" stop-color="#7adff9"/></linearGradient>
      <radialGradient id="localgpt-paw-shine"><stop stop-color="#fff" stop-opacity=".72"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
    </defs>
    <path class="localgpt-paw-fur" d="M22 51 Q16 48 13 56 Q9 62 15 69 Q11 79 22 83 Q30 89 43 85 Q53 91 64 84 Q76 84 78 72 Q84 63 77 54 Q70 47 62 52 Q47 44 36 50 Q28 45 22 51 Z"/>
    <g fill="url(#localgpt-paw-gradient)" stroke="#fce8ff" stroke-opacity=".88" stroke-width="1.7">
      <ellipse cx="19" cy="37" rx="8" ry="11" transform="rotate(-24 19 37)"/>
      <ellipse cx="36" cy="23" rx="8" ry="11" transform="rotate(-10 36 23)"/>
      <ellipse cx="56" cy="24" rx="8" ry="11" transform="rotate(12 56 24)"/>
      <ellipse cx="72" cy="37" rx="8" ry="11" transform="rotate(26 72 37)"/>
      <path d="M25 67 C24 57 34 48 39 43 Q46 39 52 43 C59 48 69 58 68 67 C67 78 58 80 48 77 C37 74 26 81 25 67 Z"/>
    </g>
    <ellipse cx="35" cy="55" rx="18" ry="11" fill="url(#localgpt-paw-shine)" opacity=".58"/>
    <g fill="#fff" opacity=".86"><circle cx="33" cy="18" r="2"/><circle cx="61" cy="56" r="1.7"/><circle cx="46" cy="68" r="1.4"/></g>
    <g class="localgpt-paw-fur-detail" fill="none" stroke-linecap="round" stroke-width="1.1">
      <path d="M18 73 l-3 3 M23 79 l-2 4 M31 83 l-1 3 M62 82 l2 3 M70 77 l4 3 M75 65 l4 1"/>
    </g>
  </svg>`;
}

function kawaiiTearTarget() {
  // The DocFX content surface is the only destructible region. Chrome, navigation,
  // live editor islands, and the existing cursor/scratch overlay are never masked.
  return document.querySelector('main.container-xxl > .content')
    || document.querySelector('main .content article')
    || document.querySelector('article.content');
}

function kawaiiTearPoints(points) {
  const first = points[0];
  const last = points[points.length - 1];
  const input = points.length > 1 && Math.hypot(last.x - first.x, last.y - first.y) > 5
    ? points : [{ x: first.x - 9, y: first.y + 11 }, { x: first.x + 9, y: first.y - 11 }];
  // Even a short click produces fine tapering instead of a two-vertex diamond.
  // Resampling also prevents long pointer-event gaps from forming polygonal loops.
  const result = [input[0]];
  for (let i = 1; i < input.length; i += 1) {
    const before = input[i - 1];
    const after = input[i];
    const distance = Math.hypot(after.x - before.x, after.y - before.y);
    const steps = Math.max(1, Math.min(24, Math.ceil(distance / 6)));
    for (let step = 1; step <= steps; step += 1) {
      const t = step / steps;
      result.push({ x: before.x + (after.x - before.x) * t,
        y: before.y + (after.y - before.y) * t });
    }
  }
  return result;
}

const KAWAII_CLAW_SPACING = 16;

function kawaiiTearGeometry(points, claw) {
  const path = kawaiiTearPoints(points);
  const left = [];
  const right = [];
  for (let i = 0; i < path.length; i += 1) {
    const point = path[i];
    const before = path[Math.max(0, i - 1)];
    const after = path[Math.min(path.length - 1, i + 1)];
    const dx = after.x - before.x;
    const dy = after.y - before.y;
    const len = Math.max(1, Math.hypot(dx, dy));
    const tx = dx / len;
    const ty = dy / len;
    const nx = -ty;
    const ny = tx;
    // Fine deterministic irregularity; no random jitter on re-render.
    const jag = Math.sin(i * 2.13 + claw * 3.29 + point.x * .049) * .58
      + Math.sin(i * .57 + point.y * .085 + claw) * .42;
    const taper = Math.min(1, .25 + i * .30, .25 + (path.length - 1 - i) * .30);
    const occasionalNick = i % 17 === (claw * 3 + 5) ? .95 : 0;
    const widthLeft = taper * Math.max(.65, 2.45 + jag + occasionalNick);
    const widthRight = taper * Math.max(.65, 2.25 - jag * .7 + (i % 19 === 7 ? .65 : 0));
    const cx = point.x + nx * (claw - 1) * KAWAII_CLAW_SPACING;
    const cy = point.y + ny * (claw - 1) * KAWAII_CLAW_SPACING;
    left.push({ x: cx + nx * widthLeft, y: cy + ny * widthLeft, nx, ny, tx, ty });
    right.push({ x: cx - nx * widthRight, y: cy - ny * widthRight, nx, ny, tx, ty });
  }
  return { left, right, claw };
}

function kawaiiTearCoords(points) {
  return points.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' L');
}

function kawaiiTearOutline(geometry) {
  return `M${kawaiiTearCoords(geometry.left)} L${kawaiiTearCoords([...geometry.right].reverse())} Z`;
}

// Raised paper rims start at the ACTUAL mask boundary, and lift only 1-2px.
// This prevents thick overlapping SVG bands when the user scribbles repeatedly.
function kawaiiTearPaperLips(geometry) {
  const strips = [];
  for (const [side, edge] of [[1, geometry.left], [-1, geometry.right]]) {
    const lifted = edge.map((p, i) => {
      const fold = i === 0 || i === edge.length - 1 ? .45
        : 1.1 + Math.sin(i * .77 + geometry.claw) * .36;
      return { x: p.x + p.nx * side * fold,
        y: p.y + p.ny * side * fold };
    });
    const d = `M${kawaiiTearCoords(edge)} L${kawaiiTearCoords(lifted.reverse())} Z`;
    strips.push(`<path class="localgpt-tear-paper-lip" d="${d}"/>`);
  }
  return strips.join('');
}

function kawaiiTearFlaps(geometry) {
  const details = [];
  const edges = [geometry.left, geometry.right];
  // Sparse 3-6px peeling corners, rather than the old 20px paper triangles.
  for (let i = 6; i < geometry.left.length - 3; i += 14) {
    const side = (i + geometry.claw) % 2;
    const edge = edges[side];
    const p = edge[i];
    const nx = p.nx * (side === 0 ? 1 : -1);
    const ny = p.ny * (side === 0 ? 1 : -1);
    const span = 2.6 + (i % 3) * 1.2;
    const lift = 2.8 + (i % 4) * .8;
    const startX = p.x - p.tx * span;
    const startY = p.y - p.ty * span;
    const endX = p.x + p.tx * span * .8;
    const endY = p.y + p.ty * span * .8;
    const tipX = p.x + nx * lift + p.tx;
    const tipY = p.y + ny * lift + p.ty;
    const d = `M${startX.toFixed(1)},${startY.toFixed(1)} L${tipX.toFixed(1)},${tipY.toFixed(1)} L${endX.toFixed(1)},${endY.toFixed(1)} Z`;
    details.push(`<path class="localgpt-tear-flap-shadow" d="${d}"/><path class="localgpt-tear-flap" d="${d}"/>`);
    details.push(`<path class="localgpt-tear-fold" d="M${startX.toFixed(1)},${startY.toFixed(1)} L${endX.toFixed(1)},${endY.toFixed(1)}"/>`);
    // Two thin, short exposed fibres per occasional peeled corner.
    for (let fiber = 0; fiber < 2; fiber += 1) {
      const x = p.x + p.tx * (fiber ? 1.4 : -1.4);
      const y = p.y + p.ty * (fiber ? 1.4 : -1.4);
      const tipFx = x + nx * (2.2 + fiber * .7) + p.tx * (fiber ? .5 : -.6);
      const tipFy = y + ny * (2.2 + fiber * .7) + p.ty * (fiber ? .5 : -.6);
      details.push(`<path class="localgpt-tear-fiber" d="M${x.toFixed(1)},${y.toFixed(1)} L${tipFx.toFixed(1)},${tipFy.toFixed(1)}"/>`);
    }
  }
  return details.join('');
}

function createKawaiiTearSession(target) {
  const layer = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  layer.setAttribute('class', 'localgpt-tear-overlay');
  layer.setAttribute('aria-hidden', 'true');
  layer.setAttribute('focusable', 'false');
  layer.innerHTML = `<defs><linearGradient id="localgpt-tear-space-dark" x1="0" y1="0" x2=".8" y2="1"><stop stop-color="#10051f"/><stop offset=".28" stop-color="#7b28b0"/><stop offset=".57" stop-color="#29115b"/><stop offset=".83" stop-color="#1688bb"/><stop offset="1" stop-color="#130d33"/></linearGradient><linearGradient id="localgpt-tear-space-light" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#ffffff"/><stop offset=".18" stop-color="#fff8ff"/><stop offset=".42" stop-color="#efe2ff"/><stop offset=".68" stop-color="#e5fbff"/><stop offset=".88" stop-color="#fff1fb"/><stop offset="1" stop-color="#ffffff"/></linearGradient><linearGradient id="localgpt-tear-flap-shade" x1="0" y1="0" x2=".85" y2="1"><stop stop-color="#fff9ff"/><stop offset=".27" stop-color="#d9c6df"/><stop offset=".6" stop-color="#9b80ac"/><stop offset="1" stop-color="#493556"/></linearGradient></defs><g class="localgpt-tear-marks"></g>`;
  document.body.appendChild(layer);
  return { target, layer, marks: layer.querySelector('.localgpt-tear-marks'),
    scratches: [], active: null, frame: 0, lastPaint: 0, lastSound: 0,
    initialMask: target.style.maskImage, initialWebkitMask: target.style.webkitMaskImage,
    initialMaskSize: target.style.maskSize, initialWebkitMaskSize: target.style.webkitMaskSize,
    initialMaskRepeat: target.style.maskRepeat, initialWebkitMaskRepeat: target.style.webkitMaskRepeat };
}

function clearKawaiiTearSession() {
  removeKawaiiTouchPaw();
  if (!kawaiiTearSession) return;
  const session = kawaiiTearSession;
  if (session.frame) window.cancelAnimationFrame(session.frame);
  session.target.style.maskImage = session.initialMask;
  session.target.style.webkitMaskImage = session.initialWebkitMask;
  session.target.style.maskSize = session.initialMaskSize;
  session.target.style.webkitMaskSize = session.initialWebkitMaskSize;
  session.target.style.maskRepeat = session.initialMaskRepeat;
  session.target.style.webkitMaskRepeat = session.initialWebkitMaskRepeat;
  session.layer.remove();
  kawaiiTearSession = null;
}

function paintKawaiiTears() {
  try {
    const session = kawaiiTearSession;
    if (!session || !session.target.isConnected) { clearKawaiiTearSession(); return; }
    session.frame = 0;
    const rect = session.target.getBoundingClientRect();
    const width = Math.max(1, Math.ceil(rect.width));
    const height = Math.max(1, Math.ceil(rect.height));
    session.layer.style.left = `${rect.left}px`;
    session.layer.style.top = `${rect.top}px`;
    session.layer.setAttribute('width', String(width));
    session.layer.setAttribute('height', String(height));
    session.layer.setAttribute('viewBox', `0 0 ${width} ${height}`);
    const geometries = session.scratches.flatMap(scratch => [0, 1, 2].map(claw => kawaiiTearGeometry(scratch, claw)));
    const shapes = geometries.map(kawaiiTearOutline);
    if (shapes.length === 0) return;
    // Alpha-mask data image has transparent cutouts. Real text, diagrams, code,
    // pictures and backdrop in this DocFX content subtree vanish through them.
    const paths = shapes.map(path => `<path d="${path}" fill="black"/>`).join('');
    const mask = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}"><defs><mask id="cut"><rect width="${width}" height="${height}" fill="white"/>${paths}</mask></defs><rect width="${width}" height="${height}" fill="white" mask="url(#cut)"/></svg>`;
    const url = `url("data:image/svg+xml,${encodeURIComponent(mask)}")`;
    session.target.style.maskImage = url;
    session.target.style.webkitMaskImage = url;
    session.target.style.maskSize = '100% 100%';
    session.target.style.webkitMaskSize = '100% 100%';
    session.target.style.maskRepeat = 'no-repeat';
    session.target.style.webkitMaskRepeat = 'no-repeat';
    // Separate transparent SVG paints the void, shredded contour and front glow.
    // It never intercepts events and doesn't affect document flow.
    session.marks.innerHTML = shapes.map(path => `<path class="localgpt-tear-shadow" d="${path}"/><path class="localgpt-tear-bloom" d="${path}"/><path class="localgpt-tear-void" d="${path}"/>`).join('')
      + geometries.map(kawaiiTearPaperLips).join('')
      + shapes.map(path => `<path class="localgpt-tear-paper" d="${path}"/><path class="localgpt-tear-rim" d="${path}"/>`).join('')
      + geometries.map(kawaiiTearFlaps).join('');
  } catch (error) {
    reportDocumentationError('paintKawaiiTears', error);
    clearKawaiiTearSession();
  }
}

function scheduleKawaiiTearPaint(immediate = false) {
  const session = kawaiiTearSession;
  if (!session || session.frame) return;
  session.frame = window.requestAnimationFrame(time => {
    if (!immediate && time - session.lastPaint < 50) {
      session.frame = 0;
      window.setTimeout(() => scheduleKawaiiTearPaint(true), 50);
      return;
    }
    session.lastPaint = time;
    paintKawaiiTears();
  });
}

function playKawaiiScratchSound(dragging = false) {
  try {
    if (prefersReducedMotion()) return;
    const Audio = window.AudioContext || window.webkitAudioContext;
    if (!Audio) return;
    kawaiiScratchAudio ||= new Audio();
    if (kawaiiScratchAudio.state === 'suspended') void kawaiiScratchAudio.resume();
    const ctx = kawaiiScratchAudio;
    const length = .085 + Math.random() * (dragging ? .065 : .045);
    const buffer = ctx.createBuffer(1, Math.round(ctx.sampleRate * length), ctx.sampleRate);
    const samples = buffer.getChannelData(0);
    for (let i = 0; i < samples.length; i += 1) samples[i] = (Math.random() * 2 - 1) * (1 - i / samples.length);
    const noise = ctx.createBufferSource();
    noise.buffer = buffer;
    const filter = ctx.createBiquadFilter();
    filter.type = 'bandpass';
    filter.frequency.value = 680 + Math.random() * 1300;
    filter.Q.value = .7 + Math.random();
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(.001, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(.035 + Math.random() * .025, ctx.currentTime + .012);
    gain.gain.exponentialRampToValueAtTime(.001, ctx.currentTime + length);
    noise.connect(filter).connect(gain).connect(ctx.destination);
    noise.start();
    noise.stop(ctx.currentTime + length);
  } catch (error) {
    reportDocumentationError('playKawaiiScratchSound', error);
  }
}

let kawaiiTouchPaw = null;

function removeKawaiiTouchPaw() {
  kawaiiTouchPaw?.remove();
  kawaiiTouchPaw = null;
}

function moveKawaiiTouchPaw(x, y) {
  if (!kawaiiTouchPaw) {
    const overlay = ensureKawaiiPointerOverlay();
    if (!overlay) return;
    kawaiiTouchPaw = document.createElement('span');
    kawaiiTouchPaw.className = 'localgpt-cursor-paw localgpt-touch-paw';
    kawaiiTouchPaw.setAttribute('aria-hidden', 'true');
    kawaiiTouchPaw.innerHTML = makeKawaiiGalaxyPaw();
    overlay.appendChild(kawaiiTouchPaw);
  }
  kawaiiTouchPaw.style.left = `${x}px`;
  kawaiiTouchPaw.style.top = `${y}px`;
  kawaiiTouchPaw.style.opacity = '1';
}

function installKawaiiTouchScratchToggle() {
  if (!window.matchMedia('(any-pointer: coarse)').matches || prefersReducedMotion()) return;
  if (document.querySelector('.localgpt-touch-scratch-toggle')) return;
  const control = document.createElement('button');
  control.type = 'button';
  control.className = 'localgpt-touch-scratch-toggle';
  control.setAttribute('aria-pressed', 'false');
  control.setAttribute('aria-label', 'Enable cat scratching with your finger');
  control.textContent = '🐾 Scratch';
  control.addEventListener('click', () => {
    const enabled = document.documentElement.classList.toggle('localgpt-touch-scratch-active');
    control.setAttribute('aria-pressed', String(enabled));
    control.setAttribute('aria-label', enabled ? 'Return to normal scrolling' : 'Enable cat scratching with your finger');
    control.textContent = enabled ? '↕ Scroll' : '🐾 Scratch';
    if (!enabled) {
      if (kawaiiTearSession) kawaiiTearSession.active = null;
      removeKawaiiTouchPaw();
    }
  });
  document.body.appendChild(control);
}

function installKawaiiTearInteraction() {
  if (document.documentElement.dataset.localgptTearInstalled === 'true') return;
  document.documentElement.dataset.localgptTearInstalled = 'true';
  // Left-button pointer paths accumulate for the current document only.
  document.addEventListener('pointerdown', event => {
    try {
      const isTouch = event.pointerType === 'touch';
      const touchDrawing = isTouch && document.documentElement.classList.contains('localgpt-touch-scratch-active');
      if (event.button !== 0 || !event.isPrimary || event.shiftKey || prefersReducedMotion()) return;
      if (isTouch ? !touchDrawing : !(event.pointerType === 'pen' || supportsFinePointer())) return;
      if (event.target instanceof Element && event.target.closest('a,button,input,textarea,select,summary,[contenteditable="true"]')) return;
      const target = kawaiiTearTarget();
      if (!target || !(event.target instanceof Node) || !target.contains(event.target)) return;
      const rect = target.getBoundingClientRect();
      if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) return;
      event.preventDefault(); // Drag makes a tear, not an accidental text selection. Shift+drag still selects text.
      if (kawaiiTearSession && kawaiiTearSession.target !== target) clearKawaiiTearSession();
      const session = kawaiiTearSession || (kawaiiTearSession = createKawaiiTearSession(target));
      const points = [{ x: event.clientX - rect.left, y: event.clientY - rect.top }];
      session.scratches.push(points);
      session.active = { pointerId: event.pointerId, points };
      if (isTouch) moveKawaiiTouchPaw(event.clientX, event.clientY);
      playKawaiiScratchSound();
      scheduleKawaiiTearPaint(true);
    } catch (error) { reportDocumentationError('tearPointerDown', error); }
  }, { passive: false });
  document.addEventListener('pointermove', event => {
    try {
      const session = kawaiiTearSession;
      if (!session?.active || session.active.pointerId !== event.pointerId) return;
      if (event.pointerType !== 'touch' && (event.buttons & 1) === 0) return;
      if (event.pointerType === 'touch') moveKawaiiTouchPaw(event.clientX, event.clientY);
      const rect = session.target.getBoundingClientRect();
      const points = session.active.points;
      const x = Math.max(0, Math.min(rect.width, event.clientX - rect.left));
      const y = Math.max(0, Math.min(rect.height, event.clientY - rect.top));
      const previous = points[points.length - 1];
      if (Math.hypot(x - previous.x, y - previous.y) < 6) return;
      points.push({ x, y });
      // Streamline very long drags without truncating their damage.
      if (points.length > 200) points.splice(1, 1);
      scheduleKawaiiTearPaint();
      if (performance.now() - session.lastSound > 230) {
        session.lastSound = performance.now();
        playKawaiiScratchSound(true);
      }
    } catch (error) { reportDocumentationError('tearPointerMove', error); }
  }, { passive: true });
  const endTear = event => {
    try {
      const session = kawaiiTearSession;
      if (!session?.active || session.active.pointerId !== event.pointerId) return;
      session.active = null;
      removeKawaiiTouchPaw();
      scheduleKawaiiTearPaint(true);
    } catch (error) { reportDocumentationError('tearPointerEnd', error); }
  };
  document.addEventListener('pointerup', endTear, { passive: true });
  document.addEventListener('pointercancel', endTear, { passive: true });
  document.addEventListener('dragstart', event => {
    if (kawaiiTearSession?.active && kawaiiTearSession.target.contains(event.target)) event.preventDefault();
  });
  // DocFX normally navigates by full document load; handle history/hash routes too.
  window.addEventListener('popstate', clearKawaiiTearSession);
  window.addEventListener('hashchange', clearKawaiiTearSession);
  window.addEventListener('pagehide', clearKawaiiTearSession);
  window.addEventListener('blur', () => { if (kawaiiTearSession) kawaiiTearSession.active = null; removeKawaiiTouchPaw(); });
  window.addEventListener('scroll', () => {
    const session = kawaiiTearSession;
    if (!session || !session.target.isConnected) return;
    const rect = session.target.getBoundingClientRect();
    session.layer.style.left = `${rect.left}px`;
    session.layer.style.top = `${rect.top}px`;
  }, { passive: true });
  window.addEventListener('resize', () => { if (kawaiiTearSession) scheduleKawaiiTearPaint(true); });
}

function startKawaiiDocumentation() {
  try {
      document.documentElement.classList.add("localgpt-kawaii-docs");
      installThemePersistence();
      createKawaiiSky();
      scheduleMermaidRecovery();
      decorateBrand();
      ensureCursorCompanion();
      installKawaiiTearInteraction();
      installKawaiiTouchScratchToggle();

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
