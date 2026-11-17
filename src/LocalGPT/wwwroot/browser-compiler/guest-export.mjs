// Generate source for the ALREADY provisioned Hyperlight Node.js guest only.
// This is not a native WASIX loader, nor an application-host Node execution path.
export async function createReviewedGuestSource(bytes, maximumSourceCharacters) {
    if (!(bytes instanceof Uint8Array) || bytes.byteLength > Math.floor(maximumSourceCharacters * 3 / 4) || !WebAssembly.validate(bytes)) throw new Error('Module does not fit the existing single-source Hyperlight submission limit.');
    const module = await WebAssembly.compile(bytes);
    const imports = WebAssembly.Module.imports(module);
    const exports = WebAssembly.Module.exports(module);
    if (!imports.every(item => item.module === 'wasi_snapshot_preview1' && item.kind === 'function') ||
        !exports.some(item => item.name === '_start' && item.kind === 'function') ||
        !exports.some(item => item.name === 'memory' && item.kind === 'memory')) {
        throw new Error('This module is not a standalone WASI Preview 1 command. WASIX-specific or other imports need a compatible verified Hyperlight runtime.');
    }
    const hash = Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', bytes)), part => part.toString(16).padStart(2, '0')).join('');
    let binary = '';
    for (const byte of bytes) binary += String.fromCharCode(byte);
    const base64 = btoa(binary);
    const source = `// Explicitly review and run ONLY in LocalGPT's Hyperlight Node.js guest.\n// WASI Preview 1 module SHA-256: ${hash}\nconst { WASI } = require('node:wasi');\nconst { createHash } = require('node:crypto');\nconst bytes = Buffer.from('${base64}', 'base64');\nif (createHash('sha256').update(bytes).digest('hex') !== '${hash}') throw new Error('Module digest mismatch');\nconst moduleBytes = new WebAssembly.Module(bytes);\nif (!WebAssembly.Module.imports(moduleBytes).every(item => item.module === 'wasi_snapshot_preview1' && item.kind === 'function')) throw new Error('Unsupported WASM imports');\nconst wasi = new WASI({ version: 'preview1', args: [], env: {}, preopens: {}, returnOnExit: true });\nconst instance = new WebAssembly.Instance(moduleBytes, { wasi_snapshot_preview1: wasi.wasiImport });\nprocess.exitCode = wasi.start(instance);\n`;
    if (source.length > maximumSourceCharacters) throw new Error('Encoded guest snippet exceeds the existing Hyperlight source limit.');
    return source;
}
