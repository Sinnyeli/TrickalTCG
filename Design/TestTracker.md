# Test Tracker

Every known bug and core rule has an ID. Each ID has one or more **battle scenarios**
that run automatically as EditMode tests, so a fix can't quietly break again.

## Running the tests

1. In Unity, open **Window › General › Test Runner** and pick the **EditMode** tab.
2. Expand `Trickal.Battle.EditModeTests › BattleScenarioTests › Run`. Each scenario is listed as
   `<ID> <Method>`, for example `BUG-02 ShockTradeKillsBothUnits`.
3. Click **Run All**, or right-click one test to run it alone. A failure message says what was
   expected and what happened instead.

The **Regression** and **Rules** categories in the Test Runner's category filter show one group at a time.

## How it fits together

| File | Role |
| --- | --- |
| `Assets/Scripts/Testing/BattleSandbox.cs` | Builds a headless battle: the real managers, wired up with no scene, prefabs or UI. It also has helpers to create cards, summon units, attack and play cards. |
| `Assets/Scripts/Testing/BattleScenarios.cs` | The scenarios. Each one is a `[BattleScenario("ID", "summary")]` method. |
| `Assets/Scripts/Testing/ScenarioProbeEffect.cs` | A test effect that counts how often it fires (for deathrattles, on-draw effects and so on). |
| `Assets/Tests/Battle/Editor/BattleScenarioTests.cs` | Finds every scenario and runs it as its own test. |
| `Assets/Tests/Multiplayer/Editor/BattleActionTests.cs` | Older multiplayer action and deck validation tests. |

Everything under `Assets/Scripts/Testing` is wrapped in `#if UNITY_EDITOR`, so none of it ships in builds.

## Adding a scenario

1. Add the issue below with the next free ID (`BUG-xx` for a bug, `RULE-xx` for a game rule you want to protect).
2. In `BattleScenarios.cs`, add a method:

   ```csharp
   [BattleScenario("BUG-09", "Frozen units cannot attack")]
   public static void FrozenUnitsCannotAttack(BattleSandbox s)
   {
       RuntimeCard unit = s.Summon(2, 2, PlayerSide.Opponent);
       unit.ApplyFreeze();
       s.BeginTurn(PlayerSide.Opponent, 10);
       BattleSandbox.Expect(!s.Attack(unit, s.Summon(1, 1, PlayerSide.Player)), "A frozen unit attacked.");
   }
   ```

3. Run it in the Test Runner. It should fail before the fix and pass after.

Tip: test refused attacks from the **Opponent** seat. When the Player seat is refused, the game shows an
on-screen notice, which needs the battle UI.

## Bugs

| ID | Issue | Fix | Scenarios | Status |
| --- | --- | --- | --- | --- |
| BUG-01 | `GameManager.EndPlayerTurn` called itself forever (stack overflow) | `GameManager.cs`: forwards to `TurnManager.EndPlayerTurn` | `EndPlayerTurnPassesTheTurn` | Fixed |
| BUG-02 | A unit destroyed by Shock or `Kill()` came back to life when another unit died first in the same trade | `RuntimeCard.Kill` records lethal damage | `ShockTradeKillsBothUnits`, `KilledUnitIsNotRevivedByRecalculation` | Fixed |
| BUG-03 | Units paid mana after their Battlecry, so mana restored by a Battlecry was lost | `HandManager.PayAndPlayUnit` pays first and refunds if the play fails | `UnitPaysManaBeforeBattlecry`, `FailedUnitPlayKeepsMana` | Fixed |
| BUG-04 | `InitializeCombatStats` had an `else` attached to the wrong `if` (Rush/Stealth) | `RuntimeCard.InitializeCombatStats` rewritten | `SummonedUnitAttackReadiness` | Fixed |
| BUG-05 | Artifacts on a dead unit stayed in the Field zone | `BattlefieldManager.RemoveCard` unequips them after their deathrattles fire | `ArtifactsGoToGraveyardWithTheirUnit` | Fixed |
| BUG-06 | The two commanders were set up in a different order | `GameManager.SetUpCommander` used for both sides | `CommandersAreSetUpConsistently` | Fixed |
| BUG-07 | On-draw effects must fire even for cards burnt from a full hand | Already worked; now documented and tested | `BurntCardStillFiresOnDraw` | Protected |
| BUG-08 | Jubee's unlimited copies relied on the card name | `DeckRules.GetCopyLimit` checks card ID `A_JUBEE` | `JubeeLimitUsesCardID` | Fixed |
| BUG-09 | Pressing Play crashed Unity: `GameAudio.Awake` started music, which called `Get()` before the instance was registered and created another `GameAudio`, forever | `GameAudio.Awake` registers the instance first; empty clips return before `Get()` | Manual: press Play in any scene (play-mode only, so no EditMode scenario) | Fixed |

## Core rules

| ID | Rule | Scenarios |
| --- | --- | --- |
| RULE-01 | Taunt must be attacked before other units | `TauntBlocksOtherTargets` |
| RULE-02 | First Strike kills the defender before it can hit back | `FirstStrikeAvoidsRetaliation` |
| RULE-03 | Endure reduces each instance of damage by 1 | `EndureReducesDamage` |
| RULE-04 | A unit attacks once per turn | `UnitAttacksOncePerTurn` |

## Open questions

- `Assets/TrickalTCG-CardData-Localized-Import.csv` and `...Import(1).csv` hold different card data
  (Butter's name, Momo's cost and text, Frostnova Fridge stats). Decide which is current and delete the other.
- AI opponents start with 1 card while remote opponents draw the full starting hand (`GameManager.StartGame`).
  Confirm that's intended.
