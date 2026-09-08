using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SilenceEffect",
    menuName = "Card Effects/Silence"
)]
public class SilenceEffect : CardEffect
{
    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null || targets == null)
            return;

        foreach (RuntimeCard target in targets)
        {
            if (target == null)
                continue;

            target.Silence();
            target.RemoveSilenceableModifiers();

            Debug.Log(
                $"{source.Data.cardName} silenced " +
                $"{target.Data.cardName}."
            );
            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshPassives();
            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshMinionView(target);
        }
    }
}