using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Alice Destiny Draw")]
public class AliceDestinyDrawEffect : CardEffect
{
    [Range(0, 2), SerializeField] private int kind;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source != null) GameManager.Instance?.RecordDestinyDraw(source.Owner, kind);
    }
}
