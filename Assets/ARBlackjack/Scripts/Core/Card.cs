using System;

namespace ARBlackjack.Core
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }

    public enum Rank
    {
        Ace = 1, Two = 2, Three = 3, Four = 4, Five = 5,
        Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10,
        Jack = 11, Queen = 12, King = 13
    }

    [Serializable]
    public struct Card
    {
        public Suit Suit;
        public Rank Rank;

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public int FaceValue => Rank switch
        {
            Rank.Ace => 11,
            Rank.Jack or Rank.Queen or Rank.King => 10,
            _ => (int)Rank
        };

        public bool IsAce => Rank == Rank.Ace;

        public string AssetKey => $"{Suit.ToString().ToLower()}_{Rank.ToString().ToLower()}";
    }
}
