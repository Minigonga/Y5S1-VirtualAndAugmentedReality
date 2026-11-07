using UnityEngine;
using ARBlackjack.AR;
using ARBlackjack.Core;
using ARBlackjack.Network;

namespace ARBlackjack.Interaction
{
    public class BetSpot : MonoBehaviour, IInteractable, ICustomDwellInteractable
    {
        [SerializeField] private int _playerIndex;
        [SerializeField] private PlayerChipDisplay _chipDisplay;
        [SerializeField] private TMPro.TextMeshPro _betLabel;
        [SerializeField] private TMPro.TextMeshPro _zoneLabel;
        [Tooltip("Where the BET label sits relative to the chip pile (metres).")]
        [SerializeField] private Vector3 _betLabelOffset = new(0f, 0.026f, -0.07f);
        [Tooltip("Seconds the hand marker must hold over the zone before chips start dropping.")]
        [SerializeField] private float _startDelay = 3f;

        private Renderer _zoneRenderer;
        private Renderer _zoneLabelRenderer;
        private TableMarkerAnchor _table;
        private bool _showZone;
        private bool _showPrompt;
        private string _promptText;
        private string _countdownText;

        private PlayerController _player;

        public int PlayerIndex => _playerIndex;

        public bool IsActive =>
            GameManager.Instance != null &&
            GameManager.Instance.State == GameState.Betting &&
            GameManager.Instance.IsSeated(_playerIndex) &&
            _player != null &&
            _player.Chips >= ChipSelector.Selected &&
            !(BettingController.Instance != null && BettingController.Instance.IsConfirmed(_playerIndex));

        public bool RepeatsWhileHeld => true;
        public float DwellDuration => _startDelay;

        private void Start()
        {
            GameManager.Instance?.OnStateChanged.AddListener(OnStateChanged);
            GameManager.Instance?.OnRoundResolved.AddListener(OnRoundResolved);
            BettingController.Instance?.OnConfirmationChanged.AddListener(UpdateLabel);
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
            _zoneRenderer = GetComponent<Renderer>();
            if (_zoneLabel != null)
            {
                _zoneLabelRenderer = _zoneLabel.GetComponent<Renderer>();
                _promptText = _zoneLabel.text;
            }
            _table = GetComponentInParent<TableMarkerAnchor>();
            SetupBetLabel();
            BindPlayer();
            UpdateLabel();
        }

        private void OnDestroy()
        {
            GameManager.Instance?.OnStateChanged.RemoveListener(OnStateChanged);
            GameManager.Instance?.OnRoundResolved.RemoveListener(OnRoundResolved);
            BettingController.Instance?.OnConfirmationChanged.RemoveListener(UpdateLabel);
            if (_player != null) _player.OnBetChanged.RemoveListener(OnBetChanged);
        }

        private void Update()
        {
            if (_player == null)
                BindPlayer();
            RefreshZoneVisibility();
        }

        private void OnStateChanged(GameState state)
        {
            BindPlayer();
            if (state == GameState.MainMenu)
                _chipDisplay?.Clear();
            else if (_player != null)
                _chipDisplay?.SetAmount(_player.TotalBet);
            UpdateLabel();
        }

        private void OnRoundResolved(HandResult[] results, int[] payouts)
        {
            if (_chipDisplay == null || _player == null || _playerIndex >= payouts.Length) return;
            if (!GameManager.Instance.IsSeated(_playerIndex)) return;

            int bet = _player.TotalBet;
            int payout = payouts[_playerIndex]; // Includes the stake
            if (bet <= 0) return;

            if (payout == 0)
                _chipDisplay.TakeBet();
            else if (payout > bet)
                _chipDisplay.AddWinnings(payout - bet);
        }

        private void BindPlayer()
        {
            var p = GameManager.Instance?.GetPlayer(_playerIndex);
            if (p == _player) return;
            if (_player != null) _player.OnBetChanged.RemoveListener(OnBetChanged);
            _player = p;
            if (_player != null)
                _player.OnBetChanged.AddListener(OnBetChanged);
        }


        public void OnHoverEnter() { }
        public void OnHoverExit()  => OnDwellCancelled();

        public void OnDwellProgress(float normalizedProgress)
        {
            int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(_startDelay * (1f - normalizedProgress)), 1, Mathf.CeilToInt(_startDelay));
            _countdownText = normalizedProgress >= 1f ? null : $"{secondsLeft}..";
            RefreshZoneLabel();
        }

        public void OnDwellCancelled()
        {
            _countdownText = null;
            RefreshZoneLabel();
        }

        public void OnSelect(int playerIndex)
        {
            if (playerIndex != _playerIndex || !IsActive) return;

            GameManager.Instance.RequestBetChip(_playerIndex, ChipSelector.Selected);
        }


        private void OnBetChanged()
        {
            if (_player != null)
                _chipDisplay?.SetAmount(_player.TotalBet);
            UpdateLabel();
        }

        private void SetupBetLabel()
        {
            if (_betLabel != null) return;
            var go = new GameObject("BetLabel");
            var labelParent = _chipDisplay != null ? _chipDisplay.transform : transform;
            go.transform.SetParent(labelParent, false);
            go.transform.SetPositionAndRotation(
                labelParent.position + labelParent.rotation * _betLabelOffset,
                labelParent.rotation * Quaternion.Euler(90f, 0f, 0f));
            _betLabel = go.AddComponent<TMPro.TextMeshPro>();
            _betLabel.fontSize = 0.3f;
            _betLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _betLabel.color = Color.yellow;
            _betLabel.rectTransform.sizeDelta = new Vector2(0.15f, 0.03f);
        }

        private void UpdateLabel()
        {
            int bet = _player != null ? _player.TotalBet : 0;
            bool confirmed = BettingController.Instance != null && BettingController.Instance.IsConfirmed(_playerIndex);
            if (_betLabel != null)
                _betLabel.text = bet > 0 ? (confirmed ? $"Bet: {bet}$ ✓" : $"Bet: {bet}$") : "";

            var gm = GameManager.Instance;
            _showZone = gm != null && gm.State == GameState.Betting && gm.IsSeated(_playerIndex) && !confirmed &&
                        NetworkConfig.IsLocalSeat(_playerIndex);
            _showPrompt = _showZone; // "Bet Here" stays up until the seat is READY
            RefreshZoneVisibility();
        }

        public void RefreshLabel() => UpdateLabel();

        private void RefreshZoneVisibility()
        {
            bool tracked = _table == null || _table.IsTracked;
            if (_zoneRenderer != null) _zoneRenderer.enabled = tracked && _showZone;
            if (_zoneLabelRenderer != null) _zoneLabelRenderer.enabled = tracked && (_showPrompt || _countdownText != null);
        }

        private void RefreshZoneLabel()
        {
            if (_zoneLabel != null) _zoneLabel.text = _countdownText ?? _promptText;
            RefreshZoneVisibility();
        }
    }
}
