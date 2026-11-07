using System.Collections.Generic;
using UnityEngine;
using ARBlackjack.Core;

namespace ARBlackjack.Interaction
{

    public class CardSpawner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int _playerIndex = 0;
        [SerializeField] private bool _isDealer = false;

        [Header("Prefab")]
        [SerializeField] private GameObject _cardPrefab;

        [Header("Layout (world units)")]
        [SerializeField] private float _heightAboveMarker = 0.002f;
        [SerializeField] private float _stackSpacing = 0.001f;
        [SerializeField] private float _handSpacing = 0.14f;
        [SerializeField] private Vector2 _cascadeOffset = new(0.02f, 0.025f); 
        private static readonly Quaternion FlatOnMarker = Quaternion.Euler(90f, 0f, 0f);
        private readonly List<List<CardVisual>> _piles = new();
        private PlayerController _player;

        public int PlayerIndex => _playerIndex;
        public bool IsDealer => _isDealer;

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged.AddListener(OnStateChanged);

            if (_isDealer)
            {
                var dealer = FindAnyObjectByType<DealerController>();
                if (dealer != null)
                {
                    dealer.OnCardDealt.AddListener(OnDealerCardDealt);
                    dealer.OnHoleCardRevealed.AddListener(OnHoleCardRevealed);
                }
            }
            else
            {
                _player = FindPlayer();
                if (_player != null)
                    _player.OnHandChanged.AddListener(() => RefreshPlayer(_player));
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Betting || state == GameState.MainMenu)
                ClearCards();
        }

        private void RefreshPlayer(PlayerController player)
        {
            if (_cardPrefab == null) return;
            var hands = player.AllHands;

            while (_piles.Count > hands.Count)
            {
                DestroyFrom(_piles[^1], 0);
                _piles.RemoveAt(_piles.Count - 1);
            }
            while (_piles.Count < hands.Count)
                _piles.Add(new List<CardVisual>());

            for (int h = 0; h < hands.Count; h++)
            {
                var hand = hands[h];
                var pile = _piles[h];

                int keep = 0;
                while (keep < pile.Count && keep < hand.CardCount && SameCard(pile[keep].Card, hand.Cards[keep]))
                    keep++;
                DestroyFrom(pile, keep);

                for (int c = keep; c < hand.CardCount; c++)
                    pile.Add(SpawnCard(hand.Cards[c], true, PilePosition(h, hands.Count, c)));
            }

            Layout();
        }

        private void OnDealerCardDealt(Card card, bool faceDown)
        {
            if (_cardPrefab == null) return;
            if (_piles.Count == 0) _piles.Add(new List<CardVisual>());
            var pile = _piles[0];
            pile.Add(SpawnCard(card, !faceDown, PilePosition(0, 1, pile.Count)));
        }

        private void OnHoleCardRevealed()
        {
            if (_piles.Count == 0) return;
            foreach (var vis in _piles[0])
            {
                if (vis.IsFaceUp) continue;
                vis.Flip();
                return;
            }
        }

        private CardVisual SpawnCard(Card card, bool faceUp, Vector3 position)
        {
            var go = Instantiate(_cardPrefab, position, transform.rotation * FlatOnMarker, transform);
            var vis = go.GetComponent<CardVisual>();
            var dealFrom = Camera.main != null ? Camera.main.transform.position : position + transform.up * 0.3f;
            vis.Initialize(card, faceUp, dealFrom);
            return vis;
        }

        private void Layout()
        {
            for (int h = 0; h < _piles.Count; h++)
                for (int c = 0; c < _piles[h].Count; c++)
                    _piles[h][c].SetTargetPosition(PilePosition(h, _piles.Count, c));
        }

        public Vector3 GetHandCenter(int handIndex)
        {
            int pileCount = Mathf.Max(_piles.Count, 1);
            int cardCount = handIndex < _piles.Count ? _piles[handIndex].Count : 0;
            var center = PilePosition(handIndex, pileCount, Mathf.Max(cardCount - 1, 0) * 0.5f);
            return center + transform.up * (Mathf.Max(cardCount - 1, 0) * 0.5f * _stackSpacing);
        }

        private Vector3 PilePosition(int pileIndex, int pileCount, float cardIndex)
        {
            float side = (pileIndex - (pileCount - 1) * 0.5f) * _handSpacing + cardIndex * _cascadeOffset.x;
            float forward = cardIndex * _cascadeOffset.y;
            float height = _heightAboveMarker + StackSlot(cardIndex) * _stackSpacing;
            return transform.position + transform.right * side + transform.forward * forward + transform.up * height;
        }

        private float StackSlot(float cardIndex)
        {
            if (!_isDealer) return cardIndex;
            if (Mathf.Approximately(cardIndex, 0f)) return 1f;
            if (Mathf.Approximately(cardIndex, 1f)) return 0f;
            return cardIndex;
        }

        private static bool SameCard(Card a, Card b) => a.Suit == b.Suit && a.Rank == b.Rank;

        private static void DestroyFrom(List<CardVisual> pile, int index)
        {
            for (int i = index; i < pile.Count; i++)
                if (pile[i] != null) Destroy(pile[i].gameObject);
            pile.RemoveRange(index, pile.Count - index);
        }

        private void ClearCards()
        {
            foreach (var pile in _piles)
                DestroyFrom(pile, 0);
            _piles.Clear();
        }

        private PlayerController FindPlayer()
        {
            foreach (var pc in FindObjectsByType<PlayerController>())
                if (pc.PlayerIndex == _playerIndex) return pc;
            return null;
        }
    }
}
