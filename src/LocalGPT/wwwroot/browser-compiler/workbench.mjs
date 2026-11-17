import {createReviewedGuestSource} from './guest-export.mjs';
// The page has no Blazor circuit, upload endpoint, or connection to a server execution service.
const element = id => document.getElementById(id);
const controls = ['language', 'standard', 'sourceFile', 'source', 'moduleFile'];
let policy, artifact, receipt, activeWorker, timeout, moduleUrl, receiptUrl, guestUrl;
let ready = false;
const status = text => { element('status').textContent = text; };
const output = text => { element('output').textContent = text; };
function update() {
    for (const name of controls) element(name).disabled = !ready || !!activeWorker;
    element('compile').disabled = !ready || !!activeWorker;
    element('run').disabled = !ready || !!activeWorker || !artifact;
    element('stop').disabled = !activeWorker;
    element('guestExport').disabled = !ready || !!activeWorker || !artifact;
    element('guestDownload').hidden = !guestUrl;
    element('moduleDownload').hidden = !artifact;
    element('receiptDownload').hidden = !receipt;
}
function clearReceipt() {
    receipt = undefined;
    if (receiptUrl) URL.revokeObjectURL(receiptUrl);
    receiptUrl = undefined;
    element('receiptDownload').removeAttribute('href');
}
function setArtifact(bytes) {
    artifact = bytes;
    if (guestUrl) URL.revokeObjectURL(guestUrl);
    guestUrl = undefined;
    element('guestDownload').removeAttribute('href');
    if (moduleUrl) URL.revokeObjectURL(moduleUrl);
    moduleUrl = undefined;
    element('moduleDownload').removeAttribute('href');
    if (bytes) {
        moduleUrl = URL.createObjectURL(new Blob([bytes], {type: 'application/wasm'}));
        element('moduleDownload').href = moduleUrl;
    }
    update();
}
function showReceipt(value) {
    clearReceipt();
    receipt = {...value, receivedAtUtc: new Date().toISOString()};
    receiptUrl = URL.createObjectURL(new Blob([JSON.stringify(receipt, null, 2)], {type: 'application/json'}));
    element('receiptDownload').href = receiptUrl;
    output([receipt.diagnostics, receipt.stdout, receipt.stderr, `Exit code: ${receipt.exitCode}`, receipt.artifactSha256 ? `WASM SHA-256: ${receipt.artifactSha256}` : ''].filter(Boolean).join('\n'));
    status(receipt.ok ? `${receipt.operation} succeeded in the browser.` : `${receipt.operation} failed in the browser.`);
    update();
}
function stopWorker(message) {
    if (activeWorker) activeWorker.terminate();
    activeWorker = undefined;
    clearTimeout(timeout);
    if (message) status(message);
    update();
}
function launch(request) {
    if (!ready || activeWorker) return;
    clearReceipt();
    if (request.operation === 'compile') setArtifact(undefined);
    output('');
    const worker = new Worker(new URL('./compiler-worker.mjs', import.meta.url), {type: 'module'});
    activeWorker = worker;
    update();
    status('Starting isolated browser worker…');
    timeout = setTimeout(() => stopWorker('Browser operation timed out. No server fallback or success receipt.'), policy.timeoutMilliseconds);
    worker.onmessage = ({data}) => {
        if (activeWorker !== worker) return;
        if (data.progress) { status(data.progress); return; }
        stopWorker();
        if (data.error) { status('Browser operation failed.'); output(data.error); return; }
        if (!data.result?.receipt) { status('Browser operation returned no receipt.'); return; }
        if (data.result.bytes) setArtifact(data.result.bytes);
        showReceipt(data.result.receipt);
    };
    worker.onerror = event => {
        if (activeWorker !== worker) return;
        stopWorker('Browser worker failed.'); output(event.message || 'Worker could not start.');
    };
}
function setStandards() {
    const language = policy.languages.find(item => item.key === element('language').value);
    element('standard').replaceChildren(...language.standards.map(value => new Option(value, value)));
    element('standard').value = language.defaultStandard;
}
function changed() { clearReceipt(); setArtifact(undefined); }
element('language').onchange = () => { setStandards(); changed(); };
element('standard').onchange = changed;
element('source').oninput = changed;
element('sourceFile').onchange = async () => {
    const file = element('sourceFile').files[0];
    if (!file || activeWorker) return;
    if (file.size > policy.maximumSourceBytes) { status('Source file too large.'); return; }
    try {
        const text = await file.text();
        if (activeWorker || element('sourceFile').files[0] !== file) return;
        element('language').value = /\.c$/i.test(file.name) ? 'c' : 'cpp';
        setStandards(); element('source').value = text; changed();
    } catch (error) { status(String(error)); }
};
element('moduleFile').onchange = async () => {
    const file = element('moduleFile').files[0];
    if (!file || activeWorker) return;
    changed();
    try {
        if (file.size > policy.maximumModuleBytes) throw new Error('WASM file too large.');
        const bytes = new Uint8Array(await file.arrayBuffer());
        if (activeWorker || element('moduleFile').files[0] !== file) return;
        if (!WebAssembly.validate(bytes)) throw new Error('Invalid WebAssembly module.');
        setArtifact(bytes); status('Local artifact loaded; choose Execute to run it.');
    } catch (error) { status(String(error)); }
};
element('compile').onclick = () => launch({operation: 'compile', source: element('source').value, language: element('language').value, standard: element('standard').value});
element('run').onclick = () => { if (artifact) launch({operation: 'execute', bytes: artifact}); };
element('stop').onclick = () => stopWorker('Browser operation cancelled. No success receipt.');
element('guestExport').onclick = async () => {
    if (!artifact || activeWorker) return;
    const selected = artifact;
    try {
        const source = await createReviewedGuestSource(selected, policy.maximumHyperlightSnippetCharacters);
        if (artifact !== selected || activeWorker) return;
        if (guestUrl) URL.revokeObjectURL(guestUrl);
        guestUrl = URL.createObjectURL(new Blob([source], {type: 'text/javascript'}));
        element('guestDownload').href = guestUrl;
        status('Guest source prepared for explicit review. No Hyperlight execution has occurred.');
    } catch (error) { status(String(error.message ?? error)); }
    update();
};
window.addEventListener('pagehide', () => stopWorker());
async function initialize() {
    try {
        if (!window.isSecureContext || !window.crossOriginIsolated) throw new Error('Browser requires HTTPS or localhost plus cross-origin isolation.');
        const [descriptor, installed] = await Promise.all(['./runtime.json', './toolchain/manifest.json'].map(async path => {
            const response = await fetch(path, {cache: 'no-store', credentials: 'omit', redirect: 'error'});
            if (!response.ok) throw new Error('Reviewed browser toolchain not provisioned. See docs/reference/browser-wasix-compiler.md.');
            return response.json();
        }));
        if (descriptor.sdkVersion !== installed.sdkVersion || descriptor.compilerPackage !== installed.compilerPackage || !/^[0-9a-f]{64}$/i.test(installed.compilerSha256)) throw new Error('Installed toolchain does not match the release descriptor.');
        policy = descriptor;
        element('language').replaceChildren(...policy.languages.map(value => new Option(value.compilerLanguage, value.key)));
        element('language').value = 'cpp'; setStandards();
        element('source').value = '#include <stdio.h>\nint main() { puts("Hello from Potato Prime"); return 0; }\n';
        ready = true; status('Local toolchain manifest found. Actual compiler and module validation occur on execution.');
    } catch (error) { status(String(error.message ?? error)); }
    update();
}
void initialize();
