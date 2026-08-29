using UnityEngine;

public enum ApostleType
{
    Sprite, Witch, Dragon, Beastfolk, Elf, Elemental, Ghost
}


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

    [Header("Equipment")]
    public int maxEquipment = 3;

 
}