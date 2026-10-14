# LocalGPT 3.6.1 — macOS launcher, AI-runtime helper, workspace repair

- Reworked the generated macOS `.app` launcher so it no longer treats a missing/delayed `runtime/server.json` as proof that startup failed.
- Added an HTTP readiness probe for the authoritative LocalGPT loopback endpoint `http://127.0.0.1:5000`.
- Increased installed-app startup allowance from 30 seconds to five minutes while leaving the application process alive for diagnosis.
- Added a Terminal startup-log follower after 20 seconds so slow startup is visible and actionable.
- Opens the browser immediately when either a valid runtime endpoint file or the HTTP readiness probe succeeds.
- Expanded `install-dependencies.sh` with opt-in Ollama installation through Homebrew, Ollama model pulling, LM Studio download guidance, and runtime/model status reporting.
- Releases completed Unix RID staging trees and transient macOS `.app` working bundles after native artifacts are validated, reducing release-disk pressure without deleting the durable DocFX cache or final packages.
- Version advanced from 3.6.0 to 3.6.1.
