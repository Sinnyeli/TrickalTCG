using UnityEngine;

public class BattlefieldLayout : MonoBehaviour
{
    [Header("Spacing")]
    [SerializeField] private float spacing = 180f;

    [Header("Position")]
    [SerializeField] private float centerY = 0f;

    [Header("Scale")]
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1f;

    [Header("Rotation")]
    [SerializeField] private float maxRotation = 0f;

    private RectTransform battlefieldRect;

    private void Awake()
    {
        battlefieldRect =
            GetComponent<RectTransform>();
    }
        private void Start()
    {
        RefreshLayout();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        int count = transform.childCount;

        if (count == 0)
            return;

        float scale = CalculateScale(count);

        for (int i = 0; i < count; i++)
        {
            RectTransform minion =
                transform.GetChild(i)
                as RectTransform;

            if (minion == null)
                continue;

            // -----------------------------------------
            // Center the entire group
            // -----------------------------------------

            float centerIndex =
                (count - 1) / 2f;

            float offset =
                i - centerIndex;

            float x =
                offset * spacing * scale;

            // -----------------------------------------
            // Rotation
            // -----------------------------------------

            float normalizedPosition =
                count == 1
                    ? 0.5f
                    : (float)i / (count - 1);

            float rotation =
                Mathf.Lerp(
                    maxRotation,
                    -maxRotation,
                    normalizedPosition
                );

            // -----------------------------------------
            // Apply
            // -----------------------------------------

            minion.anchoredPosition =
                new Vector2(
                    x,
                    centerY
                );

            minion.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation
                );

            minion.localScale =
                Vector3.one * scale;
        }
    }

    private float CalculateScale(int count)
    {
        if (count <= 1)
            return maxScale;

        float availableWidth =
            battlefieldRect.rect.width;

        float requiredWidth =
            spacing * (count - 1);

        if (requiredWidth <= availableWidth)
            return maxScale;

        float scale =
            availableWidth / requiredWidth;

        return Mathf.Clamp(
            scale,
            minScale,
            maxScale
        );
    }

    // Generally tell location of where card is dragged at so index can change
    public int GetInsertionIndex(float mouseX)
    {
        int count = transform.childCount;

        if (count == 0)
            return 0;

        float spacing = this.spacing;

        float centerIndex =
            (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float offset =
                i - centerIndex;

            float cardX =
                offset * spacing;

            if (mouseX < cardX)
            {
                return i;
            }
        }

        return count;
    }
}