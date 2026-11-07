using UnityEngine;
using ARBlackjack.Core;
using ARBlackjack.UI;

namespace ARBlackjack.Interaction
{
    public class ActionSelector : MonoBehaviour
    {
        [SerializeField] private PlayerAction _action;
        [SerializeField] private int _playerIndex;
        [SerializeField] private Renderer _buttonRenderer;
        [SerializeField] private Material _normalMat;
        [SerializeField] private Material _hoverMat;
        [SerializeField] private Material _disabledMat;

        [Header("Zone (on the table below the button)")]
        [SerializeField] private Vector2 _zoneSize = new(0.07f, 0.07f);
        [SerializeField] private float _zoneHeight = 0.003f;
        [SerializeField] private float _hoverHeight = 0.05f;
        [SerializeField] private float _dwellDuration = 3f;
        [Tooltip("Label that shows the 3.. 2.. 1.. countdown (left empty: the child TextMeshPro named \"label\").")]
        [SerializeField] private TMPro.TextMeshPro _label;
        private string _labelText;
        private ActionZone _zone;
        private Renderer _zoneRenderer;
        private TMPro.TextMeshPro _zoneLabel;
        private bool _hovered;

        private static readonly Quaternion FlatOnSeat = Quaternion.Euler(90f, 0f, 0f);

        public PlayerAction Action => _action;

        public bool IsActive =>
            GameManager.Instance != null &&
            GameManager.Instance.CurrentPlayerIndex == _playerIndex &&
            GameManager.Instance.IsActionAvailable(_action);

        private void Start()
        {
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
            if (_label == null)
                foreach (var t in GetComponentsInChildren<TMPro.TextMeshPro>(true))
                    if (t.name == "label") { _label = t; break; }
            if (_label != null) _labelText = _label.text;
            BuildZone();
        }

        private void OnEnable()
        {
            if (_zone != null) _zone.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_zone != null) _zone.gameObject.SetActive(false);
            _hovered = false;
        }

        private void OnDestroy()
        {
            if (_zone != null) Destroy(_zone.gameObject);
        }

        private void LateUpdate()
        {
            if (_zone == null) return;

            var seat = Seat();
            if (_zone.transform.parent != seat) _zone.transform.SetParent(seat, true);

            Vector3 up = seat != null ? seat.up : Vector3.up;
            Vector3 surface = seat != null ? seat.position : Vector3.zero;
            Vector3 p = transform.position;
            _zone.transform.position = p - up * Vector3.Dot(p - surface, up) + up * _zoneHeight;
            _zone.transform.rotation = seat != null ? seat.rotation : Quaternion.identity;

            var mat = _hovered && IsActive ? _hoverMat : IsActive ? _normalMat : _disabledMat;
            if (_buttonRenderer && mat) _buttonRenderer.sharedMaterial = mat;
            if (_zoneRenderer && mat) _zoneRenderer.sharedMaterial = mat;
        }

        private Transform Seat()
        {
            var panel = GetComponentInParent<GameOptionsPanel>(true);
            return panel != null ? panel.transform.parent : transform.parent;
        }


        public float DwellDuration => _dwellDuration;

        public void OnHoverEnter() => _hovered = true;
        public void OnHoverExit()
        {
            _hovered = false;
            ShowCountdown(1f);
        }

        public void ShowCountdown(float normalizedProgress)
        {
            if (_label == null) return;
            int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(_dwellDuration * (1f - normalizedProgress)), 1, Mathf.CeilToInt(_dwellDuration));
            _label.text = normalizedProgress >= 1f ? _labelText : $"{secondsLeft}..";
        }

        public void Select(int playerIndex)
        {
            if (playerIndex != _playerIndex || !IsActive) return;
            GameManager.Instance.PlayerAction(_playerIndex, _action);
        }


        private void BuildZone()
        {
            var root = new GameObject($"{_action}Zone");
            root.transform.SetParent(Seat(), false);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Area";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localRotation = FlatOnSeat;
            quad.transform.localScale = new Vector3(_zoneSize.x, _zoneSize.y, 1f);
            _zoneRenderer = quad.GetComponent<Renderer>();

            var label = new GameObject("Label").AddComponent<TMPro.TextMeshPro>();
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = Vector3.up * 0.001f;
            label.transform.localRotation = FlatOnSeat;
            label.rectTransform.sizeDelta = _zoneSize;
            _zoneLabel = label;
            label.text = _action == PlayerAction.DoubleDown ? "Double" : "Split";
            label.fontSize = 0.18f;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = Color.white;

            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(_zoneSize.x, _hoverHeight, _zoneSize.y);
            box.center = new Vector3(0f, _hoverHeight * 0.5f, 0f);

            _zone = root.AddComponent<ActionZone>();
            _zone.Init(this);
            root.SetActive(isActiveAndEnabled);
        }
    }
}
