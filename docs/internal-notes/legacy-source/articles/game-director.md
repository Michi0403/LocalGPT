# GameDirector runtime

The GameDirector is the authoritative game engine. Player controllers, creature Councils and reactive map objects submit proposals; they do not directly mutate the game state.

Every control passes through the GameDirector before the session advances. Creature and reactive-object subdirectors can predict bounded consequences, while the deterministic resolver remains the final authority. A later low-parameter model can be assigned by user configuration without changing the action contract.

Runtime classes describe sessions, maps, players, directors, creatures, reactive objects and frames. Factory-owned actor descriptors map each creature and reactive object to a `World Actor` Council assignment slot while keeping the actual state transition inside the GameDirector. The current low-parameter model name is configuration metadata for the Council workflow; deterministic legality and turn checks remain authoritative.
