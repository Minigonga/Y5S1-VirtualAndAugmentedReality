using System.Collections.Generic;
using UnityEngine;
using ARBlackjack.Core;

namespace ARBlackjack.AR
{
    public class TableMarkerAnchor : MarkerEventHandler
    {
        private static TableMarkerAnchor _instance;
        private static bool _waiting;

        [Header("Stabilization")]
        [Tooltip("The marker must move further than this (metres) before the table follows it.")]
        [SerializeField] private float _moveThreshold = 0.02f;
        [Tooltip("...or turn more than this (degrees).")]
        [SerializeField] private float _angleThreshold = 4f;
        [Tooltip("How long (seconds) the marker must stay moved before the table jumps to it.")]
        [SerializeField] private float _confirmTime = 0.2f;

        // Parent for the table's content; runtime-created table objects should go here too
        public Transform Content => _content;

        private Transform _content;
        private bool _hasPose;
        private Vector3 _stablePosition;
        private Quaternion _stableRotation;
        private float _movedFor;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            BuildContentRoot();
        }

        private void OnEnable()  => Application.onBeforeRender += ApplyStablePose;
        private void OnDisable() => Application.onBeforeRender -= ApplyStablePose;

        public static void WaitForTable()
        {
            _waiting = true;
            CheckReady();
        }

        protected override void OnMarkerFound() => CheckReady();

        private static void CheckReady()
        {
            if (!_waiting || _instance == null || !_instance.IsTracked) return;
            _waiting = false;
            GameManager.Instance?.OnAllMarkersReady();
        }

        // ── Stabilization ────────────────────────────────────────────────

        private void BuildContentRoot()
        {
            var children = new List<Transform>();
            foreach (Transform child in transform) children.Add(child);

            _content = new GameObject("StableContent").transform;
            _content.SetParent(transform, false);
            foreach (var child in children)
                child.SetParent(_content, false); // Content sits at identity, so nothing moves
        }

        private void LateUpdate()
        {
            UpdateStablePose();
            ApplyStablePose();
        }

        private void UpdateStablePose()
        {
            if (!IsTracked) { _movedFor = 0f; return; }

            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;

            if (!_hasPose)
            {
                AcceptPose(position, rotation); // First sighting
                return;
            }

            bool moved = Vector3.Distance(position, _stablePosition) > _moveThreshold ||
                         Quaternion.Angle(rotation, _stableRotation) > _angleThreshold;
            _movedFor = moved ? _movedFor + Time.deltaTime : 0f;
            if (_movedFor >= _confirmTime)
                AcceptPose(position, rotation);
        }

        private void AcceptPose(Vector3 position, Quaternion rotation)
        {
            _stablePosition = position;
            _stableRotation = rotation;
            _hasPose = true;
            _movedFor = 0f;
        }

        // Also run right before rendering, in case Vuforia updates the target after our LateUpdate
        private void ApplyStablePose()
        {
            if (_content == null) return;
            if (_hasPose)
                _content.SetPositionAndRotation(_stablePosition, _stableRotation);
            else
                _content.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}
