# LocalGPT 3.3.1

LocalGPT 3.3.1 is a cross-platform developer-build and local-runtime setup repair release built on 3.3.0.

A clean macOS/Linux source checkout can now pass the two repository-level restore gates that previously stopped development before normal compilation: the Windows WinUI/WebView wrapper explicitly allows Windows cross-targeting, and the normal NuGet configuration no longer requires a missing repository-local `./packages` source. The maintained PowerShell build paths were reviewed for `pwsh` portability, macOS/Linux documentation-browser discovery was added, and Unix documentation builds no longer attempt the Windows-only Node fallback.

The Install workbench now exposes Ollama and LM Studio/llmster setup more clearly and links directly to the existing service-backed, confirmation-gated setup assistant. Ollama executable discovery is separated behind `IOllamaPlatformService`, with Windows, macOS, Linux and fallback implementations selected through dependency injection while the shared process coordinator keeps the existing lifecycle/logging behavior.

DevExpress 25.2 developer licensing now has a repository-side preflight and registration helper for the official Windows/macOS/Linux per-user key locations and case-sensitive environment variables. License values are never printed or included in the repository.

InteractiveServer render-mode boundaries are unchanged. DevExpress remains **25.2.9**. PublisherStudio is unchanged by this archive.

See `CHANGELOG-v3.3.1-CROSS-PLATFORM-INSTALL-BUILD-LICENSING.md` and `VALIDATION-v3.3.1-source.md`.
