# LocalGPT 2.9.1 — Live Council transcript status

## Changed

- Replaced the transient orange/animated live-Council waiting box inside chat answers with a calm inline transcript status paragraph.
- The status remains part of the generated Council transcript with normal spacing before and after it, so long Council runs no longer force repeated toast-like layout/scrollbar movement.
- Disabled live-region announcements for this frequently changing status text while preserving the visible run/round/member detail.

- Removed legacy `static` declarations from Razor component helpers/state; they are normal component instance members now, and the architecture audit covers Razor declarations as well as C# files.

## Preserved

- Council execution, rejoin/circuit recovery, role-member coordination, reasoning/function traces, provider routing and session persistence are unchanged.
- LocalGPT Wire Protocol remains 2.1.1.
- No `@rendermode` directive is intentionally changed.
