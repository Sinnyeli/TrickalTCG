using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

// Unity Lobby supplies automatic pairing; Relay carries host-authoritative custom messages.
// This uses the individual UGS packages supported by the repository's Unity 2022.3 editor.
public class UnityRemoteMatch : MonoBehaviour
{
    public static UnityRemoteMatch Instance { get; private set; }
    public static bool IsGuest => Instance != null && Instance.inMatch && Instance.network != null && !Instance.network.IsServer;
    public DeckData RemoteDeck { get; private set; }
    public bool TargetPrompt => latest != null && latest.targetPrompt;
    public bool HasRemoteChoice => latest != null && !string.IsNullOrEmpty(latest.choiceID);
    private const string QueueVersion = "Trickal-alpha-actions-v1";
    private const string MessageName = "Trickal.Match.v1";
    private NetworkManager network;
    private UnityTransport transport;
    private Lobby lobby;
    private bool ownsLobby, searching, inMatch, battleReady, helloSent;
    private bool closing;
    private int generation;
    private string matchID;
    private ulong remoteClient;
    private DeckSaveData selected;
    private CardDatabase database;
    private float nextState, nextHeartbeat;
    private RemoteBattleState latest;
    private string shownChoice;
    private readonly Dictionary<string, RuntimeCard> mirrors = new Dictionary<string, RuntimeCard>();
    private readonly List<RuntimeCard> hiddenCards = new List<RuntimeCard>();
    private SpellData hiddenData;
    private MatchmakingNotice notice;

    [Serializable] private class Envelope
    {
        public string kind, payload;
        public string protocol = QueueVersion;
    }
    [Serializable] private class Hello { public DeckSaveData deck; public string catalog; }
    [Serializable] private class StartMatch { public string id; }

    public static UnityRemoteMatch Get()
    {
        if (Instance == null) new GameObject("Unity Remote Match").AddComponent<UnityRemoteMatch>();
        return Instance;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Battlefield" && inMatch) _ = LeaveMatch();
        if (searching && scene.name != "DeckSelector") CancelSearch();
    }
    public async void FindMatch(DeckSaveData deck, CardDatabase cards)
    {
        if (searching || inMatch || closing) return;
        if (!ValidateDeck(deck, cards, out var validated, out string invalid)) { MatchmakingNotice.Show(invalid, () => { }).ShowError(invalid); Debug.LogError(invalid); return; }
        Destroy(validated);
        selected = deck; database = cards; searching = true;
        int attempt = ++generation;
        notice = MatchmakingNotice.Show("Finding an opponent… / 상대를 찾는 중…", CancelSearch);
        try
        {
            string profile;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                profile = "tcg" + BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(LocalAccountSession.LoginID ?? "guest"))).Replace("-", "").Substring(0,16);
            await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(profile));
            if (AuthenticationService.Instance.Profile != profile)
            {
                if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
                AuthenticationService.Instance.SwitchProfile(profile);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            if (!Current(attempt)) return;
            CreateNetwork();
            try
            {
                lobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions { Filter = Filters() });
                ownsLobby = false;
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.NoOpenLobbies)
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(1);
                if (!Current(attempt)) return;
                string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                if (!Current(attempt)) return;
                transport.SetRelayServerData(new RelayServerData(allocation, "wss"));
                if (!network.StartHost()) throw new InvalidOperationException("Could not start Relay host.");
                RegisterMessages();
                lobby = await LobbyService.Instance.CreateLobbyAsync("Trickal queue", 2, new CreateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { "version", new DataObject(DataObject.VisibilityOptions.Public, QueueVersion + ":" + CatalogHash(), DataObject.IndexOptions.S1) },
                        { "relay", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    } });
                ownsLobby = true; nextHeartbeat = Time.unscaledTime + 10;
            }
            if (!Current(attempt)) { await CleanupNetwork(); return; }
            if (!ownsLobby) await ConnectGuest(attempt);
            float deadline = Time.realtimeSinceStartup + 10;
            while (Current(attempt) && !inMatch && Time.realtimeSinceStartup < deadline)
            {
                await WaitMilliseconds(500);
                if (!Current(attempt) || inMatch) break;
                // Concurrent arrivals can create two lobbies. The newer empty host joins the older one.
                if (ownsLobby && network.ConnectedClientsIds.Count <= 1)
                {
                    var response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Filters = Filters(), Count = 10 });
                    var older = response.Results.Where(l => l.Id != lobby.Id && (l.Created < lobby.Created ||
                        (l.Created == lobby.Created && string.CompareOrdinal(l.Id, lobby.Id) < 0)))
                        .OrderBy(l => l.Created).ThenBy(l => l.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (older != null && Current(attempt) && !inMatch)
                    {
                        var previous = lobby;
                        try
                        {
                            // Claim a slot before deleting the old lobby; unsuccessful claims leave us queued.
                            var joined = await LobbyService.Instance.JoinLobbyByIdAsync(older.Id);
                            if (inMatch || !Current(attempt)) { await LobbyService.Instance.RemovePlayerAsync(joined.Id, AuthenticationService.Instance.PlayerId); break; }
                            await LobbyService.Instance.DeleteLobbyAsync(previous.Id);
                            network.Shutdown(); await WaitMilliseconds(150);
                            lobby = joined; ownsLobby = false; await ConnectGuest(attempt);
                        }
                        catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyFull || e.Reason == LobbyExceptionReason.LobbyNotFound) { }
                    }
                }
            }
            if (!Current(attempt) || inMatch) return;
            // Only an unpaired host may fall back. Joining/connection errors are displayed as errors.
            if (!ownsLobby) throw new TimeoutException("The opponent connection timed out. Please retry.");
            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions { IsLocked = true });
            // Give an already claimed lobby slot time to finish its Relay handshake.
            float grace = Time.realtimeSinceStartup + 5;
            while (Current(attempt) && !inMatch && network.ConnectedClientsIds.Count > 1 && Time.realtimeSinceStartup < grace) await WaitMilliseconds(100);
            if (!Current(attempt) || inMatch) return;
            searching = false; ++generation;
            await CleanupNetwork();
            if (notice != null) Destroy(notice.gameObject);
            BattleSession.CreateAIMatch(selected.deckID, AIType.Jubee);
            BattleActions.ResetMatch(); SceneManager.LoadScene("Battlefield");
        }
        catch (Exception error)
        {
            if (!Current(attempt) || inMatch) return;
            searching = false; ++generation;
            Debug.LogException(error); await CleanupNetwork();
            if (notice != null) notice.ShowError("Connection failed. Retry Play. / 연결 실패. 다시 시도하세요.");
        }
        finally
        {
            if (!searching && !inMatch && network != null && network.IsListening) await CleanupNetwork();
        }
    }
    // Avoid thread-based timers: WebGL runs the game on a browser main thread.
    private static async Task WaitMilliseconds(int milliseconds)
    {
        float until = Time.realtimeSinceStartup + milliseconds / 1000f;
        while (Time.realtimeSinceStartup < until) await Task.Yield();
    }
    private bool Current(int attempt) => searching && attempt == generation;
    private List<QueryFilter> Filters() => new List<QueryFilter> {
        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
        new QueryFilter(QueryFilter.FieldOptions.S1, QueueVersion + ":" + CatalogHash(), QueryFilter.OpOptions.EQ)
    };
    private string CatalogHash()
    {
        string catalog = string.Join("\n", database.AllCards.Where(c => c != null).OrderBy(c => c.CardID, StringComparer.Ordinal)
            .Select(c => c.CardID + ":" + c.manaCost + ":" + c.description + (c is MinionData m ? ":" + m.attack + ":" + m.health : "")));
        using (var sha = System.Security.Cryptography.SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(catalog))).Replace("-", "");
    }
    private void CreateNetwork()
    {
        if (network != null) return;
        var go = new GameObject("Trickal Netcode", typeof(UnityTransport), typeof(NetworkManager));
        DontDestroyOnLoad(go); network = go.GetComponent<NetworkManager>(); transport = go.GetComponent<UnityTransport>();
        transport.UseWebSockets = true;
        network.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = true, ConnectionData = Array.Empty<byte>() };
        network.ConnectionApprovalCallback = (request, response) => {
            response.Approved = searching && !inMatch && (request.ClientNetworkId == NetworkManager.ServerClientId || network.ConnectedClientsIds.Count < 2);
            response.CreatePlayerObject = false; response.Pending = false;
        };
        network.OnClientConnectedCallback += Connected;
        network.OnClientDisconnectCallback += Disconnected;
    }
    private async Task ConnectGuest(int attempt)
    {
        if (!Current(attempt)) return;
        if (lobby.Data == null || !lobby.Data.TryGetValue("relay", out var code)) throw new InvalidOperationException("Session has no Relay allocation.");
        var allocation = await RelayService.Instance.JoinAllocationAsync(code.Value);
        if (!Current(attempt)) return;
        transport.SetRelayServerData(new RelayServerData(allocation, "wss"));
        helloSent = false;
        if (!network.StartClient()) throw new InvalidOperationException("Could not start Relay client.");
        RegisterMessages();
    }
    private void RegisterMessages() => network.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, Receive);
    private void Connected(ulong client)
    {
        if (network.IsServer || helloSent) return;
        helloSent = true;
        Send(NetworkManager.ServerClientId, "hello", JsonUtility.ToJson(new Hello { deck = selected, catalog = CatalogHash() }));
    }
    private void Receive(ulong sender, FastBufferReader reader)
    {
        try
        {
            reader.ReadValueSafe(out string json);
            if (json.Length > 60000) return;
            var message = JsonUtility.FromJson<Envelope>(json);
            if (message == null || message.protocol != QueueVersion) return;
            if (network.IsServer)
            {
                if (sender == NetworkManager.ServerClientId) return;
                if (message.kind == "hello" && searching && !inMatch)
                {
                    var hello = JsonUtility.FromJson<Hello>(message.payload);
                    if (hello == null || hello.catalog != CatalogHash() || !ValidateDeck(hello.deck, database, out var deck, out _))
                    { network.DisconnectClient(sender); return; }
                    RemoteDeck = deck; remoteClient = sender; matchID = Guid.NewGuid().ToString("N");
                    inMatch = true; searching = false; ++generation;
                    Send(sender, "start", JsonUtility.ToJson(new StartMatch { id = matchID }));
                    _ = LockLobby(); BeginBattle();
                }
                else if (message.kind == "action" && inMatch && battleReady && sender == remoteClient)
                {
                    var request = JsonUtility.FromJson<BattleActionRequest>(message.payload);
                    var result = BattleActions.Execute(PlayerSide.Opponent, request);
                    Send(sender, "result", JsonUtility.ToJson(result)); SendState();
                }
            }
            else if (sender == NetworkManager.ServerClientId)
            {
                if (message.kind == "start" && searching)
                {
                    matchID = JsonUtility.FromJson<StartMatch>(message.payload).id;
                    inMatch = true; searching = false; ++generation; BeginBattle();
                }
                else if (message.kind == "state" && inMatch)
                {
                    var state = JsonUtility.FromJson<RemoteBattleState>(message.payload);
                    if (state != null && state.matchID == matchID) { latest = state; if (battleReady) ApplyState(); }
                }
                else if (message.kind == "combat" && inMatch) AnimateRemoteAttack(JsonUtility.FromJson<BattleActionRequest>(message.payload));
                else if (message.kind == "result")
                {
                    var result = JsonUtility.FromJson<BattleActionResult>(message.payload);
                    if (result != null)
                    {
                        if (!result.accepted) CombatFailureUI.ShowText(result.error);
                        GameManager.Instance?.CombatManager.ClearSelection();
                    }
                }
            }
        }
        catch (Exception error) { Debug.LogException(error); }
    }
    private async Task LockLobby()
    {
        try { if (ownsLobby && lobby != null) await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions { IsLocked = true }); }
        catch (Exception error) { Debug.LogWarning(error.Message); }
    }
    private void BeginBattle()
    {
        BattleActions.ResetMatch(matchID); BattleSession.CreateOnlineMatch(selected.deckID);
        if (notice != null) Destroy(notice.gameObject);
        battleReady = false; latest = null; shownChoice = null; mirrors.Clear(); hiddenCards.Clear();
        SceneManager.LoadScene("Battlefield");
    }
    public void BattleReady(CardDatabase cards)
    {
        database = cards; battleReady = true;
        if (IsGuest) { if (latest != null) ApplyState(); } else SendState();
    }
    public bool SendAction(BattleActionRequest request)
    {
        if (!IsGuest || !battleReady || !network.IsConnectedClient) return false;
        Send(NetworkManager.ServerClientId, "action", JsonUtility.ToJson(request)); return true;
    }
    private void Send(ulong client, string kind, string payload)
    {
        string json = JsonUtility.ToJson(new Envelope { kind = kind, payload = payload });
        int size = System.Text.Encoding.UTF8.GetByteCount(json) * 2 + 16;
        if (size > 60000) throw new InvalidOperationException("Match message exceeds the protocol limit.");
        using (var writer = new FastBufferWriter(size, Allocator.Temp))
        {
            writer.WriteValueSafe(json);
            network.CustomMessagingManager.SendNamedMessage(MessageName, client, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
    }
    public void AuthorityActionResolved(BattleActionRequest request, BattleActionResult result)
    {
        if (!inMatch || !battleReady || network == null || !network.IsServer || !result.accepted) return;
        if (request.kind == BattleActionKind.AttackUnit || request.kind == BattleActionKind.AttackHero)
            try { Send(remoteClient, "combat", JsonUtility.ToJson(request)); } catch (Exception e) { Debug.LogWarning(e.Message); }
        SendState();
    }
    private void AnimateRemoteAttack(BattleActionRequest request)
    {
        if (!battleReady || !mirrors.TryGetValue(request.cardID ?? "", out var attacker)) return;
        Transform target = null;
        if (request.kind == BattleActionKind.AttackUnit && mirrors.TryGetValue(request.targetID ?? "", out var defender))
            target = CombatPresentation.FindView(defender)?.transform;
        else if (request.kind == BattleActionKind.AttackHero)
            target = GameManager.Instance.GetPlayerView(RemoteBattleState.Swap(request.hero))?.transform;
        if (target != null) CombatPresentation.Attack(attacker, target);
    }
    private void SendState()
    {
        if (!inMatch || !battleReady || network == null || !network.IsServer || GameManager.Instance == null || GameManager.Instance.GetPlayerView(PlayerSide.Player) == null) return;
        try { Send(remoteClient, "state", JsonUtility.ToJson(RemoteBattleState.Capture())); }
        catch (Exception e) { Debug.LogWarning(e.Message); }
    }
    private async void Update()
    {
        if (lobby != null && ownsLobby && Time.unscaledTime >= nextHeartbeat)
        {
            nextHeartbeat = Time.unscaledTime + 10;
            try { await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id); } catch (Exception e) { Debug.LogWarning(e.Message); }
        }
        if (inMatch && battleReady && network != null && network.IsServer && Time.unscaledTime >= nextState)
        { nextState = Time.unscaledTime + .25f; SendState(); }
    }
    private RuntimeCard Mirror(RemoteCardState state, PlayerSide side)
    {
        var data = database.GetCardByID(state.dataID);
        if (data == null) throw new InvalidOperationException("Client catalog is missing " + state.dataID);
        if (!mirrors.TryGetValue(state.id, out var card)) mirrors[state.id] = card = new RuntimeCard(data, state.commander);
        card.ApplyRemoteView(state, data, side, database); return card;
    }
    private void ApplyState()
    {
        var game = GameManager.Instance; if (game == null || latest == null || !battleReady) return;
        game.TurnManager.ApplyRemoteTurn(latest);
        game.GetPlayerView(PlayerSide.Player).ApplyRemoteHealth(latest.ownHealth);
        game.GetPlayerView(PlayerSide.Opponent).ApplyRemoteHealth(latest.enemyHealth);
        game.GetDeck(PlayerSide.Player).ApplyRemoteCount(latest.ownDeck); game.GetDeck(PlayerSide.Opponent).ApplyRemoteCount(latest.enemyDeck);
        game.GetBattlefield(PlayerSide.Player).ApplyRemoteField(latest.ownField.Select(c => Mirror(c, PlayerSide.Player)).ToList());
        game.GetBattlefield(PlayerSide.Opponent).ApplyRemoteField(latest.enemyField.Select(c => Mirror(c, PlayerSide.Opponent)).ToList());
        game.GetHandManager(PlayerSide.Player).ApplyRemoteHand(latest.hand.Select(c => Mirror(c, PlayerSide.Player)).ToList());
        if (hiddenData == null) { hiddenData = ScriptableObject.CreateInstance<SpellData>(); hiddenData.cardName = ""; }
        while (hiddenCards.Count < latest.enemyHand)
        { var card = new RuntimeCard(hiddenData); card.SetOwner(PlayerSide.Opponent); card.ChangeZone(CardZone.Hand); hiddenCards.Add(card); }
        if (hiddenCards.Count > latest.enemyHand) hiddenCards.RemoveRange(latest.enemyHand, hiddenCards.Count - latest.enemyHand);
        var enemyHand = new List<RuntimeCard>(hiddenCards);
        if (latest.enemyCommander != null) enemyHand.Add(Mirror(latest.enemyCommander, PlayerSide.Opponent));
        game.GetHandManager(PlayerSide.Opponent).ApplyRemoteHand(enemyHand);
        if (!string.IsNullOrEmpty(latest.choiceID) && latest.choiceID != shownChoice)
        {
            shownChoice = latest.choiceID;
            string id = shownChoice;
            SpellChoiceUI.Choose(PlayerSide.Player, latest.choices.Select(c => mirrors.TryGetValue(c.id, out var existing) ? existing : Mirror(c, RemoteBattleState.Swap(c.owner))).ToList(),
                card => BattleActions.Submit(PlayerSide.Player, BattleActionKind.ChooseCard, target: card, choice: id));
        }
        if (latest.over && !game.IsGameOver) game.PlayerDefeated(RemoteBattleState.Swap(latest.winner));
    }
    public bool CanChoose(RuntimeCard card) => latest != null && latest.targetPrompt && card != null && latest.targetUnits.Contains(card.InstanceID);
    public bool CanChooseHero(PlayerView hero) => latest != null && latest.targetPrompt && hero != null && latest.targetHeroes.Contains(hero.Side);
    private void Disconnected(ulong client)
    {
        if (!inMatch || !battleReady || GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        if (network.IsServer && client == remoteClient) GameManager.Instance.PlayerDefeated(PlayerSide.Opponent);
        else if (!network.IsServer) GameManager.Instance.PlayerDefeated(PlayerSide.Player);
    }
    public async void CancelSearch()
    {
        if (!searching) return;
        searching = false; ++generation; closing = true;
        try { await CleanupNetwork(); if (notice != null) Destroy(notice.gameObject); } finally { closing = false; }
    }
    private async Task LeaveMatch()
    {
        inMatch = false; battleReady = false; latest = null; closing = true; BattleChoiceRequests.Clear();
        await CleanupNetwork(); mirrors.Clear(); hiddenCards.Clear();
        if (RemoteDeck != null) Destroy(RemoteDeck); RemoteDeck = null; closing = false;
    }
    private async Task CleanupNetwork()
    {
        var old = lobby; bool host = ownsLobby; lobby = null; ownsLobby = false;
        if (network != null && network.IsListening) network.Shutdown();
        try
        {
            if (old != null && AuthenticationService.Instance.IsSignedIn)
            {
                if (host) await LobbyService.Instance.DeleteLobbyAsync(old.Id);
                else await LobbyService.Instance.RemovePlayerAsync(old.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception error) { Debug.LogWarning("Session cleanup: " + error.Message); }
    }
    public static bool ValidateDeck(DeckSaveData saved, CardDatabase catalog, out DeckData deck, out string error)
    {
        deck = null; error = "No saved deck was selected.";
        if (saved == null) return false;
        error = "Card database is unavailable.";
        if (catalog == null) return false;
        error = "Saved deck has no card list.";
        if (saved.cards == null) return false;
        var commander = catalog.GetCardByID(saved.commanderID) as ApostleData;
        error = $"Commander could not be found: {saved.commanderID}.";
        if (commander == null) return false;
        var cards = new List<CardData>(); var counts = new Dictionary<string,int>();
        foreach (var entry in saved.cards)
        {
            error = "Saved deck contains an invalid card count.";
            if (entry == null || entry.count <= 0 || entry.count > 30) return false;
            var card = catalog.GetCardByID(entry.cardID);
            error = $"Card could not be found: {entry.cardID}.";
            if (card == null) return false;
            error = $"{card.cardName} is not collectible.";
            if (!card.Collectible) return false;
            counts.TryGetValue(entry.cardID, out int previous); counts[entry.cardID] = previous + entry.count;
            // Match DeckEditorManager: the commander may also appear in the main deck.
            int limit = card.cardName == "Jubee" ? -1 : card is SpellData ? 3 :
                card is ApostleData || card is MonsterData || card is ArtifactData ? 2 : 0;
            error = $"{card.cardName} has {counts[entry.cardID]} copies; the limit is {limit}.";
            if (limit >= 0 && counts[entry.cardID] > limit) return false;
            for (int i = 0; i < entry.count; i++) cards.Add(card);
            error = $"Deck must contain 30 main-deck cards; found more than 30.";
            if (cards.Count > 30) return false;
        }
        error = $"Deck must contain 30 main-deck cards; found {cards.Count}.";
        if (cards.Count != 30) return false;
        deck = ScriptableObject.CreateInstance<DeckData>(); deck.commander = commander; deck.cards = cards;
        error = null; return true;
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this) Instance = null;
    }
}
