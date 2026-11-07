using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ARBlackjack.Core;

namespace ARBlackjack.Interaction
{

    public class BettingController : MonoBehaviour
    {
        public static BettingController Instance { get; private set; }

        [SerializeField] private float _readyZoneOffset = 0.1f;
        public UnityEvent OnConfirmationChanged = new();
        public int ConfirmedCount => _confirmedPlayers.Count;

        private readonly HashSet<int> _confirmedPlayers = new();
        private readonly List<ReadyZone> _readyZones = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameManager.Instance?.OnStateChanged.AddListener(OnStateChanged);
            CreateReadyZones();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Betting)
                OnStateChanged(GameState.Betting);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Betting || state == GameState.RoundEnd)
            {
                _confirmedPlayers.Clear();
                OnConfirmationChanged.Invoke();
            }

            RefreshReadyZones();
        }

        public bool IsConfirmed(int playerIndex) => _confirmedPlayers.Contains(playerIndex);

        public void MarkConfirmed(int playerIndex)
        {
            if (!_confirmedPlayers.Add(playerIndex)) return;
            OnConfirmationChanged.Invoke();
            RefreshReadyZones();

            foreach (var spot in FindObjectsByType<BetSpot>(FindObjectsSortMode.None))
                spot.RefreshLabel();
        }

        private void CreateReadyZones()
        {
            _readyZones.Clear();
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                var existing = player.GetComponentInChildren<ReadyZone>(true);
                if (existing != null)
                {
                    _readyZones.Add(existing);
                    continue;
                }

                var zoneObject = new GameObject("ReadyZone");
                zoneObject.transform.SetParent(player.transform, false);
                zoneObject.transform.localPosition = new Vector3(0f, 0.002f, _readyZoneOffset);
                var collider = zoneObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.16f, 0.1f, 0.08f);
                collider.center = new Vector3(0f, 0.05f, 0f);
                var zone = zoneObject.AddComponent<ReadyZone>();
                zone.Configure(player.PlayerIndex);
                _readyZones.Add(zone);
            }
        }

        private void RefreshReadyZones()
        {
            foreach (var zone in _readyZones)
                if (zone != null) zone.RefreshVisuals();
        }
    }
}
