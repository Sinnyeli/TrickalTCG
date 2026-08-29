using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMonster",
    menuName = "TCG/Cards/Monster"
)]
public class MonsterData : CardData
{
    [Header("Monster Stats")]
    public int attack;
    public int health;

    [Header("Race")]
    public int raceID;
}