# LocalGPT 5.7.5 static source verification

- Baseline: supplied 5.7.4 source ZIP; 5.7.1 and 5.6.1 used only for historical comparison.
- Scope: LocalGPT only; PublisherStudio unchanged.
- Expected checks: archive integrity, XML parser on MSBuild targets / project, CSS parser, isolated Razor popup markup structure, PowerShell script references, no new Python dependency, documentation target default, version consistency.
- No .NET compilation, Windows/DevExpress runtime test or PowerShell 5.1 execution was possible in this environment; **not a compile-verified release**.
- Manual validation: build ordinary `LocalGPT` Debug from Visual Studio or CLI with Node.js 20+ and DocFX prerequisites; check `bin/Debug/net10.0/wwwroot/help-docs/index.html`, `api/index.html`, `documentation-status.json`, and `LocalGPT-5.7.5.pdf`; open `/help` HTML and PDF; test LanguageSwitcher under an open drawer in Office White and dark shell themes; open and close Theme Fusion at large/tablet/mobile viewport widths; test route reset without a JS interop TaskCanceledException and all three Fusion sections.
