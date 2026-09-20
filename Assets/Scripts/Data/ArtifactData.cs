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
}