using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

namespace ARBlackjack.Network
{
    public class JoinRoomNetwork : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TMP_InputField _codeInput;
        [SerializeField] private Button _searchButton;
        [SerializeField] private Button _returnButton;

        private bool _wantsToJoin;
        private string _defaultPlaceholder;

        private TMP_Text Placeholder => _codeInput.placeholder as TMP_Text;

        private void Start()
        {
            NetworkConfig.Init();

            if (Placeholder != null) _defaultPlaceholder = Placeholder.text;
            _searchButton.onClick.AddListener(OnSearchClicked);
            _codeInput.onSubmit.AddListener(_ => OnSearchClicked());
            if (_returnButton != null) _returnButton.onClick.AddListener(OnReturnClicked);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !_wantsToJoin)
                OnReturnClicked();
        }

        private void OnReturnClicked()
        {
            if (_wantsToJoin) return;
            SceneManager.LoadScene(NetworkConfig.MainMenuScene);
        }

        private void OnSearchClicked()
        {
            if (_wantsToJoin) return;

            string code = _codeInput.text.Trim().ToUpper();
            if (code.Length == 0)
            {
                ShowMessage("Enter a room ID");
                return;
            }

            _wantsToJoin = true;
            _searchButton.interactable = false;
            ShowMessage(_defaultPlaceholder);

            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
                JoinRoom();
            else if (!PhotonNetwork.IsConnected)
                PhotonNetwork.ConnectUsingSettings();
        }

        private void JoinRoom()
        {
            string code = _codeInput.text.Trim().ToUpper();
            Debug.Log($"[JoinRoomNetwork] Joining room {code}");
            PhotonNetwork.JoinRoom(code);
        }


        public override void OnConnectedToMaster(){if (_wantsToJoin) JoinRoom();}

        public override void OnJoinedRoom()
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("curScn"))
                SceneManager.LoadScene(NetworkConfig.LobbyScene);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[JoinRoomNetwork] Join failed ({returnCode}): {message}");
            switch (returnCode)
            {
                case ErrorCode.GameDoesNotExist: Fail("Room not found");       break;
                case ErrorCode.GameFull:         Fail("Room is full");         break;
                case ErrorCode.GameClosed:       Fail("Game already started"); break;
                default:                         Fail("Could not join room");  break;
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            if (_wantsToJoin) Fail("Connection failed");
        }

        private void Fail(string msg)
        {
            _wantsToJoin = false;
            _searchButton.interactable = true;
            _codeInput.text = string.Empty;
            ShowMessage(msg);
        }

        private void ShowMessage(string msg){if (Placeholder != null) Placeholder.text = msg;}
    }
}
