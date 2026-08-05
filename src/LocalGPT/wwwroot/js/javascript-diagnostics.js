(() => { try {
    "use strict";

    // javascript-diagnostics: guarded-runtime
    let dotNetReference = null;
    let reporting = false;
    const pendingReports = new Map();
    const wrappedCallbacks = new WeakMap();

    function errorDetails(error) {
        try {
            const normalized = error instanceof Error ? error : new Error(String(error ?? "Unknown JavaScript error."));
            return {
                message: normalized.message || String(error ?? "Unknown JavaScript error."),
                stack: normalized.stack || ""
            };
        } catch (detailsError) {
            console.error("LocalGPT JavaScript diagnostics could not normalize an error.", detailsError);
            return { message: String(error ?? "Unknown JavaScript error."), stack: "" };
        }
    }

    function queueReport(source, details) { try {
        try {
            pendingReports.set(`${source}\n${details.message}`, { source, message: details.message, stack: details.stack });
        } catch (queueError) {
            console.error("LocalGPT JavaScript diagnostics could not buffer an error before the application logger attached.", queueError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:queueReport.', __javascriptError); throw __javascriptError; }}

    function flushPendingReports() { try {
        try {
            if (!dotNetReference || reporting || pendingReports.size === 0) return;
            const first = pendingReports.entries().next();
            if (first.done) return;
            pendingReports.delete(first.value[0]);
            forward(first.value[1].source, first.value[1]);
        } catch (flushError) {
            console.error("LocalGPT JavaScript diagnostics could not flush a buffered error.", flushError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:flushPendingReports.', __javascriptError); throw __javascriptError; }}

    function forward(source, details) { try {
        try {
            if (!dotNetReference || reporting) {
                queueReport(source, details);
                return;
            }

            reporting = true;
            dotNetReference.invokeMethodAsync("ReportJavaScriptErrorAsync", source, details.message, details.stack)
                .catch(reportError => { try {
                    console.error("LocalGPT JavaScript diagnostics could not forward an error to the application logger.", reportError);
                 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:forward.catch.', __javascriptError); throw __javascriptError; }})
                .finally(() => { try {
                    reporting = false;
                    flushPendingReports();
                 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:forward.finally.', __javascriptError); throw __javascriptError; }});
        } catch (forwardError) {
            reporting = false;
            console.error("LocalGPT JavaScript diagnostics failed while forwarding an error.", forwardError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:forward.', __javascriptError); throw __javascriptError; }}

    function report(context, error) { try {
        try {
            const details = errorDetails(error);
            const source = String(context || "browser-runtime");
            console.error(`LocalGPT JavaScript error in ${source}: ${details.message}`, error);
            if (!dotNetReference || reporting) {
                queueReport(source, details);
                return;
            }
            forward(source, details);
        } catch (reportError) {
            reporting = false;
            console.error("LocalGPT JavaScript diagnostics failed while reporting an error.", reportError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:report.', __javascriptError); throw __javascriptError; }}

    function guard(context, callback) { try {
        try {
            if (typeof callback !== "function") return callback;
            if (wrappedCallbacks.has(callback)) return wrappedCallbacks.get(callback);

            const guarded = function (...args) { try {
                try {
                    const result = callback.apply(this, args);
                    if (result && typeof result.then === "function") {
                        return result.catch(error => { try {
                            report(context, error);
                            throw error;
                         } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:callback:result.catch@52.', __javascriptError); throw __javascriptError; }});
                    }
                    return result;
                } catch (error) {
                    report(context, error);
                    throw error;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guarded@48.', __javascriptError); throw __javascriptError; }};
            wrappedCallbacks.set(callback, guarded);
            return guarded;
        } catch (error) {
            report("javascript-diagnostics.guard", error);
            return callback;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guard@43.', __javascriptError); throw __javascriptError; }}

    function guardObject(context, value) { try {
        try {
            if (!value || typeof value !== "object") return value;
            for (const key of Reflect.ownKeys(value)) {
                try {
                    const member = value[key];
                    if (typeof member === "function") value[key] = guard(`${context}.${String(key)}`, member);
                    else if (member && typeof member === "object") guardObject(`${context}.${String(key)}`, member);
                } catch (error) {
                    report(`${context}.${String(key)}`, error);
                }
            }
            return value;
        } catch (error) {
            report("javascript-diagnostics.guardObject", error);
            return value;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guardObject@71.', __javascriptError); throw __javascriptError; }}

    function guardClass(context, classType) { try {
        try {
            if (typeof classType !== "function") return classType;
            const protect = (target, prefix) => { try {
                try {
                    for (const key of Reflect.ownKeys(target)) {
                        if (key === "constructor" || key === "prototype" || key === "name" || key === "length") continue;
                        const descriptor = Object.getOwnPropertyDescriptor(target, key);
                        if (!descriptor || descriptor.configurable === false) continue;
                        if (typeof descriptor.value === "function") descriptor.value = guard(`${prefix}.${String(key)}`, descriptor.value);
                        if (typeof descriptor.get === "function") descriptor.get = guard(`${prefix}.get:${String(key)}`, descriptor.get);
                        if (typeof descriptor.set === "function") descriptor.set = guard(`${prefix}.set:${String(key)}`, descriptor.set);
                        Object.defineProperty(target, key, descriptor);
                    }
                } catch (error) {
                    report(`${prefix}.prototype`, error);
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:protect@93.', __javascriptError); throw __javascriptError; }};
            protect(classType, context);
            if (classType.prototype) protect(classType.prototype, context);
            return classType;
        } catch (error) {
            report("javascript-diagnostics.guardClass", error);
            return classType;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guardClass@90.', __javascriptError); throw __javascriptError; }}

    function bindDotNet(reference) { try {
        try {
            dotNetReference = reference || null;
            flushPendingReports();
        } catch (error) {
            report("javascript-diagnostics.bindDotNet", error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:bindDotNet@117.', __javascriptError); throw __javascriptError; }}

    function unbindDotNet() { try {
        try {
            dotNetReference = null;
        } catch (error) {
            report("javascript-diagnostics.unbindDotNet", error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:unbindDotNet@125.', __javascriptError); throw __javascriptError; }}

    window.addEventListener("error", event => {
        try {
            report(event?.filename ? `${event.filename}:${event.lineno || 0}:${event.colno || 0}` : "window.error", event?.error || event?.message);
        } catch (error) {
            console.error("LocalGPT window error diagnostics failed.", error);
        }
    });

    window.addEventListener("unhandledrejection", event => {
        try {
            report("window.unhandledrejection", event?.reason);
        } catch (error) {
            console.error("LocalGPT promise-rejection diagnostics failed.", error);
        }
    });

    function installCallbackGuards() { try {
        try {
            const listenerWrappers = new WeakMap();
            const originalAddEventListener = EventTarget.prototype.addEventListener;
            const originalRemoveEventListener = EventTarget.prototype.removeEventListener;

            function captureOption(options) { try {
                try {
                    return typeof options === "boolean" ? options : Boolean(options?.capture);
                } catch (error) {
                    report("javascript-diagnostics.captureOption", error);
                    return false;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:captureOption@155.', __javascriptError); throw __javascriptError; }}

            function eventWrapper(target, type, listener, options) { try {
                try {
                    if (!listener || (typeof listener !== "function" && typeof listener.handleEvent !== "function")) return listener;
                    let targetMap = listenerWrappers.get(listener);
                    if (!targetMap) {
                        targetMap = new WeakMap();
                        listenerWrappers.set(listener, targetMap);
                    }
                    let keyMap = targetMap.get(target);
                    if (!keyMap) {
                        keyMap = new Map();
                        targetMap.set(target, keyMap);
                    }
                    const key = `${String(type)}:${captureOption(options)}`;
                    if (keyMap.has(key)) return keyMap.get(key);
                    const callback = typeof listener === "function"
                        ? listener
                        : function (event) { try {
                            try {
                                return listener.handleEvent.call(listener, event);
                            } catch (error) {
                                report(`event:${String(type)}.handleEvent`, error);
                                throw error;
                            }
                         } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:FunctionExpression@181.', __javascriptError); throw __javascriptError; }};
                    const wrapped = guard(`event:${String(type)}`, callback);
                    keyMap.set(key, wrapped);
                    return wrapped;
                } catch (error) {
                    report("javascript-diagnostics.eventWrapper", error);
                    return listener;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:eventWrapper@164.', __javascriptError); throw __javascriptError; }}

            EventTarget.prototype.addEventListener = function (type, listener, options) { try {
                try {
                    return originalAddEventListener.call(this, type, eventWrapper(this, type, listener, options), options);
                } catch (error) {
                    report(`addEventListener:${String(type)}`, error);
                    throw error;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:EventTarget.prototype.addEventListener@198.', __javascriptError); throw __javascriptError; }};

            EventTarget.prototype.removeEventListener = function (type, listener, options) { try {
                try {
                    return originalRemoveEventListener.call(this, type, eventWrapper(this, type, listener, options), options);
                } catch (error) {
                    report(`removeEventListener:${String(type)}`, error);
                    throw error;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:EventTarget.prototype.removeEventListener@207.', __javascriptError); throw __javascriptError; }};

            const originalSetTimeout = window.setTimeout.bind(window);
            const originalSetInterval = window.setInterval.bind(window);
            const originalRequestAnimationFrame = window.requestAnimationFrame?.bind(window);
            const originalQueueMicrotask = window.queueMicrotask?.bind(window);

            window.setTimeout = function (callback, delay, ...args) { try {
                try {
                    return originalSetTimeout(typeof callback === "function" ? guard("setTimeout", callback) : callback, delay, ...args);
                } catch (error) {
                    report("javascript-diagnostics.setTimeout", error);
                    throw error;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:window.setTimeout@221.', __javascriptError); throw __javascriptError; }};
            window.setInterval = function (callback, delay, ...args) { try {
                try {
                    return originalSetInterval(typeof callback === "function" ? guard("setInterval", callback) : callback, delay, ...args);
                } catch (error) {
                    report("javascript-diagnostics.setInterval", error);
                    throw error;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:window.setInterval@229.', __javascriptError); throw __javascriptError; }};
            if (originalRequestAnimationFrame) {
                window.requestAnimationFrame = function (callback) { try {
                    try {
                        return originalRequestAnimationFrame(guard("requestAnimationFrame", callback));
                    } catch (error) {
                        report("javascript-diagnostics.requestAnimationFrame", error);
                        throw error;
                    }
                 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:window.requestAnimationFrame@238.', __javascriptError); throw __javascriptError; }};
            }
            if (originalQueueMicrotask) {
                window.queueMicrotask = function (callback) { try {
                    try {
                        return originalQueueMicrotask(guard("queueMicrotask", callback));
                    } catch (error) {
                        report("javascript-diagnostics.queueMicrotask", error);
                        throw error;
                    }
                 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:window.queueMicrotask@248.', __javascriptError); throw __javascriptError; }};
            }

            for (const observerName of ["MutationObserver", "ResizeObserver", "IntersectionObserver"]) {
                try {
                    const NativeObserver = window[observerName];
                    if (typeof NativeObserver !== "function") continue;
                    window[observerName] = class GuardedObserver extends NativeObserver {
                        constructor(callback) { try {
                            try {
                                super(guard(observerName, callback));
                            } catch (error) {
                                report(`javascript-diagnostics.${observerName}`, error);
                                throw error;
                            }
                         } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:Constructor@263.', __javascriptError); throw __javascriptError; }}
                    };
                } catch (error) {
                    report(`javascript-diagnostics.install.${observerName}`, error);
                }
            }
        } catch (error) {
            report("javascript-diagnostics.installCallbackGuards", error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:installCallbackGuards@149.', __javascriptError); throw __javascriptError; }}

    installCallbackGuards();

    window.localGptJavaScriptDiagnostics = {
        report,
        guard,
        guardObject,
        guardClass,
        bindDotNet,
        unbindDotNet
    };
 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:ArrowFunction@1.', __javascriptError); throw __javascriptError; }})();
