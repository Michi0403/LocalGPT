// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
var DemoPageHelper = (function() { try {
    function scrollToElementTop(element) { try {
        if(element.scroll)
            element.scroll(0, 0);
        else {
            element.scrollTop = 0;
            element.scrollLeft = 0;
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:scrollToElementTop@3', __javascriptError); throw __javascriptError; }}
    function ensureNavigationTargetIsVisible() { try {
        var targetSelector = document.location.hash;
        if(targetSelector) {
            var demoAnchorLinks = Array.from(document.querySelectorAll('.demo-anchor'));
            var targetElement = demoAnchorLinks.filter(function(l) { try { return l.href.toLowerCase() === document.location.href.toLowerCase() && l.href.endsWith(targetSelector);  } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:callback:demoAnchorLinks.filter@15', __javascriptError); throw __javascriptError; }})[0];
            if(targetElement)
                targetElement.scrollIntoView();
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:ensureNavigationTargetIsVisible@11', __javascriptError); throw __javascriptError; }}

    function getCookie(name) { try {
        name = escape(name);
        var cookies = document.cookie.split(';');
        for(var i = 0; i < cookies.length; i++) {
            var cookie = cookies[i].trim();
            if(cookie.indexOf(name + '=') == 0)
                return unescape(cookie.substring(name.length + 1, cookie.length));
            else if(cookie.indexOf(name + ';') == 0 || cookie === name)
                return '';
        }
        return null;
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:getCookie@21', __javascriptError); throw __javascriptError; }}
    function setCookie(name, value, date) { try {
        document.cookie = escape(name) + '=' + escape(value.toString()) + '; expires=' + date.toGMTString() + '; path=/';
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:setCookie@33', __javascriptError); throw __javascriptError; }}

    function getThemeName(cookieName) { try {
        return getCookie(cookieName);
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:getThemeName@37', __javascriptError); throw __javascriptError; }}
    function setThemeName(cookieName, themeName) { try {
        var date = new Date();
        date.setFullYear(date.getFullYear() + 1);
        setCookie(cookieName, themeName, date);
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:setThemeName@40', __javascriptError); throw __javascriptError; }}

    function demoMatchesQuery(mediaQuery, dotNetHelper) { try {
        var query = window.matchMedia(mediaQuery), pendingCall;
        handleQuery(query).then(function() { try {
            return query.addListener(handleQuery);
         } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:callback:handleQuery(query).then@48', __javascriptError); throw __javascriptError; }});

        function handleQuery(queryMatch) { try {
            return (pendingCall || Promise.resolve(true))
                .then(function() { try {
                    return pendingCall = new Promise(function(resolve, reject) { try {
                        dotNetHelper.invokeMethodAsync('OnQueryChanged', queryMatch.matches).then(resolve).catch((__promiseError) => { try { localGptDiagnostics.report('js/page-helper.js:promise-catch@56', __promiseError); return (reject)(__promiseError); } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:promise-catch@56:handler', __javascriptError); throw __javascriptError; } });
                     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:FunctionExpression@55', __javascriptError); throw __javascriptError; }});
                 } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:callback:(pendingCall || Promise.resolve(true)) .then@54', __javascriptError); throw __javascriptError; }});
         } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:handleQuery@52', __javascriptError); throw __javascriptError; }}
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:demoMatchesQuery@46', __javascriptError); throw __javascriptError; }}

    function patchAppElement() { try {
        var appEl = document.getElementById("app");
        if(appEl) appEl.className = "root";
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:patchAppElement@62', __javascriptError); throw __javascriptError; }}

    function raiseWindowOnResize() { try {
        window.setTimeout(function() { try {
            var event = window.document.createEvent('UIEvents');
            event.initUIEvent('resize', true, false, window, 0);
            window.dispatchEvent(event);
         } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:callback:window.setTimeout@68', __javascriptError); throw __javascriptError; }}, 100);
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:raiseWindowOnResize@67', __javascriptError); throw __javascriptError; }}

    return {
        scroll: {
            toElementTop: scrollToElementTop,
            ensureNavigationTargetIsVisible: ensureNavigationTargetIsVisible
        },
        themes: {
            getThemeName: getThemeName,
            setThemeName: setThemeName
        },
        getCookie: getCookie,
        setCookie: setCookie,
        demoMatchesQuery: demoMatchesQuery,
        patchAppElement: patchAppElement,
        raiseWindowOnResize: raiseWindowOnResize
    };
 } catch (__javascriptError) { localGptDiagnostics.report('js/page-helper.js:FunctionExpression@2', __javascriptError); throw __javascriptError; }})();

window["_dx_demoPageHelper"] = DemoPageHelper;
localGptDiagnostics.guardObject("DemoPageHelper", DemoPageHelper);
