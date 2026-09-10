using UnityEngine;

[CreateAssetMenu(
    fileName = "NewApostle",
    menuName = "TCG/Cards/Apostle"
)]
public class ApostleData : CardData
{

     [Header("Apostle Visual")]
    public Sprite typeIcon;

    [Header("Apostle Stats")]
    public int attack;
    public int health;

    [Header("Race")]
    public CardRace cardRace;

    [Header("Equipment")]
    public int maxEquipment = 3;

 
}