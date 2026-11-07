using System.Collections;
using UnityEngine;
using ARBlackjack.Core;
using ARBlackjack.Interaction;
using Photon.Pun;
using ARBlackjack.Network;

namespace ARBlackjack.AR
{
    public class HandMarkerInteractor : MarkerEventHandler
    {
        [Header("Player Binding")]
        [SerializeField] private int _playerIndex;

        public int PlayerIndex => _playerIndex;

        private bool IsOwnedByLocalPlayer => NetworkConfig.IsLocalSeat(_playerIndex);

        [Header("Interaction")]
        [SerializeField] private float _hoverRadius = 0.12f;
        [SerializeField] private float _dwellTime = 1.5f;
        [SerializeField] private float _repeatInterval = 0.6f;
        [SerializeField] private LayerMask _interactableLayer;

        [Header("Feedback")]
        [SerializeField] private GameObject _hoverIndicator;
        [SerializeField] private UnityEngine.UI.Image _dwellFill; 

        private IInteractable _currentTarget;
        private Coroutine _dwellCoroutine;
        private bool _selectionDone;

        protected override bool CountsAsTracked(Vuforia.Status status) =>
            status == Vuforia.Status.TRACKED;

        private void Update()
        {
            if (!IsTracked || !IsOwnedByLocalPlayer) return;
            CheckProximity();
        }

        private void CheckProximity()
        {
            Collider[] hits = _interactableLayer == 0
                ? Physics.OverlapSphere(transform.position, _hoverRadius, Physics.AllLayers, QueryTriggerInteraction.Collide)
                : Physics.OverlapSphere(transform.position, _hoverRadius, _interactableLayer, QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            float bestDist = float.MaxValue;
            int bestPriority = int.MaxValue;

            foreach (var hit in hits)
            {
                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.IsActive) continue;
                if (interactable is BetSpot betSpot && betSpot.PlayerIndex != _playerIndex) continue;
                if (interactable is ReadyZone readyZone && readyZone.PlayerIndex != _playerIndex) continue;
                if (interactable is UndoBetZone undoZone && undoZone.PlayerIndex != _playerIndex) continue;

                float dist = Vector3.Distance(transform.position, hit.bounds.center);
                // Bet, undo and READY sit close together: the nearest one wins
                int priority = interactable is BetSpot or UndoBetZone or ReadyZone ? 0 : 1;
                if (priority < bestPriority || (priority == bestPriority && dist < bestDist))
                {
                    bestPriority = priority;
                    bestDist = dist;
                    nearest = interactable;
                }
            }

            if (nearest != _currentTarget)
            {
                if (_dwellCoroutine != null) StopCoroutine(_dwellCoroutine);

                if (!_selectionDone) _currentTarget?.OnHoverExit();
                _currentTarget = nearest;
                _selectionDone = false;

                if (_currentTarget != null)
                {
                    _currentTarget.OnHoverEnter();
                    SetIndicatorActive(true);
                    _dwellCoroutine = StartCoroutine(DwellRoutine());
                }
                else
                {
                    SetIndicatorActive(false);
                    SetDwellFill(0f);
                }
            }
        }

        private IEnumerator DwellRoutine()
        {
            var customDwell = _currentTarget as ICustomDwellInteractable;
            bool repeats = _currentTarget != null && _currentTarget.RepeatsWhileHeld;

            // Countdown first; a repeating target (the bet zone) only gets one when it asks for it
            if (!repeats || customDwell != null)
            {
                float dwellDuration = customDwell != null ? customDwell.DwellDuration : _dwellTime;
                float elapsed = 0f;
                while (elapsed < dwellDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / dwellDuration);
                    SetDwellFill(progress);
                    customDwell?.OnDwellProgress(progress);
                    yield return null;
                }
                SetDwellFill(1f);
            }

            if (repeats)
            {
                yield return ProximityRepeatRoutine();
                yield break;
            }

            _currentTarget?.OnSelect(_playerIndex);

            _currentTarget?.OnHoverExit();
            if (_currentTarget is ICustomDwellInteractable completedTarget)
                completedTarget.OnDwellProgress(1f);
            _selectionDone = true;
            _dwellCoroutine = null;
            SetIndicatorActive(false);
            SetDwellFill(0f);
        }

        private IEnumerator ProximityRepeatRoutine()
        {
            while (_currentTarget != null && _currentTarget.IsActive)
            {
                _currentTarget.OnSelect(_playerIndex);

                float remaining = _repeatInterval;
                while (remaining > 0f)
                {
                    SetDwellFill(remaining / _repeatInterval);
                    remaining -= Time.deltaTime;
                    yield return null;
                }
                SetDwellFill(0f);
            }

            _currentTarget?.OnHoverExit();
            if (_currentTarget is ICustomDwellInteractable cancelledTarget)
                cancelledTarget.OnDwellCancelled();
            _selectionDone = true;
            _dwellCoroutine = null;
            SetIndicatorActive(false);
            SetDwellFill(0f);
        }

        private void SetDwellFill(float value)
        {
            if (_dwellFill)
                _dwellFill.fillAmount = value;
        }

        private void SetIndicatorActive(bool active)
        {
            if (_hoverIndicator)
                _hoverIndicator.SetActive(active);
        }

        protected override void OnMarkerLost()
        {
            if (_dwellCoroutine != null) StopCoroutine(_dwellCoroutine);
            _dwellCoroutine = null;
            if (!_selectionDone) _currentTarget?.OnHoverExit();
            if (_currentTarget is ICustomDwellInteractable cancelledTarget)
                cancelledTarget.OnDwellCancelled();
            _currentTarget = null;
            _selectionDone = false;
            SetIndicatorActive(false);
            SetDwellFill(0f);
        }
    }
}
