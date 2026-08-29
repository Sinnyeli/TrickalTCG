using UnityEngine;

public class BattlefieldManager : MonoBehaviour
{
    [SerializeField] private MinionArray playerMinions;

    [SerializeField] private GameObject minionPrefab;

    public bool SummonMinion(RuntimeCard card)
    {
        GameObject minionObject = Instantiate(minionPrefab);

        MinionView minion = minionObject.GetComponent<MinionView>();

        minion.Initialize(card);

        if (!playerMinions.AddMinion(minion))
        {
            Destroy(minionObject);
            return false;
        }

        return true;
    }
}