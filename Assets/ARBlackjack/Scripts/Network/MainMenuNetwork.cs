using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ARBlackjack.Network
{
    public class MainMenuNetwork : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _manualButton;

        private void Start()
        {
            NetworkConfig.Init();

            _createButton.onClick.AddListener(OnCreateClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
            if (_manualButton != null) _manualButton.onClick.AddListener(OnManualClicked);
        }

        private void OnCreateClicked()
        {
            SceneManager.LoadScene(NetworkConfig.SettingsScene);
        }

        private void OnJoinClicked()
        {
            SceneManager.LoadScene(NetworkConfig.InsertRoomIdScene);
        }

        private void OnManualClicked()
        {
            SceneManager.LoadScene(NetworkConfig.ManualScene);
        }

    }
}
