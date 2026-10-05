using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
[Header("Turn")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text currentTurnText;

    [Header("Player")]
    [SerializeField] private TMP_Text playerDeckText;
    [SerializeField] private TMP_Text playerManaText;

    [Header("Opponent")]
    [SerializeField] private TMP_Text opponentDeckText;
    [SerializeField] private TMP_Text opponentManaText;

    [SerializeField] private Button endTurnButton;

    private void Update()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return;

        TurnManager turnManager =
            gameManager.TurnManager;

        if (turnManager == null)
            return;

        if (endTurnButton != null)
            endTurnButton.interactable = turnManager.CanPlayerEndTurn;

        // Turn
        turnText.text =
            $"Turn {turnManager.TurnNumber}";

        currentTurnText.text =
            $"{turnManager.CurrentSide} Turn\n{Mathf.CeilToInt(turnManager.RemainingTurnSeconds)}s";

        // Player
        playerDeckText.text =
            $"Deck: {gameManager.GetDeck(PlayerSide.Player).GetDeckCount()}";

        playerManaText.text =
            $"Mana: {turnManager.GetMana(PlayerSide.Player)} / " +
            $"{turnManager.GetMaxMana(PlayerSide.Player)}";

        // Opponent
        opponentDeckText.text =
            $"Deck: {gameManager.GetDeck(PlayerSide.Opponent).GetDeckCount()}";

        opponentManaText.text =
            $"Mana: {turnManager.GetMana(PlayerSide.Opponent)} / " +
            $"{turnManager.GetMaxMana(PlayerSide.Opponent)}";
    }
}
