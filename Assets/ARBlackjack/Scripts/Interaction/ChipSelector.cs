using System;

namespace ARBlackjack.Interaction
{

    public static class ChipSelector
    {
        public const int FixedDenomination = 100;
        public static readonly int[] Denominations = { 100, 200, 500, 1000, 2000 };

        public static int SelectedIndex { get; private set; }
        public static int Selected => Denominations[SelectedIndex];

        public static event Action<int> OnChanged;

        public static void SelectAt(int index)
        {
            if (index < 0 || index >= Denominations.Length) return;
            SelectedIndex = index;
            OnChanged?.Invoke(Selected);
        }

        public static int IndexOf(int denomination) => Array.IndexOf(Denominations, denomination);
    }
}
