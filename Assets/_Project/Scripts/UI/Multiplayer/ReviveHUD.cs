using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerRevive))]
    public sealed class ReviveHUD : NetworkBehaviour
    {
        public TMP_FontAsset font;
        private PlayerRevive revive;
        private PlayerHealth health;
        private GameObject root;
        private TMP_Text prompt;
        private RectTransform fill;
        private GameObject progressBar;
        private PlayerInteraction interaction;
        private Button restart;
        private TMP_Text restartText, chipStatus;
        private float nextHintRefresh;
        private string hint = "";
        public Canvas LocalCanvas => root != null ? root.GetComponent<Canvas>() : null;
        public string VisibleText => prompt != null ? prompt.text : "";
        public string ChipText => chipStatus != null ? chipStatus.text : "";
        public bool ProgressVisible => progressBar != null && progressBar.activeSelf;
        public float Progress => fill != null ? fill.anchorMax.x : 0;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            revive = GetComponent<PlayerRevive>(); health = GetComponent<PlayerHealth>();
            interaction = GetComponent<PlayerInteraction>();
            root = new GameObject("Local Revive HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 8;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            prompt = Label(root.transform, new Vector2(0, -140), new Vector2(1100, 100), 28);
            progressBar = new GameObject("Shared Interaction Progress", typeof(RectTransform), typeof(Image));
            progressBar.transform.SetParent(root.transform, false);
            var barRect = (RectTransform)progressBar.transform;
            barRect.anchoredPosition = new Vector2(0, -205); barRect.sizeDelta = new Vector2(440, 14);
            progressBar.GetComponent<Image>().color = new Color(.04f, .08f, .12f, .95f);
            progressBar.GetComponent<Image>().raycastTarget = false;
            var foreground = new GameObject("Progress Fill", typeof(RectTransform), typeof(Image));
            foreground.transform.SetParent(progressBar.transform, false);
            foreground.GetComponent<Image>().color = new Color(.1f, .85f, .95f);
            foreground.GetComponent<Image>().raycastTarget = false;
            fill = (RectTransform)foreground.transform; fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            progressBar.SetActive(false);
            var button = new GameObject("Host Restart", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(root.transform, false);
            var rect = (RectTransform)button.transform; rect.anchoredPosition = new Vector2(0, -230); rect.sizeDelta = new Vector2(440, 55);
            button.GetComponent<Image>().color = new Color(0.05f, 0.4f, 0.5f);
            restart = button.GetComponent<Button>(); restart.targetGraphic = button.GetComponent<Image>();
            restartText = Label(button.transform, Vector2.zero, rect.sizeDelta, 25);
            restartText.text = "RETRY CHECKPOINT (HOST)";
            restart.onClick.AddListener(() => MultiplayerSession.Instance?.Room?.RestartRoundServer());
            restart.gameObject.SetActive(false);
            chipStatus = Label(root.transform, Vector2.zero, new Vector2(570, 170), 22);
            chipStatus.name = "Chip Tutorial";
            chipStatus.rectTransform.anchorMin = chipStatus.rectTransform.anchorMax = chipStatus.rectTransform.pivot = new Vector2(1, 1);
            chipStatus.rectTransform.anchoredPosition = new Vector2(-24, -100);
            chipStatus.alignment = TextAlignmentOptions.TopRight;
            chipStatus.color = new Color(1, .8f, .4f);
        }
        private TMP_Text Label(Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(parent, false); label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = size;
            label.font = font; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private void Update()
        {
            if (root == null || !IsSpawned) return;
            var room = MultiplayerSession.Instance?.Room;
            var mission = room?.GetComponent<NetworkMissionState>();
            var chip = room?.GetComponent<NetworkChip>();
            bool wiped = room?.TeamWiped.Value == true;
            bool failed = mission?.Snapshot.Phase == MissionPhase.Failed;
            bool checkpoint = mission?.UsesCheckpoints == true;
            restartText.text = checkpoint ? "RETRY CHECKPOINT (HOST)" : "RESTART TEST (HOST)";
            restart.gameObject.SetActive((wiped || failed) && IsServer);
            if (IsServer) restart.interactable = NetworkManager.ConnectedClients.Count >= MultiplayerSession.Instance.config.minimumPlayersToStart;
            RefreshChipStatus(chip, mission);
            string text;
            float progress = -1;
            if (wiped || failed) text = (wiped ? "TEAM DOWN" : "OPERATION INTERRUPTED") + "\n"
                + (checkpoint ? "Retry: " + CheckpointRules.Name(mission.Checkpoint) + " | Confirmed data retained"
                    : IsServer ? "Restart the test or leave the session." : "Waiting for Host to restart the test.");
            else
            {
                PlayerRevive channel = revive.IsChanneling ? revive : null;
                if (health.Snapshot.IsDowned)
                    foreach (var other in FindObjectsByType<PlayerRevive>())
                        if (other.IsChanneling && other.Snapshot.TargetObjectId == NetworkObjectId) { channel = other; break; }
                if (channel != null)
                {
                    var state = channel.Snapshot;
                    int percent = Mathf.Clamp(Mathf.FloorToInt((float)((NetworkManager.ServerTime.Time - state.StartedAt) / state.Duration) * 100), 0, 99);
                    progress = percent / 100f;
                    text = (health.Snapshot.IsDowned ? "TEAMMATE IS REVIVING YOU" : "REVIVING TEAMMATE - HOLD E") + "\n" + percent + "%";
                }
                else if (health.Snapshot.IsDowned) text = chip?.Snapshot.Location == ChipLocation.Dropped && chip.Snapshot.PreviousCarrier == NetworkObjectId
                    ? "DOWNED - CHIP EJECTED\nTeammate: hold E to secure chip, release E, then revive."
                    : "DOWNED\nWait for a teammate to hold E nearby.";
                else
                {
                    // Proximity is a local hint; refresh at 5 Hz instead of searching every frame.
                    if (Time.unscaledTime >= nextHintRefresh)
                    { nextHintRefresh = Time.unscaledTime + 0.2f; hint = interaction?.HintRevive != null ? "HOLD E TO REVIVE | Stay still and avoid damage" : ""; }
                    text = hint;
                    var chipHint = interaction?.ActiveChip ?? interaction?.HintChip;
                    var station = interaction?.ActiveStation ?? interaction?.HintStation;
                    if (chipHint != null)
                    {
                        var s = chipHint.Snapshot;
                        if (s.Active)
                        {
                            progress = Mathf.Clamp01((float)((NetworkManager.ServerTime.Time - s.StartedAt) / s.Duration));
                            text = (s.Action == InteractionKind.EnemyCapture ? "HUNTER SECURING CHIP - SHOOT TO INTERRUPT" : s.Actor != NetworkObjectId ? "TEAMMATE IS INTERACTING" : s.Action == InteractionKind.RecoverChip
                                ? "SECURING CONTROL CHIP - HOLD E" : "STARTING EMERGENCY DRILL - HOLD E")
                                + "\n" + Mathf.Min(99, Mathf.FloorToInt(progress * 100)) + (s.Action == InteractionKind.EnemyCapture ? "% | Recover chip after interrupting" : "% | Release E to cancel");
                        }
                        else if (interaction.HintChipAction == InteractionKind.RelayDrill)
                            text = "HOLD E TO BEGIN RELAY DRILL\nA teammate must be nearby | You will be downed safely";
                        else text = "HOLD E TO SECURE CONTROL CHIP\nChip recovery first | Release E before reviving";
                    }
                    else if (hint.Length == 0 && station != null)
                    {
                        var state = station.Snapshot;
                        if (state.Completed) text = station.SuccessText + "\n" + station.completionMessage;
                        else if (state.Active)
                        {
                            progress = Mathf.Clamp01((float)((NetworkManager.ServerTime.Time - state.StartedAt) / state.Duration));
                            text = (state.Actor == NetworkObjectId ? station.ActionText + " - HOLD E" : "TEAMMATE IS INTERACTING")
                                + "\n" + Mathf.Min(99, Mathf.FloorToInt(progress * 100))
                                + (state.Actor == NetworkObjectId ? "% | Release E to cancel" : "% | Terminal busy");
                        }
                        else if (station.RequiresCarrier && chip != null && !chip.IsCarrier(NetworkObjectId)) text = "CHIP CARRIER REQUIRED\nProtect the carrier while they hold E at the uplink";
                        else text = station.Available ? "HOLD E TO " + station.ActionText + "\n" + station.displayName + " | Stay still"
                            : "LOCKED | Complete the current objective\n" + station.displayName;
                    }
                }
            }
            progressBar.SetActive(progress >= 0);
            if (progress >= 0) fill.anchorMax = new Vector2(Mathf.Min(.99f, progress), 1);
            if (prompt.text != text) prompt.text = text;
        }
        private void RefreshChipStatus(NetworkChip chip, NetworkMissionState mission)
        {
            chipStatus.gameObject.SetActive(chip?.EnabledForMission == true);
            if (chip?.EnabledForMission != true) return;
            var s = chip.Snapshot;
            string carrier = "INACTIVE";
            if (s.Location == ChipLocation.Dropped) carrier = "CHIP DROPPED - RECOVER WITH E";
            if (s.Active && s.Action == InteractionKind.EnemyCapture) carrier = "HUNTER SECURING CHIP - INTERRUPT IT";
            if (s.Location == ChipLocation.Captured) carrier = "HUNTER HAS CHIP - DISABLE OR DESTROY IT";
            else if (s.Location == ChipLocation.Carried)
            {
                if (s.Carrier == NetworkObjectId) carrier = "YOU CARRY THE CONTROL CHIP";
                else if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(s.Carrier, out var p))
                    carrier = "CARRIER: PLAYER " + (MultiplayerSession.Instance.Room.GetSlot(p.OwnerClientId) + 1);
            }
            chipStatus.text = carrier + "\n" + (s.TutorialComplete ? "Relay learned | Reviving does not move the chip"
                : mission.Snapshot.ObjectiveIndex < 2 ? "Relay tutorial after securing all three records"
                : s.Location == ChipLocation.Dropped ? "Hold E to recover, release, then E to revive"
                : "Meet at ADMIN 01 | Carrier: hold E for relay drill");
            if (s.Trace > 0) chipStatus.text += "\nTRACE " + Mathf.CeilToInt(s.Trace) + "/100 | Recover before 100";
        }
        public override void OnNetworkDespawn() => Cleanup();
        public override void OnDestroy() { Cleanup(); base.OnDestroy(); }
        private void Cleanup() { if (root != null) Destroy(root); }
    }
}
