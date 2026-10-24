using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkMissionState))]
    public sealed class EnemyDirector : NetworkBehaviour
    {
        public EnemyEncounterProfile profile;
        private readonly NetworkVariable<EnemyDirectorState> state = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> validationStage = new(0);
        private readonly NetworkVariable<uint> validationChecks = new(0);
        public EnemyDirectorState Snapshot => state.Value;
        public AITelemetry Telemetry { get; private set; }
        public event Action<EnemyDirectorState> Changed;
        private NetworkMissionState mission;
        private UplinkEncounter encounter;
        private NetworkChip chip;
        private EnemyDirectorHUD hud;
        private readonly List<NoiseEvent> noises = new();
        private readonly Dictionary<DeadlineReason, double> debounces = new();
        private readonly Dictionary<ulong, bool> previousDown = new();
        private readonly Dictionary<ulong, double> reviveGrace = new();
        private uint noiseSequence, checkpointBits;
        private int checkpointObjective, savedMeter, retentionFloor;
        private uint previousTransfers;
        private double nextSample, beganAt, nextPressure, lastPursuit, nextSprint;
        private bool modifierApplied;
        public bool EnabledForMission => profile != null && mission?.UsesCheckpoints == true;
        public bool EncounterActive => EnabledForMission && mission.Snapshot.Phase == MissionPhase.Active
            && (encounter?.Snapshot.Phase == UplinkEncounterPhase.Active||GetComponent<ChapterOperation>()?.CombatActive==true);
        public Vector3 Node => GetComponent<ChapterOperation>()?.Enabled==true?GetComponent<ChapterOperation>().Node:profile != null ? profile.protectedNode : transform.position;
        public struct NoiseEvent { public uint Sequence; public Vector3 Position; public EnemyNoise Kind; public float Intensity; public double At; }
        public override void OnNetworkSpawn()
        {
            mission = GetComponent<NetworkMissionState>(); encounter = GetComponent<UplinkEncounter>(); chip = GetComponent<NetworkChip>();
            state.OnValueChanged += OnChanged; mission.Changed += OnMissionChanged;
            if (IsClient && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            { hud = gameObject.AddComponent<EnemyDirectorHUD>(); hud.Bind(this); }
        }
        private void OnChanged(EnemyDirectorState before, EnemyDirectorState after) => Changed?.Invoke(after);
        private void OnMissionChanged(MissionSnapshot current)
        {
            if (!IsServer || !IsSpawned || !EnabledForMission) return;
            EnsureAttempt();
            if (current.Phase != MissionPhase.Active) { noises.Clear(); return; }
            var saved = mission.Checkpoint;
            if (saved.CompletedStations != checkpointBits || saved.ObjectiveIndex != checkpointObjective)
            {
                bool cleanData = (saved.CompletedStations & 7u) != (checkpointBits & 7u);
                bool generator = (saved.CompletedStations & 8u) != 0 && (checkpointBits & 8u) == 0;
                checkpointBits = saved.CompletedStations; checkpointObjective = saved.ObjectiveIndex;
                if (cleanData) AdjustMeter(0, -3, DeadlineReason.CleanData, false);
                if (generator) AdjustMeter(20, 8, DeadlineReason.Alarm, false);
                savedMeter = Snapshot.MissionMeter; retentionFloor = savedMeter;
            }
        }
        private void EnsureAttempt()
        {
            uint attempt = mission.Snapshot.Attempt;
            if (Snapshot.Attempt == attempt) return;
            var old = Snapshot;
            int retained = old.Attempt == 0 ? 0 : DirectorRules.Retain(old.MissionMeter, savedMeter, retentionFloor, profile.checkpointRetention);
            retentionFloor = retained;
            state.Value = new EnemyDirectorState { Attempt = attempt, MissionMeter = retained, CampaignThreat = old.CampaignThreat,
                Reason = old.Attempt == 0 ? DeadlineReason.None : DeadlineReason.Checkpoint };
            modifierApplied = false; beganAt = 0; noises.Clear(); debounces.Clear(); previousDown.Clear(); reviveGrace.Clear();
            previousTransfers = chip != null ? chip.Snapshot.Transfers : 0;
            Debug.Log($"AI_CHECKPOINT attempt={attempt} missionMeter={retained} floor={retentionFloor}");
        }
        internal bool BeginEncounterServer()
        {
            if (!IsServer || !IsSpawned || !EnabledForMission || !profile.Validate(out _)) return false;
            EnsureAttempt(); beganAt = Time.realtimeSinceStartupAsDouble; nextPressure = beganAt + 10; lastPursuit = beganAt;
            var next = Snapshot; next.EncounterAlert = 20; state.Value = next;
            previousTransfers = chip != null ? chip.Snapshot.Transfers : 0;
            return true;
        }
        public EnemyAIProfile RoleProfile(EnemyRole role)
        { int i = (int)role; return profile != null && i >= 0 && i < profile.roster.Length ? profile.roster[i].GetComponent<EnemyBrain>().profile : null; }
        public void EmitNoiseServer(Vector3 point, EnemyNoise kind, float intensity = 1)
        {
            if (!IsServer || !IsSpawned || !EnabledForMission || mission.Snapshot.Phase != MissionPhase.Active
                || !MissionRules.Finite(point) || !MissionRules.Positive(intensity) || intensity > 2) return;
            noises.Add(new NoiseEvent { Sequence = ++noiseSequence, Position = point, Kind = kind, Intensity = intensity, At = Time.realtimeSinceStartupAsDouble });
            if (noises.Count > 32) noises.RemoveAt(0);
            if (kind == EnemyNoise.Gunshot) AdjustMeter(2, 2, DeadlineReason.Gunfire);
        }
        internal bool Hear(EnemyBrain listener, ref uint consumed, out NoiseEvent heard)
        {
            heard = default; bool found = false;
            foreach (var noise in noises)
            {
                if (noise.Sequence <= consumed || Time.realtimeSinceStartupAsDouble - noise.At > 2
                    || !PerceptionRules.CanHear(listener.transform.position, noise.Position, listener.CurrentProfile.hearingRange, noise.Intensity)) continue;
                heard = noise; found = true;
            }
            consumed = noiseSequence; return found;
        }
        internal void ShareContact(EnemyBrain source, Vector3 point, ulong target)
        {
            if (!IsServer || !EncounterActive) return;
            lastPursuit = Time.realtimeSinceStartupAsDouble;
            AdjustMeter(12, 3, DeadlineReason.VisualContact);
            foreach (var ally in FindObjectsByType<EnemyBrain>())
                if (ally != source && ally.Group == source.Group && ally.IsAlive
                    && Vector3.Distance(source.transform.position, ally.transform.position) <= source.CurrentProfile.groupRadius)
                    ally.ReceiveShared(point, target);
        }
        internal void ObservePursuit() { if (IsServer) lastPursuit = Time.realtimeSinceStartupAsDouble; }
        internal bool IsReviveProtected(ulong objectId) => reviveGrace.TryGetValue(objectId, out double until) && Time.realtimeSinceStartupAsDouble < until;
        internal float TargetBonus(EnemyRole role, PlayerHealth target)
        {
            float value = 0;
            if (role == EnemyRole.Hunter && chip != null && chip.IsCarrier(target.NetworkObjectId)) value += 18;
            if (Snapshot.Modifier == AdaptiveModifier.ReviveHunter && modifierApplied && role == EnemyRole.Hunter
                && target.GetComponent<PlayerRevive>()?.IsChanneling == true) value += 14;
            return value;
        }
        internal void RecordShot(PlayerHealth player, bool hit, float range)
        {
            if (!IsServer || !EncounterActive || player == null) return;
            var t = Telemetry; t.Shots++;
            if (player.GetComponent<PlayerMovement>()?.PlanarSpeed < .2f) t.StaticShots++;
            Telemetry = t; EmitNoiseServer(player.transform.position, EnemyNoise.Gunshot);
        }
        internal void RecordDamage(PlayerHealth attacker, int amount, Vector3 point)
        {
            if (!IsServer || !EncounterActive || attacker == null || amount <= 0) return;
            var t = Telemetry; t.Hits++;
            if (Vector3.Distance(attacker.transform.position, point) <= 7) t.NearDamage += amount; else t.FarDamage += amount;
            Telemetry = t;
        }
        // Gameplay adapters call this only after validating a real hack; no client RPC exposes telemetry.
        internal void RecordCleanHackServer()
        { if (IsServer && EnabledForMission) { var t = Telemetry; t.HackActions++; Telemetry = t; AdjustMeter(-5, -3, DeadlineReason.CleanData); } }
        private void AdjustMeter(int alert, int meter, DeadlineReason reason, bool debounce = true)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (debounce && debounces.TryGetValue(reason, out double until) && now < until) return;
            debounces[reason] = now + 5;
            var next = Snapshot; next.EncounterAlert = Mathf.Clamp(next.EncounterAlert + alert, 0, 100);
            next.MissionMeter = Mathf.Clamp(next.MissionMeter + meter, 0, 100); next.Reason = reason; state.Value = next;
        }
        private void Update()
        {
            if (!IsSpawned || !IsServer || !EnabledForMission || mission.Snapshot.Phase != MissionPhase.Active) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSample) return; nextSample = now + .5;
            noises.RemoveAll(x => now - x.At > 2);
            if (!EncounterActive) return;
            var enemies = FindObjectsByType<RobotEnemy>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enemies.Length > 0 && enemies.All(x => x.DebugPaused)) return;
#endif
            var players = NetworkManager.ConnectedClients.Values.Select(x => x.PlayerObject?.GetComponent<PlayerHealth>()).Where(x => x != null && x.IsSpawned).ToArray();
            var t = Telemetry;
            foreach (var p in players)
            {
                bool down = p.Snapshot.IsDowned;
                if (previousDown.TryGetValue(p.NetworkObjectId, out bool wasDown))
                {
                    if (down && !wasDown) { t.Downs++; AdjustMeter(5, 3, DeadlineReason.SquadDown); }
                    if (!down && wasDown) { t.Revives++; reviveGrace[p.NetworkObjectId] = now + 3; }
                }
                previousDown[p.NetworkObjectId] = down;
                if (!down && p.GetComponent<PlayerMovement>()?.PlanarSpeed > 5.5f && now >= nextSprint)
                    EmitNoiseServer(p.transform.position, EnemyNoise.Sprint, .55f);
            }
            nextSprint = now + .5;
            if (chip != null)
            {
                if (chip.Snapshot.Transfers > previousTransfers) t.Transfers += (int)(chip.Snapshot.Transfers - previousTransfers);
                previousTransfers = chip.Snapshot.Transfers;
                if (chip.Snapshot.Location != ChipLocation.Carried) AdjustMeter(2, 1, DeadlineReason.ChipExposed);
            }
            var alive = players.Where(x => x.CanAct).ToArray();
            if (alive.Length >= 2)
            {
                t.Samples++; float largest = 0;
                for (int i = 0; i < alive.Length; i++) for (int j = i + 1; j < alive.Length; j++) largest = Mathf.Max(largest, Vector3.Distance(alive[i].transform.position, alive[j].transform.position));
                if (largest < 5) t.TightSamples++; if (largest > 14) t.SpreadSamples++;
            }
            Telemetry = t;
            if (now >= nextPressure) { nextPressure = now + 10; AdjustMeter(0, 1, DeadlineReason.CombatTime, false); }
            if (now - lastPursuit >= 8 && Snapshot.EncounterAlert != 0)
            { var cleared = Snapshot; cleared.EncounterAlert = 0; cleared.Reason = DeadlineReason.PursuitLost; state.Value = cleared; }
            if (!Snapshot.SelectionLocked && beganAt > 0 && now - beganAt >= profile.adaptationDelay)
            {
                var next = Snapshot; next.Tendency = t.Tag; next.Modifier = DirectorRules.Choose(t.Tag, profile.whitelist);
                next.SelectionLocked = true; next.Decisions = 1; next.ApplyAt = NetworkManager.ServerTime.Time + profile.adaptationWarning; state.Value = next;
                Debug.Log($"AI_DIRECTOR attempt={next.Attempt} tag={next.Tendency} modifier={next.Modifier} reason=telemetry shots:{t.Shots},hits:{t.Hits},near:{t.NearDamage},far:{t.FarDamage},revives:{t.Revives},tight:{t.TightSamples}/{t.Samples} cap=one modifier; alive<={profile.maximumAlive}");
            }
            if (Snapshot.SelectionLocked && !modifierApplied && NetworkManager.ServerTime.Time >= Snapshot.ApplyAt) ApplyModifier();
        }
        private void ApplyModifier()
        {
            modifierApplied = true;
            var brains = FindObjectsByType<EnemyBrain>().Where(x => x.IsAlive).OrderBy(x => x.Enemy.NetworkObjectId).ToArray();
            var role = Snapshot.Modifier switch { AdaptiveModifier.FlankAmbush => EnemyRole.Flanker,
                AdaptiveModifier.AreaDenial => EnemyRole.Controller, AdaptiveModifier.CyberShield => EnemyRole.Support, _ => EnemyRole.Suppressor };
            if (Snapshot.Modifier == AdaptiveModifier.FlankAmbush || Snapshot.Modifier == AdaptiveModifier.AreaDenial || Snapshot.Modifier == AdaptiveModifier.CyberShield)
            {
                var change = brains.FirstOrDefault(x => x.CurrentProfile.role != EnemyRole.Hunter && x.CurrentProfile.role != EnemyRole.Frontliner);
                if (change != null) change.AssignRoleServer(RoleProfile(role));
            }
            foreach (var brain in brains) brain.Armored = Snapshot.Modifier == AdaptiveModifier.ArmorResponse && brain.CurrentProfile.role == EnemyRole.Frontliner;
        }
        internal EnemyRole ResponseRole(EnemyRole role)
        {
            if(!modifierApplied)return role;
            return Snapshot.Modifier switch{AdaptiveModifier.FlankAmbush=>EnemyRole.Flanker,AdaptiveModifier.AreaDenial=>EnemyRole.Controller,AdaptiveModifier.CyberShield=>EnemyRole.Support,_=>role};
        }
        internal void ConfigureReinforcement(EnemyBrain brain)
        {if(IsServer&&brain!=null)brain.Armored=modifierApplied&&Snapshot.Modifier==AdaptiveModifier.ArmorResponse&&brain.CurrentProfile.role==EnemyRole.Frontliner;}
        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnChanged; if (mission != null) mission.Changed -= OnMissionChanged;
            if (hud != null) Destroy(hud); noises.Clear(); previousDown.Clear(); reviveGrace.Clear();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public NetworkVariable<int> TestStage => validationStage;
        public NetworkVariable<uint> TestChecks => validationChecks;
        public bool TestTryClientWrite() => !IsServer && IsSpawned && state.CanClientWrite(NetworkManager.LocalClientId);
#endif
    }
}
