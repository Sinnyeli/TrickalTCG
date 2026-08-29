using System.Collections.Generic;
using UnityEngine;

public class MinionArray : MonoBehaviour
{
    [SerializeField] private float spacing = 180f;
    [SerializeField] private int maxMinions = 6;

    private List<MinionView> minions = new List<MinionView>();

    public int Count => minions.Count;

    public bool AddMinion(MinionView minion)
    {
        if (minions.Count >= maxMinions)
        {
            Debug.Log("Field is full!");
            return false;
        }

        minions.Add(minion);

        minion.transform.SetParent(transform);

        RepositionMinions();

        return true;
    }

    public void RemoveMinion(MinionView minion)
    {
        if (!minions.Remove(minion))
            return;

        RepositionMinions();
    }

    private void RepositionMinions()
    {
        int count = minions.Count;

        if (count == 0)
            return;

        float totalWidth = (count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            minions[i].transform.localPosition =
                new Vector3(
                    startX + i * spacing,
                    0f,
                    0f
                );
        }
    }
}