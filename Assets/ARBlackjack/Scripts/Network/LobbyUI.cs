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
    public class LobbyUI : MonoBehaviourPunCallbacks
    {
        [Header("Room Info")]
        [Tooltip("Every label that shows the room code (e.g. text + its shadow copy)")]
        [SerializeField] private TextMeshProUGUI[] _roomCodeLabels;
        [Tooltip("One label per player slot, in order")]
        [SerializeField] private TextMeshProUGUI[] _playerLabels;

        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _leaveButton;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == NetworkConfig.ManualScene)
                return;

            // Opened without a room (e.g. scene played directly in the editor)
            if (!PhotonNetwork.InRoom)
            {
                SceneManager.LoadScene(NetworkConfig.MainMenuScene);
                return;
            }

            _startButton.onClick.AddListener(OnStartClicked);
            if (_leaveButton != null) _leaveButton.onClick.AddListener(OnLeaveClicked);
            RefreshUI();
        }

        private void Update(){ if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) OnLeaveClicked();}

        private void RefreshUI()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            foreach (var label in _roomCodeLabels)
                if (label != null) label.text = $"Room ID:{PhotonNetwork.CurrentRoom.Name} - {GameModeRules.DisplayName(NetworkConfig.RoomMode)}";

            var players = PhotonNetwork.PlayerList;
            for (int i = 0; i < _playerLabels.Length; i++)
            {
                if (_playerLabels[i] == null) continue;
                _playerLabels[i].text = i < players.Length
                    ? $"{players[i].NickName}{(players[i].IsMasterClient ? " (Host)" : "")}{(players[i].IsLocal ? " - You" : "")}"
                    : "Waiting for player...";
            }

            _startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            _startButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount >= 1;
        }

        private void OnStartClicked()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            NetworkConfig.AssignSeats();
            _startButton.interactable = false;
            PhotonNetwork.LoadLevel(NetworkConfig.GameScene);
        }

        private void OnLeaveClicked() {if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();}
        public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshUI();
        public override void OnPlayerLeftRoom(Player otherPlayer)  => RefreshUI();
        public override void OnMasterClientSwitched(Player newMaster) => RefreshUI();
        public override void OnLeftRoom() => SceneManager.LoadScene(NetworkConfig.MainMenuScene);
        public override void OnDisconnected(DisconnectCause cause) => SceneManager.LoadScene(NetworkConfig.MainMenuScene);
    }
}
