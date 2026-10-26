#if UNITY_EDITOR
using System;
using System.Collections.Generic;

/// <summary>
/// Marks a method in <see cref="BattleScenarios"/> as a runnable scenario.
/// <paramref name="id"/> matches a row in Design/TestTracker.md (BUG-xx for regressions, RULE-xx for core rules).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BattleScenarioAttribute : Attribute
{
    public string Id { get; }
    public string Summary { get; }
    public string Category => Id.StartsWith("BUG") ? "Regression" : "Rules";

    public BattleScenarioAttribute(string id, string summary)
    {
        Id = id;
        Summary = summary;
    }
}

/// <summary>
/// Battle scenarios, each run as its own EditMode test (Window > General > Test Runner)
/// with a fresh <see cref="BattleSandbox"/>.
/// To add one: write a public static void method taking a BattleSandbox, tag it with
/// [BattleScenario], and add its ID to Design/TestTracker.md.
/// </summary>
public static class BattleScenarios
{
    // =========================================
    // REGRESSIONS
    // =========================================

    [BattleScenario(
        "BUG-01",
        "GameManager.EndPlayerTurn passes the turn instead of recursing forever"
    )]
    public static void EndPlayerTurnPassesTheTurn(BattleSandbox s)
    {
        BattleSandbox.Expect(
            s.Game.EndPlayerTurn(),
            "EndPlayerTurn was refused on the player's own turn."
        );
        BattleSandbox.ExpectEqual(
            PlayerSide.Opponent,
            s.Turns.CurrentSide,
            "Side to act after ending the turn"
        );
    }

    [BattleScenario(
        "BUG-02",
        "A Shock kill is not undone when the attacker also dies in the trade"
    )]
    public static void ShockTradeKillsBothUnits(BattleSandbox s)
    {
        RuntimeCard attacker = s.Summon(1, 1, PlayerSide.Player, CardKeyword.Shock);
        RuntimeCard defender = s.Summon(1, 5, PlayerSide.Opponent);

        BattleSandbox.Expect(s.Attack(attacker, defender), "Attack was refused.");
        BattleSandbox.Expect(
            !s.IsOnField(attacker),
            "Attacker took lethal damage but is still on the field."
        );
        BattleSandbox.Expect(
            !s.IsOnField(defender),
            $"Shocked defender survived with {defender.CurrentHealth} Health."
        );
    }

    [BattleScenario(
        "BUG-02",
        "A destroyed unit stays dead when passives are recalculated before its death check"
    )]
    public static void KilledUnitIsNotRevivedByRecalculation(BattleSandbox s)
    {
        RuntimeCard unit = s.Summon(2, 4, PlayerSide.Opponent);
        unit.Kill();
        unit.RecalculateCurrentHealth();
        BattleSandbox.ExpectEqual(0, unit.CurrentHealth, "Health after Kill and recalculation");
    }

    [BattleScenario(
        "BUG-03",
        "A unit pays its cost before its Battlecry, so mana it restores is not lost"
    )]
    public static void UnitPaysManaBeforeBattlecry(BattleSandbox s)
    {
        var restore = s.Effect<RestoreManaEffect>();
        BattleSandbox.SetField(restore, "randomAmount", false);
        BattleSandbox.SetField(restore, "minimumAmount", 2);
        BattleSandbox.SetField(restore, "maximumAmount", 2);

        MonsterData data = s.UnitData(2, 2);
        data.manaCost = 3;
        BattleSandbox.SetField(data, "battlecry", restore);

        s.SetMana(PlayerSide.Player, 5);
        RuntimeCard card = s.GiveToHand(data, PlayerSide.Player);

        BattleSandbox.Expect(s.Play(card), "Play was refused.");
        BattleSandbox.ExpectEqual(
            4,
            s.Turns.GetMana(PlayerSide.Player),
            "Mana after 5 - 3 cost + 2 restored"
        );
    }

    [BattleScenario(
        "BUG-03",
        "A unit that cannot enter a full field costs no mana and stays in hand"
    )]
    public static void FailedUnitPlayKeepsMana(BattleSandbox s)
    {
        for (int i = 0; i < 6; i++)
            s.Summon(1, 1, PlayerSide.Player);

        MonsterData data = s.UnitData(1, 1);
        data.manaCost = 2;
        s.SetMana(PlayerSide.Player, 5);
        RuntimeCard card = s.GiveToHand(data, PlayerSide.Player);

        BattleSandbox.Expect(!s.Play(card), "A unit was played onto a full field.");
        BattleSandbox.ExpectEqual(
            5,
            s.Turns.GetMana(PlayerSide.Player),
            "Mana after the refused play"
        );
        BattleSandbox.Expect(s.IsInHand(card), "The refused card left the hand.");
    }

    [BattleScenario(
        "BUG-04",
        "Played units: Rush can attack units only, Stealth hides, plain units wait a turn"
    )]
    public static void SummonedUnitAttackReadiness(BattleSandbox s)
    {
        RuntimeCard rush = s.GiveToHand(s.UnitData(1, 1, CardKeyword.Rush), PlayerSide.Player);
        RuntimeCard stealth = s.GiveToHand(
            s.UnitData(1, 1, CardKeyword.Stealth),
            PlayerSide.Player
        );
        RuntimeCard plain = s.GiveToHand(s.UnitData(1, 1), PlayerSide.Player);

        BattleSandbox.Expect(
            s.Play(rush) && s.Play(stealth) && s.Play(plain),
            "A play was refused."
        );

        BattleSandbox.Expect(rush.CanAttack, "Rush unit cannot attack the turn it is played.");
        BattleSandbox.Expect(
            rush.CannotAttackHero,
            "Rush unit can attack the hero the turn it is played."
        );
        BattleSandbox.Expect(stealth.IsStealthed, "Stealth unit is not stealthed.");
        BattleSandbox.Expect(
            !stealth.CanAttack,
            "Stealth unit without Rush can attack the turn it is played."
        );
        BattleSandbox.Expect(!plain.CanAttack, "Plain unit can attack the turn it is played.");
    }

    [BattleScenario(
        "BUG-05",
        "A dying unit's artifacts fire their deathrattles, then go to the Graveyard"
    )]
    public static void ArtifactsGoToGraveyardWithTheirUnit(BattleSandbox s)
    {
        ScenarioProbeEffect deathrattle = s.Probe();
        RuntimeCard carrier = s.Summon(s.ApostleData(1, 1), PlayerSide.Player);
        RuntimeCard artifact = s.Card(s.ArtifactData(deathrattle: deathrattle), PlayerSide.Player);
        BattleSandbox.Expect(carrier.EquipArtifact(artifact), "Artifact could not be equipped.");

        RuntimeCard enemy = s.Summon(5, 5, PlayerSide.Opponent);
        BattleSandbox.Expect(s.Attack(carrier, enemy), "Attack was refused.");

        BattleSandbox.Expect(!s.IsOnField(carrier), "Carrier survived the attack.");
        BattleSandbox.ExpectEqual(1, deathrattle.TimesResolved, "Artifact deathrattle activations");
        BattleSandbox.ExpectEqual(
            CardZone.Graveyard,
            artifact.Zone,
            "Artifact zone after its carrier died"
        );
        BattleSandbox.ExpectEqual(
            0,
            carrier.EquippedArtifacts.Count,
            "Artifacts still equipped to the dead unit"
        );
    }

    [BattleScenario(
        "BUG-06",
        "Both commanders are set up the same way: correct owner, in the commander slot"
    )]
    public static void CommandersAreSetUpConsistently(BattleSandbox s)
    {
        foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
        {
            s.Game.GetDeck(side).InitializeDeck(s.ApostleData(2, 2), new List<CardData>());
            BattleSandbox.Invoke(s.Game, "SetUpCommander", side);

            RuntimeCard commander = s.Game.GetDeck(side).GetCommander();
            BattleSandbox.ExpectEqual(side, commander.Owner, $"{side} commander owner");
            BattleSandbox.Expect(s.IsInHand(commander), $"{side} commander is not in hand.");
            BattleSandbox.ExpectEqual(
                0,
                s.Game.GetHandManager(side).HandSize,
                $"{side} normal cards in hand"
            );
        }
    }

    [BattleScenario(
        "BUG-07",
        "A card burnt from a full hand still fires its on-draw effects, then is discarded"
    )]
    public static void BurntCardStillFiresOnDraw(BattleSandbox s)
    {
        ScenarioProbeEffect onDraw = s.Probe();
        MonsterData data = s.UnitData(1, 1);
        BattleSandbox.SetField(data, "onDrawEffects", new List<CardEffect> { onDraw });

        s.FillHand(PlayerSide.Player);
        RuntimeCard card = s.Card(data, PlayerSide.Player);
        card.ChangeZone(CardZone.Hand);
        s.Game.HandleDrawnCard(card, PlayerSide.Player);

        BattleSandbox.ExpectEqual(
            1,
            onDraw.TimesResolved,
            "On-draw activations for the burnt card"
        );
        BattleSandbox.Expect(!s.IsInHand(card), "Burnt card was added to a full hand.");
        BattleSandbox.Expect(
            s.Game.GetDeck(PlayerSide.Player).GetDiscardPileSnapshot().Contains(card),
            "Burnt card is not in the discard pile."
        );
    }

    [BattleScenario(
        "BUG-08",
        "Jubee's unlimited copies are keyed on card ID A_JUBEE, not the card name"
    )]
    public static void JubeeLimitUsesCardID(BattleSandbox s)
    {
        ApostleData jubee = s.ApostleData(1, 1);
        BattleSandbox.SetField(jubee, "cardID", DeckRules.JubeeCardID);
        jubee.cardName = "주비";

        ApostleData impostor = s.ApostleData(1, 1);
        impostor.cardName = "Jubee";

        BattleSandbox.ExpectEqual(-1, DeckRules.GetCopyLimit(jubee), "Copy limit for A_JUBEE");
        BattleSandbox.ExpectEqual(
            2,
            DeckRules.GetCopyLimit(impostor),
            "Copy limit for another card named Jubee"
        );
    }

    // =========================================
    // CORE RULES (examples to copy when adding more)
    // =========================================

    [BattleScenario("RULE-01", "Taunt must be attacked before other units")]
    public static void TauntBlocksOtherTargets(BattleSandbox s)
    {
        // Refused attacks are tested from the opponent's seat: the player's seat shows an on-screen notice.
        s.BeginTurn(PlayerSide.Opponent, 10);
        RuntimeCard attacker = s.Summon(3, 3, PlayerSide.Opponent);
        RuntimeCard taunt = s.Summon(1, 5, PlayerSide.Player, CardKeyword.Taunt);
        RuntimeCard other = s.Summon(1, 1, PlayerSide.Player);

        BattleSandbox.Expect(!s.Attack(attacker, other), "Attacked past a Taunt unit.");
        BattleSandbox.Expect(s.Attack(attacker, taunt), "Could not attack the Taunt unit.");
    }

    [BattleScenario("RULE-02", "First Strike kills the defender before it can hit back")]
    public static void FirstStrikeAvoidsRetaliation(BattleSandbox s)
    {
        RuntimeCard attacker = s.Summon(3, 1, PlayerSide.Player, CardKeyword.FirstStrike);
        RuntimeCard defender = s.Summon(5, 3, PlayerSide.Opponent);

        BattleSandbox.Expect(s.Attack(attacker, defender), "Attack was refused.");
        BattleSandbox.Expect(
            !s.IsOnField(defender),
            "Defender survived lethal First Strike damage."
        );
        BattleSandbox.ExpectEqual(1, attacker.CurrentHealth, "First Strike attacker Health");
    }

    [BattleScenario("RULE-03", "Endure reduces each instance of damage by 1")]
    public static void EndureReducesDamage(BattleSandbox s)
    {
        RuntimeCard unit = s.Summon(1, 5, PlayerSide.Player, CardKeyword.Endure);
        unit.TakeDamage(3);
        BattleSandbox.ExpectEqual(3, unit.CurrentHealth, "Health after 3 damage with Endure");
    }

    [BattleScenario("RULE-04", "A unit cannot attack twice in one turn")]
    public static void UnitAttacksOncePerTurn(BattleSandbox s)
    {
        // Refused attacks are tested from the opponent's seat: the player's seat shows an on-screen notice.
        s.BeginTurn(PlayerSide.Opponent, 10);
        RuntimeCard attacker = s.Summon(1, 10, PlayerSide.Opponent);
        RuntimeCard defender = s.Summon(1, 10, PlayerSide.Player);

        BattleSandbox.Expect(s.Attack(attacker, defender), "First attack was refused.");
        BattleSandbox.Expect(
            !s.Attack(attacker, defender),
            "Second attack in the same turn was allowed."
        );
    }
}
#endif
