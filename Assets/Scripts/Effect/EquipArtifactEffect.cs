using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipArtifactEffect",
    menuName = "Card Effects/Equip Artifact"
)]
public class EquipArtifactEffect : CardEffect
{
    [Header("Artifact")]
    [SerializeField]
    private ArtifactData artifactToEquip;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null ||
            artifactToEquip == null)
            return;

        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(
                source.Owner
            );

        if (battlefield == null)
            return;

        List<RuntimeCard> candidates =
            new List<RuntimeCard>();

        foreach (RuntimeCard card in battlefield.Minions)
        {
            if (card == null)
                continue;

            // Don't equip the dying source.
            if (card == source)
                continue;

            // Must still be alive on the field.
            if (card.Zone != CardZone.Field)
                continue;

            if (card.CurrentHealth <= 0)
                continue;

            // Only Apostles can equip Artifacts.
            if (!(card.Data is ApostleData))
                continue;

            // Maximum 3 Artifacts.
            if (card.EquippedArtifacts.Count >= 3)
                continue;

            candidates.Add(card);
        }

        // No valid target = effect fizzles.
        if (candidates.Count == 0)
        {
            Debug.Log(
                $"{source.Data.cardName}'s effect fizzled. " +
                $"No valid Apostle could equip " +
                $"{artifactToEquip.cardName}."
            );

            return;
        }

        RuntimeCard target =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        RuntimeCard artifact =
            new RuntimeCard(artifactToEquip);

        artifact.SetOwner(source.Owner);

        target.EquipArtifact(artifact);

        battlefield.RefreshArtifactEffects();
        battlefield.RefreshMinionView(
            target
        );

        Debug.Log(
            $"{artifactToEquip.cardName} was equipped " +
            $"to {target.Data.cardName}."
        );
    }
}