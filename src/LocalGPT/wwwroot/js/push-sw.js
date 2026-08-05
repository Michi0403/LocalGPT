// javascript-diagnostics: guarded-service-worker

function reportServiceWorkerError(context, error) {
    try {
        const message = error instanceof Error ? error.message : String(error ?? "Unknown service-worker error.");
        console.error(`LocalGPT service-worker JavaScript error in ${String(context || "push-worker")}: ${message}`, error);
    } catch (reportError) {
        console.error("LocalGPT service-worker diagnostics failed while reporting an error.", reportError);
    }
}

function guardServiceWorkerCallback(context, callback) {
    try {
        return function (...args) {
            try {
                const result = callback.apply(this, args);
                if (result && typeof result.then === "function") {
                    return result.catch(error => { try {
                        reportServiceWorkerError(context, error);
                        throw error;
                     } catch (__javascriptError) { reportServiceWorkerError('js/push-sw.js:callback:result.catch@18', __javascriptError); throw __javascriptError; }});
                }
                return result;
            } catch (error) {
                reportServiceWorkerError(context, error);
                throw error;
            }
        };
    } catch (error) {
        reportServiceWorkerError("guardServiceWorkerCallback", error);
        return callback;
    }
}

self.addEventListener("error", guardServiceWorkerCallback("service-worker.error", event => {
    try {
        reportServiceWorkerError(event?.filename ? `${event.filename}:${event.lineno || 0}:${event.colno || 0}` : "service-worker.error", event?.error || event?.message);
    } catch (error) {
        reportServiceWorkerError("service-worker.error-handler", error);
    }
}));

self.addEventListener("unhandledrejection", guardServiceWorkerCallback("service-worker.unhandledrejection", event => {
    try {
        reportServiceWorkerError("service-worker.unhandledrejection", event?.reason);
    } catch (error) {
        reportServiceWorkerError("service-worker.unhandledrejection-handler", error);
    }
}));

self.addEventListener("install", guardServiceWorkerCallback("push.install", event => {
    try {
        event.waitUntil(self.skipWaiting());
    } catch (error) {
        reportServiceWorkerError("push.install", error);
        throw error;
    }
}));

self.addEventListener("activate", guardServiceWorkerCallback("push.activate", event => {
    try {
        event.waitUntil(self.clients.claim());
    } catch (error) {
        reportServiceWorkerError("push.activate", error);
        throw error;
    }
}));

self.addEventListener("push", guardServiceWorkerCallback("push.message", event => {
    try {
        let payload = {};
        try {
            payload = event.data?.json() ?? {};
        } catch (error) {
            reportServiceWorkerError("push.message.payload", error);
        }

        event.waitUntil(self.registration.showNotification(payload.title || "🔔", {
            body: payload.body || "",
            icon: payload.icon || "/android-chrome-192x192.png",
            badge: payload.badge || "/favicon-32x32.png",
            image: payload.image,
            tag: payload.tag,
            renotify: Boolean(payload.renotify),
            requireInteraction: Boolean(payload.requireInteraction),
            actions: payload.actions || [],
            data: payload.data || {}
        }));
    } catch (error) {
        reportServiceWorkerError("push.message", error);
        throw error;
    }
}));

async function recordPushReaction(data, action) {
    try {
        if (!data.eventId) return;
        await fetch("/api/push/reaction", {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ eventId: data.eventId, action })
        });
    } catch (error) {
        reportServiceWorkerError("push.notificationclick.record-reaction", error);
        throw error;
    }
}

async function performPushAction(spec) {
    try {
        if (!spec.api?.url) return;
        await fetch(spec.api.url, {
            method: spec.api.method || "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: spec.api.body ? JSON.stringify(spec.api.body) : undefined
        });
    } catch (error) {
        reportServiceWorkerError("push.notificationclick.perform-action", error);
        throw error;
    }
}

async function focusOrOpenApplication(navigationUrl) {
    try {
        const windows = await clients.matchAll({ type: "window", includeUncontrolled: true });
        for (const applicationWindow of windows) {
            try {
                await applicationWindow.navigate(navigationUrl);
                return await applicationWindow.focus();
            } catch (error) {
                reportServiceWorkerError("push.notificationclick.focus-window", error);
            }
        }
        return clients.openWindow ? await clients.openWindow(navigationUrl) : undefined;
    } catch (error) {
        reportServiceWorkerError("push.notificationclick.open-application", error);
        throw error;
    }
}

self.addEventListener("notificationclick", guardServiceWorkerCallback("push.notificationclick", event => {
    try {
        event.notification.close();
        const action = event.action || "default";
        const data = event.notification.data || {};
        const actionMap = data.actions || {};
        const actionSpecification = actionMap[action] || {};
        const navigationUrl = actionSpecification.navigate || data.defaultUrl || "/";

        event.waitUntil((async () => {
            try {
                try {
                    await recordPushReaction(data, action);
                } catch (error) {
                    reportServiceWorkerError("push.notificationclick.record-reaction-boundary", error);
                }
                try {
                    await performPushAction(actionSpecification);
                } catch (error) {
                    reportServiceWorkerError("push.notificationclick.perform-action-boundary", error);
                }
                return await focusOrOpenApplication(navigationUrl);
            } catch (error) {
                reportServiceWorkerError("push.notificationclick.workflow", error);
                throw error;
            }
        })());
    } catch (error) {
        reportServiceWorkerError("push.notificationclick", error);
        throw error;
    }
}));

self.addEventListener("notificationclose", guardServiceWorkerCallback("push.notificationclose", event => {
    try {
        const { eventId } = event.notification.data || {};
        if (!eventId) return;
        event.waitUntil(fetch("/api/push/reaction", {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ eventId, action: "dismiss" })
        }).catch(error => { try {
            reportServiceWorkerError("push.notificationclose.record-reaction", error);
         } catch (__javascriptError) { reportServiceWorkerError('js/push-sw.js:callback:fetch("/api/push/reaction", { method: "POST", credentials: "include", @185', __javascriptError); throw __javascriptError; }}));
    } catch (error) {
        reportServiceWorkerError("push.notificationclose", error);
        throw error;
    }
}));
