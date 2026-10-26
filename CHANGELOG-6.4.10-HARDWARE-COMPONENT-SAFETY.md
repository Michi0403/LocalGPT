# LocalGPT 6.4.10 — hardware component safety repair

- Fixes the build-blocking `RAZORLOG0001` raised by `Assert-ComponentSafety.ps1` after extraction of `InitialSetupHardwareForm.razor` in 6.4.9.
- Restores the required component-local `ILogger<InitialSetupHardwareForm>`, `INotificationService`, and `IComponentActivityService` top-level directives. No safety gate, shared service, DI lifetime, hardware callback, HTTP route, or workflow was removed or relaxed.
- Adds a regression test covering the three component-safety injections, their directive position, important hardware callbacks, and the build-wired safety gate.
- Updates the project to 6.4.10 and current version-pin assertions.

The extensive 6.4.9 form inventory remains available in `FORM_AUDIT_ALL_COMPONENTS.*`. Its outstanding refactoring candidates were not changed by this focused build repair.

See `VALIDATION-6.4.10-OFFLINE.md` for the validation boundary.
