using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Builds and poses ASCII stick rigs from the <c>rig</c> parameters of archetype rows: joints, hashed or keyword glyphs,
/// pose shifts (x multiplied by the facing direction), Bresenham links and joint markers.
/// </summary>
/// <param name="parameters">Reads the rig parameters.</param>
/// <param name="hashes">Hashes rig identities.</param>
/// <param name="logger">Receives rig diagnostics.</param>
public sealed class GameRigService(
    IGameParameterReader parameters,
    IGameStableHashService hashes,
    ILogger<GameRigService> logger) : IGameRigService
{
    /// <inheritdoc />
    public CouncilAsciiActorRig Build(GameProjectArchetype archetype, string identity, string descriptor)
    {
        try
        {
            var rig = parameters.ReadObject(parameters.Parse(archetype.ParametersJson), "rig");
            var material = parameters.ReadBool(rig, "hashIncludesDescriptor", false)
                ? (identity ?? string.Empty) + "|" + (descriptor ?? string.Empty)
                : identity ?? string.Empty;
            var hash = hashes.Hash(material);
            return new CouncilAsciiActorRig
            {
                RigKey = $"{parameters.ReadString(rig, "rigKeyPrefix", archetype.ArchetypeKey)}:{Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant()}",
                Width = parameters.ReadInt(rig, "width", 13),
                Height = parameters.ReadInt(rig, "height", 6),
                HeadGlyph = Glyph(rig, "head", descriptor ?? string.Empty, hash[0]),
                TorsoGlyph = Glyph(rig, "torso", descriptor ?? string.Empty, hash[1]),
                Joints = parameters.ReadObjects(rig, "joints")
                    .Select(joint => new CouncilAsciiActorJoint
                    {
                        Name = parameters.ReadString(joint, "name", string.Empty),
                        X = parameters.ReadInt(joint, "x", 0),
                        Y = parameters.ReadInt(joint, "y", 0)
                    })
                    .ToList()
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the {ArchetypeKey} rig failed.", archetype.ArchetypeKey);
            throw;
        }
    }

    /// <inheritdoc />
    public List<string> Render(GameProjectArchetype archetype, CouncilAsciiActorRig rig, string pose, bool facingRight)
    {
        try
        {
            var rigParameters = parameters.ReadObject(parameters.Parse(archetype.ParametersJson), "rig");
            var width = Math.Clamp(rig.Width, 9, 28);
            var height = Math.Clamp(rig.Height, 4, 10);
            var canvas = Enumerable.Range(0, height).Select(_ => Enumerable.Repeat(' ', width).ToArray()).ToArray();
            var joints = new Dictionary<string, (int X, int Y)>(StringComparer.OrdinalIgnoreCase);
            foreach (var joint in rig.Joints)
                joints[joint.Name] = (joint.X, joint.Y);

            var forward = facingRight ? 1 : -1;
            var poses = parameters.ReadObject(rigParameters, "poses");
            foreach (var shift in parameters.ReadObjects(poses, pose))
            {
                var name = parameters.ReadString(shift, "joint", string.Empty);
                if (!joints.TryGetValue(name, out var point))
                    continue;
                joints[name] = (
                    Math.Clamp(point.X + forward * parameters.ReadInt(shift, "dx", 0), 0, width - 1),
                    Math.Clamp(point.Y + parameters.ReadInt(shift, "dy", 0), 0, height - 1));
            }

            if (!facingRight)
            {
                foreach (var key in joints.Keys.ToList())
                {
                    var point = joints[key];
                    joints[key] = (width - 1 - point.X, point.Y);
                }
            }

            foreach (var link in parameters.ReadObjects(rigParameters, "links"))
                Link(canvas, joints, parameters.ReadString(link, "from", string.Empty), parameters.ReadString(link, "to", string.Empty));
            foreach (var marker in parameters.ReadObjects(rigParameters, "markers"))
            {
                if (!joints.TryGetValue(parameters.ReadString(marker, "joint", string.Empty), out var point))
                    continue;
                var glyph = parameters.ReadString(marker, "glyph", "*");
                Put(canvas, point, glyph switch
                {
                    "@head" => rig.HeadGlyph,
                    "@torso" => rig.TorsoGlyph,
                    _ => glyph.Length == 0 ? ' ' : glyph[0]
                });
            }
            return canvas.Select(row => new string(row).TrimEnd()).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the {ArchetypeKey} rig in pose {Pose} failed.", archetype.ArchetypeKey, pose);
            throw;
        }
    }

    /// <summary>
    /// Picks a rig glyph: the first keyword rule matching the descriptor, else the default glyph, else a hashed glyph.
    /// </summary>
    /// <param name="rig">The rig parameters.</param>
    /// <param name="part">The part prefix, <c>head</c> or <c>torso</c>.</param>
    /// <param name="descriptor">The free-text descriptor.</param>
    /// <param name="hashByte">The hash byte selecting a hashed glyph.</param>
    /// <returns>The glyph.</returns>
    private char Glyph(JsonElement rig, string part, string descriptor, byte hashByte)
    {
        try
        {
            foreach (var rule in parameters.ReadObjects(rig, $"{part}Keywords"))
            {
                if (parameters.ReadStrings(rule, "words").Any(word => word.Length > 0 && descriptor.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    var glyph = parameters.ReadString(rule, "glyph", "*");
                    return glyph.Length == 0 ? ' ' : glyph[0];
                }
            }
            var fixedGlyph = parameters.ReadString(rig, $"{part}Default", string.Empty);
            if (fixedGlyph.Length > 0)
                return fixedGlyph[0];
            var glyphs = parameters.ReadString(rig, $"{part}Glyphs", "@");
            return glyphs.Length == 0 ? '@' : glyphs[hashByte % glyphs.Length];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Choosing the rig {RigPart} glyph failed.", part);
            throw;
        }
    }

    /// <summary>
    /// Draws a Bresenham link between two joints, leaving the joint cells free for markers.
    /// </summary>
    /// <param name="canvas">The rig canvas.</param>
    /// <param name="joints">The posed joints.</param>
    /// <param name="first">The first joint.</param>
    /// <param name="second">The second joint.</param>
    private void Link(char[][] canvas, Dictionary<string, (int X, int Y)> joints, string first, string second)
    {
        try
        {
            if (!joints.TryGetValue(first, out var a) || !joints.TryGetValue(second, out var b))
                return;
            var x = a.X;
            var y = a.Y;
            var dx = Math.Abs(b.X - a.X);
            var sx = a.X < b.X ? 1 : -1;
            var dy = -Math.Abs(b.Y - a.Y);
            var sy = a.Y < b.Y ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                if ((x, y) != a && (x, y) != b)
                {
                    var glyph = a.Y == b.Y ? '-' : a.X == b.X ? '|' : ((b.X - a.X) * (b.Y - a.Y) > 0 ? '\\' : '/');
                    Put(canvas, (x, y), glyph);
                }
                if (x == b.X && y == b.Y)
                    break;
                var doubled = 2 * error;
                if (doubled >= dy) { error += dy; x += sx; }
                if (doubled <= dx) { error += dx; y += sy; }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Linking rig joints {FirstJoint} and {SecondJoint} failed.", first, second);
            throw;
        }
    }

    /// <summary>
    /// Writes a glyph inside the canvas bounds.
    /// </summary>
    /// <param name="canvas">The rig canvas.</param>
    /// <param name="point">The cell.</param>
    /// <param name="glyph">The glyph.</param>
    private void Put(char[][] canvas, (int X, int Y) point, char glyph)
    {
        try
        {
            if (point.Y >= 0 && point.Y < canvas.Length && point.X >= 0 && point.X < canvas[point.Y].Length)
                canvas[point.Y][point.X] = glyph;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Writing a rig glyph failed.");
            throw;
        }
    }
}
