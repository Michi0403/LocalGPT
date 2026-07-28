# LocalGPT 2.0.1 final24

## Fixed

- Streamed AI Council and chat response content is explicitly selectable and uses the native browser context menu, so selected text can be copied while generation is still running.
- Former model thoughts expose the same copyable-content contract.
- The chat host now grows into viewport space released when optional Council, memory, project, or architecture controls are collapsed instead of leaving unused space below the running session bar.
- The JavaScript diagnostics build guard now verifies the stable copyable markers, native context-menu bypass, selectable response CSS, and fluid chat-height contract.

## Preserved

- Existing JavaScript try/catch and console-to-ILogger diagnostics remain mandatory.
- final19 security and 1-Wire preservation rules were not changed.
- Runtime-value ownership and service architecture rules were not changed.
