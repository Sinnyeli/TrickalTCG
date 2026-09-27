public enum BattleOpponentType
{
    None,
    AI,
    RemotePlayer
}


public enum AIType
{
    None,
    Jubee
}


public static class BattleSession
{
    public static string PlayerDeckID
    {
        get;
        private set;
    }

    public static BattleOpponentType OpponentType
    {
        get;
        private set;
    }

    public static AIType SelectedAI
    {
        get;
        private set;
    }


    // =========================================================
    // CREATE AI MATCH
    // =========================================================

    public static void CreateAIMatch(
        string playerDeckID,
        AIType aiType)
    {
        PlayerDeckID =
            playerDeckID;

        OpponentType =
            BattleOpponentType.AI;

        SelectedAI =
            aiType;
    }


    // =========================================================
    // CREATE ONLINE MATCH
    // =========================================================

    public static void CreateOnlineMatch(
        string playerDeckID)
    {
        PlayerDeckID =
            playerDeckID;

        OpponentType =
            BattleOpponentType.RemotePlayer;

        SelectedAI =
            AIType.None;
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public static void Clear()
    {
        PlayerDeckID = null;

        OpponentType =
            BattleOpponentType.None;

        SelectedAI =
            AIType.None;
    }


    // =========================================================
    // VALID
    // =========================================================

    public static bool HasPlayerDeck()
    {
        return !string.IsNullOrEmpty(
            PlayerDeckID
        );
    }
}