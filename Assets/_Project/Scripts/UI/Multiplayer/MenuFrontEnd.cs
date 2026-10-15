using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    public enum MenuPage { Home, Levels, Join, Lobby, Controls, Error }

    public sealed class MenuFrontEnd : MonoBehaviour
    {
        public RectTransform safeFrame;
        public GameObject homePage, levelsPage, joinPage, lobbyPage, controlsPage, errorPage, busyOverlay;
        public Button playButton, openJoinButton, controlsButton, quitButton, levelsBackButton, confirmLevelButton,
            joinBackButton, joinButton, copyButton, changeLevelButton, readyButton, startButton, leaveButton,
            controlsBackButton, errorBackButton;
        public Button[] levelButtons;
        public Image[] selectionStrips;
        public TMP_Text[] selectionBadges, rosterNames, rosterStates;
        public TMP_InputField joinInput;
        public TMP_Text breadcrumb, detailTitle, detailDescription, confirmLevelLabel, joinHint, roomCode,
            playerCount, lobbyLevelTitle, lobbyLevelSummary, readyLabel, lobbyHint, errorMessage, busyLabel, copyLabel;
        public MenuPage Page { get; private set; }
        public int DraftLevel { get; private set; }
        private MultiplayerSession session;
        private bool copied;
        private string localError;
        private Vector2 lastCanvasSize;

        private void LateUpdate()
        {
            if (safeFrame == null) return;
            Vector2 size = ((RectTransform)transform).rect.size;
            if (size == lastCanvasSize) return;
            lastCanvasSize = size;
            float scale = Mathf.Min(1, (size.x - 80) / 1680, (size.y - 70) / 920);
            safeFrame.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        private void Start()
        {
            session = MultiplayerSession.Instance;
            if (session == null) { Debug.LogError("Menu requires MultiplayerSession."); enabled = false; return; }
            playButton.onClick.AddListener(ShowLevels);
            openJoinButton.onClick.AddListener(() => Show(MenuPage.Join));
            controlsButton.onClick.AddListener(() => Show(MenuPage.Controls));
            quitButton.onClick.AddListener(Quit);
            levelsBackButton.onClick.AddListener(() => Show(session.Room != null ? MenuPage.Lobby : MenuPage.Home));
            confirmLevelButton.onClick.AddListener(ConfirmLevel);
            joinBackButton.onClick.AddListener(() => Show(MenuPage.Home));
            joinButton.onClick.AddListener(() => Run(session.JoinRoomAsync(joinInput.text)));
            copyButton.onClick.AddListener(() => { GUIUtility.systemCopyBuffer = session.JoinCode; copied = true; Refresh(); });
            changeLevelButton.onClick.AddListener(ShowLevels);
            readyButton.onClick.AddListener(() =>
            {
                var room = session.Room;
                if (room != null && !session.IsBusy) room.SetReadyRpc(!room.IsReady(NetworkManager.Singleton.LocalClientId));
            });
            startButton.onClick.AddListener(session.StartGame);
            leaveButton.onClick.AddListener(() => Run(session.LeaveRoomAsync()));
            controlsBackButton.onClick.AddListener(() => Show(MenuPage.Home));
            errorBackButton.onClick.AddListener(() =>
            {
                localError = null;
                if (session.State == SessionState.Lobby) Show(MenuPage.Lobby);
                else Run(session.LeaveRoomAsync());
            });
            for (int i = 0; i < levelButtons.Length; i++)
            { int id = i; levelButtons[i].onClick.AddListener(() => { DraftLevel = id; Refresh(); }); }
            joinInput.onValueChanged.AddListener(OnJoinInput);
            session.Changed += Refresh; session.ErrorRaised += OnError;
            DraftLevel = session.SelectedLevelId;
            Refresh();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MenuPreviewHarness.TryStart(this);
#endif
        }
        public void ShowLevels()
        {
            if (session.IsBusy || (session.Room != null && !session.IsHost)) return;
            DraftLevel = session.SelectedLevelId; Show(MenuPage.Levels);
        }
        private void ConfirmLevel()
        {
            if (!session.TrySelectLevel(DraftLevel)) return;
            if (session.Room != null) Show(MenuPage.Lobby);
            else Run(session.CreateRoomAsync());
        }
        private void OnJoinInput(string value)
        {
            string normalized = ConnectionPolicy.NormalizeJoinCode(value);
            if (joinInput.text != normalized) joinInput.SetTextWithoutNotify(normalized);
            RefreshJoin();
        }
        private void RefreshJoin()
        {
            bool valid = ConnectionPolicy.IsValidJoinCode(joinInput.text);
            joinButton.interactable = !session.IsBusy && valid;
            joinHint.text = valid ? "CODE READY / CONNECT TO THE HOST" : "Enter the Host's 6-character room code.";
        }
        private async void Run(Task task)
        {
            try
            {
                await task;
                if (session.State == SessionState.Lobby) { copied = false; Show(MenuPage.Lobby); }
                else if (session.State == SessionState.Offline) Show(MenuPage.Home);
                else Refresh();
            }
            catch (Exception exception) { OnError(exception.Message); }
        }
        private void OnError(string message) { localError = message; Show(MenuPage.Error); }
        private void Show(MenuPage value) { Page = value; Refresh(); }
        private void Refresh()
        {
            if (session == null) return;
            if (session.State == SessionState.Error) { Page = MenuPage.Error; localError = session.LastError; }
            else if (session.Room != null && (Page == MenuPage.Home || Page == MenuPage.Join)) Page = MenuPage.Lobby;
            else if (session.State == SessionState.Offline && Page == MenuPage.Lobby) Page = MenuPage.Home;
            homePage.SetActive(Page == MenuPage.Home); levelsPage.SetActive(Page == MenuPage.Levels);
            joinPage.SetActive(Page == MenuPage.Join); lobbyPage.SetActive(Page == MenuPage.Lobby);
            controlsPage.SetActive(Page == MenuPage.Controls); errorPage.SetActive(Page == MenuPage.Error);
            breadcrumb.text = "LAST PROTOCOL  /  " + (Page == MenuPage.Levels ? "OPERATIONS" : Page.ToString().ToUpperInvariant());
            bool busy = session.IsBusy || session.State == SessionState.LoadingGame;
            busyOverlay.SetActive(busy);
            busyLabel.text = session.State == SessionState.LoadingGame ? "DEPLOYING SQUAD..."
                : session.State == SessionState.Leaving ? "CLOSING SESSION..." : "CONNECTING...\nPlease wait";
            Button[] actions = { playButton, openJoinButton, controlsButton, quitButton, levelsBackButton, confirmLevelButton,
                joinBackButton, copyButton, changeLevelButton, readyButton, startButton, leaveButton, controlsBackButton, errorBackButton };
            foreach (var button in actions) button.interactable = !busy;
            joinInput.interactable = !busy; RefreshJoin();
            var detail = LevelCatalog.Get(DraftLevel) ?? LevelCatalog.Get(LevelCatalog.Training);
            detailTitle.text = detail.Title; detailDescription.text = detail.Briefing;
            confirmLevelLabel.text = session.Room != null ? "CONFIRM OPERATION  >" : "CREATE ROOM  >";
            confirmLevelButton.interactable = !busy && (session.Room == null || session.IsHost) && LevelCatalog.IsValid(DraftLevel);
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool selected = i == DraftLevel;
                levelButtons[i].interactable = !busy && (session.Room == null || session.IsHost);
                selectionStrips[i].color = selected ? LevelCatalog.Get(i).Accent : new Color(0.16f, 0.23f, 0.29f);
                selectionBadges[i].text = selected ? "SELECTED" : "AVAILABLE";
                selectionBadges[i].color = selected ? LevelCatalog.Get(i).Accent : new Color(0.53f, 0.64f, 0.69f);
            }
            var room = session.Room;
            var selectedLevel = LevelCatalog.Get(session.SelectedLevelId);
            lobbyLevelTitle.text = selectedLevel?.Title ?? "NO OPERATION";
            lobbyLevelSummary.text = selectedLevel?.Summary ?? "";
            roomCode.text = string.IsNullOrEmpty(session.JoinCode) ? "------" : session.JoinCode;
            copyLabel.text = copied ? "COPIED" : "COPY CODE";
            playerCount.text = (room?.Members.Count ?? 0) + " / " + MultiplayerConfig.MaxPlayers + " AGENTS";
            for (int slot = 0; slot < MultiplayerConfig.MaxPlayers; slot++)
            {
                rosterNames[slot].text = "OPEN SLOT"; rosterStates[slot].text = "Waiting for teammate";
                rosterStates[slot].color = new Color(0.43f, 0.53f, 0.6f);
                if (room != null) foreach (var member in room.Members)
                    if (member.Slot == slot)
                    {
                        rosterNames[slot].text = member.DisplayName.ToString() + (member.Host ? "  / HOST" : "  / CLIENT");
                        rosterStates[slot].text = member.Ready ? "READY" : "NOT READY";
                        rosterStates[slot].color = member.Ready ? new Color(0.23f, 0.9f, 0.67f) : new Color(1, 0.68f, 0.34f);
                    }
            }
            bool ready = room != null && NetworkManager.Singleton != null && room.IsReady(NetworkManager.Singleton.LocalClientId);
            readyLabel.text = ready ? "CANCEL READY" : "READY UP";
            readyButton.interactable = !busy && room != null && room.Phase.Value == MatchPhase.Lobby;
            changeLevelButton.gameObject.SetActive(session.IsHost);
            startButton.gameObject.SetActive(session.IsHost); startButton.interactable = !busy && session.CanStartGame;
            int minimum = session.config.minimumPlayersToStart;
            lobbyHint.text = !session.IsHost ? "The Host chooses the operation. Ready up to deploy."
                : room != null && room.Members.Count < minimum ? "Waiting for at least " + minimum + " agents to deploy."
                : session.CanStartGame ? "Squad ready. Deploy when you are ready."
                : "Every agent must be READY. Changing operation resets Ready.";
            errorMessage.text = localError ?? session.LastError;
        }
        private void OnDestroy()
        { if (session != null) { session.Changed -= Refresh; session.ErrorRaised -= OnError; } }
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
