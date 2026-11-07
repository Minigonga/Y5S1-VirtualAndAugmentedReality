using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ARBlackjack.Core
{
    public class Hand
    {
        private readonly List<Card> _cards = new();

        public ReadOnlyCollection<Card> Cards => _cards.AsReadOnly();
        public int CardCount => _cards.Count;

        // Set on both hands made by a split: a split 21 is not a blackjack, and split Aces get one card each
        public bool IsFromSplit { get; private set; }
        public bool IsSplitAces { get; private set; }
        public void MarkSplit(bool aces) { IsFromSplit = true; IsSplitAces = aces; }

        public int Value
        {
            get
            {
                var (total, aces) = RawTotal();
                while (total > 21 && aces > 0) { total -= 10; aces--; }
                return total;
            }
        }

        public bool IsBust => Value > 21;
        public bool IsBlackjack => !IsFromSplit && _cards.Count == 2 && Value == 21;

        public bool IsSoft { get { var (total, aces) = RawTotal(); return aces > 0 && total <= 21; } }

        private (int total, int aces) RawTotal()
        {
            int total = 0, aces = 0;
            foreach (var card in _cards)
            {
                total += card.FaceValue;
                if (card.IsAce) aces++;
            }
            return (total, aces);
        }

        public bool CanSplit => _cards.Count == 2 && _cards[0].FaceValue == _cards[1].FaceValue;

        public void AddCard(Card card) => _cards.Add(card);

        public Card RemoveLastCard()
        {
            var card = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            return card;
        }

        public void Clear()
        {
            _cards.Clear();
            IsFromSplit = IsSplitAces = false;
        }
    }
}
