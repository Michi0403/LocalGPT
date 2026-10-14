# LocalGPT 5.0.3 source validation

Source-only validation performed in the handoff environment; no .NET toolchain or GitHub access was used.

- application version is 5.0.3;
- all semantic-version components are single-digit;
- InteractiveServer declarations from 5.0.2 were preserved rather than moving render mode to the root router;
- microphone JavaScript keeps capture instances module-local behind opaque ids, exports `start`/`stop`/`cancel`/`dispose`, and the Razor cache-buster is 5.0.3;
- ThemeJsChangeDispatcher performs no JavaScript interop from its final dispose path;
- user-visible JSON formatting pre-sanitizes unescaped JSON-string control characters;
- the shared layout envelope contains FormLayout, GridLayout, StackLayout, Tabs, Splitter, Carousel, and Content branches;
- Layout Studio exposes all seven persisted envelope choices and orientation;
- Chat registers and consumes layout surfaces for function-call, message-content, and ASCII popup body templates;
- the configuration workbench active state forces readable LocalGPT text tokens;
- MainLayout uses a native language selector and one drawer-content scroll owner;
- Council coding guidance explicitly continues independent source generation when optional web search is HumanApprovalPending;
- static source and archive checks were run after editing.
