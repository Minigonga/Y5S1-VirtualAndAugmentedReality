using Photon.Pun;
using ARBlackjack.Network;

namespace ARBlackjack.Core
{
    public enum GameMode { Classic, DoubleExposure, Spanish21, HighStakes }

    public static class GameModeRules
    {
        public static GameMode Selected { get; set; } = GameMode.Classic;

        public static GameMode Current => PhotonNetwork.InRoom ? NetworkConfig.RoomMode : Selected;

        public static string DisplayName(GameMode mode) => mode switch
        {
            GameMode.DoubleExposure => "Double Exposure",
            GameMode.Spanish21      => "Spanish 21",
            GameMode.HighStakes     => "High Stakes",
            _                       => "Classic"
        };

        public static int StartingChips => Current == GameMode.HighStakes ? 10000 : 2000;
        public static int MinBet => Current == GameMode.HighStakes ? 500 : 100;
        public static int ChipValue => MinBet;

        public static bool DealerHoleCardFaceUp => Current == GameMode.DoubleExposure;
        public static bool DealerWinsTies => Current == GameMode.DoubleExposure;
        public static float BlackjackPayout => Current == GameMode.DoubleExposure ? 1f : 1.5f;

        public static bool NoTens => Current == GameMode.Spanish21;
        public static bool Player21AlwaysWins => Current == GameMode.Spanish21;
        public static bool FiveCard21Bonus => Current == GameMode.Spanish21;
        public const float FiveCard21Payout = 1.5f;
    }
}
