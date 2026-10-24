// The newest Ollama pull lines stay at the bottom only while the user is tailing.
// Scrolling upward switches to manual inspection; the next batch must not yank it down.
const scrollIntent = new WeakMap();
export function preserveTailScroll(element) {
    if (!element) return;
    let intent = scrollIntent.get(element);
    if (!intent) {
        intent = { follow: true };
        scrollIntent.set(element, intent);
        element.addEventListener('scroll', () => {
            intent.follow = element.scrollHeight - element.scrollTop - element.clientHeight < 48;
        }, { passive: true });
    }
    if (intent.follow) element.scrollTop = element.scrollHeight;
}
