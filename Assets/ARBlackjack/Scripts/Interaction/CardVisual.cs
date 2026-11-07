using System.Collections;
using UnityEngine;
using ARBlackjack.Core;

namespace ARBlackjack.Interaction
{

    public class CardVisual : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private Transform _cardRoot;
        [SerializeField] private Renderer _frontRenderer;
        [SerializeField] private Renderer _backRenderer;

        [Header("Animation")]
        [SerializeField] private float _flipDuration = 0.4f;
        [SerializeField] private float _dealDuration = 0.3f;
        [SerializeField] private AnimationCurve _dealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Card _card;
        private bool _isFaceUp;
        private Coroutine _flipRoutine;
        private Quaternion _baseRotation;
        private Vector3 _targetLocal;
        private bool _dealing;

        public Card Card => _card;
        public bool IsFaceUp => _isFaceUp;

        private Transform Root => _cardRoot != null ? _cardRoot : transform;

        public void Initialize(Card card, bool faceUp, Vector3 fromPosition)
        {
            _card = card;
            _isFaceUp = faceUp;
            _baseRotation = Root.localRotation;
            _targetLocal = transform.localPosition;
            ApplyCardTexture();
            ShowFace(faceUp);
            StartCoroutine(DealAnimRoutine(fromPosition));
        }

        public void SetTargetPosition(Vector3 worldPosition)
        {
            _targetLocal = transform.parent != null
                ? transform.parent.InverseTransformPoint(worldPosition)
                : worldPosition;
            if (!_dealing) transform.localPosition = _targetLocal;
        }

        public void Flip()
        {
            if (_flipRoutine != null) StopCoroutine(_flipRoutine);
            _flipRoutine = StartCoroutine(FlipRoutine());
        }

        private IEnumerator DealAnimRoutine(Vector3 fromWorld)
        {
            _dealing = true;
            Vector3 from = transform.parent != null ? transform.parent.InverseTransformPoint(fromWorld) : fromWorld;
            transform.localPosition = from;
            float elapsed = 0f;
            while (elapsed < _dealDuration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.LerpUnclamped(from, _targetLocal, _dealCurve.Evaluate(elapsed / _dealDuration));
                yield return null;
            }
            transform.localPosition = _targetLocal;
            _dealing = false;
        }

        private IEnumerator FlipRoutine()
        {
            float from = FaceAngle(_isFaceUp);
            _isFaceUp = !_isFaceUp;
            yield return RotateCard(from, FaceAngle(_isFaceUp), _flipDuration);
            ShowFace(_isFaceUp);
        }

        private IEnumerator RotateCard(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float angle = Mathf.Lerp(from, to, elapsed / duration);
                Root.localRotation = _baseRotation * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }
        }

        private static float FaceAngle(bool faceUp) => faceUp ? 0f : 180f;

        private void ShowFace(bool faceUp)
        {
            if (_frontRenderer != null) _frontRenderer.enabled = true;
            if (_backRenderer != null) _backRenderer.enabled = true;
            Root.localRotation = _baseRotation * Quaternion.Euler(0f, FaceAngle(faceUp), 0f);
        }

        private void ApplyCardTexture()
        {
            var faceTex = Resources.Load<Texture2D>($"Cards/{_card.AssetKey}");
            var backTex = Resources.Load<Texture2D>("Cards/back");

            if (_frontRenderer != null && faceTex != null)
                _frontRenderer.material.mainTexture = faceTex;
            if (_backRenderer != null && backTex != null)
                _backRenderer.material.mainTexture = backTex;
        }
    }
}
