using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Makes battle actions readable: damage/heal numbers, death fades, a turn banner,
// a last-seconds countdown and highlights for cards and units that can act.
// Everything is driven by what the views display, so it behaves the same for the host,
// the AI opponent and an online guest (who only receives state snapshots).
public class BattleFeedback : MonoBehaviour
{
    private const float ImpactDelay = .14f; // matches the lunge in CombatPresentation
    private const float WarningSeconds = 10f;

    private static readonly Color DamageColor = new Color(1f, .32f, .28f);
    private static readonly Color HealColor = new Color(.45f, 1f, .45f);
    private static readonly Color PlayerTurnColor = new Color(1f, .82f, .4f);
    private static readonly Color OpponentTurnColor = new Color(1f, .45f, .45f);

    private static BattleFeedback instance;
    private RectTransform root;
    private TMP_FontAsset font;
    private TextMeshProUGUI countdown;
    private readonly HashSet<Transform> punching = new HashSet<Transform>();
    private int lastTurnNumber;
    private PlayerSide lastTurnSide;
    private bool warnedThisTurn;
    private bool wasGameOver;

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
        instance = new GameObject("Battle Feedback").AddComponent<BattleFeedback>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private static BattleFeedback Active =>
        Application.isPlaying && instance != null && InBattle ? instance : null;

    private static bool InBattle => SceneManager.GetActiveScene().name == "Battlefield";

    private static bool Korean =>
        LocalizationManager.Instance != null
        && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Korean;

    // =========================================
    // SETUP
    // =========================================

    private void Awake()
    {
        BattleUXResources resources = GameAudio.Clips;
        font =
            resources != null && resources.menuFont != null
                ? resources.menuFont
                : TMP_Settings.defaultFontAsset;

        // No GraphicRaycaster: feedback never blocks clicks on the board.
        var canvasObject = new GameObject(
            "Battle Feedback Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250; // above combat lunges (200), below the Escape menu
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        root = (RectTransform)canvasObject.transform;

        countdown = CreateText("Turn Countdown", 72, DamageColor);
        countdown.rectTransform.anchoredPosition = new Vector2(0, 330);
        countdown.gameObject.SetActive(false);

        RuntimeCard.OnGameplayTrigger += HandleGameplayTrigger;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDestroy()
    {
        RuntimeCard.OnGameplayTrigger -= HandleGameplayTrigger;
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        punching.Clear();
        for (int i = root.childCount - 1; i >= 0; i--)
            if (root.GetChild(i) != countdown.transform)
                Destroy(root.GetChild(i).gameObject);
        countdown.gameObject.SetActive(false);
        lastTurnNumber = 0;
        warnedThisTurn = false;
        wasGameOver = false;
    }

    // =========================================
    // TURN BANNER, COUNTDOWN AND GAME OVER
    // =========================================

    private void Update()
    {
        GameManager game = GameManager.Instance;
        TurnManager turns = game != null ? game.TurnManager : null;
        if (!InBattle || turns == null)
        {
            countdown.gameObject.SetActive(false);
            return;
        }

        if (game.IsGameOver)
        {
            countdown.gameObject.SetActive(false);
            if (!wasGameOver)
                GameAudio.PlaySfx(
                    game.WinnerSide == PlayerSide.Player
                        ? GameAudio.Clips?.victorySound
                        : GameAudio.Clips?.defeatSound
                );
            wasGameOver = true;
            return;
        }

        if (
            turns.TurnNumber > 0
            && (turns.TurnNumber != lastTurnNumber || turns.CurrentSide != lastTurnSide)
        )
        {
            lastTurnNumber = turns.TurnNumber;
            lastTurnSide = turns.CurrentSide;
            warnedThisTurn = false;
            bool mine = turns.CurrentSide == PlayerSide.Player;
            StartCoroutine(
                Banner(
                    mine
                        ? (Korean ? "내 턴" : "Your Turn")
                        : (Korean ? "상대 턴" : "Opponent's Turn"),
                    mine ? PlayerTurnColor : OpponentTurnColor
                )
            );
            if (mine)
                GameAudio.PlaySfx(GameAudio.Clips?.turnStartSound);
        }

        bool warning = turns.CanPlayerEndTurn && turns.RemainingTurnSeconds <= WarningSeconds;
        countdown.gameObject.SetActive(warning);
        if (!warning)
            return;

        if (!warnedThisTurn)
        {
            warnedThisTurn = true;
            GameAudio.PlaySfx(GameAudio.Clips?.turnTimerWarningSound);
        }
        int seconds = Mathf.CeilToInt(turns.RemainingTurnSeconds);
        countdown.text = Korean ? $"{seconds}초" : $"{seconds}s";
        float pulse = 1f + .08f * Mathf.Sin(Time.unscaledTime * 8f);
        countdown.rectTransform.localScale = Vector3.one * pulse;
    }

    private IEnumerator Banner(string message, Color color)
    {
        RectTransform strip = Element("Turn Banner", root, Vector2.zero, new Vector2(0, 150));
        strip.anchorMin = new Vector2(0, .5f);
        strip.anchorMax = new Vector2(1, .5f);
        strip.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .6f);
        strip.GetComponent<Image>().raycastTarget = false;
        CanvasGroup group = strip.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        TextMeshProUGUI label = CreateText("Label", 84, color, strip);
        label.text = message;

        yield return Fade(group, label.rectTransform, 0f, 1f, 1.15f, 1f, .2f);
        yield return new WaitForSecondsRealtime(.8f);
        yield return Fade(group, label.rectTransform, 1f, 0f, 1f, 1f, .3f);
        Destroy(strip.gameObject);
    }

    // =========================================
    // NUMBERS, ATTACK PUNCH AND DEATH
    // =========================================

    /// <summary>Called by unit and hero views when the Health they show changes.</summary>
    public static void HealthChanged(Transform anchor, int delta)
    {
        BattleFeedback feedback = Active;
        if (feedback == null || anchor == null || delta == 0)
            return;
        feedback.StartCoroutine(feedback.Number(anchor.position, delta, CurrentDelay));
        GameAudio.PlaySfx(delta < 0 ? GameAudio.Clips?.damageSound : GameAudio.Clips?.healSound);
    }

    /// <summary>Briefly enlarges a stat label so a changed value catches the eye.</summary>
    public static void Punch(Transform target)
    {
        BattleFeedback feedback = Active;
        // One punch at a time per label, so overlapping changes can't leave it enlarged.
        if (feedback != null && target != null && feedback.punching.Add(target))
            feedback.StartCoroutine(feedback.PunchRoutine(target));
    }

    /// <summary>Called just before a unit's view is destroyed, to leave a fading copy behind.</summary>
    public static void UnitRemoved(MinionViewBase view, bool died)
    {
        BattleFeedback feedback = Active;
        if (feedback == null || view == null)
            return;

        // A unit removed before its view showed the final blow still shows the lost Health.
        RuntimeCard card = view.RuntimeCard;
        if (died && card != null && view.ShownHealth > card.CurrentHealth)
            HealthChanged(view.transform, card.CurrentHealth - view.ShownHealth);

        GameObject ghost = CombatPresentation.CopyVisuals(view.transform, feedback.root);
        RectTransform rect = (RectTransform)ghost.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = feedback.ToLocal(view.transform.position);
        Vector3 rootScale = feedback.root.lossyScale;
        Vector3 viewScale = view.transform.lossyScale;
        rect.localScale = new Vector3(viewScale.x / rootScale.x, viewScale.y / rootScale.y, 1f);
        rect.rotation = view.transform.rotation;
        CanvasGroup group = ghost.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        feedback.StartCoroutine(feedback.Vanish(group, rect, died, CurrentDelay));
    }

    public static void UnitEntered() => GameAudio.PlaySfx(GameAudio.Clips?.cardPlayedSound);

    public static void AttackLanded() => GameAudio.PlaySfx(GameAudio.Clips?.attackHitSound);

    // Hold feedback until a lunge in progress lands, so numbers appear on impact.
    private static float CurrentDelay => CombatPresentation.IsAnimating ? ImpactDelay : 0f;

    private void HandleGameplayTrigger(
        CardTriggerType type,
        RuntimeCard actor,
        RuntimeCard subject,
        RuntimeModifier snapshot
    )
    {
        // Units announce themselves through BattlefieldManager; spells only through this event (host side).
        if (type == CardTriggerType.SpellCast && InBattle)
            UnitEntered();
    }

    private IEnumerator Number(Vector3 worldPosition, int delta, float delay)
    {
        if (delay > 0)
            yield return new WaitForSecondsRealtime(delay);
        TextMeshProUGUI text = CreateText("Health Change", 64, delta < 0 ? DamageColor : HealColor);
        text.text = delta > 0 ? "+" + delta : delta.ToString();
        text.outlineWidth = .25f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        RectTransform rect = text.rectTransform;
        Vector2 start = ToLocal(worldPosition) + new Vector2(Random.Range(-18f, 18f), 20f);

        const float duration = .9f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / duration;
            rect.anchoredPosition = start + new Vector2(0, 90f * Mathf.Sqrt(t));
            rect.localScale =
                Vector3.one
                * (
                    t < .15f
                        ? Mathf.Lerp(.6f, 1.25f, t / .15f)
                        : Mathf.Lerp(1.25f, 1f, (t - .15f) / .85f)
                );
            text.alpha = t < .6f ? 1f : 1f - (t - .6f) / .4f;
            yield return null;
        }
        Destroy(text.gameObject);
    }

    private IEnumerator PunchRoutine(Transform target)
    {
        Vector3 original = target.localScale;
        const float duration = .3f;
        try
        {
            for (
                float elapsed = 0;
                elapsed < duration && target != null;
                elapsed += Time.unscaledDeltaTime
            )
            {
                target.localScale =
                    original * (1f + .45f * Mathf.Sin(Mathf.PI * elapsed / duration));
                yield return null;
            }
        }
        finally
        {
            if (target != null)
                target.localScale = original;
            punching.Remove(target);
        }
    }

    private IEnumerator Vanish(CanvasGroup group, RectTransform rect, bool died, float delay)
    {
        if (delay > 0)
        {
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(delay);
        }
        if (died)
        {
            GameAudio.PlaySfx(GameAudio.Clips?.unitDiedSound);
            foreach (Graphic graphic in rect.GetComponentsInChildren<Graphic>())
                graphic.color = Color.Lerp(
                    graphic.color,
                    new Color(1f, .3f, .3f, graphic.color.a),
                    .45f
                );
        }
        Vector3 startScale = rect.localScale;
        Vector2 startPosition = rect.anchoredPosition;
        float duration = died ? .5f : .3f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / duration;
            group.alpha = 1f - t;
            if (died)
                rect.localScale = startScale * Mathf.Lerp(1f, .65f, t);
            else
                rect.anchoredPosition = startPosition + new Vector2(0, 60f * t);
            yield return null;
        }
        Destroy(rect.gameObject);
    }

    private static IEnumerator Fade(
        CanvasGroup group,
        RectTransform target,
        float fromAlpha,
        float toAlpha,
        float fromScale,
        float toScale,
        float duration
    )
    {
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            target.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
            yield return null;
        }
        group.alpha = toAlpha;
        target.localScale = Vector3.one * toScale;
    }

    // =========================================
    // WHAT THE PLAYER CAN DO RIGHT NOW (used by ActionHighlight)
    // =========================================

    public static bool CanPlayNow(RuntimeCard card)
    {
        GameManager game = GameManager.Instance;
        if (
            !PlayerMayAct(game)
            || card == null
            || card.Owner != PlayerSide.Player
            || card.Zone != CardZone.Hand
        )
            return false;
        if (card.GetManaCost() > game.TurnManager.GetMana(PlayerSide.Player))
            return false;

        BattlefieldManager field = game.GetBattlefield(PlayerSide.Player);
        if (card.Data is MinionData)
            return field.HasAvailableMinionSlot;
        if (card.Data is ArtifactData)
        {
            foreach (RuntimeCard unit in field.Minions)
                if (
                    unit.Data is ApostleData
                    && unit.EquippedArtifacts.Count < unit.GetMaxEquipment()
                )
                    return true;
            return false;
        }
        return true;
    }

    public static bool CanAttackNow(RuntimeCard unit) =>
        PlayerMayAct(GameManager.Instance)
        && unit != null
        && unit.Owner == PlayerSide.Player
        && unit.Zone == CardZone.Field
        && unit.CanAttack;

    private static bool PlayerMayAct(GameManager game) =>
        game != null
        && !game.IsGameOver
        && game.TurnManager != null
        && game.TurnManager.IsMyTurn(PlayerSide.Player)
        && !BattleChoiceRequests.HasPending
        && (EffectTargetManager.Instance == null || !EffectTargetManager.Instance.IsSelectingTarget)
        && (UnityRemoteMatch.Instance == null || !UnityRemoteMatch.Instance.HasRemoteChoice);

    // =========================================
    // HELPERS
    // =========================================

    private Vector2 ToLocal(Vector3 worldPosition)
    {
        // Battle canvases are Screen Space Overlay, so world positions are screen positions.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root,
            worldPosition,
            null,
            out Vector2 local
        );
        return local;
    }

    private TextMeshProUGUI CreateText(
        string name,
        float size,
        Color color,
        Transform parent = null
    )
    {
        TextMeshProUGUI text = Element(
            name,
            parent != null ? parent : root,
            Vector2.zero,
            new Vector2(900, 160)
        )
            .gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform Element(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size
    )
    {
        RectTransform rect = new GameObject(
            name,
            typeof(RectTransform)
        ).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
