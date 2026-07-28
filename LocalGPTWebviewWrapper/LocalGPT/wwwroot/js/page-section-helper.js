// javascript-diagnostics: guarded
var localGptDiagnostics = globalThis.localGptJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`LocalGPT JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("LocalGPT fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`LocalGPT fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`LocalGPT fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
var DemoPageSectionHelper = (function() { try {
    function switchContentPage(id, isCodeVisible) { try {
        var dNoneSelector = " d-none";
        var sectionSelector = id ? '#section-' + id + ' ' : '';
        var componentAreaEl = document.querySelector(sectionSelector + '.demo-page-section-component-area');
        var codeAreaEl = document.querySelector(sectionSelector + '.demo-page-section-code-area');
        if (!componentAreaEl || !codeAreaEl) return;

        if(isCodeVisible) {
            if(componentAreaEl.offsetHeight > 0) {
                const offset = getCodeAreaOffsetTop();

                codeAreaEl.style.height = "fit-content";
                codeAreaEl.style.maxHeight = `calc(83vh - ${offset}px)`;
            }
            codeAreaEl.className = codeAreaEl.className.replace(dNoneSelector, "");
            if(componentAreaEl.className.indexOf(dNoneSelector) === -1)
                componentAreaEl.className += dNoneSelector;
        }
        else {
            if(codeAreaEl.className.indexOf(dNoneSelector) === -1)
                codeAreaEl.className += dNoneSelector;
            componentAreaEl.className = componentAreaEl.className.replace(dNoneSelector, "");
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:switchContentPage@3', __javascriptError); throw __javascriptError; }}

    function getCodeAreaOffsetTop(element = null) { try {
        const currentElement = element || document.querySelector('.card-body');
        const parentOffsetTop = currentElement.offsetParent ? getCodeAreaOffsetTop(currentElement.offsetParent) : 0;

        return currentElement.offsetTop + parentOffsetTop;
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:getCodeAreaOffsetTop@28', __javascriptError); throw __javascriptError; }}

    function initCopyCodeButtons(id) { try {
        var sectionSelector = id ? '#section-' + id + ' ' : '';
        var codeAreaEl = document.querySelector(sectionSelector + '.demo-page-section-code-area');
        var copyCodeBtn = codeAreaEl && codeAreaEl.querySelector('.btn.copy-code');
        if(!copyCodeBtn) return;

        new ClipboardJS(copyCodeBtn, {
            text: function () { try {
                var codeContainerEl = codeAreaEl.querySelector('.code-container');
                var activeCodeIndex = codeContainerEl.dataset["activeIndex"];
                var codeEl = codeAreaEl.querySelector('pre[data-index="' + activeCodeIndex + '"] > code');
                return codeEl && codeEl.textContent;
             } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:text@42', __javascriptError); throw __javascriptError; }}
        });
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:initCopyCodeButtons@35', __javascriptError); throw __javascriptError; }}

    function initSwitchTabButtons(id) { try {
        var sectionSelector = id ? '#section-' + id + ' ' : '';
        var tabButtonsSelector = sectionSelector + '.card .card-header .nav-tabs .nav-item a.nav-link';
        var tabButtons = document.querySelectorAll(tabButtonsSelector);
        var hrefAttr = 'href';

        for(var i = 0; i < tabButtons.length; i++) {
            if (!tabButtons[i].hasAttribute(hrefAttr))
                tabButtons[i].setAttribute(hrefAttr, '#');
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:initSwitchTabButtons@51', __javascriptError); throw __javascriptError; }}

    function initExpandCodeButtons(element) { try {
        var expandBtns = element.querySelectorAll('.more-code-btn');
        for(var i = 0; i < expandBtns.length; i++) {
            (function (btn) { try { btn.addEventListener("click", function () { try { expandCode(btn);  } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:callback:btn.addEventListener@66', __javascriptError); throw __javascriptError; }})  } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:FunctionExpression@66', __javascriptError); throw __javascriptError; }})(expandBtns[i]);
        }
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:initExpandCodeButtons@63', __javascriptError); throw __javascriptError; }}
    function expandCode(element) { try {
        element.parentNode.outerHTML = element.nextSibling.innerHTML;
     } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:expandCode@69', __javascriptError); throw __javascriptError; }}

    return {
        init: function (id, isCodeVisible) { try {
            switchContentPage(id, isCodeVisible);
            initCopyCodeButtons(id);
            initSwitchTabButtons(id);
         } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:init@74', __javascriptError); throw __javascriptError; }},
        initExpandCodeButtons: initExpandCodeButtons
    };
 } catch (__javascriptError) { localGptDiagnostics.report('js/page-section-helper.js:FunctionExpression@2', __javascriptError); throw __javascriptError; }})();
localGptDiagnostics.guardObject("DemoPageSectionHelper", DemoPageSectionHelper);
