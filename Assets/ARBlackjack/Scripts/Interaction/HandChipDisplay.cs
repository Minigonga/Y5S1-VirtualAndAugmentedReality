using System.Collections.Generic;
using UnityEngine;
using ARBlackjack.AR;
using ARBlackjack.Core;

namespace ARBlackjack.Interaction
{

    [RequireComponent(typeof(HandMarkerInteractor))]
    public class HandChipDisplay : MonoBehaviour
    {
        [Header("Chip Prefabs (100, 200, 500, 1000, 2000)")]
        [SerializeField] private GameObject[] _chipPrefabs;

        [Header("Layout (marker-local)")]
        [SerializeField] private float _chipScale = 1f;
        [SerializeField, Min(1)] private int _maxStack = 10;
        [SerializeField] private Vector3 _stackOffset = new(0f, 0.002f, 0f);
        [SerializeField] private Vector3 _labelOffset = new(0f, 0.002f, -0.045f);

        private HandMarkerInteractor _marker;
        private PlayerController _player;
        private TMPro.TextMeshPro _label;
        private readonly List<GameObject> _stack = new();
        private int _stackDenomination = -1;

        private static readonly Quaternion FlatOnMarker = Quaternion.Euler(90f, 0f, 0f);

        private void Awake() => _marker = GetComponent<HandMarkerInteractor>();

        private void Start()
        {
            CreateLabel();
            ChipSelector.OnChanged += OnDenominationChanged;
            GameManager.Instance?.OnStateChanged.AddListener(OnStateChanged);
            BindPlayer();
            Refresh();
        }

        private void OnDestroy()
        {
            ChipSelector.OnChanged -= OnDenominationChanged;
            GameManager.Instance?.OnStateChanged.RemoveListener(OnStateChanged);
            if (_player != null) _player.OnChipsChanged.RemoveListener(OnChipsChanged);
        }

        private void OnDenominationChanged(int _) => Refresh();
        private void OnChipsChanged(int _) => Refresh();

        private void OnStateChanged(GameState _)
        {
            BindPlayer();
            Refresh();
        }

        private void BindPlayer()
        {
            var p = GameManager.Instance?.GetPlayer(_marker.PlayerIndex);
            if (p == _player) return;
            if (_player != null) _player.OnChipsChanged.RemoveListener(OnChipsChanged);
            _player = p;
            if (_player != null) _player.OnChipsChanged.AddListener(OnChipsChanged);
        }

        private void Refresh()
        {
            ClearStack();
            if (_chipPrefabs != null && _chipPrefabs.Length > 0)
            {
                int selectedIndex = Mathf.Clamp(ChipSelector.SelectedIndex, 0, _chipPrefabs.Length - 1);
                GameObject chip = SpawnChip(selectedIndex);
                if (chip != null)
                    _stack.Add(chip);
                _stackDenomination = ChipSelector.Selected;
            }
            if (_label != null)
                _label.text = _player != null ? $"${_player.Chips}" : string.Empty;
            _marker.RefreshContentVisibility();
        }

        private GameObject SpawnChip(int index)
        {
            if (_chipPrefabs == null || index >= _chipPrefabs.Length || _chipPrefabs[index] == null) return null;

            var chip = Instantiate(_chipPrefabs[index], transform);
            chip.transform.localScale = Vector3.one * _chipScale;
            float height = ChipPivot.LocalHeight(chip) * _chipScale;
            chip.transform.localPosition = _stackOffset + Vector3.up * (0.01f + _stack.Count * height);
            chip.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Turned to face the player holding the marker
            ChipPivot.Recenter(chip); // After the rotation, so the chip stays centred on the marker
            foreach (var col in chip.GetComponentsInChildren<Collider>()) Destroy(col);
            return chip;
        }

        private void ClearStack()
        {
            foreach (var chip in _stack)
                if (chip != null) Destroy(chip);
            _stack.Clear();
        }

        private void CreateLabel()
        {
            var go = new GameObject("ChipLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = _labelOffset;
            go.transform.localRotation = FlatOnMarker;
            _label = go.AddComponent<TMPro.TextMeshPro>();
            _label.fontSize = 0.25f;
            _label.alignment = TMPro.TextAlignmentOptions.Center;
            _label.color = Color.yellow;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = Color.black;
            _label.rectTransform.sizeDelta = new Vector2(0.12f, 0.03f);
        }
    }
}
