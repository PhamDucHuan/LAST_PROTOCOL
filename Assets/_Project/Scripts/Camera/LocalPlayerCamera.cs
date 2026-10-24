using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LastProtocol
{
    // NGO applies interpolated transforms in PreLateUpdate. Follow them afterwards,
    // before CinemachineBrain evaluates its camera in LateUpdate.
    [DefaultExecutionOrder(-50)]
    public sealed class LocalPlayerCamera : MonoBehaviour
    {
        public CinemachineCamera virtualCamera;
        public CinemachineThirdPersonFollow follow;
        private Transform playerTarget;
        private Transform pivot;
        private float yaw, pitch = 20;
        // Keep player intent separate from the temporary unlock required by focus loss.
        private bool captureRequested;
        private bool hasFocus;
        private bool paused;
        private bool restoreCaptureOnUpdate;
        private bool discardMouseDelta;
        private bool wasWiped;
        private bool MissionInterrupted => MultiplayerSession.Instance?.Room?.GetComponent<NetworkMissionState>()?.Snapshot.Phase == MissionPhase.Failed;
        public bool IsBoundTo(Transform target) => playerTarget == target;
        private void Awake()
        {
            hasFocus = Application.isFocused;
            pivot = new GameObject("Local Camera Pivot").transform;
            virtualCamera.Follow = pivot;
        }
        public void Bind(Transform target)
        {
            playerTarget = target;
            yaw = target.eulerAngles.y;
            pivot.position = target.position;
            captureRequested = true;
            ApplyCursorState();
            follow.CameraDistance = MultiplayerSession.Instance.config.cameraDistance;
            var brain = UnityEngine.Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            }
        }
        public void Unbind(Transform target)
        {
            if (playerTarget != target) return;
            playerTarget = null;
            captureRequested = false;
            restoreCaptureOnUpdate = false;
            SetCursorLocked(false);
        }
        private void Update()
        {
            if (playerTarget == null) return;
            bool wiped = MultiplayerSession.Instance?.Room?.TeamWiped.Value == true || MissionInterrupted;
            if (wiped != wasWiped) { wasWiped = wiped; ApplyCursorState(); }
            if (!Application.isFocused || paused) return;
            // Reapply after focus callbacks too: the OS/Editor can release capture later
            // in the focus transition, after OnApplicationFocus has already run.
            if (restoreCaptureOnUpdate)
            {
                restoreCaptureOnUpdate = false;
                ApplyCursorState();
            }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                captureRequested = !locked;
                ApplyCursorState();
            }
            else if (Cursor.lockState != CursorLockMode.Locked
                && Mouse.current?.leftButton.wasPressedThisFrame == true
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                // Also allow clicking the game view to resume after an intentional Esc.
                captureRequested = true;
                ApplyCursorState();
            }
            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && !discardMouseDelta)
            {
                // Mouse delta is already displacement for this frame; do not multiply by deltaTime.
                Vector2 delta = Mouse.current.delta.ReadValue();
                float sensitivity = MultiplayerSession.Instance.config.cameraSensitivity;
                yaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -25, 70);
            }
            discardMouseDelta = false;
        }
        private void LateUpdate()
        {
            if (playerTarget == null || pivot == null) return;
            pivot.SetPositionAndRotation(playerTarget.position, Quaternion.Euler(pitch, yaw, 0));
        }
        private void OnDestroy()
        {
            if (pivot != null) Destroy(pivot.gameObject);
            SetCursorLocked(false);
        }
        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            restoreCaptureOnUpdate = focused;
            ApplyCursorState();
        }
        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            restoreCaptureOnUpdate = !isPaused;
            ApplyCursorState();
        }
        private void ApplyCursorState()
        {
            bool locked = playerTarget != null && captureRequested && hasFocus && !paused
                && MultiplayerSession.Instance?.Room?.TeamWiped.Value != true && !MissionInterrupted;
            if (locked && Cursor.lockState != CursorLockMode.Locked) discardMouseDelta = true;
            SetCursorLocked(locked);
        }
        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
