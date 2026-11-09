using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;
using LocalGPT.Services;
using LocalGPT.Services.GameEngine;
using LocalGPT.Services.GameEngine.Core;
using LocalGPT.Services.GameEngine.Council;
using LocalGPT.Services.GameEngine.Director;
using LocalGPT.Services.GameEngine.Duel;
using LocalGPT.Services.GameEngine.Primitives.Actions;
using LocalGPT.Services.GameEngine.Primitives.Autoplay;
using LocalGPT.Services.GameEngine.Primitives.Maps;
using LocalGPT.Services.GameEngine.Primitives.Rules;
using LocalGPT.Services.GameEngine.Primitives.Styles;
using LocalGPT.Services.GameEngine.Primitives.Views;
using LocalGPT.Services.GameEngine.Seeding;
using LocalGPT.Services.GameEngine.Store;

namespace LocalGPT
{
    /// <summary>
    /// Registers the generic LocalGPT game engine: core services, the primitive catalog that declarative rule rows name,
    /// the duel subsystem, the Council bridge, the game-project store and the system-game seed. All services are stateless
    /// singletons; game state lives in the authoritative session service.
    /// </summary>
    public static partial class Program
    {
        /// <summary>Adds every game-engine service and primitive to the service collection.</summary>
        /// <param name="services">Service collection of the application.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureGameEngine(IServiceCollection services, ILogger logger)
        {
            try
            {
                // Core services shared by all primitives.
                services.AddSingleton<IGameParameterReader, GameParameterReader>();
                services.AddSingleton<IGameTemplateFormatter, GameTemplateFormatter>();
                services.AddSingleton<IGameTextService, GameTextService>();
                services.AddSingleton<IGameValueSpecEvaluator, GameValueSpecEvaluator>();
                services.AddSingleton<IGameGridService, GameGridService>();
                services.AddSingleton<IGameAngleService, GameAngleService>();
                services.AddSingleton<IGameStableHashService, GameStableHashService>();
                services.AddSingleton<IGameTextUtilityService, GameTextUtilityService>();
                services.AddSingleton<IGameActorQueryService, GameActorQueryService>();
                services.AddSingleton<IGameTokenService, GameTokenService>();
                services.AddSingleton<IGameWorldService, GameWorldService>();
                services.AddSingleton<IGameMessageService, GameMessageService>();
                services.AddSingleton<IGameMapSupportService, GameMapSupportService>();
                services.AddSingleton<IGameStyleReader, GameStyleReader>();
                services.AddSingleton<IGameInputMapService, GameInputMapService>();
                services.AddSingleton<IGameTouchLayoutService, GameTouchLayoutService>();

                // Primitive catalog: rule rows and game profiles name these keys.
                services.AddSingleton<IGameMapGeneratorPrimitive, RoomsAndCorridorsMapGenerator>();
                services.AddSingleton<IGameMapGeneratorPrimitive, FixedRoomsArenaMapGenerator>();
                services.AddSingleton<IGameActionPrimitive, TurnActionPrimitive>();
                services.AddSingleton<IGameActionPrimitive, MoveRelativeActionPrimitive>();
                services.AddSingleton<IGameActionPrimitive, MoveGridActionPrimitive>();
                services.AddSingleton<IGameActionPrimitive, HitscanActionPrimitive>();
                services.AddSingleton<IGameActionPrimitive, ToggleFlagActionPrimitive>();
                services.AddSingleton<IGameActionPrimitive, SetTextActionPrimitive>();
                services.AddSingleton<IGameActionPrimitive, UseMarkerActionPrimitive>();
                services.AddSingleton<IGameRulePrimitive, ClearMoveBlockRulePrimitive>();
                services.AddSingleton<IGameRulePrimitive, NormalizeFacingRulePrimitive>();
                services.AddSingleton<IGameRulePrimitive, DecayVariableRulePrimitive>();
                services.AddSingleton<IGameRulePrimitive, SetVariableRulePrimitive>();
                services.AddSingleton<IGameRulePrimitive, MarkerGoalRulePrimitive>();
                services.AddSingleton<IGameRulePrimitive, ChaseAndContactRulePrimitive>();
                services.AddSingleton<IGameStylePrimitive, CharacterStylePrimitive>();
                services.AddSingleton<IGameStylePrimitive, TokenStylePrimitive>();
                services.AddSingleton<IGameStylePrimitive, LineStylePrimitive>();
                services.AddSingleton<IGameAutoplayPrimitive, ActionCycleAutoplayPrimitive>();
                services.AddSingleton<IGameAutoplayPrimitive, HuntAndExtractAutoplayPrimitive>();
                services.AddSingleton<IGameViewPrimitive, TextTemplateView>();
                services.AddSingleton<IGameViewPrimitive, FirstPersonRaycastView>();
                services.AddSingleton<IGameViewPrimitive, DuelStageView>();
                services.AddSingleton<IGameCouncilProgressionPrimitive, SingleEliminationTeamDuelProgression>();
                services.AddSingleton<IGamePrimitiveRegistry, GamePrimitiveRegistry>();
                services.AddSingleton<IGameEngineRuntimeService, GameEngineRuntimeService>();

                // Duel subsystem used by the duel view and the single-elimination progression.
                services.AddSingleton<IGameRigService, GameRigService>();
                services.AddSingleton<IDuelSettingsReader, DuelSettingsReader>();
                services.AddSingleton<IDuelRosterService, DuelRosterService>();
                services.AddSingleton<IDuelBracketService, DuelBracketService>();
                services.AddSingleton<IDuelTimelineService, DuelTimelineService>();
                services.AddSingleton<IDuelExchangeResolver, DuelExchangeResolver>();
                services.AddSingleton<IDuelTokenService, DuelTokenService>();
                services.AddSingleton<IDuelStoryboardService, DuelStoryboardService>();
                services.AddSingleton<IDuelFrameFactory, DuelFrameFactory>();
                services.AddSingleton<IDuelClassicRenderer, DuelClassicRenderer>();
                services.AddSingleton<IDuelBattleRenderer, DuelBattleRenderer>();
                services.AddSingleton<IDuelSummaryService, DuelSummaryService>();

                // Council bridge: role answers become engine turns; the GameDirector reads declarative predictions.
                services.AddSingleton<IGameCouncilTextReader, GameCouncilTextReader>();
                services.AddSingleton<IGameSpriteReader, GameSpriteReader>();
                services.AddSingleton<IGameAsciiFrameReader, GameAsciiFrameReader>();
                services.AddSingleton<IGameSpriteSimilarityService, GameSpriteSimilarityService>();
                services.AddSingleton<IGameCouncilLineupService, GameCouncilLineupService>();
                services.AddSingleton<IGameCouncilBridgeService, GameCouncilBridgeService>();
                services.AddSingleton<IGameDirectorPredictionService, GameDirectorPredictionService>();

                // Game projects: catalog/rule-row store and the embedded system-game seed.
                services.AddSingleton<IGameRulesetStoreService, GameRulesetStoreService>();
                services.AddSingleton<IGameProjectSeedService, GameProjectSeedService>();

                services.AddSingleton<ICouncilGameDirectorService, CouncilGameDirectorService>();
                services.AddSingleton<ICouncilGameSessionService, CouncilGameSessionService>();
                logger.LogDebug("Registered the generic game engine services and primitives.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Registering the generic game engine services failed.");
                throw;
            }
        }
    }
}
