using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Default <see cref="IGameInputMapService"/>. Labels are split at <c>/</c> and <c>,</c>; single letters and digits
/// become <c>Key&lt;L&gt;</c> and <c>Digit&lt;N&gt;</c>, modifier names expand to both sides, and gamepad names follow
/// the W3C standard gamepad layout. Unrecognized label parts are ignored (they remain visible in the control guide).
/// </summary>
/// <param name="logger">Receives input-map diagnostics.</param>
public sealed class GameInputMapService(ILogger<GameInputMapService> logger) : IGameInputMapService
{
    /// <summary>Keyboard label words and the key codes they stand for.</summary>
    private readonly IReadOnlyDictionary<string, string[]> keyWords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Space"] = ["Space"],
        ["Enter"] = ["Enter"],
        ["Return"] = ["Enter"],
        ["Tab"] = ["Tab"],
        ["Esc"] = ["Escape"],
        ["Escape"] = ["Escape"],
        ["Backspace"] = ["Backspace"],
        ["Ctrl"] = ["ControlLeft", "ControlRight"],
        ["Control"] = ["ControlLeft", "ControlRight"],
        ["Shift"] = ["ShiftLeft", "ShiftRight"],
        ["Alt"] = ["AltLeft", "AltRight"]
    };

    /// <summary>Gamepad label words and the standard-layout controls they stand for.</summary>
    private readonly IReadOnlyDictionary<string, string> gamepadWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = "button:0",
        ["B"] = "button:1",
        ["X"] = "button:2",
        ["Y"] = "button:3",
        ["LB"] = "button:4",
        ["Left bumper"] = "button:4",
        ["RB"] = "button:5",
        ["Right bumper"] = "button:5",
        ["LT"] = "button:6",
        ["Left trigger"] = "button:6",
        ["RT"] = "button:7",
        ["Right trigger"] = "button:7",
        ["Back"] = "button:8",
        ["View"] = "button:8",
        ["Start"] = "button:9",
        ["Menu"] = "button:9",
        ["D-pad Up"] = "button:12",
        ["D-pad Down"] = "button:13",
        ["D-pad Left"] = "button:14",
        ["D-pad Right"] = "button:15",
        ["Left stick left"] = "axis:0:-",
        ["Left stick right"] = "axis:0:+",
        ["Left stick up"] = "axis:1:-",
        ["Left stick down"] = "axis:1:+",
        ["Right stick left"] = "axis:2:-",
        ["Right stick right"] = "axis:2:+",
        ["Right stick up"] = "axis:3:-",
        ["Right stick down"] = "axis:3:+"
    };

    /// <inheritdoc />
    public IReadOnlyList<GameInputMapping> Build(IReadOnlyList<RuntimeInputBindingDefinition> bindings)
    {
        try
        {
            var mappings = new List<GameInputMapping>();
            foreach (var binding in bindings.Where(item => !string.IsNullOrWhiteSpace(item.Action)))
            {
                var mapping = new GameInputMapping
                {
                    Action = binding.Action,
                    Keys = Parts(binding.KeyboardKey).SelectMany(KeyCodes).Distinct(StringComparer.Ordinal).ToList(),
                    Controls = Parts(binding.GamepadButton)
                        .Select(part => gamepadWords.TryGetValue(part, out var control) ? control : string.Empty)
                        .Where(control => control.Length > 0)
                        .Distinct(StringComparer.Ordinal)
                        .ToList()
                };
                if (mapping.Keys.Count > 0 || mapping.Controls.Count > 0)
                    mappings.Add(mapping);
            }
            logger.LogDebug("Built the browser input map for {BindingCount} binding(s): {MappingCount} mapped action(s).", bindings.Count, mappings.Count);
            return mappings;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the browser input map failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public string Signature(Guid sessionId, IReadOnlyList<RuntimeInputBindingDefinition> bindings)
    {
        try
        {
            if (sessionId == Guid.Empty)
                return string.Empty;
            return $"{sessionId:N}:{string.Join('|', bindings.Select(binding => $"{binding.Action}={binding.KeyboardKey}={binding.GamepadButton}"))}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the input-map signature of game session {SessionId} failed.", sessionId);
            throw;
        }
    }

    /// <summary>Splits an input label into its trimmed alternatives.</summary>
    /// <param name="label">Keyboard or gamepad label such as "W / ArrowUp".</param>
    /// <returns>The non-empty alternatives.</returns>
    private IEnumerable<string> Parts(string? label)
    {
        try
        {
            return (label ?? string.Empty)
                .Split(['/', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => part.Length > 0)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Splitting input label {Label} failed.", label);
            throw;
        }
    }

    /// <summary>Returns the key codes of one keyboard label part.</summary>
    /// <param name="part">One alternative of a keyboard label.</param>
    /// <returns>The key codes; empty when the part is not recognized.</returns>
    private IEnumerable<string> KeyCodes(string part)
    {
        try
        {
            if (part.Length == 1 && char.IsAsciiLetter(part[0]))
                return ["Key" + char.ToUpperInvariant(part[0])];
            if (part.Length == 1 && char.IsAsciiDigit(part[0]))
                return ["Digit" + part];
            if (keyWords.TryGetValue(part, out var codes))
                return codes;
            if (part.StartsWith("Arrow", StringComparison.Ordinal) || part.StartsWith("Numpad", StringComparison.Ordinal) ||
                part.StartsWith("Key", StringComparison.Ordinal) || part.StartsWith("Digit", StringComparison.Ordinal) ||
                (part.Length is 2 or 3 && part[0] == 'F' && part[1..].All(char.IsAsciiDigit)))
                return [part];
            return [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading keyboard label part {Part} failed.", part);
            throw;
        }
    }
}
