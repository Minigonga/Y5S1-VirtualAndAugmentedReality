using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ARBlackjack.AR;
using ARBlackjack.Interaction;
using ARBlackjack.Network;

namespace ARBlackjack.Core
{
    public enum GameState
    {
        MainMenu,
        Setup,        // Wait for the table marker to be detected
        Betting,      // Each player places bets via hand marker
        Dealing,      // Initial 2-card deal
        PlayerTurns,  // Each player acts in sequence
        DealerTurn,   // Dealer reveals hole card and plays
        Resolution,   // Evaluate hands, pay out chips
        RoundEnd      // Show results, ready for next round
    }

    /// <summary>
    /// Runs the round. In a Photon room the master client is the host: it owns the deck and drives
    /// the round flow, and every change to the game (state, cards, bets, turns) is sent as an RPC that
    /// runs on all devices, so every phone holds the same game. Other devices only send requests
    /// (drop a chip, confirm, hit/stand/double/split) for their own seat; the host checks them first.
    /// Outside a room the same calls simply run locally and one device plays every seat.
    ///
    /// Seats are fixed when the game starts: a player who leaves keeps their seat number, which is
    /// skipped from then on. If the host leaves, Photon makes the next player master client and that
    /// device picks the round up from the state every phone already holds (see ResumeRound).
    /// </summary>
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager Instance { get; private set; }

        // True on the device that runs the round: the master client, or the only device when offline
        public static bool IsHost => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

        // Scene PhotonView added at runtime; the same id on every device so RPCs find it
        private const int NetworkViewId = 900;

        [Header("Config")]
        [SerializeField] private float _dealAnimDelay = 0.6f;
        [Tooltip("Seats played when not in a Photon room (one shared device). In a room, one seat per player.")]
        [SerializeField, Range(1, 5)] private int _autoStartPlayerCount = 1;
        [SerializeField] private bool _autoStart = true;

        [Header("References")]
        [SerializeField] private DealerController _dealer;

        public GameState State { get; private set; } = GameState.MainMenu;
        public int ActivePlayerCount { get; private set; }
        public int CurrentPlayerIndex { get; private set; } = -1; // -1 until the first turn of PlayerTurns starts
        public PlayerController CurrentPlayer => GetPlayer(CurrentPlayerIndex);

        // Events for UI and AR layer to subscribe to
        public UnityEvent<GameState> OnStateChanged = new();
        public UnityEvent<int> OnPlayerTurnStarted = new();   // player index
        public UnityEvent<int> OnPlayerTurnEnded = new();
        public UnityEvent<HandResult[], int[]> OnRoundResolved = new(); // per-player results and payouts

        private readonly List<PlayerController> _players = new();
        private Deck _deck;          // Host only
        private bool _actionInProgress;
        private bool _started;
        private PhotonView _view;
        private bool _becameHost;     // This device just took over as host
        private bool _resumeQueued;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _view = gameObject.AddComponent<PhotonView>();
            _view.ViewID = NetworkViewId;

            // The host's RPCs can arrive before Start once the scene finishes loading
            if (_autoStart) EnsureStarted();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureStarted()
        {
            if (!_started)
                StartGame(PhotonNetwork.InRoom ? NetworkConfig.SeatCount : _autoStartPlayerCount);
        }

        public void StartGame(int playerCount)
        {
            _started = true;
            ActivePlayerCount = Mathf.Clamp(playerCount, 1, 5);

            _players.Clear();
            _players.AddRange(FindObjectsByType<PlayerController>(FindObjectsSortMode.None));
            _players.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));

            // Game mode (stored in the room by the Settings scene): shoe and chip value
            _deck = new Deck(6, GameModeRules.NoTens);
            ChipSelector.SelectAt(ChipSelector.IndexOf(GameModeRules.ChipValue));

            TableMarkerAnchor.WaitForTable();
            TransitionTo(GameState.Setup);
        }

        public void LoadMainMenu()
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene(NetworkConfig.MainMenuScene);
        }

        // Called by TableMarkerAnchor once the table marker is visible; the host opens betting for everyone
        public void OnAllMarkersReady()
        {
            if (IsHost && State == GameState.Setup)
                SendAll(nameof(RpcSetState), (int)GameState.Betting);
        }

        public PlayerController GetPlayer(int index) =>
            index >= 0 && index < _players.Count ? _players[index] : null;

        // In the game and still in the room; a seat whose player left stays empty and is skipped
        public bool IsSeated(int seat) =>
            seat >= 0 && seat < ActivePlayerCount && (!PhotonNetwork.InRoom || NetworkConfig.IsSeatOccupied(seat));

        public bool AllSeatedConfirmed()
        {
            for (int i = 0; i < ActivePlayerCount; i++)
                // While betting, a player who can't afford the minimum bet sits the round out
                if (IsSeated(i) && !IsConfirmed(i) && (State != GameState.Betting || CanAffordRound(i))) return false;
            return true;
        }

        private bool CanAffordRound(int seat)
        {
            var player = GetPlayer(seat);
            return player != null && (player.TotalBet > 0 || player.Chips >= PlayerController.MinBet);
        }

        // Seated and betting this round; players without a bet are not dealt in
        private bool IsPlaying(int seat) => IsSeated(seat) && _players[seat].TotalBet > 0;

        public bool IsActionAvailable(PlayerAction action)
        {
            if (State != GameState.PlayerTurns) return false;
            var player = CurrentPlayer;
            if (player == null) return false;
            return action switch
            {
                Core.PlayerAction.Hit        => player.CanHitNow(),
                Core.PlayerAction.Stand      => true,
                Core.PlayerAction.DoubleDown => player.CanDoubleNow(),
                Core.PlayerAction.Split      => player.CanSplitNow(),
                _                            => false
            };
        }

        private void TransitionTo(GameState next)
        {
            State = next;
            if (next == GameState.PlayerTurns) CurrentPlayerIndex = -1;
            OnStateChanged.Invoke(next);

            if (next == GameState.Setup)
                InitializePlayers();
        }

        private void InitializePlayers()
        {
            foreach (var p in _players) p.Reset(GameModeRules.StartingChips);
        }

        private static bool IsConfirmed(int seat) =>
            BettingController.Instance != null && BettingController.Instance.IsConfirmed(seat);

        public void RequestBetChip(int seat, int amount) =>
            SendToHost(nameof(RpcRequestBetChip), seat, amount);

        public void RequestConfirm(int seat) =>
            SendToHost(nameof(RpcRequestConfirm), seat);

        public bool CanUndoBet(int seat) =>
            State == GameState.Betting && IsSeated(seat) && !IsConfirmed(seat) && GetPlayer(seat) is { CanUndoBet: true };

        public void RequestUndoBet(int seat) => SendToHost(nameof(RpcRequestUndoBet), seat);

        public void UndoLastBet()
        {
            int seat = NetworkConfig.LocalSeat;
            if (seat < 0)
                for (seat = ActivePlayerCount - 1; seat > 0 && !CanUndoBet(seat); seat--) { }
            RequestUndoBet(seat);
        }

        public void ConfirmBets()
        {
            if (State != GameState.Betting) return;
            int seat = NetworkConfig.LocalSeat;
            if (seat < 0)
                for (seat = 0; seat < ActivePlayerCount - 1 && (IsConfirmed(seat) || !IsSeated(seat)); seat++) { }
            RequestConfirm(seat);
        }

        public void PlayerAction(PlayerAction action)
        {
            int seat = NetworkConfig.LocalSeat;
            PlayerAction(seat >= 0 ? seat : CurrentPlayerIndex, action);
        }

        public void PlayerAction(int seat, PlayerAction action)
        {
            if (State != GameState.PlayerTurns || seat != CurrentPlayerIndex || !IsActionAvailable(action)) return;
            SendToHost(nameof(RpcRequestAction), seat, (int)action);
        }


        [PunRPC]
        private void RpcRequestBetChip(int seat, int amount)
        {
            var player = GetPlayer(seat);
            if (State != GameState.Betting || player == null || !IsSeated(seat) || IsConfirmed(seat)) return;
            if (amount <= 0 || amount > player.Chips) return;
            SendAll(nameof(RpcBetChip), seat, amount);
        }

        [PunRPC]
        private void RpcRequestConfirm(int seat)
        {
            var player = GetPlayer(seat);
            if (player == null || !IsSeated(seat) || IsConfirmed(seat)) return;

            if (State == GameState.RoundEnd)
            {
                SendAll(nameof(RpcConfirm), seat);
                TryStartNextRound();
                return;
            }
            if (State != GameState.Betting) return;

            if (!player.HasValidBet)
            {
                if (player.Chips < PlayerController.MinBet)
                {
                    Debug.LogWarning($"[GameManager] Player {seat + 1} could not place the minimum bet.");
                    return;
                }
                SendAll(nameof(RpcBetChip), seat, PlayerController.MinBet);
            }

            SendAll(nameof(RpcConfirm), seat);
            TryStartDeal();
        }

        private void TryStartDeal()
        {
            if (State == GameState.Betting && AllSeatedConfirmed())
                StartCoroutine(DealInitialCards());
        }

        private void TryStartNextRound()
        {
            if (State == GameState.RoundEnd && AllSeatedConfirmed())
                SendAll(nameof(RpcNewRound));
        }

        [PunRPC]
        private void RpcRequestUndoBet(int seat)
        {
            if (CanUndoBet(seat))
                SendAll(nameof(RpcUndoBet), seat);
        }

        [PunRPC]
        private void RpcRequestAction(int seat, int action)
        {
            var a = (PlayerAction)action;
            if (_actionInProgress || seat != CurrentPlayerIndex || !IsActionAvailable(a)) return;
            StartCoroutine(HandlePlayerAction(a));
        }

        private IEnumerator DealInitialCards()
        {
            SendAll(nameof(RpcSetState), (int)GameState.Dealing);

            if (_deck.NeedsReshuffle) _deck.Reset();

            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < ActivePlayerCount; i++)
                {
                    if (!IsPlaying(i) || _players[i].ActiveHand.CardCount > round) continue;
                    SendAll(nameof(RpcDealPlayer), i, Encode(_deck.Deal()));
                    yield return new WaitForSeconds(_dealAnimDelay);
                }
                if (_dealer != null && _dealer.CardCount <= round)
                {
                    SendAll(nameof(RpcDealDealer), Encode(_deck.Deal()), round == 1 && !GameModeRules.DealerHoleCardFaceUp);
                    yield return new WaitForSeconds(_dealAnimDelay);
                }
            }

            // Dealer peek: with a dealer blackjack the hand ends now, before anyone can double or split,
            // so players only lose their original bet (a player blackjack pushes)
            if (_dealer != null && _dealer.HasBlackjack)
            {
                SendAll(nameof(RpcRevealHoleCard));
                yield return new WaitForSeconds(_dealAnimDelay);
                StartCoroutine(ResolveRound());
                yield break;
            }

            bool anyActive = false;
            for (int i = 0; i < ActivePlayerCount; i++)
            {
                if (IsPlaying(i) && !_players[i].ActiveHand.IsBlackjack)
                    anyActive = true;
            }

            if (!anyActive)
                StartCoroutine(DealerTurnRoutine());
            else
                StartPlayerTurns();
        }

        private void StartPlayerTurns()
        {
            SendAll(nameof(RpcSetState), (int)GameState.PlayerTurns);
            _actionInProgress = false;
            AdvanceToNextActivePlayer(0);
        }

        private void AdvanceToNextActivePlayer(int from)
        {
            for (int i = from; i < ActivePlayerCount; i++)
            {
                var p = _players[i];
                if (IsPlaying(i) && !p.ActiveHand.IsBlackjack && !p.ActiveHand.IsBust)
                {
                    StartHandTurn(i);
                    return;
                }
            }
            StartCoroutine(DealerTurnRoutine());
        }

        private IEnumerator HandlePlayerAction(PlayerAction action)
        {
            _actionInProgress = true;
            int seat = CurrentPlayerIndex;
            var player = CurrentPlayer;

            switch (action)
            {
                case Core.PlayerAction.Hit:
                    SendAll(nameof(RpcDealPlayer), seat, Encode(_deck.Deal()));
                    yield return new WaitForSeconds(_dealAnimDelay);
                    if (player.ActiveHand.IsBust || player.ActiveHand.Value == 21)
                        EndCurrentPlayerTurn();
                    break;

                case Core.PlayerAction.Stand:
                    EndCurrentPlayerTurn();
                    break;

                case Core.PlayerAction.DoubleDown:
                    SendAll(nameof(RpcDouble), seat, Encode(_deck.Deal()));
                    yield return new WaitForSeconds(_dealAnimDelay);
                    EndCurrentPlayerTurn();
                    break;

                case Core.PlayerAction.Split:
                    SendAll(nameof(RpcSplit), seat, Encode(_deck.Deal()), Encode(_deck.Deal()));
                    yield return new WaitForSeconds(_dealAnimDelay);
                    StartHandTurn(seat); // Split Aces (or a dealt 21) stand by themselves
                    break;
            }
            _actionInProgress = false;
            SkipLeftPlayers(); 
        }

        private void SkipLeftPlayers()
        {
            while (State == GameState.PlayerTurns && CurrentPlayerIndex >= 0 && !IsSeated(CurrentPlayerIndex))
                EndCurrentPlayerTurn();
        }

        private void EndCurrentPlayerTurn()
        {
            int seat = CurrentPlayerIndex;
            var player = CurrentPlayer;
            bool moreHands = player.ActiveHandIndex < player.AllHands.Count - 1;

            SendAll(nameof(RpcTurnEnded), seat, moreHands);
            if (moreHands)
                StartHandTurn(seat);
            else
                AdvanceToNextActivePlayer(seat + 1);
        }

        // Host: starts the turn for the seat's active hand; a hand with nothing left to play
        // (split Aces get one card only, or a 21) stands by itself
        private void StartHandTurn(int seat)
        {
            SendAll(nameof(RpcTurnStarted), seat);
            var hand = _players[seat].ActiveHand;
            if (hand.IsSplitAces || hand.Value >= 21)
                EndCurrentPlayerTurn();
        }

        private IEnumerator DealerTurnRoutine()
        {
            SendAll(nameof(RpcSetState), (int)GameState.DealerTurn);

            if (_dealer != null)
            {
                SendAll(nameof(RpcRevealHoleCard));
                yield return new WaitForSeconds(_dealAnimDelay);

                while (BlackjackRules.DealerShouldHit(_dealer.Hand))
                {
                    SendAll(nameof(RpcDealDealer), Encode(_deck.Deal()), false);
                    yield return new WaitForSeconds(_dealAnimDelay);
                }
            }

            StartCoroutine(ResolveRound());
        }

        private IEnumerator ResolveRound()
        {
            SendAll(nameof(RpcSetState), (int)GameState.Resolution);
            yield return new WaitForSeconds(0.5f);

            SendAll(nameof(RpcResolve));
        }

        [PunRPC]
        private void RpcSetState(int state)
        {
            EnsureStarted();
            TransitionTo((GameState)state);
        }

        [PunRPC]
        private void RpcBetChip(int seat, int amount) => GetPlayer(seat)?.AddBetChip(amount);

        [PunRPC]
        private void RpcConfirm(int seat) => BettingController.Instance?.MarkConfirmed(seat);

        [PunRPC]
        private void RpcUndoBet(int seat) => GetPlayer(seat)?.UndoLastBetChip();

        [PunRPC]
        private void RpcDealPlayer(int seat, int card)
        {
            var player = GetPlayer(seat);
            if (player == null) return;
            player.ActiveHand.AddCard(Decode(card));
            player.RefreshVisuals();
        }

        [PunRPC]
        private void RpcDealDealer(int card, bool faceDown) => _dealer?.DealCard(Decode(card), faceDown);

        [PunRPC]
        private void RpcRevealHoleCard() => _dealer?.RevealHoleCard();

        [PunRPC]
        private void RpcDouble(int seat, int card)
        {
            var player = GetPlayer(seat);
            if (player == null) return;
            player.DoubleBet();
            player.ActiveHand.AddCard(Decode(card));
            player.RefreshVisuals();
        }

        [PunRPC]
        private void RpcSplit(int seat, int cardForFirstHand, int cardForSecondHand)
        {
            var player = GetPlayer(seat);
            if (player == null) return;
            player.Split(Decode(cardForFirstHand), Decode(cardForSecondHand));
            player.RefreshVisuals();
        }

        [PunRPC]
        private void RpcTurnStarted(int seat)
        {
            CurrentPlayerIndex = seat;
            OnPlayerTurnStarted.Invoke(seat);
        }

        [PunRPC]
        private void RpcTurnEnded(int seat, bool moreHands)
        {
            OnPlayerTurnEnded.Invoke(seat);
            if (moreHands) GetPlayer(seat)?.AdvanceToNextHand();
        }

        [PunRPC]
        private void RpcResolve()
        {
            var results = new HandResult[ActivePlayerCount];
            var payouts = new int[ActivePlayerCount];

            for (int i = 0; i < ActivePlayerCount; i++)
            {
                if (!IsSeated(i)) continue;
                int totalReturn = 0;
                var dealerHand = _dealer != null ? _dealer.Hand : new Hand();
                foreach (var hand in _players[i].AllHands)
                {
                    var result = BlackjackRules.Evaluate(hand, dealerHand);
                    totalReturn += BlackjackRules.CalculateReturn(result, _players[i].GetBetForHand(hand));
                    results[i] = result;
                }
                payouts[i] = totalReturn;
                _players[i].ReceivePayout(totalReturn);
            }

            OnRoundResolved.Invoke(results, payouts);
            TransitionTo(GameState.RoundEnd);
        }

        [PunRPC]
        private void RpcNewRound()
        {
            foreach (var p in _players) p.ResetForNewRound();
            _dealer?.Reset();
            TransitionTo(GameState.Betting);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (IsHost) QueueResume();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (!newMasterClient.IsLocal) return;
            Debug.Log("[GameManager] Previous host left; this device is now the host.");
            _becameHost = true;
            QueueResume();
        }

        private void QueueResume()
        {
            if (_resumeQueued) return;
            _resumeQueued = true;
            StartCoroutine(ResumeNextFrame());
        }

        private IEnumerator ResumeNextFrame()
        {
            yield return null;
            _resumeQueued = false;
            if (!IsHost || !_started) yield break;

            if (_becameHost)
            {
                _becameHost = false;
                ResumeRound();
            }
            else if (State == GameState.Betting)
                TryStartDeal(); 
            else if (State == GameState.RoundEnd)
                TryStartNextRound();
            else if (!_actionInProgress)
                SkipLeftPlayers();
        }

        private void ResumeRound()
        {
            _actionInProgress = false;
            switch (State)
            {
                case GameState.Setup:
                    TableMarkerAnchor.WaitForTable();
                    break;
                case GameState.Betting:
                    TryStartDeal();
                    break;
                case GameState.Dealing:
                    StartCoroutine(DealInitialCards());
                    break;
                case GameState.PlayerTurns:
                    var player = CurrentPlayer;
                    if (player == null)
                        AdvanceToNextActivePlayer(0);
                    else if (player.ActiveHand.IsBust || player.ActiveHand.Value >= 21 || player.ActiveHand.IsSplitAces)
                        EndCurrentPlayerTurn(); 
                    SkipLeftPlayers();
                    break;
                case GameState.DealerTurn:
                    StartCoroutine(DealerTurnRoutine());
                    break;
                case GameState.Resolution:
                    StartCoroutine(ResolveRound());
                    break;
                case GameState.RoundEnd:
                    TryStartNextRound();
                    break;
            }
        }

        private void SendAll(string method, params object[] args)
        {
            if (PhotonNetwork.InRoom) _view.RPC(method, RpcTarget.All, args);
            else RunLocal(method, args);
        }
        private void SendToHost(string method, params object[] args)
        {
            if (PhotonNetwork.InRoom) _view.RPC(method, RpcTarget.MasterClient, args);
            else RunLocal(method, args);
        }

        private void RunLocal(string method, object[] args) =>
            GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, args);

        private static int Encode(Card card) => (int)card.Suit * 16 + (int)card.Rank;
        private static Card Decode(int code) => new((Suit)(code / 16), (Rank)(code % 16));
    }

    public enum PlayerAction { Hit, Stand, DoubleDown, Split }
}
