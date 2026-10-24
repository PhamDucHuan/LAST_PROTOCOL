using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    // Local presentation only. No trigger/RPC here can advance the mission.
    public sealed class MissionHUD : MonoBehaviour
    {
        private NetworkMissionState mission;
        private GameObject root, marker;
        private TMP_Text title, objective, progress, worldLabel, checkpointLabel;
        private LineRenderer ring;
        private UplinkEncounter encounter;
        private NetworkChip chip;
        private ChapterOperation chapter;
        private GameObject secondaryMarker;
        private LineRenderer secondaryRing;
        private TMP_Text secondaryLabel;
        public bool Visible => root != null && root.activeSelf;
        public string ObjectiveText => objective != null ? objective.text : string.Empty;
        public void Bind(NetworkMissionState source)
        {
            mission = source;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            root = new GameObject("Squad Mission HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 8;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var panel = new GameObject("Mission", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(root.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(18, -94); panel.sizeDelta = new Vector2(600, 190);
            panel.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.055f, 0.88f);
            panel.GetComponent<Image>().raycastTarget = false;
            title = Label(panel, font, -18, 24, new Color(0.25f, 0.9f, 0.95f));
            objective = Label(panel, font, -56, 24, Color.white);
            progress = Label(panel, font, -97, 20, new Color(0.8f, 0.88f, 0.92f));
            checkpointLabel = Label(panel, font, -152, 18, new Color(.5f, .95f, .65f));
            checkpointLabel.rectTransform.sizeDelta = new Vector2(572, 30);
            marker = new GameObject("Local Mission Destination"); marker.transform.SetParent(transform, false);
            ring = marker.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true;
            ring.positionCount = 64; ring.startWidth = ring.endWidth = 0.14f;
            ring.sharedMaterial = source.markerMaterial;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            worldLabel = new GameObject("Destination Name", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            worldLabel.transform.SetParent(marker.transform, false);
            worldLabel.font = font; worldLabel.fontSize = 3.5f; worldLabel.alignment = TextAlignmentOptions.Center;
            worldLabel.rectTransform.sizeDelta = new Vector2(18, 3); worldLabel.color = new Color(0.3f, 1, 1);
            encounter = source.GetComponent<UplinkEncounter>();
            chip = source.GetComponent<NetworkChip>();
            chapter=source.GetComponent<ChapterOperation>();
            if(chapter!=null)chapter.Changed+=OnChapterChanged;
            secondaryMarker=Instantiate(marker,transform);secondaryMarker.name="Secondary signal destination";
            secondaryRing=secondaryMarker.GetComponent<LineRenderer>();secondaryLabel=secondaryMarker.GetComponentInChildren<TMP_Text>();secondaryMarker.SetActive(false);
            if (chip != null) chip.Changed += OnChipChanged;
            if (encounter != null) encounter.Changed += OnEncounterChanged;
            mission.Changed += Refresh; Refresh(mission.Snapshot);
        }
        private void OnEncounterChanged(UplinkEncounterState state) => Refresh(mission.Snapshot);
        private void OnChipChanged(ChipState state) => Refresh(mission.Snapshot);
        private void OnChapterChanged(ChapterOperationState state)=>Refresh(mission.Snapshot);
        private static TMP_Text Label(RectTransform parent, TMP_FontAsset font, float y, int size, Color color)
        {
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0, 1);
            text.rectTransform.anchoredPosition = new Vector2(14, y); text.rectTransform.sizeDelta = new Vector2(572, 46);
            text.font = font; text.fontSize = size; text.color = color; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }
        private void Refresh(MissionSnapshot snapshot)
        {
            var definition = mission.Definition;
            bool visible = definition != null && snapshot.Phase != MissionPhase.None;
            root.SetActive(visible); marker.SetActive(visible && snapshot.Phase == MissionPhase.Active);
            secondaryMarker.SetActive(false);
            if (!visible) return;
            title.text = definition.title + $" | {Mathf.Min(snapshot.ObjectiveIndex + 1, definition.objectives.Length)}/{definition.objectives.Length}";
            checkpointLabel.gameObject.SetActive(mission.UsesCheckpoints);
            checkpointLabel.text = mission.UsesCheckpoints ? $"Checkpoint: {CheckpointRules.Name(mission.Checkpoint)} | Attempt {snapshot.Attempt}" : "";
            if(mission.IsLongChapter&&chapter!=null)checkpointLabel.text=$"CP: {CheckpointRules.Name(mission.Checkpoint)} | Try {snapshot.Attempt} | {ChapterPlan.Clock(chapter.Snapshot.ElapsedSeconds)}";
            if (snapshot.Phase == MissionPhase.Completed)
            { objective.text = mission.IsLongChapter?"RESCUE ROUTE SECURED":mission.IsLegacyRecon ? "RECON COMPLETE" : "COMMS RESTORED"; progress.text = definition.completionText; return; }
            if (snapshot.Phase == MissionPhase.Failed)
            {
                objective.text = mission.IsLegacyRecon ? "RECON FAILED" : "OPERATION INTERRUPTED";
                progress.text = snapshot.Failure == MissionFailure.TeamWiped
                    ? "Squad down. Host can retry the checkpoint."
                    : snapshot.Failure == MissionFailure.EncounterUnavailable ? "Defence unavailable. Host can retry the checkpoint."
                    : snapshot.Failure == MissionFailure.ChipTraced ? "Chip Trace reached 100. Host can retry the checkpoint."
                    : "Not enough players. Leave the session and regroup.";
                return;
            }
            var current = definition.objectives[snapshot.ObjectiveIndex];
            objective.text = current.title;
            progress.text = ChapterPlan.Timed(current.kind)
                ? $"Progress {ChapterPlan.Clock(snapshot.HeldSeconds)}/{ChapterPlan.Clock(current.holdSeconds)} | Positions {snapshot.Inside}/{snapshot.Required}\n{current.instruction}"
                : current.kind == ObjectiveKind.Rendezvous
                ? $"Squad {snapshot.Inside}/{snapshot.Required} | Hold {snapshot.HeldSeconds:0.0}/{current.holdSeconds:0.0}s\n{current.instruction}"
                : $"Secured {MissionRules.Collected(definition, snapshot, snapshot.ObjectiveIndex)}/{current.stationIds.Length} | Hold E at a marked device\n{current.instruction}";
            if(mission.IsLongChapter&&chapter!=null&&ChapterPlan.Timed(current.kind)&&chapter.Snapshot.NextWaveAt>0)
            {
                double remaining=chapter.Snapshot.NextWaveAt-mission.NetworkManager.ServerTime.Time;
                if(remaining>0&&remaining<=3)progress.text=$"Progress {ChapterPlan.Clock(snapshot.HeldSeconds)}/{ChapterPlan.Clock(current.holdSeconds)} | Sites {snapshot.Inside}/{snapshot.Required}\nREINFORCEMENTS IN {Mathf.CeilToInt((float)remaining)}s | Prepare cover";
            }
            if (encounter != null && encounter.EnabledForMission && current.kind == ObjectiveKind.Stations
                && System.Array.IndexOf(current.stationIds, DataStation.UplinkId) >= 0)
            {
                var wave = encounter.Snapshot;
                if (wave.Phase == UplinkEncounterPhase.Active)
                    progress.text = $"Robots {wave.Alive}/{wave.Spawned} | Squad: protect the operator\nHold E at uplink. Release to cancel the channel.";
                else if (wave.Phase == UplinkEncounterPhase.Cleared)
                    progress.text = "Area clear | Hold E to restore communications\nThe security wave will not return this round.";
            }
            Vector3 destination = current.center; float radius = current.radius;
            if(current.kind==ObjectiveKind.SplitScan)
            {
                secondaryMarker.SetActive(true);secondaryLabel.text="SIGNAL B";secondaryLabel.transform.position=current.secondaryCenter+Vector3.up*3;
                for(int i=0;i<secondaryRing.positionCount;i++){float a=i*Mathf.PI*2/secondaryRing.positionCount;secondaryRing.SetPosition(i,current.secondaryCenter+new Vector3(Mathf.Cos(a)*radius,.22f,Mathf.Sin(a)*radius));}
            }
            if (current.kind == ObjectiveKind.Stations)
                foreach (string id in current.stationIds)
                {
                    if (!MissionRules.CanUseStation(definition, snapshot, id)) continue;
                    foreach (var station in FindObjectsByType<DataStation>())
                        if (station.stationId == id) { destination = station.transform.position; radius = station.range; break; }
                    break;
                }
            if (chip?.EnabledForMission == true && !chip.Snapshot.TutorialComplete && snapshot.ObjectiveIndex == 2)
            {
                objective.text = "RELAY TUTORIAL | ADMIN 01";
                progress.text = chip.Snapshot.Location == ChipLocation.Dropped
                    ? "Teammate: hold E to recover the ejected chip\nRelease E, then hold E again to revive the carrier."
                    : "Bring a teammate to ADMIN 01\nCarrier: hold E to start the emergency relay drill.";
                destination = chip.drillPosition; radius = chip.range;
            }
            if (chip?.EnabledForMission == true && chip.Snapshot.Location == ChipLocation.Dropped)
            { destination = chip.Snapshot.DropPosition; destination.y = 0; radius = chip.range; }
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                // Campus pavers reach 0.135 m; keep the ring above their visible surface.
                ring.SetPosition(i, destination + new Vector3(Mathf.Cos(angle) * radius, 0.22f, Mathf.Sin(angle) * radius));
            }
            worldLabel.transform.position = destination + Vector3.up * 3;
            worldLabel.text = chip?.EnabledForMission == true && chip.Snapshot.Location == ChipLocation.Dropped ? "RECOVER CONTROL CHIP"
                : chip?.DrillPending == true ? "RELAY DRILL | ADMIN 01" : current.kind==ObjectiveKind.SplitScan?"SIGNAL A":current.title;
        }
        private void LateUpdate()
        {
            if (worldLabel != null && marker.activeSelf && Camera.main != null)
                worldLabel.transform.rotation = Camera.main.transform.rotation;
            if(secondaryMarker!=null&&secondaryMarker.activeSelf&&Camera.main!=null)secondaryLabel.transform.rotation=Camera.main.transform.rotation;
        }
        private void OnDestroy()
        {
            if (mission != null) mission.Changed -= Refresh;
            if (encounter != null) encounter.Changed -= OnEncounterChanged;
            if (chip != null) chip.Changed -= OnChipChanged;
            if(chapter!=null)chapter.Changed-=OnChapterChanged;
            if (root != null) Destroy(root);
            if (marker != null) Destroy(marker);
            if(secondaryMarker!=null)Destroy(secondaryMarker);
        }
    }
}
