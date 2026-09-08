using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewArtifact",
    menuName = "TCG/Cards/Artifact"
)]
public class ArtifactData : CardData
{
    [Header("Artifact")]
    public int attackBonus;
    public int healthBonus;

    [Header("Artifact Effect")]
    [SerializeField] private CardEffect artifactEffect;

    public CardEffect ArtifactEffect => artifactEffect;
}