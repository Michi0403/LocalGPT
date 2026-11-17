# Browser-owned WASIX C/C++ compiler

The LocalGPT Chat session sandbox panel links to a separate, circuit-independent frontend workbench. It is adapted from the supplied MeowReal Wasmer SDK 0.8.0 browser compiler. Existing Hyperlight runtimes and their approvals, host-deny policy, persistence, and runtime-plugin execution are unchanged.

## Install an offline reviewed toolchain

The source archive contains no Wasmer/Clang binaries and no JavaScript downloaded from a CDN. LocalGPT does not attempt a registry download from the browser. The release descriptor `src/LocalGPT/wwwroot/browser-compiler/runtime.json` pins the SDK version and compiler package identity, language standards, and bounds. The operator must obtain an authentic SDK npm archive (e.g. `npm pack @wasmer/sdk@0.8.0 --ignore-scripts` from a trusted registry), independently verify its integrity, and extract it into a reviewed local directory. Obtain the actual `clang/clang@0.160000.1` WebC package through a trusted Wasmer package distribution channel and independently verify its SHA-256. A hash calculated from an untrusted file is not provenance. The package and hash must refer to the exact WebC bytes, not an archive containing them.

After the normal LocalGPT build, run the explicit PowerShell installer:

```powershell
./build/Provision-BrowserCompilerToolchain.ps1 `
  -OutputDirectory ./src/LocalGPT/bin/Debug/net10.0 `
  -SdkPackageDirectory /path/to/reviewed/npm/package `
  -ClangPackagePath /path/to/reviewed/clang.webc `
  -ExpectedClangSha256 '<independently verified 64-digit SHA-256>'
```

For RID-specific builds or publish, choose the actual application output directory. The installer works with pre-extracted, administrator-reviewed SDK assets, not arbitrary user/plugin ZIP uploads. It copies the SDK dist files and compiler package into `wwwroot/browser-compiler/toolchain`, records installed SDK asset hashes and the reviewed compiler hash, and refuses to replace an installed bundle. Preserve SDK/package licenses and provenance with any distribution. The SDK files are trusted same-origin application code; their hash inventory is an install receipt, not browser-side code signing. Recheck the bundle when distributing or moving it to another machine. Repeat installation in the target build/publish output when necessary. No automatic package provisioning is included in this source candidate.

## Use and verify

1. Open Chat → Session code sandboxes → **Open client-side C/C++ WASIX compiler** in a separate tab. Use a secure context: HTTPS or trusted localhost. Only the workbench route receives COOP/COEP and its restrictive CSP; ordinary Chat, DevExpress, MCP, and other routes retain their existing behavior.
2. Select C89/C99/C11/C17 or C++98/03/11/14/17/20 and load or paste a single source file. The offered standards are compiler flags, not certified support for every library feature. Compile and inspect diagnostics. Successful compilation permits WASM download; run requires a separate click. Existing `.wasm` modules may also be loaded and explicitly run.
3. The worker loads the exact locally provisioned SDK and WebC file, verifies the compiler bytes against the manifest, uses only the SDK virtual `/project` directory, passes fixed flags and the selected allowlisted standard, and validates the generated WebAssembly. No source upload, registered server function, server shell, API credentials, or host filesystem mount is supplied. For module execution, no directory is mounted. No external network bridge is configured. CSP allows only workbench-path resource connections and blob workers; verify the browser's network panel, including workers.
4. Save the module and local receipt if desired. Receipts include execution location, compiler/source/module hashes where relevant, bounded output and exit code. They are explicitly **client-reported**, not authenticated server build or Hyperlight verification. Do not use them to satisfy the Council artifact-verification gate.
5. Stop or timeout terminates the owning browser worker and discards pending results. Browser memory limits, Wasmer child-worker reclamation, SDK/Clang ABI compatibility, and actual C/C++ standard support require target-browser tests. Output display is bounded after runtime collection; this is not a hard micro-VM resource quota. Browser disconnect is not equivalent to backend session persistence.

## Provider boundaries and extension plan

- Browser WASIX is the client execution provider: no host compiler needed, no source sent through Blazor Server, useful on heterogeneous clients.
- The existing Hyperlight/Unikraft provider owns unattended isolated backend runs. It is not changed or falsely advertised as able to compile WASIX packages. The explicit guest-snippet export is a limited transfer path for small standalone WASI Preview 1 modules only: the browser checks imports and the command entrypoint, embeds bytes and SHA-256 into JavaScript, and exports source for independent human review. Paste the source into the existing Hyperlight Node.js guest, not a host interpreter. The wrapper checks the hash and import namespaces again and configures WASI with empty environment, arguments and preopens. The existing 160,000-character submission ceiling applies before export. No auto-run, native WASIX support, or proof of Node guest compatibility is claimed. A complete WASIX-capable Hyperlight runtime is still needed for modules with WASIX imports and for in-guest compilation.
- Runtime plugins presently execute trusted host extensions. This iteration intentionally does not register arbitrary WASM as a host extension or expose an AI-callable browser function. A follow-up must implement an explicit WASM runtime-plugin provider, capability discovery, client-presence and consent routing, project workspace/receipt binding and separate verified guest toolchain. For unattended work, provision a real compatible guest and run the native isolation acceptance tests before advertising capability. Rust and Go require compiler-specific toolchains and tests rather than simply adding language labels.
- Browser workbench is deliberately a separate static document, not a replacement Razor subform: its source editor and controls must function without an InteractiveServer circuit and must never bind source to a server component. The existing DevExpress session controls remain unchanged.

## Acceptance pending

Run the repository's PowerShell guards and normal .NET build. On actual browsers, verify the local bundle loads offline, C11 and C++17 examples compile and run, C++98/11/14 flags work on known sample programs, imported WASM runs only after approval, bad code reports compiler diagnostics, source edits invalidate stale artifacts, cancellation discards late output, missing/changed bundles fail closed, and attempted guest network/host filesystem access fails. Test the separate guest-export path with a small known WASI Preview 1 command, reject WASIX imports or oversized modules, and explicitly run the reviewed wrapper in the real bundled Hyperlight Node.js guest to confirm its WASI/crypto builtins are available. Check HTTP headers on the workbench and unchanged normal Chat routes. Verify neither browser execution nor client receipts can satisfy backend Hyperlight evidence checks. No such runtime tests were performed in the editing environment.
