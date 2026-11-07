using UnityEngine;

namespace ARBlackjack.Core
{
    public enum HandResult { Win, Lose, Push, Bust, DealerBust, Blackjack, DealerBlackjack, FiveCard21 }
    public static class BlackjackRules
    {
        public static bool CanHit(Hand hand) => !hand.IsBust && hand.Value < 21 && !hand.IsSplitAces;

        // Double on any first two cards, also after a split (except split Aces)
        public static bool CanDoubleDown(Hand hand)
            => hand.CardCount == 2 && !hand.IsSplitAces;

        // Up to 3 splits (4 hands); split Aces are not re-split
        public static bool CanSplit(Hand hand, int splitCount = 0)
            => hand.CanSplit && splitCount < 3 && !hand.IsSplitAces;

        public static bool DealerShouldHit(Hand hand)
            => hand.Value < 17 || (hand.IsSoft && hand.Value == 17);

        public static HandResult Evaluate(Hand playerHand, Hand dealerHand)
        {
            if (playerHand.IsBust) return HandResult.Bust;

            // Spanish 21: a player 21 always wins (blackjack even beats a dealer blackjack)
            if (GameModeRules.Player21AlwaysWins && playerHand.Value == 21)
            {
                if (playerHand.IsBlackjack) return HandResult.Blackjack;
                if (GameModeRules.FiveCard21Bonus && playerHand.CardCount >= 5) return HandResult.FiveCard21;
                return dealerHand.IsBust ? HandResult.DealerBust : HandResult.Win;
            }

            if (dealerHand.IsBust) return HandResult.DealerBust;
            if (playerHand.IsBlackjack && !dealerHand.IsBlackjack) return HandResult.Blackjack;
            if (dealerHand.IsBlackjack && !playerHand.IsBlackjack) return HandResult.DealerBlackjack;
            if (playerHand.Value > dealerHand.Value) return HandResult.Win;
            if (playerHand.Value < dealerHand.Value) return HandResult.Lose;

            // Double Exposure: the dealer wins ties, except a blackjack tie
            return GameModeRules.DealerWinsTies && !playerHand.IsBlackjack ? HandResult.Lose : HandResult.Push;
        }

        public static int CalculateReturn(HandResult result, int bet)
        {
            return result switch
            {
                HandResult.Blackjack  => bet + Mathf.RoundToInt(bet * GameModeRules.BlackjackPayout),
                HandResult.FiveCard21 => bet + Mathf.RoundToInt(bet * GameModeRules.FiveCard21Payout),
                HandResult.Win        => bet * 2,
                HandResult.DealerBust => bet * 2,
                HandResult.Push       => bet,
                _                     => 0
            };
        }
    }
}
