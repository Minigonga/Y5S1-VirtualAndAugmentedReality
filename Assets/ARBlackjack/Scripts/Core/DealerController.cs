using UnityEngine;
using UnityEngine.Events;

namespace ARBlackjack.Core
{
    public class DealerController : MonoBehaviour
    {
        public Hand Hand { get; private set; } = new();

        // Cards on the table, the face-down hole card included
        public int CardCount => Hand.CardCount + (_holeCardStored ? 1 : 0);

        // Dealer peek: an Ace or 10-value up card whose hole card makes 21 (both cards visible in Double Exposure)
        public bool HasBlackjack
        {
            get
            {
                if (!_holeCardStored) return Hand.IsBlackjack;
                if (Hand.CardCount != 1) return false;
                var up = Hand.Cards[0];
                return (up.IsAce || up.FaceValue == 10) && up.FaceValue + _holeCard.FaceValue == 21;
            }
        }

        public UnityEvent<Card, bool> OnCardDealt = new();
        public UnityEvent OnHoleCardRevealed = new();

        private Card _holeCard;
        private bool _holeCardStored;

        public void DealCard(Card card, bool faceDown)
        {
            if (faceDown)
            {
                _holeCard = card;
                _holeCardStored = true;
            }
            else
            {
                Hand.AddCard(card);
            }
            OnCardDealt.Invoke(card, faceDown);
        }

        public void RevealHoleCard()
        {
            if (!_holeCardStored) return;
            Hand.AddCard(_holeCard);
            _holeCardStored = false;
            OnHoleCardRevealed.Invoke();
        }

        public void Reset()
        {
            Hand.Clear();
            _holeCardStored = false;
        }
    }
}
