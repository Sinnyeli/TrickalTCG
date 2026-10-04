using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Elena Fusion")]
public class ElenaFusionEffect : CardEffect
{
    [SerializeField] private MinionData meow;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || meow == null || GameManager.Instance == null) return;
        var field = GameManager.Instance.GetBattlefield(source.Owner);
        if (field == null) return;
        var machines = new List<RuntimeCard>();
        var artifacts = new List<RuntimeCard>();
        int attack = 0, health = 0;
        foreach (var unit in field.Minions)
        {
            if (unit == source || unit.CurrentHealth <= 0 || !(unit.Data is MinionData data) || !data.HasRace(CardRace.Machine)) continue;
            machines.Add(unit); attack += unit.GetAttack(); health += unit.CurrentHealth;
            foreach (var artifact in unit.EquippedArtifacts)
                if (artifact.Data is ArtifactData equipment) artifacts.Add(new RuntimeCard(equipment.CreateFusionCopy()));
        }
        // Capture everything before any deathrattle can mutate another victim or the field.
        var definition = meow.CreateFusionDefinition(machines);
        foreach (var unit in machines)
        {
            if (unit.Zone != CardZone.Field) continue;
            unit.Kill(source); GameManager.Instance.CombatManager.CheckDeath(unit);
        }
        // With no Machines the token keeps its printed 1/1 base.
        if (machines.Count == 0) attack = health = 1;
        var fusion = new RuntimeCard(definition);
        fusion.SetOwner(source.Owner); fusion.ChangeZone(CardZone.Hand);
        fusion.PrepareFusion(artifacts, attack, health);
        if (!field.PlayCard(fusion)) return; // Deathrattle summons may have filled the freed slots.
        fusion.CompleteFusion(attack, health);
        field.RefreshMinionView(fusion);
    }
}
