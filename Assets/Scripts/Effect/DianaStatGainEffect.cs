using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DianaStatGainEffect",
    menuName = "Card Effects/Apostles/Diana Stat Gain"
)]
public class DianaStatGainEffect : CardEffect
{
    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        // This effect is event-driven.
        // Resolve itself does not immediately
        // perform the buff.
    }


    public void HandleStatsGained(
        RuntimeCard source,
        RuntimeCard buffedCard,
        RuntimeModifier modifier)
    {
        if (source == null ||
            buffedCard == null ||
            modifier == null)
        {
            return;
        }


        // =========================================
        // DIANA MUST BE ON FIELD
        // =========================================

        if (source.Zone != CardZone.Field)
            return;


        // =========================================
        // ANOTHER UNIT
        // =========================================

        if (buffedCard == source)
            return;


        // =========================================
        // FRIENDLY
        // =========================================

        if (buffedCard.Owner != source.Owner)
            return;


        // =========================================
        // BEASTFOLK
        // =========================================

        bool isBeastfolk = false;


        if (buffedCard.Data
            is ApostleData apostle)
        {
            isBeastfolk =
                apostle.HasRace(CardRace.Beastfolk);
        }
        else if (buffedCard.Data
                 is MonsterData monster)
        {
            isBeastfolk =
                monster.HasRace(CardRace.Beastfolk);
        }


        if (!isBeastfolk)
            return;


        // =========================================
        // ACTUAL STAT GAIN
        // =========================================

        if (modifier.AttackBonus <= 0 &&
            modifier.HealthBonus <= 0)
        {
            return;
        }


        // =========================================
        // DIANA +1 ATTACK
        // =========================================

        RuntimeModifier dianaBuff =
            new RuntimeModifier(
                1,
                0,
                true,
                source,
                false
            );


        source.AddModifier(
            dianaBuff
        );


        GameManager.Instance
            .GetBattlefield(source.Owner)
            .RefreshMinionView(source);


        Debug.Log(
            $"{source.Data.cardName} gained " +
            $"+1 Attack because " +
            $"{buffedCard.Data.cardName} " +
            $"gained stats."
        );
    }
}