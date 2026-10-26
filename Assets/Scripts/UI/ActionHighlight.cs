using UnityEngine;
using UnityEngine.UI;

// A soft pulsing frame behind a card or unit while the player can use it
// (an affordable card in hand, or a unit that can still attack).
public class ActionHighlight : MonoBehaviour
{
    private static readonly Color GlowColor = new Color(1f, .85f, .3f);
    private const float Padding = 10f;

    public System.Func<bool> Condition { get; set; }

    // Optional glow outline (instead of the square panel) and the child rect it should hug.
    public Sprite Shape { get; set; }
    public RectTransform ShapeArea { get; set; }

    private Image glow;

    private void Start()
    {
        RectTransform rect = new GameObject(
            "Action Highlight",
            typeof(RectTransform)
        ).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.SetAsFirstSibling(); // drawn behind the card art
        glow = rect.gameObject.AddComponent<Image>();
        if (Shape != null && ShapeArea != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = ShapeArea.anchoredPosition;
            rect.sizeDelta = ShapeArea.sizeDelta + new Vector2(2 * Padding, 2 * Padding);
            glow.sprite = Shape;
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-Padding, -Padding);
            rect.offsetMax = new Vector2(Padding, Padding);
            glow.sprite = UiSprites.Panel;
            glow.type = Image.Type.Sliced;
        }
        glow.raycastTarget = false;
        glow.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (glow == null)
            return;
        bool show = Condition != null && Condition();
        if (glow.gameObject.activeSelf != show)
            glow.gameObject.SetActive(show);
        if (show)
            glow.color = new Color(
                GlowColor.r,
                GlowColor.g,
                GlowColor.b,
                .45f + .3f * Mathf.Sin(Time.unscaledTime * 4f)
            );
    }
}
