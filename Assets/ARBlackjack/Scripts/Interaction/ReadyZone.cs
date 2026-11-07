using UnityEngine;
using ARBlackjack.Core;
using ARBlackjack.AR;
using Photon.Pun;
using ARBlackjack.Network;

namespace ARBlackjack.Interaction
{

    public class ReadyZone : MonoBehaviour, IInteractable, ICustomDwellInteractable
    {
        [SerializeField] private int _playerIndex;
        [SerializeField] private float _dwellDuration = 3f;
        [Tooltip("Scene label; its own text is the betting prompt (e.g. \"Ready?\"). Left empty: one is created.")]
        [SerializeField] private TMPro.TextMeshPro _label;
        private Renderer _markerRenderer;  // This object's renderer (the scene zone), or a created quad
        private Renderer _labelRenderer;
        private string _promptText = "Ready?";
        private PlayerController _player;
        private TableMarkerAnchor _table;
        private bool _visible;

        public int PlayerIndex => _playerIndex;
        public bool RepeatsWhileHeld => false;
        public float DwellDuration => _dwellDuration;
        private bool IsBetting =>
            GameManager.Instance != null &&
            GameManager.Instance.State == GameState.Betting;
        private bool IsRoundEnd =>
            GameManager.Instance != null &&
            GameManager.Instance.State == GameState.RoundEnd;
        private bool HasPlayer => _player != null;
        private bool IsConfirmed =>
            BettingController.Instance != null &&
            BettingController.Instance.IsConfirmed(_playerIndex);
        private bool IsLocalPlayerZone => NetworkConfig.IsLocalSeat(_playerIndex);

        public bool IsActive =>
            IsLocalPlayerZone &&
            HasPlayer &&
            !IsConfirmed &&
            ((IsBetting && _player.HasValidBet) ||
             (IsRoundEnd && GameManager.Instance.IsSeated(_playerIndex)));

        private string PromptText => IsRoundEnd ? "Next round?" : _promptText;
        private string ConfirmedText => IsRoundEnd ? "One more round" : "Ready";

        public void Configure(int playerIndex)
        {
            _playerIndex = playerIndex;
            BindPlayer();
            CreateVisuals();
            RefreshVisuals();
        }

        private void Start()
        {
            _table = GetComponentInParent<TableMarkerAnchor>();
            var collider = GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            CreateVisuals();
            BindPlayer();
            RefreshVisuals();
            GameManager.Instance?.OnStateChanged.AddListener(OnStateChanged);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.OnStateChanged.RemoveListener(OnStateChanged);
            if (_player != null) _player.OnBetChanged.RemoveListener(OnBetChanged);
        }

        private void LateUpdate() => ApplyVisibility();

        private void ApplyVisibility()
        {
            bool show = _visible && (_table == null || _table.IsTracked);
            if (_markerRenderer != null) _markerRenderer.enabled = show;
            if (_labelRenderer != null) _labelRenderer.enabled = show;
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() { OnDwellCancelled(); }

        public void OnSelect(int playerIndex)
        {
            if (playerIndex != _playerIndex) return;
            GameManager.Instance?.RequestConfirm(_playerIndex);
        }

        public void OnDwellProgress(float normalizedProgress)
        {
            if (_label == null) return;
            int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(_dwellDuration * (1f - normalizedProgress)), 1, Mathf.CeilToInt(_dwellDuration));
            _label.text = normalizedProgress >= 1f ? ConfirmedText : $"{secondsLeft}..";
        }

        public void OnDwellCancelled()
        {
            if (_label != null && IsActive)
                _label.text = PromptText;
        }

        private void OnStateChanged(GameState _) => RefreshVisuals();
        private void OnBetChanged() => RefreshVisuals();

        private void BindPlayer()
        {
            var player = GameManager.Instance?.GetPlayer(_playerIndex);
            if (player == _player) return;
            if (_player != null) _player.OnBetChanged.RemoveListener(OnBetChanged);
            _player = player;
            if (_player != null) _player.OnBetChanged.AddListener(OnBetChanged);
        }

        public void RefreshVisuals()
        {
            BindPlayer();
            bool confirmed = (IsBetting || IsRoundEnd) && HasPlayer && IsConfirmed;
            bool showPrompt = IsActive;
            _visible = IsLocalPlayerZone && (confirmed || showPrompt);
            if (_label != null)
            {
                if (confirmed) _label.text = ConfirmedText;
                else if (showPrompt) _label.text = PromptText;
            }
            ApplyVisibility();
        }

        // Scene zone: use its cube and label. Zone created at runtime (no label): build a green quad and a label.
        private void CreateVisuals()
        {
            if (_labelRenderer != null) return;
            if (_label != null)
            {
                _markerRenderer = GetComponent<Renderer>();
                _labelRenderer = _label.GetComponent<Renderer>();
                if (!string.IsNullOrEmpty(_label.text)) _promptText = _label.text;
                return;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "ReadyMarker";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = new Vector3(0.13f, 0.06f, 1f);
            _markerRenderer = marker.GetComponent<Renderer>();
            _markerRenderer.material.color = Color.green;
            Destroy(marker.GetComponent<Collider>());

            var labelObject = new GameObject("ReadyLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _label = labelObject.AddComponent<TMPro.TextMeshPro>();
            _label.text = PromptText;
            _label.fontSize = 0.32f;
            _label.alignment = TMPro.TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = Color.black;
            _label.rectTransform.sizeDelta = new Vector2(0.18f, 0.08f);
            _labelRenderer = _label.GetComponent<Renderer>();
        }
    }
}
