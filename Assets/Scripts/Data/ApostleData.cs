using UnityEngine;

[CreateAssetMenu(
    fileName = "NewApostle",
    menuName = "TCG/Cards/Apostle"
)]
public class ApostleData : MinionData
{

     [Header("Apostle Visual")]
    [SerializeField]private Sprite typeIcon;
    public Sprite TypeIcon => typeIcon;

    [Header("Equipment")]
    [Min(0)]
    [SerializeField]private int maxEquipment = 3;
    public int MaxEquipment => maxEquipment;

 
}