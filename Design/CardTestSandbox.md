# Card Test Sandbox

Based on GitHub main commit 799196c (Upload).

## Install and run

1. Extract the ZIP into the Unity project root, alongside Assets, Packages and ProjectSettings. Keep the included .meta files.
2. Wait for Unity to compile. Stop any running Play Mode session.
3. Open **Tools > Cards > Open Card Test Scene**, or double-click **Assets/Scenes/CardTest.unity**.
4. Press Play. No login or saved deck is required. Both fields and hands start empty; both players start with 10/10 mana and 20 health. AI and the automatic turn timer are disabled.
5. Use the left panel. Hide it whenever you need unobstructed access to the board; use Show Card Test to reopen it.
6. Stop Play Mode when finished. Changes to runtime units, decks, mana and health are temporary; card ScriptableObjects and account saves are not edited by the controls.

Do not add CardTest to player Build Settings. The harness is Editor-only, and the build validator rejects accidental inclusion. Normal Battlefield/network scenes and scripts are unchanged. The existing ESC menu does not recognize CardTest as a normal Battlefield; use Stop Play Mode to exit.

If you change the Battlefield layout later, **Tools > Cards > Rebuild Card Test From Battlefield** regenerates the copied sandbox. This replaces custom changes made to CardTest, after confirmation, and preserves Battlefield itself.

## How to use the controls

- **Player/Opponent controls** chooses the owner for setup cards and normal action requests. It does not silently change turns. **End active turn** runs normal end/start-turn effects, draws and mana ramp. Use the Opponent controls during the opponent turn.
- Search by card name or ID. The list includes every entry in CardDatabase, including non-collectible generated cards. A card absent from that database must be initialized/registered before it can be selected.
- **Add to hand** respects the seven-card limit. **Create as Commander** applies only to Apostles. Use one Commander per side when testing Commander behavior.
- **Summon** costs no mana and respects field capacity. Disable **Trigger Battlecry** to suppress only the entering unit's own battlecry; summon reactions, passive effects and other units' triggers still run.
- **Play/cast/equip** uses the production BattleActions pipeline. Turn ownership, mana, targeting and pending-choice restrictions still apply. Choose a hand card, then a target unit or player.
- Select **Actor** and **Target** separately in the unit lists. Select the correct control side before normal attacks. **Ready actor** is a setup shortcut that clears freeze and turn attack restrictions; it does not run start-turn abilities.
- Unit damage/heal/buffs/silence/kill use runtime rule methods. For setup damage, Target is an optional damage source. Do not use setup damage as a substitute for a combat test: use the normal Attack buttons to exercise Shock, First Strike, retaliation and on-attack triggers.
- **Set stats** respects stat locks such as Sylphyr's ability. **Grant keyword** grants it permanently for the current runtime unit. Endure remains governed by production rules.
- Player health setting is setup-only and does not declare defeat at zero. **Damage player** uses the normal damage/death rules.
- **Add as next draw** inserts before all existing draw-pile cards; the next eight are displayed in draw order. **Add to deck bottom** preserves existing order. **Draw normally** runs on-draw handling. End-turn effects can independently change these piles.
- Resolve Player card choices using the normal popup. Opponent card choices and both sides' valid pending unit/player targets are selectable in the panel. Pending effects must resolve before normal actions continue.
- **Clear field/hand** silently removes setup cards without death triggers or discard-pile insertion; it is not a gameplay action. **Kill actor** resolves normal death processing instead.
- **Reset entire test** reloads CardTest, clearing match state, cards, selections and temporary rules. Use it between independent test cases.
- The event log captures the existing Console messages and exceptions, up to 160 entries. It is not a complete effect replay. **Copy log** copies the captured text for a bug report.

## Quick manual checks

### Momo
Search A_MOMO; summon with Battlecry enabled. Expect Momo plus one separate 2/2 clone, with no repeated summoning. Reset; disable Battlecry and repeat. Expect one Momo. If this fails, check the underlying card configuration; the harness does not change card definitions.

### Targeted spell
Add a targeted spell to Player hand. Summon a target for the Opponent. Select the spell in the hand list and the target using Target; cast on that unit. Check mana payment, hand removal and effect resolution. Repeat while it is the Opponent's turn to verify rejection.

### End-turn effect
Summon the Apostle, configure its required board, then End active turn. Repeat on the Opponent side to check owner-specific timing without AI interference.

### Shock versus Endure
Summon an attacker and defender; grant Shock to the attacker and Endure to the defender. Set attack to 1, ready the attacker, and attack through the normal action control. Under the agreed design, zero actual damage should not trigger Shock. This sandbox intentionally exposes current gameplay behavior; it does not implement the previously discussed Shock/Stealth rule corrections.

### Deck interaction
Add cards as next draw in reverse of the desired order. Check the displayed next-draw list. Draw normally or advance turns to verify on-draw effects and reactions.

## Verification and limits

- New C# files pass tree-sitter syntax parsing.
- Scene structure check confirms every existing Battlefield object/reference/layout is retained exactly, plus the sandbox root.
- Sandbox reflection fields were checked against this commit's runtime definitions; reflection is confined to Editor-only setup controls.
- No existing production files or Build Settings were changed.
- Unity compilation and Play Mode execution were not possible in the authoring environment. First import and Play Mode startup require local verification.
- This is a local two-sided testing harness. It does not test network transport, remote snapshot fidelity, saved setup persistence, deterministic random replay, or provide automated pass/fail assertions.
