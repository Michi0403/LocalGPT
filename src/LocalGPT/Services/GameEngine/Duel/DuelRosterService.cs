using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Owns entrant owners and their team members: creation from Council seeds, active member, switching, bench rest and team health.
/// Member actors reference their owner through <see cref="GameActorState.OwnerKey"/>; owners record their active member in the
/// text <c>activeMember</c>, their action in <c>lastAction</c> and their switch and win counters in the stats <c>switches</c> and <c>wins</c>.
/// </summary>
/// <param name="parameters">Reads naming templates, text limits and sprite parameters.</param>
/// <param name="formatter">Formats naming templates.</param>
/// <param name="textUtility">Compacts texts and shortens model labels.</param>
/// <param name="rigs">Builds owner and member rigs.</param>
/// <param name="bracket">Prepares the bracket after initialization.</param>
/// <param name="logger">Receives roster diagnostics.</param>
public sealed class DuelRosterService(
    IGameParameterReader parameters,
    IGameTemplateFormatter formatter,
    IGameTextUtilityService textUtility,
    IGameRigService rigs,
    IDuelBracketService bracket,
    ILogger<DuelRosterService> logger) : IDuelRosterService
{
    /// <inheritdoc />
    public void Initialize(GameEngineContext context, DuelSettings settings, IReadOnlyList<GameEntrantSeed> entrants)
    {
        try
        {
            var session = context.Session;
            var ruleset = context.Ruleset;
            var ownerArchetype = Archetype(ruleset, settings.OwnerArchetype);
            var memberArchetype = Archetype(ruleset, settings.MemberArchetype);
            var contestants = entrants
                .Where(item => !string.IsNullOrWhiteSpace(item.OwnerModelName) && !string.IsNullOrWhiteSpace(item.MemberModelName))
                .DistinctBy(item => item.MemberModelName, StringComparer.OrdinalIgnoreCase)
                .Take(settings.MaximumEntrants)
                .ToList();
            if (contestants.Count < settings.MinimumEntrants)
                throw new InvalidOperationException($"Game '{session.GameKey}' requires at least {settings.MinimumEntrants} distinct owner/member pairings.");

            var limits = parameters.ReadObject(settings.Parameters, "textLimits");
            var sprite = parameters.ReadObject(settings.Parameters, "sprite");
            var defaultAction = settings.OwnerActions.FirstOrDefault(action => action.IsDefault)?.Key ?? string.Empty;
            session.Actors.RemoveAll(actor =>
                string.Equals(actor.ArchetypeKey, ownerArchetype.ArchetypeKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actor.ArchetypeKey, memberArchetype.ArchetypeKey, StringComparison.OrdinalIgnoreCase));
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ownerKeys = new List<string>();
            for (var index = 0; index < contestants.Count; index++)
            {
                var seed = contestants[index];
                var numberTokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["number"] = index + 1 };
                var ownerKey = formatter.Format(parameters.ReadString(settings.Parameters, "entrantKeyTemplate", "entrant-{number:00}"), numberTokens);
                var memberModel = seed.MemberModelName.Trim();
                var fallbackName = formatter.Format(parameters.ReadString(settings.Parameters, "memberNameFallbackTemplate", "Member {number}"), numberTokens);
                var name = SafeName(SeedText(seed, "member.name"), fallbackName, parameters.ReadInt(limits, "member.name", 28));
                var species = textUtility.Compact(SeedText(seed, "member.species"), parameters.ReadInt(limits, "member.species", 36));
                var style = textUtility.Compact(SeedText(seed, "member.style"), parameters.ReadInt(limits, "member.style", 36));
                var form = textUtility.Compact(SeedText(seed, "member.form"), parameters.ReadInt(limits, "member.form", 48));
                var trait = textUtility.Compact(SeedText(seed, "member.trait"), parameters.ReadInt(limits, "member.trait", 48));
                var voice = textUtility.Compact(SeedText(seed, "member.voice"), parameters.ReadInt(limits, "member.voice", 72));
                var owner = new GameActorState
                {
                    Key = ownerKey,
                    ArchetypeKey = ownerArchetype.ArchetypeKey,
                    Role = ownerArchetype.Role,
                    Name = seed.OwnerModelName.Trim(),
                    Glyph = ownerArchetype.Glyph,
                    IsActive = true,
                    Rig = rigs.Build(ownerArchetype, seed.OwnerModelName, string.Empty)
                };
                owner.Texts["model"] = seed.OwnerModelName.Trim();
                owner.Texts["memberModel"] = memberModel;
                owner.Texts["greeting"] = textUtility.Compact(SeedText(seed, "owner.greeting"), parameters.ReadInt(limits, "owner.greeting", 88));
                owner.Texts["lastAction"] = defaultAction;
                owner.Stats["switches"] = 0;
                owner.Stats["wins"] = 0;

                if (!usedNames.Add(name))
                {
                    var qualified = name;
                    var added = false;
                    foreach (var template in parameters.ReadStrings(settings.Parameters, "duplicateNameTemplates"))
                    {
                        qualified = SafeName(formatter.Format(template, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["name"] = name,
                            ["memberShortModel"] = textUtility.ShortModel(memberModel),
                            ["number"] = index + 1
                        }), fallbackName, parameters.ReadInt(limits, "member.name", 28));
                        if (usedNames.Add(qualified))
                        {
                            added = true;
                            break;
                        }
                    }
                    if (!added)
                        usedNames.Add(qualified);
                    name = qualified;
                }

                var palette = SanitizePalette(sprite, seed.SpritePalette);
                var (rows, kind) = SanitizeSprite(sprite, seed.SpriteRows, seed.SpriteKind);
                var spriteAuthor = string.IsNullOrWhiteSpace(seed.SpriteAuthorModelName) ? memberModel : seed.SpriteAuthorModelName;
                var variants = parameters.ReadObjects(sprite, "paletteVariants");
                var rosterCount = Math.Clamp(settings.MembersPerOwner, 1, settings.MemberNameSuffixes.Count);
                var members = new List<GameActorState>();
                for (var slot = 0; slot < rosterCount; slot++)
                {
                    var slotTokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["name"] = name,
                        ["slot"] = slot + 1,
                        ["form"] = form,
                        ["trait"] = trait,
                        ["owner"] = ownerKey
                    };
                    var memberName = slot == 0
                        ? name
                        : SafeName(name + settings.MemberNameSuffixes[slot],
                            formatter.Format(parameters.ReadString(settings.Parameters, "reserveNameFallbackTemplate", "{name}-{slot}"), slotTokens),
                            parameters.ReadInt(limits, "member.name", 28));
                    var memberForm = slot == 0 ? form : textUtility.Compact(formatter.Format(parameters.ReadString(settings.Parameters, "reserveFormTemplate", "{form}"), slotTokens), parameters.ReadInt(limits, "member.form", 48));
                    var memberTrait = slot == 0 ? trait : textUtility.Compact(formatter.Format(parameters.ReadString(settings.Parameters, "reserveTraitTemplate", "{trait}"), slotTokens), parameters.ReadInt(limits, "member.trait", 48));
                    var memberTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["species"] = species,
                        ["style"] = style,
                        ["form"] = memberForm,
                        ["trait"] = memberTrait,
                        ["voice"] = voice,
                        ["spriteAuthor"] = spriteAuthor
                    };
                    var descriptor = string.Join(' ', settings.RigDescriptorFields.Select(field => memberTexts.TryGetValue(field, out var value) ? value : string.Empty));
                    var member = new GameActorState
                    {
                        Key = formatter.Format(parameters.ReadString(settings.Parameters, "memberKeyTemplate", "{owner}-member-{slot:00}"), slotTokens),
                        ArchetypeKey = memberArchetype.ArchetypeKey,
                        Role = memberArchetype.Role,
                        Name = memberName,
                        Glyph = memberArchetype.Glyph,
                        OwnerKey = ownerKey,
                        IsActive = slot == 0,
                        Texts = memberTexts,
                        Rig = rigs.Build(memberArchetype, $"{memberModel}|{slot}", descriptor),
                        SpriteRows = [.. rows],
                        SpriteKind = kind,
                        SpritePalette = VariantPalette(variants, palette, slot)
                    };
                    member.Stats[settings.HealthStat] = settings.StartingHealth;
                    member.Stats[$"{settings.HealthStat}:max"] = settings.StartingHealth;
                    member.Stats["resting"] = slot == 0 ? 0 : 1;
                    member.Stats["rested"] = 0;
                    members.Add(member);
                }
                owner.Texts["activeMember"] = members[0].Key;
                session.Actors.Add(owner);
                session.Actors.AddRange(members);
                ownerKeys.Add(ownerKey);
            }

            var progression = session.Progression;
            session.Timeline.Clear();
            progression.TimelineSequence = 0;
            progression.RoundEntrantKeys = ownerKeys;
            progression.NextRoundEntrantKeys = [];
            progression.Match = 0;
            progression.Round = 1;
            progression.Exchange = 0;
            progression.ChampionKey = string.Empty;
            progression.Initialized = true;
            bracket.Prepare(session);
            logger.LogInformation("Game session {SessionId} initialized {EntrantCount} duel entrant(s) with {MemberCount} member(s) each.",
                session.Id, ownerKeys.Count, Math.Clamp(settings.MembersPerOwner, 1, settings.MemberNameSuffixes.Count));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Initializing the duel roster of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public List<GameActorState> Owners(CouncilGameSessionState session, DuelSettings settings)
    {
        try
        {
            return session.Actors.Where(actor => string.Equals(actor.ArchetypeKey, settings.OwnerArchetype, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing duel owners failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public GameActorState? Find(CouncilGameSessionState session, string? key)
    {
        try
        {
            return string.IsNullOrWhiteSpace(key)
                ? null
                : session.Actors.FirstOrDefault(actor => string.Equals(actor.Key, key, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding duel actor {ActorKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public List<GameActorState> Members(CouncilGameSessionState session, GameActorState owner)
    {
        try
        {
            return session.Actors.Where(actor => string.Equals(actor.OwnerKey, owner.Key, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing the members of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public GameActorState? ActiveMember(CouncilGameSessionState session, GameActorState owner)
    {
        try
        {
            var members = Members(session, owner);
            var activeKey = owner.Texts.TryGetValue("activeMember", out var key) ? key : string.Empty;
            return members.FirstOrDefault(member => string.Equals(member.Key, activeKey, StringComparison.OrdinalIgnoreCase))
                ?? members.FirstOrDefault(member => member.IsActive)
                ?? members.FirstOrDefault();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding the active member of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public int Health(DuelSettings settings, GameActorState? member)
    {
        try
        {
            return member is not null && member.Stats.TryGetValue(settings.HealthStat, out var health) ? health : 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading duel member health failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public int TeamHealth(CouncilGameSessionState session, DuelSettings settings, GameActorState owner)
    {
        try
        {
            return Members(session, owner).Sum(member => Math.Max(0, Health(settings, member)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Summing the team health of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public bool HasHealthyMember(CouncilGameSessionState session, DuelSettings settings, GameActorState owner)
    {
        try
        {
            return Members(session, owner).Any(member => Health(settings, member) > 0);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing the team health of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public void Sync(CouncilGameSessionState session, GameActorState owner)
    {
        try
        {
            var active = ActiveMember(session, owner);
            if (active is null)
                return;
            owner.Texts["activeMember"] = active.Key;
            foreach (var member in Members(session, owner))
                member.IsActive = string.Equals(member.Key, active.Key, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Synchronizing the active member of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public bool Switch(CouncilGameSessionState session, DuelSettings settings, GameActorState owner, string requestedTarget, bool countAgainstLimit)
    {
        try
        {
            var members = Members(session, owner);
            if (members.Count <= 1)
                return false;
            if (countAgainstLimit && owner.Stats.GetValueOrDefault("switches") >= settings.MaximumSwitchesPerMatch)
                return false;

            var current = ActiveMember(session, owner);
            var target = !string.IsNullOrWhiteSpace(requestedTarget)
                ? members.FirstOrDefault(member => Health(settings, member) > 0 && !member.IsActive &&
                    (member.Name.Contains(requestedTarget, StringComparison.OrdinalIgnoreCase) ||
                     member.Key.Contains(requestedTarget, StringComparison.OrdinalIgnoreCase)))
                : null;
            target ??= members.FirstOrDefault(member => Health(settings, member) > 0 && !member.IsActive);
            if (target is null)
                return false;

            if (current is not null)
            {
                current.IsActive = false;
                current.Stats["resting"] = Health(settings, current) > 0 ? 1 : 0;
            }
            target.IsActive = true;
            target.Stats["resting"] = 0;
            owner.Texts["activeMember"] = target.Key;
            if (countAgainstLimit)
                owner.Stats["switches"] = owner.Stats.GetValueOrDefault("switches") + 1;
            Sync(session, owner);
            logger.LogDebug("Duel owner {OwnerKey} switched to {MemberKey} (counted={Counted}).", owner.Key, target.Key, countAgainstLimit);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Switching the active member of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public void RestBench(CouncilGameSessionState session, DuelSettings settings, GameActorState owner)
    {
        try
        {
            foreach (var member in Members(session, owner).Where(member => !member.IsActive && Health(settings, member) > 0))
            {
                member.Stats["resting"] = 1;
                member.Stats["rested"] = member.Stats.GetValueOrDefault("rested") + 1;
                member.Stats[settings.HealthStat] = Math.Min(settings.StartingHealth, Health(settings, member) + settings.BenchRecoveryPerExchange);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resting the bench of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <summary>
    /// Finds an archetype row.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="key">The archetype key.</param>
    /// <returns>The archetype.</returns>
    private GameProjectArchetype Archetype(GameRuleset ruleset, string key)
    {
        try
        {
            return ruleset.Archetypes.FirstOrDefault(item => string.Equals(item.ArchetypeKey, key, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"The duel ruleset has no archetype '{key}'.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding duel archetype {ArchetypeKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Reads an entrant seed text.
    /// </summary>
    /// <param name="seed">The seed.</param>
    /// <param name="key">The text key.</param>
    /// <returns>The text or an empty string.</returns>
    private string SeedText(GameEntrantSeed seed, string key)
    {
        try
        {
            return seed.Texts.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading entrant text {TextKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Compacts a name and falls back when it is empty.
    /// </summary>
    /// <param name="value">The name.</param>
    /// <param name="fallback">The fallback name.</param>
    /// <param name="maximumLength">The maximum length.</param>
    /// <returns>The safe name.</returns>
    private string SafeName(string? value, string fallback, int maximumLength)
    {
        try
        {
            var compact = textUtility.Compact(value, maximumLength);
            return string.IsNullOrWhiteSpace(compact) ? fallback : compact;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a safe duel name failed.");
            throw;
        }
    }

    /// <summary>
    /// Clamps the declared palette and fills missing entries from the sprite parameter <c>defaultPalette</c>.
    /// </summary>
    /// <param name="sprite">The sprite parameters.</param>
    /// <param name="palette">The declared palette.</param>
    /// <returns>The palette.</returns>
    private List<int> SanitizePalette(JsonElement sprite, IReadOnlyList<int> palette)
    {
        try
        {
            var defaults = parameters.ReadInts(sprite, "defaultPalette");
            return Enumerable.Range(0, defaults.Count)
                .Select(index => index < palette.Count ? Math.Clamp(palette[index], 0, 255) : defaults[index])
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sanitizing a sprite palette failed.");
            throw;
        }
    }

    /// <summary>
    /// Keeps a usable sprite within the declared pixel or ASCII bounds.
    /// </summary>
    /// <param name="sprite">The sprite parameters.</param>
    /// <param name="rows">The drawn rows.</param>
    /// <param name="kind">The drawn kind.</param>
    /// <returns>The sanitized rows and kind; empty when unusable.</returns>
    private (List<string> Rows, string Kind) SanitizeSprite(JsonElement sprite, IReadOnlyList<string> rows, string kind)
    {
        try
        {
            if (rows.Count == 0)
                return (new List<string>(), string.Empty);
            if (string.Equals(kind, "pixel", StringComparison.Ordinal))
            {
                var legend = parameters.ReadString(sprite, "pixelLegend", ".#");
                var empty = legend.Length == 0 ? '.' : legend[0];
                var pixelRows = rows.Take(parameters.ReadInt(sprite, "maximumPixelRows", 16))
                    .Select(row => new string((row ?? string.Empty).Take(parameters.ReadInt(sprite, "maximumPixelColumns", 20))
                        .Select(cell => legend.Contains(cell) ? cell : empty).ToArray()))
                    .ToList();
                return pixelRows.Any(row => row.Any(cell => cell != empty)) ? (pixelRows, "pixel") : (new List<string>(), string.Empty);
            }
            if (string.Equals(kind, "ascii", StringComparison.Ordinal))
            {
                var asciiRows = rows.Take(parameters.ReadInt(sprite, "maximumAsciiRows", 14))
                    .Select(row => new string((row ?? string.Empty).Take(parameters.ReadInt(sprite, "maximumAsciiColumns", 36))
                        .Select(cell => cell >= ' ' && cell <= '~' ? cell : ' ').ToArray()).TrimEnd())
                    .ToList();
                return asciiRows.Any(row => row.Length > 0) ? (asciiRows, "ascii") : (new List<string>(), string.Empty);
            }
            return (new List<string>(), string.Empty);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sanitizing a member sprite failed.");
            throw;
        }
    }

    /// <summary>
    /// Rearranges the palette for reserve slots with the sprite parameter <c>paletteVariants</c> (index lists cycled by slot).
    /// </summary>
    /// <param name="variants">The variant index lists.</param>
    /// <param name="palette">The member palette.</param>
    /// <param name="slot">The zero-based slot.</param>
    /// <returns>The slot palette.</returns>
    private List<int> VariantPalette(IReadOnlyList<JsonElement> variants, List<int> palette, int slot)
    {
        try
        {
            if (variants.Count == 0)
                return [.. palette];
            var variant = variants[slot % variants.Count];
            var indexes = parameters.ReadInts(variant, "order");
            return indexes.Count == 0
                ? [.. palette]
                : indexes.Select(index => index >= 0 && index < palette.Count ? palette[index] : 0).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a reserve palette variant failed.");
            throw;
        }
    }
}
