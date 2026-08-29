using UnityEngine;

public class HandLayout : MonoBehaviour
{
    [Header("Card Layout")]
    [SerializeField] private float cardWidth = 180f;

    [Header("Horizontal Spacing")]
    [SerializeField] private float minimumSpacing = 70f;
    [SerializeField] private float maximumSpacing = 150f;

    [Header("Fan")]
    [SerializeField] private float maxFanAngle = 15f;
    [SerializeField] private float fanHeight = 35f;

    [Header("Position")]
    [SerializeField] private float cardY = 0f;

    public void RefreshLayout()
    {
        int cardCount = transform.childCount;

        if (cardCount == 0)
            return;

        RectTransform container =
            GetComponent<RectTransform>();

        float availableWidth = container.rect.width;

        // -------------------------------------------------
        // Calculate horizontal spacing
        // -------------------------------------------------

        float spacing;

        if (cardCount == 1)
        {
            spacing = 0f;
        }
        else
        {
            spacing = Mathf.Min(
                maximumSpacing,
                (availableWidth - cardWidth) / (cardCount - 1)
            );

            spacing = Mathf.Max(
                minimumSpacing,
                spacing
            );
        }

        // -------------------------------------------------
        // Calculate total hand width
        // -------------------------------------------------

        float totalWidth =
            cardWidth +
            spacing * (cardCount - 1);

        float startX =
            -(totalWidth / 2f) +
            (cardWidth / 2f);

        // -------------------------------------------------
        // Position cards
        // -------------------------------------------------

        for (int i = 0; i < cardCount; i++)
        {
            RectTransform card =
                transform.GetChild(i) as RectTransform;

            if (card == null)
                continue;

            // Normalized position:
            // 0 = left
            // 0.5 = center
            // 1 = right

            float normalizedPosition;

            if (cardCount == 1)
            {
                normalizedPosition = 0.5f;
            }
            else
            {
                normalizedPosition =
                    (float)i / (cardCount - 1);
            }

            // -------------------------------------------------
            // X position
            // -------------------------------------------------

            float x =
                startX +
                spacing * i;

            // -------------------------------------------------
            // Fan rotation
            // -------------------------------------------------

            float angle =
                Mathf.Lerp(
                    maxFanAngle,
                    -maxFanAngle,
                    normalizedPosition
                );

            // -------------------------------------------------
            // Fan height
            // -------------------------------------------------

            // Center cards are higher.
            float distanceFromCenter =
                Mathf.Abs(normalizedPosition - 0.5f) * 2f;

            float y =
                cardY +
                (1f - distanceFromCenter) * fanHeight;

            // -------------------------------------------------
            // Apply transform
            // -------------------------------------------------

            card.anchoredPosition =
                new Vector2(x, y);

            card.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );

            card.localScale =
                Vector3.one;
        }
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