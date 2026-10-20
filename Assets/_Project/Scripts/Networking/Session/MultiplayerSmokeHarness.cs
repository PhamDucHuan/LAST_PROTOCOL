using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;

namespace LastProtocol
{
    // Opt-in CLI test harness. No CLI flag = completely inactive; never contacts UGS.
    public sealed class MultiplayerSmokeHarness : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Serializable] private sealed class Result
        {
            public string role, state, error;
            public bool confirmedConnection, sawRoomFull, enteredGame, localOwnedPlayer, serverAuthority;
            public bool cameraBound, sawHostDisconnect;
            public bool leaveCompleted, reconnected, recreatedRoom;
            public int currentMembers, managerCountAfterLeave;
            public int departuresObserved;
            public int maximumMembers, maximumPlayers;
            public int membersAfterLeave = -1;
            public int movedPlayers;
            public int rotatedPlayers;
            public int jumpedPlayers, sprintedPlayers;
            public bool excessiveJumpHeight;
            public int initializedHealthPlayers, damagedHealthPlayers, downedHealthPlayers, resetHealthPlayers, recoveredMovingPlayers;
            public bool clientHealthWritesRejected, movedWhileDowned;
            public int maximumTargets, shootingPlayers, damagingPlayers;
            public bool targetDamageObserved, targetDownObserved, targetResetObserved;
            public bool downedFireRejected, invalidAimRejected, friendlyFireBlocked, shotSpamLimited;
            public EnemySmokeReport enemy;
            public ReviveSmokeReport revive;
            public MissionSmokeReport mission;
            public int selectedLevel;
            public string activeScene;
            public bool levelSelectionSynced, readyResetObserved, clientLevelChangeRejected, loadedSelectedScene;
            public float elapsed;
            public bool networkStressConfigured;
            public ulong maximumRttMilliseconds;
        }
        private MultiplayerSession session;
        private Result result;
        private string output;
        private float startTime, duration, startDelay, nextCapture;
        private int autoStart;
        private bool active, readySent, starting, wroteFinal;
        private readonly Dictionary<ulong, Vector3> initialPositions = new Dictionary<ulong, Vector3>();
        private readonly HashSet<ulong> moved = new HashSet<ulong>();
        private readonly Dictionary<ulong, Quaternion> initialRotations = new Dictionary<ulong, Quaternion>();
        private readonly HashSet<ulong> rotated = new HashSet<ulong>();
        private readonly HashSet<ulong> jumped = new HashSet<ulong>();
        private readonly HashSet<ulong> sprinted = new HashSet<ulong>();
        private readonly Dictionary<ulong, Vector3> speedPositions = new Dictionary<ulong, Vector3>();
        private readonly Dictionary<ulong, float> speedTimes = new Dictionary<ulong, float>();
        private bool testJumpSent, testAirJumpSent;
        private bool healthTest;
        private bool combatTest;
        private EnemySmokeScenario enemyScenario;
        private ReviveSmokeScenario reviveScenario;
        private MissionSmokeScenario missionScenario;
        private bool levelTest, sawOtherLevel;
        private int requestedLevel, observedLevel = -1, levelSwitchStage;
        private int combatRequestStage;
        private bool combatPositionsSet;
        private readonly HashSet<ulong> damagedTargets = new HashSet<ulong>(), downedTargets = new HashSet<ulong>();
        private int healthStage;
        private readonly HashSet<ulong> healthy = new HashSet<ulong>(), damaged = new HashSet<ulong>(),
            downed = new HashSet<ulong>(), reset = new HashSet<ulong>(), recovered = new HashSet<ulong>();
        private readonly Dictionary<ulong, Vector3> downedPositions = new Dictionary<ulong, Vector3>();
        private float gameTime;
        private float leaveAfter, rejoinAfter, recreateAfter;
        private bool left, returning;
        private int previousMembers;
        private async void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            string role = Argument(args, "--lp-smoke");
            if (role != "host" && role != "client") { enabled = false; return; }
            output = Argument(args, "--lp-result");
            if (string.IsNullOrEmpty(output)) { Debug.LogError("Smoke harness requires --lp-result."); enabled = false; return; }
            duration = Number(args, "--lp-duration", 45);
            startDelay = Number(args, "--lp-start-delay", 20);
            autoStart = (int)Number(args, "--lp-auto-start", MultiplayerConfig.MaxPlayers);
            healthTest = Array.IndexOf(args, "--lp-health-test") >= 0;
            combatTest = Array.IndexOf(args, "--lp-combat-test") >= 0;
            if (Array.IndexOf(args, "--lp-enemy-test") >= 0) { enemyScenario = new EnemySmokeScenario(); combatTest = true; }
            if (Array.IndexOf(args, "--lp-revive-test") >= 0) { reviveScenario = new ReviveSmokeScenario(); combatTest = true; }
            if (Array.IndexOf(args, "--lp-mission-test") >= 0) missionScenario = new MissionSmokeScenario();
            healthTest |= combatTest;
            levelTest = Array.IndexOf(args, "--lp-level-test") >= 0;
            requestedLevel = (int)Number(args, "--lp-level", 0);
            leaveAfter = Number(args, "--lp-leave-after", 0);
            rejoinAfter = Number(args, "--lp-rejoin-after", 0);
            recreateAfter = Number(args, "--lp-recreate-after", 0);
            startTime = Time.realtimeSinceStartup;
            session = MultiplayerSession.Instance;
            result = new Result { role = role };
            if (enemyScenario != null) result.enemy = enemyScenario.Report;
            if (reviveScenario != null) result.revive = reviveScenario.Report;
            if (missionScenario != null) result.mission = missionScenario.Report;
            active = true;
            if (role == "host") session.TrySelectLevel(requestedLevel);
            try
            {
                await session.StartLocalTestAsync(role == "host");
                if (Array.IndexOf(args, "--lp-network-stress") >= 0 && NetworkManager.Singleton.IsListening)
                {
                    var transport = session.GetComponent<UnityTransport>();
                    transport.GetNetworkDriver().ModifyNetworkSimulatorParameters(new NetworkSimulatorParameter
                    { SendDelayMS = 60, SendJitterMS = 20, SendPacketLossPercent = 3 });
                    result.networkStressConfigured = true;
                }
            }
            catch (Exception exception) { result.error = exception.Message; }
        }
        private static string Argument(string[] args, string flag)
        { int i = Array.IndexOf(args, flag); return i >= 0 && i + 1 < args.Length ? args[i + 1] : string.Empty; }
        private static float Number(string[] args, string flag, float fallback)
        { return float.TryParse(Argument(args, flag), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : fallback; }
        private void Update()
        {
            if (!active || wroteFinal) return;
            result.elapsed = Time.realtimeSinceStartup - startTime;
            result.state = session.State.ToString();
            result.error = session.LastError;
            if (result.networkStressConfigured && NetworkManager.Singleton.IsListening)
            {
                var transport = session.GetComponent<UnityTransport>();
                foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
                    if (id != NetworkManager.Singleton.LocalClientId)
                        result.maximumRttMilliseconds = Math.Max(result.maximumRttMilliseconds, transport.GetCurrentRtt(id));
                if (!NetworkManager.Singleton.IsServer)
                    result.maximumRttMilliseconds = Math.Max(result.maximumRttMilliseconds, transport.GetCurrentRtt(NetworkManager.ServerClientId));
            }
            result.confirmedConnection |= NetworkManager.Singleton.IsConnectedClient;
            result.sawRoomFull |= result.error != null && result.error.Contains("Room Full");
            result.sawHostDisconnect |= result.role == "client" && result.confirmedConnection && session.State == SessionState.Error;
            if (session.Room != null)
            {
                if (levelTest) CheckLevelSelection();
                result.currentMembers = session.Room.Members.Count;
                if (result.currentMembers < previousMembers) result.departuresObserved++;
                previousMembers = result.currentMembers;
                result.maximumMembers = Mathf.Max(result.maximumMembers, session.Room.Members.Count);
                if (result.maximumMembers == MultiplayerConfig.MaxPlayers && session.Room.Members.Count < MultiplayerConfig.MaxPlayers)
                    result.membersAfterLeave = session.Room.Members.Count;
                if (!readySent && session.State == SessionState.Lobby && session.Room.GetSlot(NetworkManager.Singleton.LocalClientId) >= 0)
                { readySent = true; session.Room.SetReadyRpc(true); }
                if (session.IsHost && !starting && autoStart > 0 && result.elapsed >= startDelay
                    && session.Room.Members.Count == autoStart && session.Room.CanStart(session.config.minimumPlayersToStart))
                { starting = true; session.StartGame(); }
            }
            else result.currentMembers = 0;
            if (!left && leaveAfter > 0 && result.elapsed >= leaveAfter && session.State == SessionState.Lobby)
            { left = true; _ = LeaveForTestAsync(); }
            if (result.leaveCompleted && !returning && ((rejoinAfter > 0 && result.elapsed >= rejoinAfter)
                || (recreateAfter > 0 && result.elapsed >= recreateAfter)))
            { returning = true; _ = ReconnectForTestAsync(); }
            if (session.State == SessionState.InGame)
            {
                result.enteredGame = true;
                result.activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                result.loadedSelectedScene |= result.activeScene == LevelCatalog.Get(requestedLevel)?.SceneName;
                gameTime += Time.unscaledDeltaTime;
                var players = FindObjectsByType<NetworkPlayerController>();
                result.maximumPlayers = Mathf.Max(result.maximumPlayers, players.Length);
                foreach (NetworkPlayerController player in players)
                {
                    if (!initialPositions.ContainsKey(player.OwnerClientId)) initialPositions[player.OwnerClientId] = player.transform.position;
                    if (!initialRotations.ContainsKey(player.OwnerClientId)) initialRotations[player.OwnerClientId] = player.transform.rotation;
                    if (Vector3.Distance(initialPositions[player.OwnerClientId], player.transform.position) > 1) moved.Add(player.OwnerClientId);
                    if (Quaternion.Angle(initialRotations[player.OwnerClientId], player.transform.rotation) > 10) rotated.Add(player.OwnerClientId);
                    float rise = player.transform.position.y - initialPositions[player.OwnerClientId].y;
                    if (rise > 0.5f) jumped.Add(player.OwnerClientId);
                    if (rise > session.config.jumpHeight + 0.4f) result.excessiveJumpHeight = true;
                    if (!speedTimes.ContainsKey(player.OwnerClientId))
                    { speedTimes[player.OwnerClientId] = gameTime; speedPositions[player.OwnerClientId] = player.transform.position; }
                    float interval = gameTime - speedTimes[player.OwnerClientId];
                    if (interval >= 0.2f)
                    {
                        Vector3 delta = player.transform.position - speedPositions[player.OwnerClientId];
                        if (new Vector2(delta.x, delta.z).magnitude / interval > session.config.movementSpeed * 1.2f)
                            sprinted.Add(player.OwnerClientId);
                        speedTimes[player.OwnerClientId] = gameTime;
                        speedPositions[player.OwnerClientId] = player.transform.position;
                    }
                    if (player.IsOwner)
                    {
                        result.localOwnedPlayer = true;
                        int slot = session.Room.GetSlot(player.OwnerClientId);
                        player.TestInput = gameTime < 3.5f
                            ? new Vector2(slot % 2 == 0 ? -0.8f : 0.8f, slot / 2 == 0 ? -0.4f : 0.4f) : Vector2.zero;
                        player.TestSprint = gameTime >= 0.75f && gameTime < 1.75f;
                        if (healthTest && ((gameTime >= 8.3f && gameTime < 9.5f) || (gameTime >= 12 && gameTime < 13)))
                            player.TestInput = Vector2.right;
                        if (!testJumpSent && gameTime >= 1.25f)
                        { testJumpSent = true; player.RequestTestJump(); }
                        // Deliberately request another jump in mid-air; server must ignore it.
                        if (!testAirJumpSent && gameTime >= 1.45f)
                        { testAirJumpSent = true; player.RequestTestJump(); }
                        result.serverAuthority = player.GetComponent<CharacterController>().enabled == session.IsHost;
                        result.cameraBound = FindAnyObjectByType<LocalPlayerCamera>()?.IsBoundTo(player.cameraTarget) == true;
                    }
                }
                result.movedPlayers = moved.Count;
                result.rotatedPlayers = rotated.Count;
                result.jumpedPlayers = jumped.Count;
                result.sprintedPlayers = sprinted.Count;
                if (healthTest && ((enemyScenario == null && reviveScenario == null) || gameTime < 23)) CheckHealth(players);
                if (combatTest) CheckCombat(players);
                enemyScenario?.Tick(session, players, gameTime);
                reviveScenario?.Tick(session, players, gameTime);
                missionScenario?.Tick(session, players, gameTime);
            }
            if (result.elapsed >= nextCapture) { nextCapture = result.elapsed + 1; WriteResult(); }
            if (result.elapsed >= duration) { wroteFinal = true; WriteResult(); Application.Quit(); }
        }
        private void CheckLevelSelection()
        {
            var room = session.Room;
            if (session.IsHost && session.State == SessionState.Lobby && room.Members.Count == MultiplayerConfig.MaxPlayers)
            {
                bool allReady = true;
                foreach (var member in room.Members) allReady &= member.Ready;
                if (allReady && ((levelSwitchStage == 0 && result.elapsed >= 15) || (levelSwitchStage == 1 && result.elapsed >= 19)))
                {
                    bool selected = session.TrySelectLevel(levelSwitchStage == 0 ? (requestedLevel + 1) % LevelCatalog.Count : requestedLevel);
                    bool reset = selected;
                    foreach (var member in room.Members) reset &= !member.Ready;
                    result.readyResetObserved |= reset;
                    levelSwitchStage++;
                }
            }
            int current = room.SelectedLevel.Value;
            if (observedLevel != current)
            {
                if (observedLevel >= 0) result.readyResetObserved |= !room.IsReady(NetworkManager.Singleton.LocalClientId);
                observedLevel = current; readySent = false;
            }
            sawOtherLevel |= current != requestedLevel;
            result.selectedLevel = current;
            result.levelSelectionSynced |= sawOtherLevel && current == requestedLevel;
            if (!session.IsHost) result.clientLevelChangeRejected |= !session.TrySelectLevel(1 - current);
        }
        private void CheckHealth(NetworkPlayerController[] players)
        {
            foreach (var player in players)
            {
                var health = player.GetComponent<PlayerHealth>();
                if (health == null) continue;
                var state = health.Snapshot;
                ulong id = player.OwnerClientId;
                if (state.IsInitialized && state.Current == session.config.playerMaxHealth) healthy.Add(id);
                if (state.Current == session.config.playerMaxHealth - 25) damaged.Add(id);
                if (state.IsDowned)
                {
                    downed.Add(id);
                    if (!downedPositions.ContainsKey(id)) downedPositions[id] = player.transform.position;
                    Vector3 delta = player.transform.position - downedPositions[id];
                    if (gameTime >= 8.3f && gameTime < 9.5f && new Vector2(delta.x, delta.z).magnitude > 0.15f) result.movedWhileDowned = true;
                }
                if (downed.Contains(id) && state.Current == session.config.playerMaxHealth)
                {
                    reset.Add(id);
                    if (gameTime >= 10.3f && gameTime < 11.5f) downedPositions[id] = player.transform.position;
                    Vector3 delta = player.transform.position - downedPositions[id];
                    if (gameTime >= 12 && new Vector2(delta.x, delta.z).magnitude > 0.5f) recovered.Add(id);
                }
                if (player.IsOwner && !session.IsHost && !result.clientHealthWritesRejected && state.IsInitialized)
                {
                    result.clientHealthWritesRejected = !health.TryApplyDamageServer(25)
                        && !health.TryHealServer(25) && !health.DebugResetHealthServer() && state.Equals(health.Snapshot);
                }
            }
            if (session.IsHost && players.Length == MultiplayerConfig.MaxPlayers)
            {
                if (healthStage == 0 && gameTime >= 6)
                { foreach (var player in players) player.GetComponent<PlayerHealth>().TryApplyDamageServer(25); healthStage = 1; }
                else if (healthStage == 1 && gameTime >= 8)
                { foreach (var player in players) player.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue); healthStage = 2; }
                else if (healthStage == 2 && gameTime >= 10)
                { session.Room.RestartRoundServer(); healthStage = 3; }
            }
            result.initializedHealthPlayers = healthy.Count;
            result.damagedHealthPlayers = damaged.Count;
            result.downedHealthPlayers = downed.Count;
            result.resetHealthPlayers = reset.Count;
            result.recoveredMovingPlayers = recovered.Count;
        }
        private void CheckCombat(NetworkPlayerController[] players)
        {
            // Isolate shooting from the earlier movement test: four independent lanes.
            // This relocation exists only in the opt-in Development combat harness.
            if (session.IsHost && !combatPositionsSet && gameTime >= 13.2f && players.Length == MultiplayerConfig.MaxPlayers)
            {
                combatPositionsSet = true;
                foreach (var player in players)
                {
                    int slot = session.Room.GetSlot(player.OwnerClientId);
                    var controller = player.GetComponent<CharacterController>();
                    controller.enabled = false;
                    player.GetComponent<Unity.Netcode.Components.NetworkTransform>().Teleport(
                        new Vector3(-9 + slot * 6, 1.05f, -4), Quaternion.identity, Vector3.one);
                    controller.enabled = true;
                }
                Physics.SyncTransforms();
            }
            var targets = FindObjectsByType<TrainingTarget>();
            result.maximumTargets = Mathf.Max(result.maximumTargets, targets.Length);
            foreach (var target in targets)
            {
                if (target.Snapshot.Current < target.Snapshot.Maximum) damagedTargets.Add(target.NetworkObjectId);
                if (target.Snapshot.IsDowned) downedTargets.Add(target.NetworkObjectId);
                if (downedTargets.Contains(target.NetworkObjectId) && !target.Snapshot.IsDowned) result.targetResetObserved = true;
            }
            result.targetDamageObserved |= damagedTargets.Count > 0;
            result.targetDownObserved |= downedTargets.Count > 0;
            int firing = 0, damaging = 0;
            bool noShots = true, exactlyTwoShots = players.Length == MultiplayerConfig.MaxPlayers, fullHealth = exactlyTwoShots;
            foreach (var player in players)
            {
                var weapon = player.GetComponent<NetworkHitscanWeapon>();
                if (weapon == null) continue;
                if (weapon.ObservedShots > 0) firing++;
                if (weapon.ObservedDamagingHits > 0) damaging++;
                noShots &= weapon.ObservedShots == 0;
                exactlyTwoShots &= weapon.ObservedShots == 2;
                fullHealth &= player.GetComponent<PlayerHealth>().Snapshot.Current == session.config.playerMaxHealth;
                if (!player.IsOwner || targets.Length == 0) continue;
                TrainingTarget closest = targets[0];
                foreach (var target in targets)
                    if ((target.transform.position - weapon.Eye).sqrMagnitude < (closest.transform.position - weapon.Eye).sqrMagnitude)
                        closest = target;
                Vector3 direction = (closest.transform.position - weapon.Eye).normalized;
                if (combatRequestStage == 0 && gameTime >= 8.6f)
                { weapon.TestFire(weapon.Eye, direction); combatRequestStage = 1; }
                else if (combatRequestStage == 1 && gameTime >= 10.7f)
                {
                    weapon.TestFire(weapon.Eye, new Vector3(float.NaN, 0, 1));
                    weapon.TestFire(weapon.Eye + Vector3.up * 100, direction);
                    weapon.TestFire(weapon.Eye, Vector3.zero); combatRequestStage = 2;
                }
                else if (combatRequestStage == 2 && gameTime >= 13.6f)
                {
                    foreach (var teammate in players) if (teammate != player)
                    { weapon.TestFire(weapon.Eye, (teammate.GetComponent<NetworkHitscanWeapon>().Eye - weapon.Eye).normalized); break; }
                    combatRequestStage = 3;
                }
                else if (combatRequestStage == 3 && gameTime >= 14.5f)
                {
                    for (int i = 0; i < 5; i++) weapon.TestFire(weapon.Eye, direction);
                    combatRequestStage = 4;
                }
                else if (combatRequestStage >= 4 && combatRequestStage <= 6 && gameTime >= 16.5f + (combatRequestStage - 4) * 0.5f)
                { weapon.TestFire(weapon.Eye, direction); combatRequestStage++; }
            }
            if (gameTime >= 9.5f && gameTime < 10) result.downedFireRejected |= noShots;
            if (gameTime >= 11.5f && gameTime < 12) result.invalidAimRejected |= noShots;
            if (gameTime >= 14 && gameTime < 14.4f) result.friendlyFireBlocked |= fullHealth && firing == MultiplayerConfig.MaxPlayers;
            if (gameTime >= 15.7f && gameTime < 16.4f) result.shotSpamLimited |= exactlyTwoShots;
            // Despawn can precede the session's disconnect state callback by a frame.
            // Keep observed evidence, just as maximumPlayers and the movement sets do.
            result.shootingPlayers = Mathf.Max(result.shootingPlayers, firing);
            result.damagingPlayers = Mathf.Max(result.damagingPlayers, damaging);
        }
        private void WriteResult()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))); File.WriteAllText(output, JsonUtility.ToJson(result, true)); }
            catch (Exception exception) { Debug.LogError("Smoke result write failed: " + exception.Message); }
        }
        private async Task LeaveForTestAsync()
        {
            try
            {
                await session.LeaveRoomAsync();
                // Let deferred destruction of any menu duplicate finish before measuring.
                await Task.Yield();
                result.managerCountAfterLeave = FindObjectsByType<NetworkManager>().Length;
                result.leaveCompleted = session.State == SessionState.Offline && !NetworkManager.Singleton.IsListening;
                readySent = false;
            }
            catch (Exception exception) { result.error = exception.Message; }
        }
        private async Task ReconnectForTestAsync()
        {
            try
            {
                await session.StartLocalTestAsync(result.role == "host");
                result.reconnected = NetworkManager.Singleton.IsConnectedClient;
                result.recreatedRoom = result.role == "host" && result.reconnected && session.Room != null;
            }
            catch (Exception exception) { result.error = exception.Message; }
        }
#else
        // Keep a valid serialized component in release scenes; exclude all test logic.
        private void Awake() => enabled = false;
#endif
    }
}
