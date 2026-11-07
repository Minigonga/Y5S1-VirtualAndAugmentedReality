using UnityEngine;
using ARBlackjack.AR;
using ARBlackjack.Core;
using ARBlackjack.Network;

namespace ARBlackjack.Interaction
{

    public class UndoBetZone : MonoBehaviour, IInteractable, ICustomDwellInteractable
    {
        [SerializeField] private int _playerIndex;
        [SerializeField] private TMPro.TextMeshPro _label;
        [Tooltip("Seconds the hand marker must hold over the zone before chips start coming back.")]
        [SerializeField] private float _startDelay = 3f;

        private Renderer _renderer;
        private Renderer _labelRenderer;
        private TableMarkerAnchor _table;
        private string _promptText;
        private string _countdownText;

        public int PlayerIndex => _playerIndex;
        public bool RepeatsWhileHeld => true;
        public float DwellDuration => _startDelay;

        public bool IsActive =>
            GameManager.Instance != null &&
            NetworkConfig.IsLocalSeat(_playerIndex) &&
            GameManager.Instance.CanUndoBet(_playerIndex);

        private void Start()
        {
            var collider = GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            _renderer = GetComponent<Renderer>();
            if (_label != null)
            {
                _labelRenderer = _label.GetComponent<Renderer>();
                _promptText = _label.text;
            }
            _table = GetComponentInParent<TableMarkerAnchor>();
        }

        private void LateUpdate()
        {
            bool show = IsActive && (_table == null || _table.IsTracked);
            if (_renderer != null) _renderer.enabled = show;
            if (_labelRenderer != null) _labelRenderer.enabled = show;
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() => OnDwellCancelled();

        public void OnSelect(int playerIndex)
        {
            if (playerIndex != _playerIndex || !IsActive) return;
            GameManager.Instance.RequestUndoBet(_playerIndex);
        }

        public void OnDwellProgress(float normalizedProgress)
        {
            int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(_startDelay * (1f - normalizedProgress)), 1, Mathf.CeilToInt(_startDelay));
            _countdownText = normalizedProgress >= 1f ? null : $"{secondsLeft}..";
            RefreshLabel();
        }

        public void OnDwellCancelled()
        {
            _countdownText = null;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (_label != null) _label.text = _countdownText ?? _promptText;
        }
    }
}
