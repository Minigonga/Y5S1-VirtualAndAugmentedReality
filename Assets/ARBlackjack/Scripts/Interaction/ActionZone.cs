using UnityEngine;

namespace ARBlackjack.Interaction
{
    public class ActionZone : MonoBehaviour, IInteractable, ICustomDwellInteractable
    {
        private ActionSelector _owner;

        public void Init(ActionSelector owner) => _owner = owner;

        public bool IsActive => _owner != null && _owner.IsActive;
        public bool RepeatsWhileHeld => false;

        public void OnHoverEnter() => _owner?.OnHoverEnter();
        public void OnHoverExit()  => _owner?.OnHoverExit();
        public void OnSelect(int playerIndex) => _owner?.Select(playerIndex);

        public float DwellDuration => _owner != null ? _owner.DwellDuration : 3f;
        public void OnDwellProgress(float normalizedProgress) => _owner?.ShowCountdown(normalizedProgress);
        public void OnDwellCancelled() => _owner?.ShowCountdown(1f);
    }
}
