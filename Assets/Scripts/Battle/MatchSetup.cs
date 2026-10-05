using UnityEngine;

public class MatchSetup : MonoBehaviour
{
    [Header("Decks")]
    [SerializeField]
    private DeckManager opponentDeckManager;


    [Header("AI Decks")]
    [SerializeField]
    private DeckData jubeeDeck;


    // =========================================================
    // SETUP OPPONENT
    // =========================================================

    public bool SetupOpponent()
    {
        switch (BattleSession.OpponentType)
        {
            case BattleOpponentType.AI:

                return SetupAI();


            case BattleOpponentType.RemotePlayer:

                Debug.Log(
                    "Remote player match selected."
                );

                if (UnityRemoteMatch.Instance == null || opponentDeckManager == null) return false;
                DeckData remote = UnityRemoteMatch.Instance.RemoteDeck;
                if (remote == null) return false;
                opponentDeckManager.SetDeckData(remote);
                return true;


            default:

                Debug.LogError(
                    "BattleSession has no valid " +
                    "opponent type."
                );

                return false;
        }
    }


    // =========================================================
    // SETUP AI
    // =========================================================

    private bool SetupAI()
    {
        switch (BattleSession.SelectedAI)
        {
            case AIType.Jubee:

                return SetupJubee();


            default:

                Debug.LogError(
                    $"Unknown AI type: " +
                    $"{BattleSession.SelectedAI}"
                );

                return false;
        }
    }


    // =========================================================
    // JUBEE
    // =========================================================

    private bool SetupJubee()
    {
        if (opponentDeckManager == null)
        {
            Debug.LogError(
                "MatchSetup has no " +
                "Opponent DeckManager."
            );

            return false;
        }


        if (jubeeDeck == null)
        {
            Debug.LogError(
                "MatchSetup has no Jubee DeckData."
            );

            return false;
        }


        opponentDeckManager.SetDeckData(
            jubeeDeck
        );


        Debug.Log(
            "Opponent configured: Jubee AI"
        );

        return true;
    }
}