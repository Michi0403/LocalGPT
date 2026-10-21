// javascript-diagnostics: guarded
// Compatibility entry point for earlier overlay checkouts. The live Blazor component
// imports its collocated module directly; this path delegates rather than duplicating logic.
export * from "../Components/Shared/ChatGameConsole.razor.js";

export async function loadCollocatedConsole() {
    try {
        return await import("../Components/Shared/ChatGameConsole.razor.js");
    } catch (error) {
        const diagnostics = globalThis.localGptJavaScriptDiagnostics;
        if (diagnostics?.report) diagnostics.report("localgpt-game-console.compatibility", error);
        else console.error("Game Console compatibility module could not load.", error);
        throw error;
    }
}
