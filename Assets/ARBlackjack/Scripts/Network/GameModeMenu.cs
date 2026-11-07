using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ARBlackjack.Core;

namespace ARBlackjack.Network
{
    public class GameModeMenu : MonoBehaviourPunCallbacks
    {
        [Header("One button per game mode")]
        [SerializeField] private Button _classicButton;
        [SerializeField] private Button _doubleExposureButton;
        [SerializeField] private Button _spanish21Button;
        [SerializeField] private Button _highStakesButton;

        [Tooltip("Optional: shows progress and errors (e.g. \"Creating room...\").")]
        [SerializeField] private TMP_Text _statusLabel;

        private const int MaxCreateAttempts = 5;

        private bool _wantsToCreate;
        private int _createAttempts;
        private GameMode _mode;

        private void Start()
        {
            NetworkConfig.Init();
            EnsureEventSystem();

            Bind(_classicButton, GameMode.Classic);
            Bind(_doubleExposureButton, GameMode.DoubleExposure);
            Bind(_spanish21Button, GameMode.Spanish21);
            Bind(_highStakesButton, GameMode.HighStakes);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !_wantsToCreate)
                SceneManager.LoadScene(NetworkConfig.MainMenuScene);
        }

        private void Bind(Button button, GameMode mode)
        {
            if (button != null) button.onClick.AddListener(() => OnModeClicked(mode));
        }

        private void OnModeClicked(GameMode mode)
        {
            if (_wantsToCreate) return;
            _mode = mode;
            GameModeRules.Selected = mode;
            _wantsToCreate = true;
            _createAttempts = 0;
            SetButtonsInteractable(false);
            ShowStatus($"Creating {GameModeRules.DisplayName(mode)} room...");

            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
                CreateRoom();
            else if (!PhotonNetwork.IsConnected)
                PhotonNetwork.ConnectUsingSettings();
        }

        private void CreateRoom()
        {
            _createAttempts++;
            PhotonNetwork.CreateRoom(NetworkConfig.GenerateRoomCode(), NetworkConfig.CreateRoomOptions(_mode));
        }


        public override void OnConnectedToMaster()
        {
            if (_wantsToCreate) CreateRoom();
        }

        public override void OnJoinedRoom()
        {
            _wantsToCreate = false;
            PhotonNetwork.LoadLevel(NetworkConfig.LobbyScene);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (returnCode == ErrorCode.GameIdAlreadyExists && _createAttempts < MaxCreateAttempts)
            {
                CreateRoom();
                return;
            }

            Debug.LogWarning($"[GameModeMenu] Create room failed: {message}");
            Fail("Could not create the room");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[GameModeMenu] Disconnected: {cause}");
            if (_wantsToCreate) Fail("Connection failed");
        }


        private void Fail(string message)
        {
            _wantsToCreate = false;
            SetButtonsInteractable(true);
            ShowStatus(message);
        }

        private void SetButtonsInteractable(bool value)
        {
            foreach (var b in new[] { _classicButton, _doubleExposureButton, _spanish21Button, _highStakesButton })
                if (b != null) b.interactable = value;
        }

        private void ShowStatus(string message)
        {
            if (_statusLabel != null) _statusLabel.text = message;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
