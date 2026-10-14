# LocalGPT 3.8.3 source validation

Static/source validation for this package checks:

- LocalGPT version 3.8.3 in active version-bearing files;
- all built-in runtime knowledge files copied to publish output;
- provider/toolchain embedded fallback resources and fallback loader;
- macOS Ollama.app CLI discovery;
- Windows/macOS/Linux LM Studio/llmster CLI discovery;
- provider bootstrap PATH enrichment through platform resolvers;
- LM Studio guided model `get` + `load`;
- empty unverified local model defaults and removal of Chat's implicit default-preset application;
- official/manual provider-install guidance remains present;
- existing application architecture/cross-platform/async/service audits;
- exact InteractiveServer render-mode count remains unchanged.

This environment does not contain `dotnet`, PowerShell, macOS Finder, Ollama, LM Studio, Xcode signing tools, or Apple's notary service. The source audit therefore cannot replace a clean installed-app smoke test on the target operating systems.
