const previousFocus = new WeakMap();
const callbacks = new WeakMap();

function focusCloseButton(dialog) {
  window.requestAnimationFrame(() => dialog.querySelector("[data-documentation-viewer-close]")?.focus());
}

export function connect(dialog, callback) {
  if (!dialog || callbacks.has(dialog)) return;
  callbacks.set(dialog, callback);
  dialog.addEventListener("cancel", event => {
    event.preventDefault();
    callback.invokeMethodAsync("CloseFromBrowser");
  });
  dialog.addEventListener("click", event => {
    if (event.target === dialog) callback.invokeMethodAsync("CloseFromBrowser");
  });
}

export function show(dialog) {
  if (!dialog) return;
  if (!dialog.open) {
    previousFocus.set(dialog, document.activeElement);
    dialog.showModal();
  }
  focusCloseButton(dialog);
}

export function close(dialog) {
  if (!dialog) return;
  if (dialog.open) dialog.close();
  const previous = previousFocus.get(dialog);
  previousFocus.delete(dialog);
  if (previous instanceof HTMLElement && previous.isConnected) previous.focus();
}

export function disconnect(dialog) {
  callbacks.delete(dialog);
  previousFocus.delete(dialog);
}
