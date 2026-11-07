using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using ARBlackjack.Interaction;

namespace ARBlackjack.Core
{
    public class PlayerController : MonoBehaviour
    {
        public static int MinBet => GameModeRules.MinBet;

        [SerializeField] private int _playerIndex;

        public int PlayerIndex => _playerIndex;
        public int Chips { get; private set; }

        public Hand ActiveHand => _hands[_activeHandIndex];
        public int ActiveHandIndex => _activeHandIndex;
        public int ActiveBet => _bets[_activeHandIndex];
        public int TotalBet { get { int t = 0; foreach (var b in _bets) t += b; return t; } }
        public ReadOnlyCollection<Hand> AllHands => _hands.AsReadOnly();

        public UnityEvent OnHandChanged = new();
        public UnityEvent<int> OnChipsChanged = new();
        public UnityEvent OnBetChanged = new();

        private readonly List<Hand> _hands = new();
        private readonly List<int> _bets = new();
        private readonly List<int> _betChips = new();
        private int _activeHandIndex;
        private int _splitCount;

        public void Reset(int startingChips)
        {
            Chips = startingChips;
            ResetForNewRound();
        }

        public void ResetForNewRound()
        {
            _hands.Clear();
            _bets.Clear();
            _betChips.Clear();
            _hands.Add(new Hand());
            _bets.Add(0);
            _activeHandIndex = 0;
            _splitCount = 0;
            OnHandChanged.Invoke();
            OnBetChanged.Invoke();
        }


        public bool AddBetChip(int amount)
        {
            if (amount <= 0 || amount > Chips) return false;
            _bets[0] += amount;
            Chips -= amount;
            _betChips.Add(amount);
            OnChipsChanged.Invoke(Chips);
            OnBetChanged.Invoke();
            return true;
        }

        public bool CanUndoBet => _betChips.Count > 0;

        public bool UndoLastBetChip()
        {
            if (_betChips.Count == 0) return false;
            int amount = _betChips[^1];
            _betChips.RemoveAt(_betChips.Count - 1);
            _bets[0] -= amount;
            Chips += amount;
            OnChipsChanged.Invoke(Chips);
            OnBetChanged.Invoke();
            return true;
        }

        public bool HasValidBet => _bets[0] >= MinBet;

        public void DoubleBet()
        {
            int extra = _bets[_activeHandIndex];
            if (extra > Chips) return;
            Chips -= extra;
            _bets[_activeHandIndex] += extra;
            OnChipsChanged.Invoke(Chips);
            OnBetChanged.Invoke();
        }

        public int GetBetForHand(Hand hand)
        {
            int idx = _hands.IndexOf(hand);
            return idx >= 0 ? _bets[idx] : 0;
        }


        public void Split(Card cardForFirstHand, Card cardForSecondHand)
        {
            if (!BlackjackRules.CanSplit(ActiveHand, _splitCount)) return;

            bool aces = ActiveHand.Cards[0].IsAce;
            var splitCard = ActiveHand.RemoveLastCard();
            ActiveHand.AddCard(cardForFirstHand);

            var newHand = new Hand();
            newHand.AddCard(splitCard);
            newHand.AddCard(cardForSecondHand);
            ActiveHand.MarkSplit(aces);
            newHand.MarkSplit(aces);

            int splitBet = _bets[_activeHandIndex];
            Chips -= splitBet;
            OnChipsChanged.Invoke(Chips);

            _hands.Insert(_activeHandIndex + 1, newHand);
            _bets.Insert(_activeHandIndex + 1, splitBet);
            _splitCount++;
            OnHandChanged.Invoke();
            OnBetChanged.Invoke();
        }

        public bool AdvanceToNextHand()
        {
            if (_activeHandIndex < _hands.Count - 1)
            {
                _activeHandIndex++;
                OnHandChanged.Invoke();
                return true;
            }
            return false;
        }

        public void ReceivePayout(int amount)
        {
            Chips += amount;
            OnChipsChanged.Invoke(Chips);
        }

        public void RefreshVisuals() => OnHandChanged.Invoke();
        public bool CanSplitNow() => BlackjackRules.CanSplit(ActiveHand, _splitCount) && Chips >= ActiveBet;
        public bool CanDoubleNow() => BlackjackRules.CanDoubleDown(ActiveHand) && Chips >= ActiveBet;
        public bool CanHitNow() => BlackjackRules.CanHit(ActiveHand);
    }
}
