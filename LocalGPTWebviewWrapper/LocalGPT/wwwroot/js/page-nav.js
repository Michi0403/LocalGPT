// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
var DemoPageNavPanel = (function() { try {
    function addDemoAnchorIntersectionObserver() { try {
        var scrollableContainer = document.querySelector('.demo-content-container');

        var options = {
            root: scrollableContainer,
            rootMargin: '0px 0px -80% 0px',
            threshold: [0, 1]
        };
        var observer = new IntersectionObserver(demoAnchorIntersectionHandler, options);
        var demoAnchorLinks = document.querySelectorAll('.demo-anchor');
        demoAnchorLinks.forEach(link => { try { return (observer.observe(link)); } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:callback:demoAnchorLinks.forEach@13', __javascriptError); throw __javascriptError; } });

        var footerObserverOptions = {
            root: scrollableContainer,
            threshold: [0, 1]
        };
        var footerObserver = new IntersectionObserver(demoFooterIntersectionHandler, footerObserverOptions);
        var footerElement = document.querySelector('.main > .content-footer');
        footerObserver.observe(footerElement);
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:addDemoAnchorIntersectionObserver@3', __javascriptError); throw __javascriptError; }}

    function demoAnchorIntersectionHandler(entries) { try {
        entries.forEach(entry => { try {
            var demoAnchorLinkUrl = entry.target.href.toLowerCase();
            var demoNavPanelItems = Array.from(document.querySelectorAll('.demo-page-nav .nav-pills .nav-link'));
            var demoNavTargetItem = document.querySelector('.nav-target');
            if(entry.isIntersecting) {
                demoNavPanelItems.forEach(item => { try {
                    if(item.href.toLowerCase() === demoAnchorLinkUrl) {
                        if(!demoNavTargetItem || item.classList.contains('nav-target'))
                            setDemoNavPanelItemActive(item, true);
                    }
                    else
                        setDemoNavPanelItemActive(item, false);
                 } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:callback:demoNavPanelItems.forEach@30', __javascriptError); throw __javascriptError; }});
            }
         } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:callback:entries.forEach@25', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:demoAnchorIntersectionHandler@24', __javascriptError); throw __javascriptError; }}

    function demoFooterIntersectionHandler(entries) { try {
        entries.forEach(entry => { try {
            if(entry.isIntersecting) {
                var demoNavPanelItems = Array.from(document.querySelectorAll('.demo-page-nav .nav-pills .nav-link'));
                demoNavPanelItems.forEach((item, index) => { try {
                    setDemoNavPanelItemActive(item, index == demoNavPanelItems.length - 1);
                 } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:callback:demoNavPanelItems.forEach@46', __javascriptError); throw __javascriptError; }});
            }
         } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:callback:entries.forEach@43', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:demoFooterIntersectionHandler@42', __javascriptError); throw __javascriptError; }}

    function setDemoNavPanelItemActive(itemElement, isActive) { try {
        if(isActive) {
            itemElement.classList.add('active');
            if(itemElement.classList.contains('nav-target'))
                itemElement.classList.remove('nav-target');
            var headerTextElement = document.querySelector('.demo-page-nav .nav-header-text');
            headerTextElement.innerText = itemElement.querySelector(".text").innerText;
        }
        else {
            if(itemElement.classList.contains('active'))
                itemElement.classList.remove('active');
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:setDemoNavPanelItemActive@53', __javascriptError); throw __javascriptError; }}

    return {
        addDemoAnchorIntersectionObserver: addDemoAnchorIntersectionObserver
    };
 } catch (__javascriptError) { localGptDiagnostics.report('js/page-nav.js:FunctionExpression@2', __javascriptError); throw __javascriptError; }})();
localGptDiagnostics.guardObject("DemoPageNavPanel", DemoPageNavPanel);
