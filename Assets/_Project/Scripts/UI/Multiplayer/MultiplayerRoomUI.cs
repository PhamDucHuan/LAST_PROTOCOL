using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    public sealed class MultiplayerRoomUI : MonoBehaviour
    {
        public GameObject menuPanel, joinPanel, roomPanel, errorPanel, gameplayPanel;
        public TMP_InputField joinCodeInput;
        public TMP_Text roomCodeText, playerCountText, playerListText, statusText, errorText, readyText, gameStatusText;
        public Button createButton, openJoinButton, joinButton, backButton, copyButton, leaveButton,
            readyButton, startButton, errorBackButton, gameLeaveButton, quitButton;
        private MultiplayerSession session;
        private bool showingJoin;

        private void Start()
        {
            session = MultiplayerSession.Instance;
            if (session == null) { Debug.LogError("Multiplayer UI requires MultiplayerSession."); enabled = false; return; }
            if (menuPanel != null && session.config.menuPrefab != null)
            {
                Instantiate(session.config.menuPrefab);
                gameObject.SetActive(false);
                return;
            }
            Bind(createButton, () => Run(session.CreateRoomAsync()));
            Bind(openJoinButton, () => { showingJoin = true; Refresh(); });
            Bind(joinButton, () => Run(session.JoinRoomAsync(joinCodeInput.text)));
            Bind(backButton, () => { showingJoin = false; Refresh(); });
            Bind(copyButton, () => GUIUtility.systemCopyBuffer = session.JoinCode);
            Bind(leaveButton, () => Run(session.LeaveRoomAsync()));
            Bind(gameLeaveButton, () => Run(session.LeaveRoomAsync()));
            Bind(errorBackButton, () => Run(session.LeaveRoomAsync()));
            Bind(readyButton, ToggleReady);
            Bind(startButton, session.StartGame);
            Bind(quitButton, Quit);
            session.Changed += Refresh;
            session.ErrorRaised += OnError;
            Refresh();
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        { if (button != null) button.onClick.AddListener(action); }
        private async void Run(Task task)
        {
            try { await task; }
            catch (Exception exception) { OnError("Unexpected operation error: " + exception.Message); }
        }
        private void OnError(string message)
        {
            if (errorText != null) errorText.text = message;
            if (errorPanel != null) errorPanel.SetActive(true);
        }
        private void ToggleReady()
        {
            if (session.Room == null || session.IsBusy || NetworkManager.Singleton == null) return;
            ulong id = NetworkManager.Singleton.LocalClientId;
            session.Room.SetReadyRpc(!session.Room.IsReady(id));
        }

        private void Refresh()
        {
            bool busy = session.IsBusy;
            bool lobby = session.State == SessionState.Lobby || session.State == SessionState.LoadingGame;
            bool game = session.State == SessionState.InGame;
            bool error = session.State == SessionState.Error;
            bool idle = !lobby && !game && !error;
            SetActive(menuPanel, idle && !showingJoin);
            SetActive(joinPanel, idle && showingJoin);
            SetActive(roomPanel, lobby);
            SetActive(gameplayPanel, game);
            SetActive(errorPanel, error);
            Button[] operations = { createButton, openJoinButton, joinButton, backButton, copyButton,
                readyButton, startButton, errorBackButton, quitButton };
            foreach (Button button in operations) if (button != null) button.interactable = !busy;
            if (leaveButton != null) leaveButton.interactable = !busy && session.State != SessionState.LoadingGame;
            if (gameLeaveButton != null) gameLeaveButton.interactable = !busy;
            if (joinCodeInput != null) joinCodeInput.interactable = !busy;
            if (roomCodeText != null) roomCodeText.text = "ROOM CODE: " + session.JoinCode;
            if (statusText != null) statusText.text = StatusLabel();
            if (errorText != null) errorText.text = session.LastError;

            NetworkRoomState room = session.Room;
            int count = room != null ? room.Members.Count : 0;
            if (playerCountText != null) playerCountText.text = "PLAYERS " + count + "/" + MultiplayerConfig.MaxPlayers;
            if (gameStatusText != null) gameStatusText.text = (LevelCatalog.Get(session.SelectedLevelId)?.Title ?? "CO-OP") + "  " + count + "/" + MultiplayerConfig.MaxPlayers
                + "  |  WASD: move  |  Shift: sprint  |  Space: jump  |  LMB: fire  |  Esc: cursor";
            if (playerListText != null)
            {
                var text = new StringBuilder();
                if (room != null) foreach (RoomMember member in room.Members)
                    text.Append(member.Host ? "[HOST] " : "[CLIENT] ").Append(member.DisplayName.ToString())
                        .Append(member.Ready ? "  — READY" : "  — NOT READY").AppendLine();
                playerListText.text = text.ToString();
            }
            if (readyText != null && room != null && NetworkManager.Singleton != null)
                readyText.text = room.IsReady(NetworkManager.Singleton.LocalClientId) ? "SET NOT READY" : "READY";
            if (readyButton != null) readyButton.interactable = !busy && room != null && room.Phase.Value == MatchPhase.Lobby;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(session.IsHost);
                startButton.interactable = session.CanStartGame;
            }
        }
        private string StatusLabel()
        {
            switch (session.State)
            {
                case SessionState.Lobby: return session.IsHost ? "Waiting for Players — everyone must be Ready" : "Connected — waiting for Host";
                case SessionState.LoadingGame: return "Loading gameplay scene...";
                case SessionState.Initializing: return "Initializing Unity Services / Authentication...";
                case SessionState.Creating: return "Creating Relay room...";
                case SessionState.Joining: return "Joining Relay allocation...";
                case SessionState.Connecting: return "Awaiting confirmed NGO connection...";
                case SessionState.Leaving: return "Closing connection...";
                default: return "Enter the Host's six-character Join Code";
            }
        }
        private static void SetActive(GameObject panel, bool value) { if (panel != null) panel.SetActive(value); }
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy()
        {
            if (session != null) { session.Changed -= Refresh; session.ErrorRaised -= OnError; }
        }
    }
}
