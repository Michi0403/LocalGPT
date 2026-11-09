using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Single-elimination bracket over <see cref="GameProgressionState"/>: <c>RoundEntrantKeys</c> are paired from index
/// <c>Match</c>, an odd last entrant gets a bye, winners collect in <c>NextRoundEntrantKeys</c>, and the last remaining
/// entrant becomes <c>ChampionKey</c>.
/// </summary>
/// <param name="logger">Receives bracket diagnostics.</param>
public sealed class DuelBracketService(ILogger<DuelBracketService> logger) : IDuelBracketService
{
    /// <inheritdoc />
    public void Prepare(CouncilGameSessionState session)
    {
        try
        {
            var progression = session.Progression;
            while (string.IsNullOrWhiteSpace(progression.ChampionKey))
            {
                if (progression.RoundEntrantKeys.Count == 1)
                {
                    progression.ChampionKey = progression.RoundEntrantKeys[0];
                    logger.LogInformation("Game session {SessionId} bracket decided champion {EntrantKey}.", session.Id, progression.ChampionKey);
                    break;
                }

                if (progression.Match < progression.RoundEntrantKeys.Count)
                {
                    if (progression.Match == progression.RoundEntrantKeys.Count - 1)
                    {
                        progression.NextRoundEntrantKeys.Add(progression.RoundEntrantKeys[progression.Match]);
                        progression.Match++;
                        continue;
                    }
                    break;
                }

                progression.RoundEntrantKeys = progression.NextRoundEntrantKeys.ToList();
                progression.NextRoundEntrantKeys.Clear();
                progression.Match = 0;
                progression.Exchange = 0;
                progression.Round++;
                logger.LogDebug("Game session {SessionId} bracket entered round {Round} with {EntrantCount} entrant(s).", session.Id, progression.Round, progression.RoundEntrantKeys.Count);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing the bracket of session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> CurrentPairKeys(CouncilGameSessionState session)
    {
        try
        {
            var progression = session.Progression;
            if (!string.IsNullOrWhiteSpace(progression.ChampionKey) || progression.Match + 1 >= progression.RoundEntrantKeys.Count)
                return [];
            return [progression.RoundEntrantKeys[progression.Match], progression.RoundEntrantKeys[progression.Match + 1]];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the current bracket pair of session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public void AdvanceWinner(CouncilGameSessionState session, GameActorState winner)
    {
        try
        {
            session.Progression.NextRoundEntrantKeys.Add(winner.Key);
            session.Progression.Match += 2;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Advancing bracket winner {EntrantKey} failed.", winner.Key);
            throw;
        }
    }
}
