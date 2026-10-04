using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MayoEquipmentEffect", menuName = "Card Effects/Mayo/Merchandise Ability")]
public class MayoEquipmentEffect : CardEffect
{
    public enum Ability { IgnoreEndure, FreezeOnAttack, CopyRandomStatsOnEquip }
    [SerializeField] private Ability ability;
    public Ability Kind => ability;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (ability != Ability.CopyRandomStatsOnEquip || source == null || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        RuntimeCard carrier = null;
        var friendly = game.GetBattlefield(source.Owner);
        if (friendly == null) return;
        foreach (var unit in friendly.Minions)
            if (new List<RuntimeCard>(unit.EquippedArtifacts).Contains(source)) { carrier = unit; break; }
        if (carrier == null || carrier.IsSilenced) return;
        var candidates = new List<RuntimeCard>();
        foreach (var side in new[] { PlayerSide.Player, PlayerSide.Opponent })
        {
            var field = game.GetBattlefield(side);
            if (field != null) candidates.AddRange(field.Minions);
        }
        candidates.RemoveAll(c => c == null || c == carrier || c.Zone != CardZone.Field || c.CurrentHealth <= 0);
        if (candidates.Count == 0) return;
        var picked = candidates[Random.Range(0, candidates.Count)];
        carrier.CopyEquipmentStats(picked.GetAttack(), picked.CurrentHealth, source);
        friendly.RefreshMinionView(carrier);
    }
}
