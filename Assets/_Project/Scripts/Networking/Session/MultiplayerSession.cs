using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Relay;

namespace LastProtocol
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class MultiplayerSession : MonoBehaviour
    {
        public static MultiplayerSession Instance { get; private set; }
        public MultiplayerConfig config;
        public NetworkRoomState roomPrefab;
        public NetworkPlayerController playerPrefab;
        public SessionState State { get; private set; } = SessionState.Offline;
        public string JoinCode { get; private set; } = string.Empty;
        public string LastError { get; private set; } = string.Empty;
        public NetworkRoomState Room { get; private set; }
        private int preferredLevel;
        public int SelectedLevelId => Room != null ? Room.SelectedLevel.Value : preferredLevel;
        public bool TrySelectLevel(int id)
        {
            if (!LevelCatalog.IsValid(id) || IsBusy) return false;
            if (Room != null)
            {
                bool selected = IsHost && Room.SelectLevelServer(id);
                if (selected) preferredLevel = id;
                return selected;
            }
            if (State != SessionState.Offline && State != SessionState.Error) return false;
            preferredLevel = id; NotifyChanged(); return true;
        }
        public bool IsBusy => operationRunning || cleaningUp;
        public bool IsHost => manager != null && manager.IsHost;
        public bool CanMove => State == SessionState.InGame && Room != null && Room.Phase.Value == MatchPhase.Playing && !Room.TeamWiped.Value;
        public bool CanStartGame => IsHost && !IsBusy && Room != null
            && reservedClients.Count == manager.ConnectedClients.Count
            && reservedClients.Count == Room.Members.Count
            && Room.CanStart(config.minimumPlayersToStart);
        public event Action Changed;
        public event Action<string> ErrorRaised;

        private NetworkManager manager;
        private UnityTransport transport;
        private readonly RelayConnectionService relay = new RelayConnectionService();
        private readonly HashSet<ulong> reservedClients = new HashSet<ulong>();
        private CancellationTokenSource operation;
        private TaskCompletionSource<bool> connected;
        private bool operationRunning, cleaningUp, acceptingPlayers;
        private int generation;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            manager.OnTransportFailure += OnTransportFailure;
        }

        public Task CreateRoomAsync() => ConnectAsync(true, string.Empty);
        public Task JoinRoomAsync(string code) => ConnectAsync(false, ConnectionPolicy.NormalizeJoinCode(code));

        private async Task ConnectAsync(bool host, string code, bool localTest = false)
        {
            if (IsBusy || manager.IsListening || (State != SessionState.Offline && State != SessionState.Error)) return;
            if (config == null || !config.IsValid || roomPrefab == null || playerPrefab == null)
            { Fail("Missing/invalid Multiplayer Config or network prefabs. Run LastProtocol Setup."); return; }
            if (!host && !localTest && !ConnectionPolicy.IsValidJoinCode(code))
            { Fail("Invalid Join Code. Enter six letters/numbers."); return; }

            operationRunning = true;
            int currentGeneration = ++generation;
            operation = new CancellationTokenSource();
            CancellationToken token = operation.Token;
            LastError = string.Empty;
            JoinCode = string.Empty;
            reservedClients.Clear();
            try
            {
                manager.NetworkConfig.ConnectionData = ConnectionPolicy.Payload;
                manager.NetworkConfig.ConnectionApproval = true;
                manager.NetworkConfig.EnableSceneManagement = true;
                manager.NetworkConfig.TickRate = MultiplayerConfig.NetworkTickRate;
                acceptingPlayers = host;
                if (localTest)
                {
                    transport.SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
                    JoinCode = "LOCAL";
                }
                else
                {
                    SetState(SessionState.Initializing);
                    await WithTimeout(relay.InitializeAsync(config.servicesEnvironment), config.operationTimeoutSeconds, token);
                    token.ThrowIfCancellationRequested();
                    SetState(host ? SessionState.Creating : SessionState.Joining);
                    if (host)
                    {
                        RelayHostAllocation allocation = await WithTimeout(relay.AllocateHostAsync(), config.operationTimeoutSeconds, token);
                        token.ThrowIfCancellationRequested();
                        RelayConnectionService.Configure(transport, allocation.Allocation);
                        JoinCode = allocation.JoinCode;
                    }
                    else
                    {
                        var allocation = await WithTimeout(relay.AllocateClientAsync(code), config.operationTimeoutSeconds, token);
                        token.ThrowIfCancellationRequested();
                        RelayConnectionService.Configure(transport, allocation);
                        JoinCode = code;
                    }
                }
                connected = new TaskCompletionSource<bool>();
                SetState(SessionState.Connecting);
                if (!(host ? manager.StartHost() : manager.StartClient()))
                    throw new InvalidOperationException("NGO could not start the network transport.");
                // StartClient only initiates a connection. The callback must confirm it.
                await WithTimeout(connected.Task, config.connectionTimeoutSeconds, token);
                token.ThrowIfCancellationRequested();
                if (host)
                {
                    manager.SceneManager.OnSceneEvent += OnSceneEvent;
                    var room = Instantiate(roomPrefab);
                    DontDestroyOnLoad(room.gameObject);
                    room.NetworkObject.Spawn(false);
                }
                SetState(SessionState.Lobby);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                if (currentGeneration == generation)
                {
                    string message = FriendlyError(exception);
                    await CleanupNetworkAsync();
                    Fail(message);
                }
            }
            finally
            {
                if (currentGeneration == generation)
                {
                    connected = null;
                    operation?.Dispose();
                    operation = null;
                    operationRunning = false;
                    NotifyChanged();
                }
            }
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            string reason = ConnectionPolicy.RejectionReason(acceptingPlayers, reservedClients.Count, request.Payload);
            response.Approved = reason.Length == 0;
            response.CreatePlayerObject = false; // Spawn only after synchronized Gameplay scene load.
            response.Pending = false;
            response.Reason = reason;
            if (response.Approved) { reservedClients.Add(request.ClientNetworkId); NotifyChanged(); }
        }

        private void OnConnected(ulong clientId)
        {
            if (manager.IsServer) reservedClients.Add(clientId);
            if (clientId == manager.LocalClientId && manager.IsConnectedClient) connected?.TrySetResult(true);
            NotifyChanged();
        }

        private void OnDisconnected(ulong clientId)
        {
            reservedClients.Remove(clientId);
            if (cleaningUp) return;
            if (!manager.IsServer && clientId == manager.LocalClientId)
            {
                string reason = manager.DisconnectReason;
                if (string.IsNullOrWhiteSpace(reason)) reason = "Host disconnected or the network connection was lost.";
                if (connected != null && !connected.Task.IsCompleted) connected.TrySetException(new InvalidOperationException(reason));
                else _ = EndDisconnectedAsync(reason);
            }
            NotifyChanged();
        }

        private void OnTransportFailure()
        {
            const string reason = "Network transport failed. Check Internet/Relay availability.";
            if (cleaningUp) return;
            if (connected != null && !connected.Task.IsCompleted) connected.TrySetException(new InvalidOperationException(reason));
            else _ = EndDisconnectedAsync(reason);
        }

        public async Task LeaveRoomAsync()
        {
            if (cleaningUp) return;
            ++generation;
            operation?.Cancel();
            operation?.Dispose();
            operation = null;
            operationRunning = false;
            SetState(SessionState.Leaving);
            if (IsHost && Room != null) Room.Phase.Value = MatchPhase.Closing;
            await CleanupNetworkAsync();
            await ReturnToMenuAsync();
            LastError = string.Empty;
            SetState(SessionState.Offline);
        }

        private async Task EndDisconnectedAsync(string reason)
        {
            if (cleaningUp) return;
            ++generation;
            operation?.Cancel();
            operation?.Dispose();
            operation = null;
            operationRunning = false;
            await CleanupNetworkAsync();
            await ReturnToMenuAsync();
            Fail(reason);
        }

        private async Task CleanupNetworkAsync()
        {
            cleaningUp = true;
            acceptingPlayers = false;
            NotifyChanged();
            if (manager.SceneManager != null) manager.SceneManager.OnSceneEvent -= OnSceneEvent;
            manager.Shutdown();
            // NGO shutdown is asynchronous. Never start another session while it is pending.
            float deadline = Time.realtimeSinceStartup + 5;
            while ((manager.IsListening || manager.ShutdownInProgress) && Time.realtimeSinceStartup < deadline)
                await Task.Yield();
            if (Room != null) Destroy(Room.gameObject);
            Room = null;
            reservedClients.Clear();
            JoinCode = string.Empty;
            connected = null;
            cleaningUp = manager.IsListening || manager.ShutdownInProgress;
            if (cleaningUp) StartCoroutine(WaitForShutdown());
            NotifyChanged();
        }

        private IEnumerator WaitForShutdown()
        {
            while (manager != null && (manager.IsListening || manager.ShutdownInProgress)) yield return null;
            cleaningUp = false;
            NotifyChanged();
        }

        private static async Task ReturnToMenuAsync()
        {
            if (SceneManager.GetActiveScene().name == MultiplayerConfig.MenuScene) return;
            AsyncOperation load = SceneManager.LoadSceneAsync(MultiplayerConfig.MenuScene);
            if (load != null) while (!load.isDone) await Task.Yield();
        }

        public void RegisterRoom(NetworkRoomState room)
        {
            Room = room;
            if (IsHost && LevelCatalog.IsValid(preferredLevel)) Room.SelectedLevel.Value = preferredLevel;
            NotifyRoomChanged();
        }
        public void UnregisterRoom(NetworkRoomState room)
        {
            if (Room == room) Room = null;
            NotifyChanged();
        }
        public void NotifyRoomChanged()
        {
            if (Room != null && !IsBusy)
            {
                if (Room.Phase.Value == MatchPhase.Loading) State = SessionState.LoadingGame;
                else if (Room.Phase.Value == MatchPhase.Playing) State = SessionState.InGame;
            }
            NotifyChanged();
        }
        public void NotifyChanged() => Changed?.Invoke();
        private void SetState(SessionState value) { State = value; NotifyChanged(); }
        private void Fail(string message) { LastError = message; SetState(SessionState.Error); ErrorRaised?.Invoke(message); }

        public void StartGame()
        {
            // Do not begin loading while an approved client is still synchronizing its lobby.
            if (!CanStartGame) return;
            var level = LevelCatalog.Get(Room.SelectedLevel.Value);
            if (level == null || !Application.CanStreamedLevelBeLoaded(level.SceneName))
            { LastError = "Selected level is missing from the build. Run Menu Setup and rebuild all players."; ErrorRaised?.Invoke(LastError); NotifyChanged(); return; }
            acceptingPlayers = false;
            Room.Phase.Value = MatchPhase.Loading;
            SetState(SessionState.LoadingGame);
            SceneEventProgressStatus status = manager.SceneManager.LoadScene(level.SceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                acceptingPlayers = true;
                Room.Phase.Value = MatchPhase.Lobby;
                SetState(SessionState.Lobby);
                LastError = "Scene load could not start: " + status;
                ErrorRaised?.Invoke(LastError);
                NotifyChanged();
            }
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (!manager.IsServer || Room == null || sceneEvent.SceneName != LevelCatalog.Get(Room.SelectedLevel.Value)?.SceneName
                || sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;
            foreach (ulong id in sceneEvent.ClientsThatTimedOut)
                if (id != manager.LocalClientId) manager.DisconnectClient(id, "Gameplay scene loading timed out");
            var points = FindAnyObjectByType<PlayerSpawnPoints>();
            if (points == null || points.Count != MultiplayerConfig.MaxPlayers)
            { _ = EndDisconnectedAsync("Gameplay scene is missing its four spawn points."); return; }
            foreach (ulong id in sceneEvent.ClientsThatCompleted)
            {
                if (!manager.ConnectedClients.TryGetValue(id, out NetworkClient client) || client.PlayerObject != null) continue;
                int slot = Room.GetSlot(id);
                Transform point = points.GetPoint(slot);
                var player = Instantiate(playerPrefab, point.position, point.rotation);
                player.NetworkObject.SpawnAsPlayerObject(id, true);
            }
            TrainingTarget.SpawnArenaServer(playerPrefab.GetComponent<NetworkHitscanWeapon>()?.trainingTargetPrefab);
            RobotEnemy.SpawnServer(playerPrefab.GetComponent<NetworkHitscanWeapon>()?.enemyPrefab);
            Room.Phase.Value = MatchPhase.Playing;
            SetState(SessionState.InGame);
        }

        private static async Task WithTimeout(Task task, float seconds, CancellationToken token)
        {
            using (var timer = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task delay = Task.Delay(TimeSpan.FromSeconds(seconds), timer.Token);
                if (await Task.WhenAny(task, delay) != task)
                {
                    // Service requests cannot be cancelled; observe late errors and discard results.
                    _ = task.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    token.ThrowIfCancellationRequested();
                    throw new TimeoutException("Connection timed out. Check Internet and try again.");
                }
                timer.Cancel();
                await task;
            }
        }
        private static async Task<T> WithTimeout<T>(Task<T> task, float seconds, CancellationToken token)
        { await WithTimeout((Task)task, seconds, token); return await task; }

        private static string FriendlyError(Exception exception)
        {
            if (exception is RelayServiceException relayError)
            {
                // Relay exceptions expose a stable reason enum; avoid logging allocation credentials.
                string reason = relayError.Reason.ToString();
                if (relayError.Reason == RelayExceptionReason.Conflict
                    || relayError.Message.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0) return "Room Full";
                if (reason.IndexOf("NotFound", StringComparison.OrdinalIgnoreCase) >= 0
                    || reason.IndexOf("JoinCode", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Invalid/expired Join Code or Host is no longer available.";
                return "Relay unavailable (" + reason + "). Check Internet and Unity Cloud services.";
            }
            if (exception is RequestFailedException request) return "Unity Services request failed (code " + request.ErrorCode + "). Check Authentication/Relay setup.";
            return exception.Message;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Explicit development-only loopback harness; production UI always uses Relay.
        public Task StartLocalTestAsync(bool host) => ConnectAsync(host, string.Empty, true);
#endif

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            ++generation;
            operation?.Cancel();
            operation?.Dispose();
            if (manager != null)
            {
                manager.ConnectionApprovalCallback = null;
                manager.OnClientConnectedCallback -= OnConnected;
                manager.OnClientDisconnectCallback -= OnDisconnected;
                manager.OnTransportFailure -= OnTransportFailure;
                if (manager.SceneManager != null) manager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }
    }
}
