using UnityEngine;
using ARBlackjack.Core;
using ARBlackjack.Interaction;
using ARBlackjack.Network;

namespace ARBlackjack.UI
{
    /// <summary>
    /// Floats above the hand being played (follows the active split hand) and holds two things:
    ///   - the Double / Split buttons: only on the player's own phone, during their turn, and only while
    ///     Double or Split is actually possible (after a hit they disappear for that hand);
    ///   - the playing-hand marker (child "Playinghand"): shown on every phone during that seat's turn,
    ///     so everyone can see which hand is being played.
    /// The panel itself stays active so it keeps following the hand; only its children are switched.
    /// </summary>
    public class GameOptionsPanel : MonoBehaviour
    {
        [SerializeField] private int _playerIndex;
        [SerializeField] private float _heightAboveHand = 0.15f;
        [Tooltip("Marker for the hand being played (left empty: the child named \"Playinghand\").")]
        [SerializeField] private GameObject _playingHandMarker;

        private CardSpawner _spawner;
        private PlayerController _player;

        private void Start()
        {
            _spawner = FindSpawner();
            if (_spawner != null && transform.parent != _spawner.transform)
                transform.SetParent(_spawner.transform, false);

            if (_playingHandMarker == null)
                foreach (Transform child in transform)
                    if (child.name.ToLowerInvariant() == "playinghand") { _playingHandMarker = child.gameObject; break; }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnPlayerTurnStarted.AddListener(OnTurnChanged);
                gm.OnPlayerTurnEnded.AddListener(OnTurnChanged);
                gm.OnStateChanged.AddListener(OnStateChanged);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnPlayerTurnStarted.RemoveListener(OnTurnChanged);
                gm.OnPlayerTurnEnded.RemoveListener(OnTurnChanged);
                gm.OnStateChanged.RemoveListener(OnStateChanged);
            }
            if (_player != null) _player.OnHandChanged.RemoveListener(Refresh);
        }

        private void LateUpdate()
        {
            if (_spawner == null) return;
            int handIndex = _player != null ? _player.ActiveHandIndex : 0;
            transform.position = _spawner.GetHandCenter(handIndex) + _spawner.transform.up * _heightAboveHand;
        }

        private void OnTurnChanged(int _) => Refresh();
        private void OnStateChanged(GameState _) => Refresh();

        private void Refresh()
        {
            BindPlayer();
            var gm = GameManager.Instance;
            bool seatTurn = gm != null &&
                            gm.State == GameState.PlayerTurns &&
                            gm.CurrentPlayerIndex == _playerIndex;
            bool showOptions = seatTurn &&
                               NetworkConfig.IsLocalSeat(_playerIndex) &&
                               (gm.IsActionAvailable(PlayerAction.DoubleDown) || gm.IsActionAvailable(PlayerAction.Split));

            foreach (Transform child in transform)
            {
                bool isMarker = _playingHandMarker != null && child.gameObject == _playingHandMarker;
                if (isMarker) { child.gameObject.SetActive(seatTurn); continue; }
                // Double and Split each show only when that move is possible
                var selector = child.GetComponent<ActionSelector>();
                child.gameObject.SetActive(showOptions && (selector == null || gm.IsActionAvailable(selector.Action)));
            }
        }

        private void BindPlayer()
        {
            var p = GameManager.Instance?.GetPlayer(_playerIndex);
            if (p == _player) return;
            if (_player != null) _player.OnHandChanged.RemoveListener(Refresh);
            _player = p;
            if (_player != null) _player.OnHandChanged.AddListener(Refresh);
        }

        private CardSpawner FindSpawner()
        {
            foreach (var s in FindObjectsByType<CardSpawner>(FindObjectsSortMode.None))
                if (!s.IsDealer && s.PlayerIndex == _playerIndex) return s;
            return null;
        }
    }
}
