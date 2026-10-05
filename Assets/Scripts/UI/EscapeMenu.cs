using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EscapeMenu : MonoBehaviour
{
    private static EscapeMenu instance;
    private GameObject overlay;
    private RectTransform panel;
    private TMP_FontAsset font;
    public static bool IsOpen => instance != null && instance.overlay != null && instance.overlay.activeSelf;
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
        var resources = Resources.Load<BattleUXResources>("BattleUXResources");
        font = resources == null ? TMP_Settings.defaultFontAsset : resources.menuFont;
        if (LocalizationManager.Instance == null) new GameObject("Localization Manager").AddComponent<LocalizationManager>();
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
        panel = Element("Menu Panel", overlay.transform, Vector2.zero, new Vector2(520, choosingIcons ? 680 : 460));
        panel.gameObject.AddComponent<Image>().color = new Color(.13f, .16f, .21f, 1);
        Label(panel, choosingIcons ? "Choose Player Icon" : "Menu", new Vector2(0, choosingIcons ? 300 : 185), new Vector2(470, 50), 28);
        if (choosingIcons) { BuildIcons(); return; }
        Button settings = MenuButton("Settings", 120, () => { });
        settings.interactable = false;
        MenuButton(Korean ? "언어: 한국어" : "Language: English", 55, () =>
        {
            LocalizationManager.Instance.SetLanguage(Korean ? GameLanguage.English : GameLanguage.Korean);
            foreach (CardviewBase view in FindObjectsByType<CardviewBase>(FindObjectsSortMode.None))
                if (view.runtimeCard != null) view.SetCard(view.runtimeCard);
            Build();
        });
        if (InBattle)
        {
            Button surrender = MenuButton("Surrender", -10, Surrender);
            surrender.interactable = GameManager.Instance != null && !GameManager.Instance.IsGameOver;
        }
        else
        {
            Button icon = MenuButton("Player Icon", -10, () => { choosingIcons = true; Build(); });
            icon.interactable = LocalAccountSession.IsLoggedIn && catalog != null && catalog.icons.Length > 0;
        }
        MenuButton("Quit to Login", -75, Quit);
        MenuButton("Close", -140, Close);
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
        TextMeshProUGUI text = Element("Label", parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = fontSize; text.text = Translate(value);
        text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
    }
    private bool Korean => LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Korean;
    private string Translate(string value)
    {
        if (!Korean) return value;
        switch (value)
        {
            case "Menu": return "메뉴";
            case "Settings": return "설정";
            case "Player Icon": return "플레이어 아이콘";
            case "Choose Player Icon": return "플레이어 아이콘 선택";
            case "Surrender": return "항복";
            case "Quit to Login": return "로그인 화면으로";
            case "Close": return "닫기";
            case "Previous": return "이전";
            case "Next": return "다음";
            case "Back": return "뒤로";
            default: return value;
        }
    }
    private static RectTransform Element(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
}
