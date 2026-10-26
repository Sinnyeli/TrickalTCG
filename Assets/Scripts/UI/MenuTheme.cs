using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Restyles the menu scenes (title, login, deck selector and loader, deck editor) at runtime to match
// the battle HUD: gradient backdrop, dark rounded panels, gold primary buttons and dark fields.
// Only graphics that still use Unity's stock UI sprites are touched, so card art and custom images
// stay as they are, and cards shown inside the deck editor are skipped entirely.
public class MenuTheme : MonoBehaviour
{
    private const float RescanSeconds = .4f; // deck lists and card grids fill in after the scene loads

    private static readonly HashSet<string> StockSprites = new HashSet<string>
    {
        "UISprite",
        "Background",
        "InputFieldBackground",
        "UIMask",
    };

    private static MenuTheme instance;
    private readonly HashSet<int> styled = new HashSet<int>();
    private TMP_FontAsset font;
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;
        instance = new GameObject("Menu Theme").AddComponent<MenuTheme>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private static bool InMenu => SceneManager.GetActiveScene().name != "Battlefield";

    private static bool Korean =>
        LocalizationManager.Instance != null
        && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Korean;

    private void Awake()
    {
        BattleUXResources resources = GameAudio.Clips;
        font =
            resources != null && resources.menuFont != null
                ? resources.menuFont
                : TMP_Settings.defaultFontAsset;
        SceneManager.sceneLoaded += SceneLoaded;
        SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= SceneLoaded;

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        styled.Clear();
        nextScan = 0;
        if (!InMenu)
            return;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.isRootCanvas && canvas.gameObject.scene == scene)
                AddBackdrop(canvas.transform);
        if (scene.name == "TitleScreen")
            BuildTitle();
    }

    private void Update()
    {
        if (!InMenu || Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + RescanSeconds;

        foreach (
            Selectable selectable in FindObjectsByType<Selectable>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        )
            if (Eligible(selectable))
                StyleSelectable(selectable);

        // Parents first, so a panel nested in another is recognised as an inset.
        var images = new List<Image>();
        foreach (
            Image image in FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        )
            if (Eligible(image))
                images.Add(image);
        images.Sort((a, b) => Depth(a.transform).CompareTo(Depth(b.transform)));
        foreach (Image image in images)
            StylePanel(image);

        foreach (
            TMP_Text text in FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        )
            if (Eligible(text))
                StyleText(text);
    }

    // =========================================
    // CONTROLS
    // =========================================

    private void StyleSelectable(Selectable selectable)
    {
        switch (selectable)
        {
            case TMP_Dropdown dropdown:
                StyleDropdown(dropdown);
                break;
            case TMP_InputField input:
                StyleField(input.targetGraphic as Image);
                if (input.textComponent != null)
                    input.textComponent.color = Color.white;
                if (input.placeholder is TMP_Text placeholder)
                    placeholder.color = UiPalette.Muted;
                break;
            case Scrollbar scrollbar:
                StyleImage(scrollbar.GetComponent<Image>(), UiSprites.Panel, UiPalette.Inset);
                StyleImage(scrollbar.targetGraphic as Image, UiSprites.Panel, UiPalette.Gold);
                break;
            case Button button:
                StyleButton(button);
                break;
            case Toggle toggle:
                if (toggle.graphic != null)
                    toggle.graphic.color = UiPalette.Gold;
                break;
        }
    }

    private void StyleButton(Button button)
    {
        Image image = button.targetGraphic as Image;
        if (image == null || !IsStock(image) || image.color.a < .05f)
            return; // invisible click areas and buttons with custom art keep their look

        string name = button.name.ToLowerInvariant();
        bool danger = name.Contains("delete") || name.Contains("minus") || name == "x";
        bool primary =
            name.Contains("battle")
            || name.Contains("done")
            || name.Contains("save")
            || name.Contains("plus")
            || name.Contains("login");
        Color fill =
            danger ? UiPalette.HealthRed
            : primary ? UiPalette.Gold
            : UiPalette.Slate;
        StyleImage(image, UiSprites.Panel, fill);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(.8f, .8f, .8f);
        colors.disabledColor = new Color(.6f, .6f, .6f, .55f);
        button.colors = colors;

        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.color = primary ? UiPalette.DarkText : Color.white;
            label.fontStyle |= FontStyles.Bold;
            Mark(label);
        }
    }

    private void StyleDropdown(TMP_Dropdown dropdown)
    {
        StyleField(dropdown.targetGraphic as Image);
        if (dropdown.captionText != null)
            Mark(dropdown.captionText).color = Color.white;
        foreach (Image image in dropdown.GetComponentsInChildren<Image>(true))
            if (image.sprite != null && image.sprite.name == "DropdownArrow")
                Mark(image).color = UiPalette.Gold;

        RectTransform template = dropdown.template;
        if (template == null)
            return;
        StyleImage(template.GetComponent<Image>(), UiSprites.Panel, UiPalette.Field);
        foreach (Toggle item in template.GetComponentsInChildren<Toggle>(true))
        {
            Mark(item);
            if (item.targetGraphic is Image background)
                StyleImage(background, UiSprites.Panel, new Color(1, 1, 1, .06f));
            if (item.graphic != null)
                Mark(item.graphic).color = UiPalette.Gold;
            ColorBlock colors = item.colors;
            colors.highlightedColor = new Color(
                UiPalette.Gold.r,
                UiPalette.Gold.g,
                UiPalette.Gold.b,
                .9f
            );
            item.colors = colors;
        }
        if (dropdown.itemText != null)
            Mark(dropdown.itemText).color = Color.white;
    }

    private void StyleField(Image image)
    {
        if (image == null || !IsStock(image))
            return;
        StyleImage(image, UiSprites.Panel, UiPalette.Field);
        Image edge = new GameObject("Field Edge", typeof(RectTransform)).AddComponent<Image>();
        edge.rectTransform.SetParent(image.transform, false);
        edge.rectTransform.anchorMin = Vector2.zero;
        edge.rectTransform.anchorMax = Vector2.one;
        edge.rectTransform.offsetMin = edge.rectTransform.offsetMax = Vector2.zero;
        edge.rectTransform.SetAsFirstSibling();
        StyleImage(
            edge,
            UiSprites.PanelOutline,
            new Color(UiPalette.Gold.r, UiPalette.Gold.g, UiPalette.Gold.b, .55f)
        );
        edge.raycastTarget = false;
    }

    // =========================================
    // PANELS AND TEXT
    // =========================================

    private void StylePanel(Image image)
    {
        if (!IsStock(image) || image.color.a < .05f || image.GetComponent<Selectable>() != null)
            return;
        Mask mask = image.GetComponent<Mask>();
        if (mask != null && !mask.showMaskGraphic)
            return; // scroll viewports: the graphic is never drawn

        // Outer panels get the navy card look; anything nested inside one becomes a darker inset or list row.
        bool nested = HasStyledPanelAbove(image.transform);
        bool row =
            image.GetComponentInParent<ScrollRect>() != null
            && image.GetComponent<ScrollRect>() == null;
        Color color =
            row ? UiPalette.Row
            : nested ? UiPalette.Inset
            : UiPalette.Navy;
        StyleImage(image, UiSprites.Panel, color);
    }

    private void StyleText(TMP_Text text)
    {
        if (text.GetComponentInParent<Selectable>() != null)
            return; // button and field labels are handled with their control
        Color c = text.color;
        bool stockDark = Mathf.Abs(c.r - c.g) < .05f && Mathf.Abs(c.g - c.b) < .05f && c.r < .4f;
        if (stockDark)
            text.color = Color.white;
    }

    private bool HasStyledPanelAbove(Transform child)
    {
        for (Transform t = child.parent; t != null; t = t.parent)
        {
            Image image = t.GetComponent<Image>();
            if (image != null && image.sprite == UiSprites.Panel)
                return true;
        }
        return false;
    }

    // =========================================
    // TITLE SCREEN
    // =========================================

    private void BuildTitle()
    {
        GameObject battle = GameObject.Find("Battle");
        Canvas canvas =
            battle != null
                ? battle.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;
        Transform root = canvas.rootCanvas.transform;

        Label(root, "Trickal TCG", new Vector2(0, 230), 110, UiPalette.Gold, FontStyles.Bold);
        Label(
            root,
            Korean ? "카드 배틀" : "Card Battle",
            new Vector2(0, 140),
            36,
            Color.white,
            FontStyles.Normal
        );

        // Stack the two existing buttons under the title, larger and centred.
        PlaceButton(battle, new Vector2(0, -10));
        PlaceButton(GameObject.Find("Edit Deck"), new Vector2(0, -110));

        TextMeshProUGUI notice = Label(
            root,
            Korean
                ? "비공식 팬 게임입니다. 트릭컬 캐릭터와 관련 권리는 각 권리자에게 있습니다."
                : "Unofficial fan game. Trickal characters and related rights belong to their respective owners.",
            Vector2.zero,
            22,
            UiPalette.Muted,
            FontStyles.Italic
        );
        Anchor(
            notice.rectTransform,
            new Vector2(.5f, 0),
            new Vector2(0, 40),
            new Vector2(1500, 40)
        );

        TextMeshProUGUI version = Label(
            root,
            "v" + Application.version,
            Vector2.zero,
            22,
            UiPalette.Muted,
            FontStyles.Normal
        );
        version.alignment = TextAlignmentOptions.Right;
        Anchor(
            version.rectTransform,
            new Vector2(1, 0),
            new Vector2(-130, 40),
            new Vector2(220, 40)
        );
    }

    private static void PlaceButton(GameObject button, Vector2 position)
    {
        if (button == null)
            return;
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(420, 84);
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            label.fontSize = 36;
    }

    // =========================================
    // HELPERS
    // =========================================

    private void AddBackdrop(Transform root)
    {
        Image backdrop = new GameObject(
            "Menu Backdrop",
            typeof(RectTransform)
        ).AddComponent<Image>();
        RectTransform rect = backdrop.rectTransform;
        rect.SetParent(root, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        backdrop.sprite = UiSprites.VerticalGradient(
            UiPalette.BackdropTop,
            UiPalette.BackdropBottom
        );
        backdrop.raycastTarget = false;
        Mark(backdrop);
    }

    private TextMeshProUGUI Label(
        Transform parent,
        string value,
        Vector2 position,
        float size,
        Color color,
        FontStyles style
    )
    {
        TextMeshProUGUI text = new GameObject(
            "Title Label",
            typeof(RectTransform)
        ).AddComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = new Vector2(1400, size * 1.4f);
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = value;
        if (style == FontStyles.Bold)
        {
            text.outlineWidth = .2f;
            text.outlineColor = new Color32(40, 25, 10, 255);
        }
        return Mark(text);
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void StyleImage(Image image, Sprite sprite, Color color)
    {
        if (image == null)
            return;
        Mark(image);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1;
        image.color = color;
    }

    // Unity's default UI sprites (or none). The stock textures are tiny; real art is not.
    private static bool IsStock(Image image)
    {
        Sprite sprite = image.sprite;
        return sprite == null
            || (
                StockSprites.Contains(sprite.name)
                && sprite.texture != null
                && sprite.texture.width <= 64
            );
    }

    // Cards rendered inside menus (the deck editor grid, hand-style views) keep their own frames.
    private static bool InsideCard(Transform transform) =>
        transform.GetComponentInParent<DeckEditorCardView>(true) != null
        || transform.GetComponentInParent<CardviewBase>(true) != null;

    // Not yet styled, part of the menu scene itself (not the Escape menu or other overlays kept
    // across scenes), and not part of a card.
    private bool Eligible(Component component) =>
        component.gameObject.scene == SceneManager.GetActiveScene()
        && !InsideCard(component.transform)
        && styled.Add(component.GetInstanceID());

    private static int Depth(Transform transform)
    {
        int depth = 0;
        for (Transform t = transform.parent; t != null; t = t.parent)
            depth++;
        return depth;
    }

    private T Mark<T>(T component)
        where T : Component
    {
        styled.Add(component.GetInstanceID());
        return component;
    }
}
