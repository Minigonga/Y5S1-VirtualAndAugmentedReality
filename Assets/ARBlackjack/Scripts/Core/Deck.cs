using System.Collections.Generic;
using UnityEngine;

namespace ARBlackjack.Core
{
    public class Deck
    {
        private readonly List<Card> _cards = new();
        private readonly int _numDecks;
        private readonly bool _noTens; 
        private int CardsPerDeck => _noTens ? 48 : 52;
        public bool NeedsReshuffle => _cards.Count < (CardsPerDeck * _numDecks * 0.25f);

        public Deck(int numDecks = 6, bool noTens = false)
        {
            _numDecks = numDecks;
            _noTens = noTens;
            Reset();
        }

        public void Reset()
        {
            _cards.Clear();
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    if (_noTens && rank == Rank.Ten) continue;
                    for (int d = 0; d < _numDecks; d++)
                        _cards.Add(new Card(suit, rank));
                }
            Shuffle();
        }

        private void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card Deal()
        {
            if (_cards.Count == 0) Reset();
            var card = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            return card;
        }
    }
}
