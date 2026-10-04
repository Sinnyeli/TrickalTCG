using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LupoBuffEffect",
    menuName = "Card Effects/Apostles/Lupo Buff"
)]
public class LupoBuffEffect : CardEffect
{
    [Header("Special Target")]
    [SerializeField]
    private CardData tig;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null ||
            targets == null ||
            targets.Count == 0)
        {
            return;
        }

        foreach (RuntimeCard target in targets)
        {
            if (target == null ||
                target.Data == null)
            {
                continue;
            }

            // Another friendly unit.
            if (target == source)
                continue;

            if (target.Owner != source.Owner)
                continue;


            // =========================================
            // BEASTFOLK CHECK
            // =========================================

            bool isBeastfolk = false;

            if (target.Data is MinionData minion)
            {
                isBeastfolk =
                    minion.HasRace(CardRace.Beastfolk);
            }

            if (!isBeastfolk)
                continue;


            // =========================================
            // TIG BONUS
            // =========================================

            int amount =
                target.Data == tig
                    ? 2
                    : 1;


            RuntimeModifier modifier =
                new RuntimeModifier(
                    amount,
                    amount,
                    true,
                    source,
                    false
                );

            target.AddModifier(
                modifier
            );


            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshMinionView(target);


            Debug.Log(
                $"{source.Data.cardName} gave " +
                $"{target.Data.cardName} " +
                $"+{amount}/+{amount}."
            );
        }
    }
}