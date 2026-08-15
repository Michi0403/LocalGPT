# LocalGPT 2.8.7 source validation

Validation is source-only by design. No `dotnet`, MSBuild, Visual Studio build, GitHub access, restore, publish, or executable launch was performed.

Checked statically:

- all three application projects report version 2.8.7 and obey the single-digit minor/patch slot policy;
- wire protocol remains 2.1.1;
- `CouncilTextService.cs` contains `using System.Text.RegularExpressions;`;
- the Council failure-memory path narrows the request before accessing `SaveToMemory`;
- home and main navigation no longer link to `/model-council`, while `ModelCouncil.razor` still owns that direct route;
- Local Chat labels and welcome/setup strings are maintained in all six built-in cultures and contain no `ChatGPT` value;
- obsolete `LocalChatGPT` localization aliases are absent;
- all six localization catalogs have identical key sets;
- LocalGPT still contains 19 `@rendermode` directives, matching the prior source release exactly;
- repository Python regression audits were run where they do not invoke .NET tooling;
- generated Python bytecode/cache output is removed before packaging.

The Windows build and runtime test remain authoritative.
