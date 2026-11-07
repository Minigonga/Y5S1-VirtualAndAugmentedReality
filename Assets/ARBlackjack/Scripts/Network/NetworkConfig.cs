using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ARBlackjack.Core;

namespace ARBlackjack.Network
{
    public static class NetworkConfig
    {
        public const string MainMenuScene     = "MainMenuScene";
        public const string ManualScene       = "ManualScene";
        public const string InsertRoomIdScene = "InsertRoomID";
        public const string SettingsScene     = "Settings";
        public const string LobbyScene        = "LobbyScene";
        public const string GameScene         = "BlackjackScene";

        public const int MaxPlayers = 5;

        private const string SeatsKey = "seats";
        private const string ModeKey = "mode";

        public static void Init()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            if (string.IsNullOrEmpty(PhotonNetwork.NickName))
                PhotonNetwork.NickName = $"Player {Random.Range(1000, 10000)}";
        }

        public static RoomOptions CreateRoomOptions(GameMode mode)
        {
            return new RoomOptions
            {
                MaxPlayers = MaxPlayers, IsVisible = false, IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ModeKey, (int)mode } }
            };
        }

        public static GameMode RoomMode
        {
            get
            {
                var room = PhotonNetwork.CurrentRoom;
                return room != null && room.CustomProperties.TryGetValue(ModeKey, out var value) && value is int mode
                    ? (GameMode)mode
                    : GameMode.Classic;
            }
        }

        public static string GenerateRoomCode()
        {
            return Random.Range(1000, 10000).ToString();
        }
        public static void AssignSeats()
        {
            var players = PhotonNetwork.PlayerList;
            var actors = new int[players.Length];
            for (int i = 0; i < players.Length; i++) actors[i] = players[i].ActorNumber;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { SeatsKey, actors } });
        }

        private static int[] SeatActors
        {
            get
            {
                var room = PhotonNetwork.CurrentRoom;
                if (room != null && room.CustomProperties.TryGetValue(SeatsKey, out var value) && value is int[] actors)
                    return actors;

                var players = PhotonNetwork.PlayerList;
                var joinOrder = new int[players.Length];
                for (int i = 0; i < players.Length; i++) joinOrder[i] = players[i].ActorNumber;
                return joinOrder;
            }
        }

        public static int SeatCount => SeatActors.Length;

        public static int SeatOf(Player player) => System.Array.IndexOf(SeatActors, player.ActorNumber);

        public static bool IsSeatOccupied(int seat)
        {
            var actors = SeatActors;
            return seat >= 0 && seat < actors.Length && PhotonNetwork.CurrentRoom?.GetPlayer(actors[seat]) != null;
        }

        public static int LocalSeat => PhotonNetwork.InRoom ? SeatOf(PhotonNetwork.LocalPlayer) : -1;

        public static bool IsLocalSeat(int seat) => !PhotonNetwork.InRoom || LocalSeat == seat;
    }
}
