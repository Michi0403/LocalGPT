// javascript-diagnostics: guarded
const localGptDocumentationDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
  report(context, error) {
    try { console.error(`LocalGPT JavaScript error in ${String(context || "documentation-viewer")}.`, error); }
    catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); }
  }
};

const previousFocus = new WeakMap();
const callbacks = new WeakMap();

function report(context, error) {
  try { localGptDocumentationDiagnostics.report(`js/documentationViewer.js:${context}`, error); }
  catch (reportError) { console.error("LocalGPT documentation viewer diagnostics failed.", reportError); }
}

function focusCloseButton(dialog) {
  try {
    window.requestAnimationFrame(() => {
      try { dialog?.querySelector("[data-documentation-viewer-close]")?.focus(); }
      catch (error) { report("focusCloseButton.callback", error); }
    });
  } catch (error) {
    report("focusCloseButton", error);
    throw error;
  }
}

export function connect(dialog, callback) {
  try {
    if (!dialog || !callback || callbacks.has(dialog)) return;
    callbacks.set(dialog, callback);
    dialog.addEventListener("cancel", event => {
      try {
        event.preventDefault();
        void callback.invokeMethodAsync("CloseFromBrowser").catch(error => report("cancel.invoke", error));
      } catch (error) { report("cancel", error); }
    });
    dialog.addEventListener("click", event => {
      try {
        if (event.target === dialog) void callback.invokeMethodAsync("CloseFromBrowser").catch(error => report("backdrop.invoke", error));
      } catch (error) { report("backdrop", error); }
    });
  } catch (error) {
    report("connect", error);
    throw error;
  }
}

export function show(dialog) {
  try {
    if (!dialog) return;
    if (!dialog.open) {
      previousFocus.set(dialog, document.activeElement);
      dialog.showModal();
    }
    focusCloseButton(dialog);
  } catch (error) {
    report("show", error);
    throw error;
  }
}

export function close(dialog) {
  try {
    if (!dialog) return;
    if (dialog.open) dialog.close();
    const previous = previousFocus.get(dialog);
    previousFocus.delete(dialog);
    if (previous instanceof HTMLElement && previous.isConnected) previous.focus();
  } catch (error) {
    report("close", error);
    throw error;
  }
}

export function disconnect(dialog) {
  try {
    callbacks.delete(dialog);
    previousFocus.delete(dialog);
  } catch (error) {
    report("disconnect", error);
    throw error;
  }
}
