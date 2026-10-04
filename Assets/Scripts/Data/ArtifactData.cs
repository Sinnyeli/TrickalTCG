using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewArtifact",
    menuName = "TCG/Cards/Artifact"
)]
public class ArtifactData : CardData
{
    [Header("Stats")]
    [SerializeField]private int attackBonus;
    [SerializeField]private int healthBonus;

    [Header("Granted Keywords")]
    [SerializeField]
    private List<CardKeyword> grantedKeywords =
        new List<CardKeyword>();

    [Header("Artifact Effects")]
    [SerializeField]
    private CardEffect artifactEffect;

    [SerializeField]
    private CardEffect deathrattle;
    public int AttackBonus =>
        attackBonus;

    public int HealthBonus =>
        healthBonus;



    public CardEffect ArtifactEffect =>
        artifactEffect;

    public CardEffect Deathrattle =>
        deathrattle;

    public bool GrantsKeyword(
        CardKeyword keyword)
    {
        return grantedKeywords != null &&
               grantedKeywords.Contains(keyword);
    }
    public ArtifactData CreateFusionCopy()
    {
        var copy = Instantiate(this);
        copy.attackBonus = copy.healthBonus = 0; // Already included in captured totals.
        if (artifactEffect != null) copy.artifactEffect = Instantiate(artifactEffect);
        if (deathrattle != null) copy.deathrattle = Instantiate(deathrattle);
        return copy;
    }
}