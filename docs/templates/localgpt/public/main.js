const floaters = ["✨", "🌟", "✦", "🐾", "🐱", "🐶", "🌸", "♡", "⋆", "🎀"];

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
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  const target = event.target instanceof Element ? event.target.closest("a, button, .nav-link") : null;
  if (!target) return;
  const pop = document.createElement("span");
  pop.className = "localgpt-kawaii-pop";
  pop.setAttribute("aria-hidden", "true");
  pop.textContent = floaters[Math.floor(Math.random() * floaters.length)];
  pop.style.setProperty("--localgpt-pop-x", `${event.clientX}px`);
  pop.style.setProperty("--localgpt-pop-y", `${event.clientY}px`);
  document.body.appendChild(pop);
  window.setTimeout(() => pop.remove(), 1000);
}

function decorateDocumentation() {
  if (document.documentElement.dataset.localgptKawaiiStarted === "true") return;
  document.documentElement.dataset.localgptKawaiiStarted = "true";
  document.documentElement.classList.add("localgpt-kawaii-docs");
  createKawaiiSky();
  document.addEventListener("click", addKawaiiClick, { passive: true });
}

export default {
  defaultTheme: "light",
  iconLinks: [
    {
      icon: "github",
      href: "https://github.com/Michi0403/LocalGPT",
      title: "LocalGPT on GitHub"
    }
  ],
  start: decorateDocumentation
};
