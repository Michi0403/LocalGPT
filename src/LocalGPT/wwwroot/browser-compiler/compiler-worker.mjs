// Adapted from the supplied MeowReal browser-compiler.mjs (Wasmer SDK 0.8.0).
// No registry/CDN loading, host mounts, environment variables, or network bridge.
const descriptorUrl = new URL('./runtime.json', import.meta.url);
const manifestUrl = new URL('./toolchain/manifest.json', import.meta.url);
const sdkUrl = new URL('./toolchain/sdk/index.mjs', import.meta.url);
const compilerUrl = new URL('./toolchain/compiler.bin', import.meta.url);

async function readJson(url) {
    const response = await fetch(url, {cache: 'no-store', credentials: 'omit', redirect: 'error'});
    if (!response.ok) throw new Error(`Resource unavailable: ${url.pathname} (${response.status}). Provision the reviewed toolchain.`);
    return response.json();
}
async function sha256(bytes) {
    return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', bytes)), item => item.toString(16).padStart(2, '0')).join('');
}
function requireIsolation() {
    if (!self.isSecureContext || !self.crossOriginIsolated) throw new Error('A secure cross-origin-isolated browser is required. No other execution route will be used.');
}
async function loadTools() {
    requireIsolation();
    const policy = await readJson(descriptorUrl);
    const manifest = await readJson(manifestUrl);
    if (manifest.sdkVersion !== policy.sdkVersion || manifest.compilerPackage !== policy.compilerPackage || !/^[0-9a-f]{64}$/i.test(manifest.compilerSha256)) {
        throw new Error('Installed compiler manifest does not match this source release.');
    }
    const sdk = await import(sdkUrl.href);
    if (typeof sdk.init !== 'function' || typeof sdk.Wasmer?.fromFile !== 'function' || typeof sdk.Directory !== 'function') throw new Error('Incompatible local Wasmer SDK.');
    await sdk.init();
    return {sdk, policy, manifest};
}
function requireWasm(bytes, maximum) {
    if (!(bytes instanceof Uint8Array) || bytes.byteLength > maximum || bytes.byteLength < 8 || !WebAssembly.validate(bytes)) throw new Error('Invalid or oversized WebAssembly artifact.');
}
function bound(text, limit) {
    text = String(text ?? '');
    return text.length <= limit ? text : text.slice(0, limit) + '\n[output truncated]';
}
async function compile(source, languageKey, standard) {
    const {sdk, policy, manifest} = await loadTools();
    const language = policy.languages.find(item => item.key === languageKey);
    if (!language || !language.standards.includes(standard)) throw new Error('Unsupported language standard.');
    if (typeof source !== 'string' || !source.trim()) throw new Error('Source is empty.');
    const sourceBytes = new TextEncoder().encode(source);
    if (sourceBytes.byteLength > policy.maximumSourceBytes) throw new Error('Source exceeds the configured size limit.');
    self.postMessage({progress: 'Loading the locally provisioned Clang package…'});
    const response = await fetch(compilerUrl, {cache: 'no-store', credentials: 'omit', redirect: 'error'});
    if (!response.ok) throw new Error('No local Clang package installed.');
    const compilerBytes = new Uint8Array(await response.arrayBuffer());
    if (compilerBytes.byteLength > policy.maximumToolchainBytes || await sha256(compilerBytes) !== manifest.compilerSha256.toLowerCase()) throw new Error('Clang package size or SHA-256 verification failed.');
    const compiler = await sdk.Wasmer.fromFile(compilerBytes);
    if (!compiler.entrypoint) throw new Error('Clang package has no executable entrypoint.');
    const directory = new sdk.Directory();
    const name = `workshop.${language.extension}`;
    await directory.writeFile(name, source);
    self.postMessage({progress: `Compiling ${standard} inside browser WASIX…`});
    const instance = await compiler.entrypoint.run({args: ['-O2', '-x', language.compilerLanguage, `-std=${standard}`, `/project/${name}`, '-o', '/project/workshop.wasm'], mount: {'/project': directory}});
    const result = await instance.wait();
    const diagnostics = bound([result.stdout, result.stderr].filter(Boolean).join('\n'), policy.maximumOutputCharacters);
    const receipt = {providerId: policy.providerId, executionLocation: policy.executionLocation, verifiedByServer: false, operation: 'compile', language: languageKey, standard, sdkVersion: policy.sdkVersion, compilerPackage: policy.compilerPackage, compilerSha256: manifest.compilerSha256, sourceSha256: await sha256(sourceBytes), exitCode: result.code, ok: false, diagnostics};
    if (!result.ok || result.code !== 0) return {receipt};
    const output = await directory.readFile('workshop.wasm');
    const bytes = output instanceof Uint8Array ? output : new Uint8Array(output);
    requireWasm(bytes, policy.maximumModuleBytes);
    receipt.artifactSha256 = await sha256(bytes);
    receipt.ok = true;
    return {bytes, receipt};
}
async function execute(bytes) {
    const {sdk, policy} = await loadTools();
    requireWasm(bytes, policy.maximumModuleBytes);
    const program = await sdk.Wasmer.fromFile(bytes);
    if (!program.entrypoint) throw new Error('Module has no executable WASIX/WASI entrypoint.');
    self.postMessage({progress: 'Executing module inside browser WASIX…'});
    const instance = await program.entrypoint.run();
    const result = await instance.wait();
    return {receipt: {providerId: policy.providerId, executionLocation: policy.executionLocation, verifiedByServer: false, operation: 'execute', artifactSha256: await sha256(bytes), sdkVersion: policy.sdkVersion, exitCode: result.code, ok: result.ok && result.code === 0, stdout: bound(result.stdout, policy.maximumOutputCharacters), stderr: bound(result.stderr, policy.maximumOutputCharacters)}};
}
self.onmessage = async ({data}) => {
    try {
        const result = data.operation === 'compile' ? await compile(data.source, data.language, data.standard) : data.operation === 'execute' ? await execute(data.bytes) : (() => { throw new Error('Unknown browser operation.'); })();
        self.postMessage({result}, result.bytes ? [result.bytes.buffer] : []);
    } catch (error) {
        self.postMessage({error: String(error?.message ?? error).slice(0, 65536)});
    }
};
