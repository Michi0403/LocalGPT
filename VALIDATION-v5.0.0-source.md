# LocalGPT 5.0.0 source validation

Source-only validation was performed without invoking .NET, MSBuild, NuGet, publish, installer, packaging or GitHub.

Validated statically:
- application version is 5.0.0, avoiding prohibited 4.9.10;
- `ChatMicrophone` is no longer rendered as an absolute sibling after `DxAIChat`;
- `ChatMicrophone` is rendered in the DevExpress quick Council `DxFormLayout` as the fourth `Voice` item;
- Team, Models, Performance and Voice each use `ColSpanMd=3`;
- microphone/stop glyphs use self-contained SVG masks and do not depend on missing Open Iconic fallback glyphs;
- provider-neutral speech-to-text behavior and setup callbacks remain in the component;
- `DxAIChat` retains `IncludeFunctionCallInfo=true`;
- `FunctionCallInfoContentTemplate` is present and displays function name, arguments and result;
- the existing `MessageContentTemplate` remains present;
- obsolete composer-padding reservation and absolute microphone overlay rules were removed;
- MCP, OneWire, project/workspace, approval and code-generation sources were not removed or flattened.

The supplied Windows build remains the compile/runtime authority.
