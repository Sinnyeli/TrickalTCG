using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Friendly, Hearthstone-style dressing for the Battlefield scene, built in code at runtime:
// a warm board, hero health gems with an active-turn ring, deck and mana badges, a big
// End Turn button with a turn timer, and a framed Commander zone for each side.
// Gameplay objects and their layouts are untouched; decorations are added as siblings behind them.
public class BattleHud : MonoBehaviour
{
    private static readonly Color Gold = UiPalette.Gold;
    private static readonly Color Navy = UiPalette.Navy;
    private static readonly Color HealthRed = UiPalette.HealthRed;
    private static readonly Color ManaBlue = UiPalette.ManaBlue;
    private static readonly Color ReadyGreen = UiPalette.ReadyGreen;
    private static readonly Color Muted = UiPalette.Muted;

    private static BattleHud instance;
    private TMP_FontAsset font;
    private bool built;

    private Button endTurn;
    private Image endTurnImage;
    private TMP_Text endTurnLabel;
    private TextMeshProUGUI turnLabel;
    private Image timerFill;
    private readonly Dictionary<PlayerView, Hero> heroes = new Dictionary<PlayerView, Hero>();
    private readonly Dictionary<PlayerSide, CommanderZone> commanderZones =
        new Dictionary<PlayerSide, CommanderZone>();

    private sealed class Hero
    {
        public Image Ring;
        public TextMeshProUGUI Deck;
        public TextMeshProUGUI Mana;
    }

    private sealed class CommanderZone
    {
        public Image Border;
        public TextMeshProUGUI Status;
    }

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
        instance = new GameObject("Battle HUD").AddComponent<BattleHud>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private static bool Korean =>
        LocalizationManager.Instance != null
        && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Korean;

    private static string T(string english, string korean) => Korean ? korean : english;

    private void Awake()
    {
        BattleUXResources resources = GameAudio.Clips;
        font =
            resources != null && resources.menuFont != null
                ? resources.menuFont
                : TMP_Settings.defaultFontAsset;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= SceneLoaded;

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        built = false;
        endTurn = null;
        heroes.Clear();
        commanderZones.Clear();
    }

    // =========================================
    // BUILD (once per battle scene; heroes as they appear)
    // =========================================

    private void LateUpdate()
    {
        GameManager game = GameManager.Instance;
        if (game == null || SceneManager.GetActiveScene().name != "Battlefield")
            return;
        if (!built)
            built = TryBuild(game);
        if (!built)
            return;

        foreach (PlayerView view in FindObjectsByType<PlayerView>(FindObjectsSortMode.None))
            if (!heroes.ContainsKey(view))
                heroes[view] = DecorateHero(view);

        Refresh(game);
    }

    private bool TryBuild(GameManager game)
    {
        HandManager playerHand = game.GetHandManager(PlayerSide.Player);
        if (playerHand == null || playerHand.HandContainer == null)
            return false;
        Canvas canvas = playerHand.HandContainer.GetComponentInParent<Canvas>();
        if (canvas == null)
            return false;

        BuildBoard(canvas.rootCanvas.transform, game);
        foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
        {
            Transform container = game.GetHandManager(side)?.CommanderContainer;
            if (container != null)
                commanderZones[side] = BuildCommanderZone((RectTransform)container, side);
        }
        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui != null && ui.EndTurnButton != null)
            BuildEndTurn(ui.EndTurnButton);
        return true;
    }

    private void BuildBoard(Transform root, GameManager game)
    {
        Image backdrop = Panel(
            "Board Backdrop",
            root,
            UiSprites.VerticalGradient(UiPalette.BackdropTop, UiPalette.BackdropBottom),
            Color.white
        );
        backdrop.type = Image.Type.Simple;
        Stretch(backdrop.rectTransform);
        backdrop.transform.SetAsFirstSibling();

        Transform playerField = game.GetBattlefield(PlayerSide.Player)?.MinionContainer?.parent;
        Transform enemyField = game.GetBattlefield(PlayerSide.Opponent)?.MinionContainer?.parent;
        if (playerField == null || enemyField == null || playerField.parent != enemyField.parent)
            return;

        // The table: one warm panel under both halves, a lighter tint on the player's half and a divider.
        RectTransform area = (RectTransform)playerField.parent;
        Image table = Behind(area, "Board Table", UiSprites.Panel, new Color(.86f, .74f, .55f), 22);
        Behind(area, "Board Table Edge", UiSprites.PanelOutline, new Color(.45f, .3f, .17f), 22);
        Behind(
            (RectTransform)playerField,
            "Player Half",
            UiSprites.Panel,
            new Color(1f, .96f, .86f, .35f),
            0
        );
        Behind(
            (RectTransform)enemyField,
            "Enemy Half",
            UiSprites.Panel,
            new Color(.62f, .5f, .38f, .25f),
            0
        );

        Image divider = Panel(
            "Board Divider",
            table.transform.parent,
            null,
            new Color(.45f, .3f, .17f, .6f)
        );
        RectTransform line = divider.rectTransform;
        line.anchorMin = new Vector2(
            area.anchorMin.x + .02f,
            (area.anchorMin.y + area.anchorMax.y) / 2
        );
        line.anchorMax = new Vector2(area.anchorMax.x - .02f, line.anchorMin.y);
        line.sizeDelta = new Vector2(0, 4);
        line.SetSiblingIndex(area.GetSiblingIndex());
    }

    private CommanderZone BuildCommanderZone(RectTransform container, PlayerSide side)
    {
        Behind(container, "Commander Zone", UiSprites.Panel, Navy, 6);
        Image border = Behind(container, "Commander Zone Border", UiSprites.PanelOutline, Gold, 6);

        // The tab sits on the edge facing the board: top for the player, bottom for the opponent.
        bool top = side == PlayerSide.Player;
        Image tab = Panel("Commander Tab", container.parent, UiSprites.Panel, Gold);
        RectTransform tabRect = tab.rectTransform;
        tabRect.anchorMin = tabRect.anchorMax = new Vector2(
            (container.anchorMin.x + container.anchorMax.x) / 2,
            top ? container.anchorMax.y : container.anchorMin.y
        );
        tabRect.sizeDelta = new Vector2(250, 34);
        tabRect.anchoredPosition = new Vector2(0, top ? 6 : -6);
        tabRect.SetSiblingIndex(container.GetSiblingIndex()); // under the commander card, never over its cost
        TextMeshProUGUI status = Text(tabRect, "", 20, new Color(.15f, .1f, .05f));
        Stretch(status.rectTransform);
        return new CommanderZone { Border = border, Status = status };
    }

    private void BuildEndTurn(Button button)
    {
        endTurn = button;
        RectTransform rect = (RectTransform)button.transform;

        // Retire the debug labels next to the button; the HUD shows the same information.
        foreach (Transform sibling in rect.parent)
            if (sibling != rect)
                sibling.gameObject.SetActive(false);

        rect.sizeDelta = new Vector2(230, 86);
        endTurnImage = button.targetGraphic as Image;
        if (endTurnImage != null)
        {
            endTurnImage.sprite = UiSprites.Panel;
            endTurnImage.type = Image.Type.Sliced;
        }
        Image edge = Panel(
            "End Turn Edge",
            rect,
            UiSprites.PanelOutline,
            new Color(.3f, .2f, .08f, .8f)
        );
        Stretch(edge.rectTransform);
        endTurnLabel = button.GetComponentInChildren<TMP_Text>();
        if (endTurnLabel != null)
        {
            endTurnLabel.font = font;
            endTurnLabel.fontSize = 30;
            endTurnLabel.fontStyle = FontStyles.Bold;
            endTurnLabel.alignment = TextAlignmentOptions.Center;
            endTurnLabel.transform.SetAsLastSibling();
        }

        turnLabel = Text(rect.parent, "", 24, Color.white);
        AnchorLike(turnLabel.rectTransform, rect, new Vector2(0, 72), new Vector2(230, 36));

        // The turn timer: a rounded track that drains as the turn runs out.
        Image track = Panel("Turn Timer", rect.parent, UiSprites.Panel, new Color(0, 0, 0, .45f));
        AnchorLike(track.rectTransform, rect, new Vector2(0, -62), new Vector2(210, 16));
        timerFill = Panel("Turn Timer Fill", track.rectTransform, UiSprites.Panel, Gold);
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        Stretch(timerFill.rectTransform);
    }

    private Hero DecorateHero(PlayerView view)
    {
        RectTransform root = (RectTransform)view.transform;
        var hero = new Hero();

        Image ring = Panel("Active Turn Ring", root, UiSprites.Ring, Gold);
        ring.rectTransform.sizeDelta = new Vector2(250, 250);
        ring.transform.SetAsFirstSibling();
        hero.Ring = ring;

        // Health: white number on a red gem at the portrait's lower right.
        TMP_Text health = view.HealthText;
        if (health != null)
        {
            Image gem = Panel("Health Gem", root, UiSprites.Circle, HealthRed);
            gem.rectTransform.anchoredPosition = new Vector2(72, -70);
            gem.rectTransform.sizeDelta = new Vector2(74, 74);
            Image gemEdge = Panel(
                "Health Gem Edge",
                gem.rectTransform,
                UiSprites.Ring,
                new Color(.35f, .05f, .05f)
            );
            Stretch(gemEdge.rectTransform);
            RectTransform text = health.rectTransform;
            text.SetParent(gem.rectTransform, false);
            Stretch(text);
            health.font = font;
            health.fontSize = 36;
            health.fontStyle = FontStyles.Bold;
            health.color = Color.white;
            health.alignment = TextAlignmentOptions.Center;
            health.enableAutoSizing = false;
        }

        // Deck: a card-back badge to the portrait's left.
        Image deck = Panel("Deck Badge", root, UiSprites.Panel, Navy);
        deck.rectTransform.anchoredPosition = new Vector2(-175, -10);
        deck.rectTransform.sizeDelta = new Vector2(70, 96);
        Image deckEdge = Panel("Deck Badge Edge", deck.rectTransform, UiSprites.PanelOutline, Gold);
        Stretch(deckEdge.rectTransform);
        hero.Deck = Text(deck.rectTransform, "", 34, Color.white);
        Stretch(hero.Deck.rectTransform);
        TextMeshProUGUI deckCaption = Text(deck.rectTransform, T("Deck", "덱"), 18, Muted);
        deckCaption.rectTransform.anchoredPosition = new Vector2(0, -66);

        // Mana: a crystal showing available/maximum just past the end of the mana bar.
        ManaBarView bar = view.GetComponentInChildren<ManaBarView>(true);
        Image crystal = Panel("Mana Crystal", root, UiSprites.Diamond, ManaBlue);
        crystal.rectTransform.sizeDelta = new Vector2(84, 84);
        if (bar != null)
        {
            RectTransform barRect = (RectTransform)bar.transform;
            crystal.rectTransform.anchoredPosition =
                barRect.anchoredPosition + new Vector2(barRect.sizeDelta.x / 2 + 52, 0);
        }
        else
            crystal.rectTransform.anchoredPosition = new Vector2(175, -10);
        hero.Mana = Text(crystal.rectTransform, "", 24, Color.white);
        Stretch(hero.Mana.rectTransform);
        return hero;
    }

    // =========================================
    // REFRESH (every frame)
    // =========================================

    private void Refresh(GameManager game)
    {
        TurnManager turns = game.TurnManager;
        bool myTurn = turns.IsMyTurn(PlayerSide.Player) && !game.IsGameOver;
        float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 3f);

        foreach (var pair in heroes)
        {
            if (pair.Key == null)
                continue;
            PlayerSide side = pair.Key.Side;
            Hero hero = pair.Value;
            bool active = turns.IsMyTurn(side) && !game.IsGameOver;
            hero.Ring.color = new Color(Gold.r, Gold.g, Gold.b, active ? .45f + .4f * pulse : 0f);
            hero.Deck.text = game.GetDeck(side).GetDeckCount().ToString();
            hero.Mana.text = $"{turns.GetMana(side)}/{turns.GetMaxMana(side)}";
        }

        foreach (var pair in commanderZones)
            RefreshCommanderZone(game, pair.Key, pair.Value, pulse);

        if (endTurn == null)
            return;
        turnLabel.text = T($"Turn {turns.TurnNumber}", $"{turns.TurnNumber}턴");
        if (endTurnLabel != null)
            endTurnLabel.text =
                game.IsGameOver ? T("Game Over", "게임 종료")
                : myTurn ? T("End Turn", "턴 종료")
                : T("Enemy Turn", "상대 턴");

        // Like Hearthstone, the button turns green and pulses once nothing else can be done.
        bool nothingLeft = myTurn && !PlayerHasMoves(game);
        if (endTurnImage != null)
            endTurnImage.color =
                !myTurn ? new Color(.45f, .47f, .52f)
                : nothingLeft ? Color.Lerp(ReadyGreen, Color.white, .25f * pulse)
                : Gold;
        if (endTurnLabel != null)
            endTurnLabel.color = myTurn ? new Color(.15f, .1f, .05f) : new Color(.85f, .87f, .9f);
        endTurn.transform.localScale = Vector3.one * (nothingLeft ? 1f + .04f * pulse : 1f);

        float limit = Mathf.Max(1f, turns.TurnDurationSeconds);
        timerFill.fillAmount = game.IsGameOver
            ? 0
            : Mathf.Clamp01(turns.RemainingTurnSeconds / limit);
        timerFill.color = turns.RemainingTurnSeconds <= 10f ? HealthRed : Gold;
    }

    private void RefreshCommanderZone(
        GameManager game,
        PlayerSide side,
        CommanderZone zone,
        float pulse
    )
    {
        RuntimeCard commander = FindCommander(game.GetHandManager(side).Hand);
        bool inHand = commander != null;
        bool onField = !inHand && FindCommander(game.GetBattlefield(side).Minions) != null;

        string status;
        Color border = Gold;
        if (inHand && side == PlayerSide.Player && BattleFeedback.CanPlayNow(commander))
        {
            status = T("Commander · Ready!", "지휘관 · 출전 가능!");
            border = Color.Lerp(ReadyGreen, Color.white, .3f * pulse);
        }
        else if (inHand)
            status =
                side == PlayerSide.Player
                    ? T(
                        $"Commander · {commander.GetManaCost()} mana",
                        $"지휘관 · 마나 {commander.GetManaCost()}"
                    )
                    : T("Commander · Waiting", "지휘관 · 대기 중");
        else if (onField)
            status = T("Commander · In battle", "지휘관 · 전투 중");
        else
        {
            status = T("Commander · Fallen", "지휘관 · 쓰러짐");
            border = Muted;
        }
        zone.Status.text = status;
        zone.Border.color = border;
    }

    private static RuntimeCard FindCommander(IReadOnlyList<RuntimeCard> cards)
    {
        foreach (RuntimeCard card in cards)
            if (card != null && card.IsCommander)
                return card;
        return null;
    }

    private static bool PlayerHasMoves(GameManager game)
    {
        foreach (RuntimeCard card in game.GetHandManager(PlayerSide.Player).Hand)
            if (BattleFeedback.CanPlayNow(card))
                return true;
        foreach (RuntimeCard unit in game.GetBattlefield(PlayerSide.Player).Minions)
            if (BattleFeedback.CanAttackNow(unit))
                return true;
        return false;
    }

    // =========================================
    // HELPERS
    // =========================================

    // A decoration drawn behind target, covering it plus padding on every side.
    private static Image Behind(
        RectTransform target,
        string name,
        Sprite sprite,
        Color color,
        float padding
    )
    {
        Image image = Panel(name, target.parent, sprite, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = target.anchorMin;
        rect.anchorMax = target.anchorMax;
        rect.pivot = target.pivot;
        rect.offsetMin = target.offsetMin - new Vector2(padding, padding);
        rect.offsetMax = target.offsetMax + new Vector2(padding, padding);
        rect.SetSiblingIndex(target.GetSiblingIndex());
        return image;
    }

    private static Image Panel(string name, Transform parent, Sprite sprite, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type =
            sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI Text(Transform parent, string value, float size, Color color)
    {
        var rect = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(240, 40);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void AnchorLike(
        RectTransform rect,
        RectTransform reference,
        Vector2 offset,
        Vector2 size
    )
    {
        rect.anchorMin = reference.anchorMin;
        rect.anchorMax = reference.anchorMax;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = reference.anchoredPosition + offset;
        rect.sizeDelta = size;
    }
}
