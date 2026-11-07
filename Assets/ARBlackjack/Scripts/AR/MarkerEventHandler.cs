using UnityEngine;
using Vuforia;

namespace ARBlackjack.AR
{
    [RequireComponent(typeof(ObserverBehaviour))]
    public class MarkerEventHandler : MonoBehaviour
    {
        public bool IsTracked { get; private set; }

        protected ObserverBehaviour _observer;

        protected virtual void Awake()
        {
            _observer = GetComponent<ObserverBehaviour>();
        }

        protected virtual void Start()
        {
            SetContentVisible(false);
            _observer.OnTargetStatusChanged += HandleStatusChanged;
            HandleStatusChanged(_observer, _observer.TargetStatus);
        }

        protected virtual void OnDestroy()
        {
            if (_observer != null)
                _observer.OnTargetStatusChanged -= HandleStatusChanged;
        }

        protected virtual bool CountsAsTracked(Status status) =>
            status == Status.TRACKED || status == Status.EXTENDED_TRACKED;

        private void HandleStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
        {
            bool nowTracked = CountsAsTracked(status.Status);

            if (nowTracked && !IsTracked)
            {
                IsTracked = true;
                SetContentVisible(true);
                OnMarkerFound();
            }
            else if (!nowTracked && IsTracked)
            {
                IsTracked = false;
                SetContentVisible(false);
                OnMarkerLost();
            }
        }

        protected virtual void OnMarkerFound() { }
        protected virtual void OnMarkerLost()  { }

        public void RefreshContentVisibility() => SetContentVisible(IsTracked);
        private void SetContentVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = visible;
            foreach (var c in GetComponentsInChildren<Canvas>(true))   c.enabled = visible;
        }
    }
}
