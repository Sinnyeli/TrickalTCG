using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection permits testing the existing Assembly-CSharp without moving the game's scripts into new assemblies.
public class BattleActionTests
{
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
    private object previousGame;
    private object game;
    private object turn;
    private object ownHand, enemyHand;
    private Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object instance, string field) => instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(instance, value);
    private object Side(string name) => Enum.Parse(T("PlayerSide"), name);
    private object Kind(string name) => Enum.Parse(T("BattleActionKind"), name);
    private object Add(string type)
    {
        var go = new GameObject("Test " + type); created.Add(go); return go.AddComponent(T(type));
    }
    private object Call(object instance, string method, params object[] args) => instance.GetType().GetMethod(method).Invoke(instance, args);
    private object Static(string type, string method, params object[] args) => T(type).GetMethod(method).Invoke(null, args);
    [SetUp]
    public void Setup()
    {
        previousGame = T("GameManager").GetProperty("Instance").GetValue(null);
        game = Add("GameManager"); turn = Add("TurnManager");
        T("GameManager").GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, game);
        Set(game, "turnManager", turn); Set(turn,"currentSide",Side("Player")); Set(turn,"turnNumber",1);
        ownHand = Add("HandManager"); enemyHand = Add("HandManager");
        Set(game,"handManager",ownHand); Set(game,"opponentHandManager",enemyHand);
        Set(game,"playerBattlefieldManager",Add("BattlefieldManager")); Set(game,"opponentBattlefieldManager",Add("BattlefieldManager"));
        Static("BattleActions","ResetMatch","test-match");
    }
    [TearDown]
    public void Teardown()
    {
        foreach (var obj in created) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        created.Clear();
        T("GameManager").GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,previousGame);
        Static("BattleActions","ResetMatch",new object[] { null });
    }
    private object Request(string kind)
    {
        var request = Activator.CreateInstance(T("BattleActionRequest"));
        Set(request,"requestID",Guid.NewGuid().ToString("N")); Set(request,"matchID","test-match");
        Set(request,"turn",1); Set(request,"kind",Kind(kind)); return request;
    }
    private object Execute(string side, object request) => Static("BattleActions","Execute",Side(side),request);
    [Test] public void RejectsWrongMatch()
    {
        var request = Request("EndTurn"); Set(request,"matchID","another-match");
        var result = Execute("Player",request);
        Assert.False((bool)Get(result,"accepted")); Assert.AreEqual("Wrong match.",Get(result,"error"));
    }
    [Test] public void RejectsOpponentEndingPlayersTurn()
    {
        var result = Execute("Opponent",Request("EndTurn"));
        Assert.False((bool)Get(result,"accepted")); Assert.AreEqual(Side("Player"),Get(turn,"currentSide"));
    }
    [Test] public void RejectsCardOwnedByOtherSeat()
    {
        var data = Card("MonsterData","M_TEST","Test unit");
        var card = Activator.CreateInstance(T("RuntimeCard"),data,false);
        Call(card,"SetOwner",Side("Opponent")); Call(card,"ChangeZone",Enum.Parse(T("CardZone"),"Hand"));
        ((IList)Get(enemyHand,"hand")).Add(card);
        var request = Request("PlayCard"); Set(request,"cardID",T("RuntimeCard").GetProperty("InstanceID").GetValue(card));
        Assert.False((bool)Get(Execute("Player",request),"accepted"));
        Assert.AreEqual(1,((IList)Get(enemyHand,"hand")).Count);
    }
    [Test] public void DuplicateRequestReturnsOriginalResultWithoutExecutingChangedPayload()
    {
        var request = Request("EndTurn"); Set(request,"matchID","invalid");
        var first = Execute("Player",request);
        Set(request,"matchID","test-match"); Set(request,"kind",Kind("Surrender"));
        var replay = Execute("Player",request);
        Assert.AreSame(first,replay); Assert.False((bool)T("GameManager").GetProperty("IsGameOver").GetValue(game));
    }
    private ScriptableObject Card(string type, string id, string name)
    {
        var data = ScriptableObject.CreateInstance(T(type)); created.Add(data);
        T("CardData").GetField("cardID",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,id);
        T("CardData").GetField("collectible",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,true);
        T("CardData").GetField("cardName").SetValue(data,name); return data;
    }
    private object Entry(string id,int count) => Activator.CreateInstance(T("DeckCardEntry"),id,count);
    private bool Validate(object saved,ScriptableObject catalog)
    {
        var args = new object[] { saved,catalog,null,null };
        bool valid = (bool)T("UnityRemoteMatch").GetMethod("ValidateDeck").Invoke(null,args);
        if (args[2] is UnityEngine.Object generated) created.Add(generated);
        return valid;
    }
    [Test] public void DeckValidationHonorsSpellLimitAndRejectsWrongSizeAndUnitOverLimit()
    {
        var commander=Card("ApostleData","A_TEST","Commander");
        var jubee=Card("MonsterData","M_JUBEE_TEST","Jubee");
        var spell=Card("SpellData","S_TEST","Spell");
        var unit=Card("MonsterData","M_TEST","Unit");
        var catalog=ScriptableObject.CreateInstance(T("CardDatabase"));created.Add(catalog);
        var cards=(IList)Get(catalog,"allCards");cards.Add(commander);cards.Add(jubee);cards.Add(spell);cards.Add(unit);
        var saved=Activator.CreateInstance(T("DeckSaveData"));Set(saved,"commanderID","A_TEST");
        var entries=(IList)Get(saved,"cards");entries.Add(Entry("S_TEST",3));entries.Add(Entry("M_JUBEE_TEST",27));
        Assert.True(Validate(saved,catalog));
        entries.Clear();entries.Add(Entry("M_TEST",3));entries.Add(Entry("M_JUBEE_TEST",27));Assert.False(Validate(saved,catalog));
        entries.Clear();entries.Add(Entry("M_JUBEE_TEST",29));Assert.False(Validate(saved,catalog));
        entries.Clear();entries.Add(Entry("A_TEST",1));entries.Add(Entry("M_JUBEE_TEST",29));Assert.True(Validate(saved,catalog));
    }
    [Test] public void RemotePresentationUsesAuthoritativeStatsAndIdentity()
    {
        var data=Card("MonsterData","M_TEST","Unit");
        var catalog=ScriptableObject.CreateInstance(T("CardDatabase"));created.Add(catalog);
        ((IList)Get(catalog,"allCards")).Add(data);
        var card=Activator.CreateInstance(T("RuntimeCard"),data,false);
        var state=Activator.CreateInstance(T("RemoteCardState"));
        Set(state,"id","host-runtime-id");Set(state,"attack",9);Set(state,"health",4);Set(state,"maxHealth",12);Set(state,"cost",2);
        Set(state,"zone",Enum.Parse(T("CardZone"),"Field"));Set(state,"ready",true);
        Call(card,"ApplyRemoteView",state,data,Side("Player"),catalog);
        Assert.AreEqual("host-runtime-id",T("RuntimeCard").GetProperty("InstanceID").GetValue(card));
        Assert.AreEqual(9,Call(card,"GetAttack"));Assert.AreEqual(12,Call(card,"GetMaxHealth"));Assert.AreEqual(2,Call(card,"GetManaCost"));
        Assert.AreEqual(4,T("RuntimeCard").GetProperty("CurrentHealth").GetValue(card));
    }
}
