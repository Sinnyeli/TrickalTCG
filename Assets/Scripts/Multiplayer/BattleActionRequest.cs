using System;

public enum BattleActionKind { PlayCard, PlaySpellOnUnit, PlaySpellOnHero, EquipArtifact, AttackUnit, AttackHero, EndTurn, Surrender, ChooseUnit, ChooseHero, ChooseCard }

[Serializable]
public class BattleActionRequest
{
    public string requestID;
    public string matchID;
    public int turn;
    public BattleActionKind kind;
    public string cardID;
    public string targetID;
    public PlayerSide hero;
    public string choiceID;
}

[Serializable]
public class BattleActionResult
{
    public string requestID;
    public bool accepted;
    public string error;
}
