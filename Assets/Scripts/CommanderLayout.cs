using UnityEngine;

public class CommanderLayout : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private float centerX = 0f;
    [SerializeField] private float centerY = 0f;

    [Header("Scale")]
    [SerializeField] private float scale = 1f;

    [Header("Rotation")]
    [SerializeField] private float rotation = 0f;

    public void RefreshLayout()
    {
        int count = transform.childCount;

        if (count == 0)
            return;

        // Commander should normally only have one card.
        // If something goes wrong, only arrange the first one.
        RectTransform commander =
            transform.GetChild(0) as RectTransform;

        if (commander == null)
            return;

        commander.anchoredPosition =
            new Vector2(centerX, centerY);

        commander.localScale =
            Vector3.one * scale;

        commander.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotation
            );
    }

    private void Start()
    {
        RefreshLayout();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshLayout();
    }
}