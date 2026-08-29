using UnityEngine;

public class HandLayout : MonoBehaviour
{
    [Header("Card Scale")]
    [SerializeField] private float minimumScale = 0.55f;
    [SerializeField] private float maximumScale = 1.0f;

    [Header("Spacing")]
    [SerializeField] private float cardSpacing = 100f;

    [Header("Fan")]
    [SerializeField] private float maxFanAngle = 12f;
    [SerializeField] private float fanHeight = 35f;

    [Header("Position")]
    [SerializeField] private float centerY = 0f;

    [Header("Layout Mode")]
    [SerializeField] private bool isCommander = false;

    private RectTransform handRect;

    private void Awake()
    {
        handRect = GetComponent<RectTransform>();
    }

    public void RefreshLayout()
    {
        int cardCount = transform.childCount;
        

        if (isCommander)
        {
            LayoutCommander();
            return;
        }

        if (cardCount == 0)
            return;

        RectTransform firstCard =
            transform.GetChild(0) as RectTransform;

        if (firstCard == null)
            return;

        float cardWidth = firstCard.rect.width;

        if (cardWidth <= 0)
            return;

        float availableWidth = handRect.rect.width;

        // -----------------------------------------
        // Calculate spacing
        // -----------------------------------------

        float spacing = cardSpacing;

        float requiredWidth =
            cardWidth +
            spacing * (cardCount - 1);

        // -----------------------------------------
        // Calculate scale
        // -----------------------------------------

        float scale = 1f;

        if (requiredWidth > availableWidth)
        {
            scale =
                availableWidth / requiredWidth;
        }

        scale = Mathf.Clamp(
            scale,
            minimumScale,
            maximumScale
        );

        // -----------------------------------------
        // Calculate final spacing
        // -----------------------------------------

        spacing *= scale;

        float scaledWidth =
            cardWidth * scale;

        float totalWidth =
            scaledWidth +
            spacing * (cardCount - 1);

        // Start so the entire hand is centered.
        float startX =
            -(totalWidth / 2f) +
            (scaledWidth / 2f);

        // -----------------------------------------
        // Position cards
        // -----------------------------------------

                for (int i = 0; i < cardCount; i++)
        {
            RectTransform card =
                transform.GetChild(i) as RectTransform;

            CardView cardView =
                card.GetComponent<CardView>();

            if (cardView != null &&
                cardView.IsDragging)
            {
                continue;
            }

            if (card == null)
                continue;

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

            // -------------------------------------
            // X
            // -------------------------------------

            float x =
                startX +
                spacing * i;

            // -------------------------------------
            // Fan rotation
            // -------------------------------------

            float angle =
                Mathf.Lerp(
                    maxFanAngle,
                    -maxFanAngle,
                    normalizedPosition
                );

            // -------------------------------------
            // Fan height
            // -------------------------------------

            float distanceFromCenter =
                Mathf.Abs(
                    normalizedPosition - 0.5f
                ) * 2f;

            float y =
                centerY +
                (1f - distanceFromCenter)
                * fanHeight
                * scale;

            // -------------------------------------
            // Apply
            // -------------------------------------

            card.anchoredPosition =
                new Vector2(x, y);

            card.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );

            card.localScale =
                Vector3.one * scale;
        }
    }

// SPecial Layout for Commander
        private void LayoutCommander()
    {
        if (transform.childCount == 0)
            return;

        RectTransform card =
            transform.GetChild(0) as RectTransform;

        if (card == null)
            return;

        card.anchoredPosition = Vector2.zero;

        card.localRotation = Quaternion.identity;

        card.localScale =
            Vector3.one * maximumScale;
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