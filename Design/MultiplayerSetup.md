# Trickal TCG remote-player alpha integration

The Play button now enters an automatic two-player queue through Unity Lobby. A waiting player hosts the gameplay simulation through Unity Relay using secure WebSockets. Another queued player joins automatically; no room UI or join code is shown. An unpaired host starts JubeeAI after approximately 10 seconds of queue waiting. Authentication/allocation time is additional, and a connection already in progress can receive up to 5 seconds of handshake grace. Service or connection errors display a retry message rather than silently claiming a human match succeeded.

## Install and enable services

1. Copy the included Assets, Packages/manifest.json, and Design files into the project, preserving paths. This is cumulative with the missing-spell and combat-notice updates. No scene or prefab assets are replaced.
2. Open Unity and let Package Manager resolve the new dependencies. The repository records Unity 2022.3.62f3. The integration uses Authentication 3.3.1, Lobby 1.2.2, Relay 1.0.5, Netcode for GameObjects 1.12.0, and Transport 2.2.1. The existing packages-lock.json is not overwritten; Unity regenerates dependency resolution.
3. The checkout already contains a Unity cloud project link named TrickalTCG. Verify that this is the intended project in Project Settings > Services, and enable/configure Authentication anonymous sign-in, Lobby, and Relay in that project's Unity Dashboard. Ensure the services can allocate Relay connections. No service credentials or secrets are embedded in the scripts.
4. Run Tools > Cards > Initialize All Available Card Effects if you have not initialized the preceding card update.
5. Open Window > General > Test Runner, choose EditMode, and run Trickal.Multiplayer.EditModeTests. Then perform the two-client checks below before uploading a WebGL build to itch.io.

The NetworkManager and matchmaking overlay are created at runtime. BattleDeckLoader finds the existing MatchSetup component if its inspector reference is empty; no new inspector wiring is required. Both human players receive the same starting-hand count. Existing AI starting-hand behavior is preserved.

## Gameplay contract

BattleActions accepts serializable requests with a match ID, request ID, turn number, runtime card ID, target ID, and action kind. Every RuntimeCard receives an instance ID distinct from its catalog CardID. The transport determines the remote seat; clients cannot select their own authoritative seat. The host validates deck construction, ownership, hand/field membership, turn, targets, mana, and the existing combat rules. Recent request IDs are deduplicated per seat. Both player input and JubeeAI enter this path for card play, targeted spells, equipment, combat, choices, end turn, and surrender. Forced attacks and internally generated effects remain synchronous host-side gameplay operations.

The guest does not run its own deck draws, turn timer, AI, battlecries, or damage resolution. It reconciles presentation snapshots by runtime ID. Its own side appears at the bottom. The guest payload includes its own hand, public fields/commanders, health, mana, deck counts, turn time, legal choice options, and game result. It excludes the host's ordinary hand identities and both deck orders. Stats/keywords include inherited equipment; the payload carries the three displayed equipment portraits per unit. Host and guest attacks animate locally from accepted attack events.

JubeeAI uses the same action requests, including target selection, artifact equipment, granted additional attacks, and card choices. It is disabled in a human match. Choice prompts block other card/combat/end-turn requests until resolved; surrender remains available. Turn timeout clears pending choices. Source-owner choices may be resolved during another player's turn.

Connection loss after a match has started ends the game: the disconnected guest loses on the host; loss of the host connection shows defeat on the guest. This initial integration does not migrate the host or reconnect an interrupted game. Leaving Battlefield closes the Relay connection and leaves/deletes the Lobby.

The visible ID/password login and local account deck files are preserved. Unity authentication is an anonymous service identity behind the scenes, separated by a hash-derived local account profile. This is not server-side ID/password registration or cloud deck storage. Test different local account IDs or different browser/device profiles. Publish identical game builds to both clients; the queue also compares a basic catalog fingerprint and protocol version.

## Validation available here

- All project and new test C# files pass syntax parsing; git diff --check passes.
- Source checks confirm shared AI requests, authoritative seat/match/turn checks, private-hand projection, WSS transport, queue availability filtering, disconnect handling, and unchanged scene/prefab assets.
- Six Unity EditMode tests are included for wrong-match rejection, wrong-turn rejection, cross-seat card rejection, duplicate request results, deck limits, and authoritative remote presentation stats/identity.
- Unity/package compilation, the included EditMode tests, actual UGS allocation, and two-client gameplay have not been executed in this environment. Service dashboard access and a Unity Editor runtime are required to verify them.

## Two-client checks

- Solo Play enters the queue, then starts JubeeAI once, with no later human replacement.
- Two separate logged-in clients using the same build pair and enter the same match. Test simultaneous Play as well as joining an already waiting player.
- Each client has its own side at the bottom; neither sees the other's ordinary hand faces. Commanders remain public.
- Play a unit, a spell targeting a unit/hero, and an artifact from each seat. Compare mana, field, health, hand counts, and deck counts.
- Select attacker/defender, test Taunt, Rush, frozen, and already-attacked rejection, and verify the attack animation on both clients.
- Test a generated card choice, a battlecry target choice, a random effect, Bomb's draw damage/replacement draw, and surrender.
- End Turn and timeout advance one authoritative turn and synchronize mana. An out-of-turn request cannot end the other player's turn.
- Cancel before/while the other client joins. Neither client should enter both an AI match and a human match.
- Close the guest browser, then separately test host closure. The remaining client receives the defined result and returns to deck selection.
- Repeat from the itch.io WebGL page, not just the Editor. Keep the host tab active while playing; browser background suspension can interrupt a player-hosted match.

## Reference APIs

- https://docs.unity.com/relay/relay-and-ngo-standalone
- https://docs.unity.cn/Packages/com.unity.services.lobby@1.2/api/Unity.Services.Lobbies.QuickJoinLobbyOptions.html
- https://mp-docs.dl.it.unity3d.com/netcode/1.12.0/advanced-topics/message-system/custom-messages/
