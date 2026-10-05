using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EscapeMenu : MonoBehaviour
{
    private static EscapeMenu instance;
    private GameObject overlay;
    private RectTransform panel;
    private Font font;
    private PlayerIconCatalog catalog;
    private bool choosingIcons;
    private int page;
    private const int PageSize = 6;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        instance = new GameObject("Escape Menu").AddComponent<EscapeMenu>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        catalog = Resources.Load<PlayerIconCatalog>("PlayerIconCatalog");
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private void OnDestroy() { SceneManager.sceneLoaded -= SceneLoaded; }
    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Close();
        choosingIcons = false;
        page = 0;
    }
    private bool InBattle => SceneManager.GetActiveScene().name == "Battlefield";
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool pressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (!pressed) return;
        if (overlay != null && overlay.activeSelf) Close();
        else { choosingIcons = false; Build(); }
    }
    private void Close() { if (overlay != null) overlay.SetActive(false); }

    private void Build()
    {
        if (overlay != null) Destroy(overlay);
        overlay = new GameObject("Escape Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        Image shade = Element("Backdrop", overlay.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        shade.color = new Color(0, 0, 0, .65f);
        RectTransform shadeRect = shade.rectTransform;
        shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one;
        shadeRect.sizeDelta = Vector2.zero;
        panel = Element("Menu Panel", overlay.transform, Vector2.zero, new Vector2(520, choosingIcons ? 680 : 380));
        panel.gameObject.AddComponent<Image>().color = new Color(.13f, .16f, .21f, 1);
        Label(panel, choosingIcons ? "Choose Player Icon" : "Menu", new Vector2(0, choosingIcons ? 300 : 145), new Vector2(470, 50), 28);
        if (choosingIcons) { BuildIcons(); return; }
        Button settings = MenuButton("Settings", 70, () => { });
        settings.interactable = false;
        if (InBattle)
        {
            Button surrender = MenuButton("Surrender", 5, Surrender);
            surrender.interactable = GameManager.Instance != null && !GameManager.Instance.IsGameOver;
        }
        else
        {
            Button icon = MenuButton("Player Icon", 5, () => { choosingIcons = true; Build(); });
            icon.interactable = LocalAccountSession.IsLoggedIn && catalog != null && catalog.icons.Length > 0;
        }
        MenuButton("Quit to Login", -60, Quit);
        MenuButton("Close", -125, Close);
    }
    private void BuildIcons()
    {
        int total = catalog == null ? 0 : catalog.icons.Length;
        int start = page * PageSize;
        for (int i = start; i < Mathf.Min(start + PageSize, total); i++)
        {
            int index = i;
            Button button = MenuButton(catalog.icons[i].name, 220 - (i - start) * 65, () =>
            {
                catalog.Select(index);
                foreach (PlayerView view in FindObjectsByType<PlayerView>(FindObjectsSortMode.None)) view.RefreshPlayerIcon();
                choosingIcons = false;
                Build();
            });
            Image image = Element("Icon", button.transform, new Vector2(-190, 0), new Vector2(48, 48)).gameObject.AddComponent<Image>();
            image.sprite = catalog.GetSprite(i); image.preserveAspect = true; image.raycastTarget = false;
        }
        MenuButton("Previous", -180, () => { page--; Build(); }).interactable = page > 0;
        MenuButton("Next", -240, () => { page++; Build(); }).interactable = start + PageSize < total;
        MenuButton("Back", -300, () => { choosingIcons = false; Build(); });
    }
    private void Surrender()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        Close();
        GameManager.Instance.PlayerDefeated(PlayerSide.Player);
    }
    private void Quit()
    {
        if (InBattle && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            GameManager.Instance.PlayerDefeated(PlayerSide.Player);
        Close();
        Time.timeScale = 1f;
        LocalAccountSession.LogoutAndOpenLogin();
    }
    private Button MenuButton(string text, float y, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Element(text, panel, new Vector2(0, y), new Vector2(460, 52));
        Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.28f, .34f, .43f);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label(rect, text, Vector2.zero, new Vector2(440, 48), 23);
        return button;
    }
    private void Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
    {
        Text text = Element("Label", parent, position, size).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = fontSize; text.text = value;
        text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
    }
    private static RectTransform Element(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
}
