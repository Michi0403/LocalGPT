using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council participant pairing application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RoleName">Role name value supplied to the council participant pairing operation and used when producing its result.</param>
/// <param name="Participant">Participant value supplied to the council participant pairing operation and used when producing its result.</param>
/// <param name="PairedRoleName">Paired role name value supplied to the council participant pairing operation and used when producing its result.</param>
/// <param name="PairedParticipant">Paired participant value supplied to the council participant pairing operation and used when producing its result.</param>
internal sealed record CouncilParticipantPairing(
    string RoleName,
    string Participant,
    string PairedRoleName,
    string PairedParticipant);
