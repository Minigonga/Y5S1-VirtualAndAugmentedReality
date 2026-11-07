using UnityEngine;
using UnityEngine.InputSystem;
using ARBlackjack.Core;
using Gyro = UnityEngine.InputSystem.Gyroscope;

namespace ARBlackjack.Interaction
{
    public class PhoneGestureInput : MonoBehaviour
    {
        // Hit = flick the phone forward and back (tilt the top away and back);
        // Stand = flick it sideways and back. Two swooshes on the same axis, in either direction.
        [Header("Swoosh")]
        [Tooltip("Rotation speed, in rad/s, that starts one swoosh.")]
        [SerializeField] private float _swooshThreshold = 2.5f;
        [Tooltip("Rotation speed, in rad/s, below which the current swoosh is over.")]
        [SerializeField] private float _releaseThreshold = 1f;
        [Tooltip("The swoosh axis must spin this many times faster than the other axis.")]
        [SerializeField] private float _dominance = 1.5f;
        [Tooltip("Both swooshes must happen within this many seconds.")]
        [SerializeField] private float _doubleWindow = 1.2f;

        [Header("Timing")]
        [Tooltip("No new gesture is read for this long after one fires.")]
        [SerializeField] private float _cooldown = 1f;

        private enum Axis { None, Forward, Sideways }

        private float _cooldownUntil;
        private bool _swooshActive;

        private Axis _lastAxis;
        private float _lastTime;

        private void OnEnable()
        {
            if (Gyro.current != null)
                InputSystem.EnableDevice(Gyro.current);
        }

        private void Update() => ReadGyroscope();


        private void ReadGyroscope()
        {
            var gyro = Gyro.current;
            if (gyro == null || !gyro.enabled) return;

            Vector3 w = gyro.angularVelocity.ReadValue();

            if (_swooshActive)
            {
                if (w.magnitude < _releaseThreshold) _swooshActive = false;
                return;
            }
            if (Time.time < _cooldownUntil) return;

            float forward = w.x;
            float sideways = Mathf.Abs(w.y) >= Mathf.Abs(w.z) ? w.y : w.z;

            Axis axis = Axis.None;
            float sign = 0f;
            if (Mathf.Abs(forward) > _swooshThreshold && Mathf.Abs(forward) > Mathf.Abs(sideways) * _dominance)
            {
                axis = Axis.Forward;
                sign = Mathf.Sign(forward);
            }
            else if (Mathf.Abs(sideways) > _swooshThreshold && Mathf.Abs(sideways) > Mathf.Abs(forward) * _dominance)
            {
                axis = Axis.Sideways;
                sign = Mathf.Sign(sideways);
            }
            if (axis == Axis.None) return;

            _swooshActive = true;
            OnSwoosh(axis, sign);
        }

        private void OnSwoosh(Axis axis, float sign)
        {
            // The second swoosh may go either way, so the natural return stroke counts
            bool sameAxis = axis == _lastAxis && Time.time - _lastTime <= _doubleWindow;

            if (sameAxis)
            {
                ResetSwoosh();
                Fire(axis == Axis.Forward ? Hit : Stand);
                return;
            }

            _lastAxis = axis;
            _lastTime = Time.time;
        }

        private void ResetSwoosh()
        {
            _lastAxis = Axis.None;
        }

        private void Fire(System.Action gesture)
        {
            _cooldownUntil = Time.time + _cooldown;
            gesture();
        }


        private static void Hit()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.PlayerTurns) return;
            gm.PlayerAction(PlayerAction.Hit);
        }

        private static void Stand()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.State == GameState.Betting)
                gm.ConfirmBets();
            else if (gm.State == GameState.PlayerTurns)
                gm.PlayerAction(PlayerAction.Stand);
        }
    }
}
