using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Ghost Board Stats")]
public class GhostBoardStatsEffect : CardEffect
{
    public enum Mode { ExchangeAll, SwapOthers, LowestOthers, RandomRangeOthers }
    [SerializeField] private Mode mode;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var units = new List<RuntimeCard>();
        foreach (var side in new[] { PlayerSide.Player, PlayerSide.Opponent })
        {
            var field = GameManager.Instance.GetBattlefield(side);
            if (field != null) foreach (var unit in field.Minions)
                if (unit.Zone == CardZone.Field && unit.CurrentHealth > 0) units.Add(unit);
        }
        if (units.Count == 0) return;
        // One snapshot of both sides, including the caster for board-wide min/max bounds.
        var values = new List<int>();
        foreach (var unit in units) { values.Add(unit.GetAttack()); values.Add(unit.CurrentHealth); }
        int min = values[0], max = values[0];
        foreach (int value in values) { min = Mathf.Min(min, value); max = Mathf.Max(max, value); }
        var assigned = new List<Vector2Int>();
        if (mode == Mode.ExchangeAll)
            for (int i = values.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int value = values[i]; values[i] = values[j]; values[j] = value; }
        for (int i = 0; i < units.Count; i++)
        {
            if (mode == Mode.ExchangeAll) assigned.Add(new Vector2Int(values[i * 2], values[i * 2 + 1]));
            else if (mode == Mode.SwapOthers) assigned.Add(new Vector2Int(values[i * 2 + 1], values[i * 2]));
            else if (mode == Mode.LowestOthers) assigned.Add(new Vector2Int(min, min));
            else assigned.Add(new Vector2Int(Random.Range(min, max + 1), Random.Range(min, max + 1)));
        }
        // Apply the snapshot before death checks so a zero-Health assignment cannot alter later values.
        for (int i = 0; i < units.Count; i++)
            if ((mode == Mode.ExchangeAll || units[i] != source) && units[i].Zone == CardZone.Field)
                units[i].SetCurrentStats(assigned[i].x, assigned[i].y, source);
        foreach (var unit in units)
        {
            if (unit.Zone != CardZone.Field) continue;
            GameManager.Instance.GetBattlefield(unit.Owner)?.RefreshMinionView(unit);
            GameManager.Instance.CombatManager.CheckDeath(unit);
        }
    }
}
