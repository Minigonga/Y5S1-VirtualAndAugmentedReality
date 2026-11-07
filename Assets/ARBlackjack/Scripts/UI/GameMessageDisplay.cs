using UnityEngine;
using TMPro;
using ARBlackjack.Core;
using ARBlackjack.Interaction;
using ARBlackjack.Network;

namespace ARBlackjack.UI
{
    public class GameMessageDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private GameObject _messagePanel;

        private bool _bcSubscribed;
        private PlayerController _turnPlayer; // Whose hand the turn message is following

        private void Awake()
        {
            EnsureEventSystem();

            if (_messageText == null)
            {
                foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (t.gameObject.name == "Message") { _messageText = t; break; }
                }
            }

            if (_messagePanel == null && _messageText != null)
            {
                var p = _messageText.transform.parent;
                while (p != null)
                {
                    if (p.name == "MessageBox") { _messagePanel = p.gameObject; break; }
                    p = p.parent;
                }
            }

            if (_messagePanel != null)
                _messagePanel.SetActive(false);
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnStateChanged.AddListener(OnStateChanged);
            gm.OnPlayerTurnStarted.AddListener(OnPlayerTurnStarted);
            gm.OnRoundResolved.AddListener(OnRoundResolved);
            SubscribeToBettingController();
            Show("");
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged.RemoveListener(OnStateChanged);
                gm.OnPlayerTurnStarted.RemoveListener(OnPlayerTurnStarted);
                gm.OnRoundResolved.RemoveListener(OnRoundResolved);
            }
            if (_turnPlayer != null) _turnPlayer.OnHandChanged.RemoveListener(RefreshTurnMessage);
            var bc = BettingController.Instance;
            if (bc != null) bc.OnConfirmationChanged.RemoveListener(RefreshBettingMessage);
        }

        private void SubscribeToBettingController()
        {
            if (_bcSubscribed) return;
            var bc = BettingController.Instance;
            if (bc == null) return;
            bc.OnConfirmationChanged.AddListener(RefreshBettingMessage);
            _bcSubscribed = true;
        }

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Betting:
                    SubscribeToBettingController();
                    RefreshBettingMessage();
                    break;
                case GameState.Dealing:
                    Show("Dealing cards...");
                    break;
                case GameState.DealerTurn:
                    Show("Dealer's turn");
                    break;
                case GameState.PlayerTurns:
                case GameState.MainMenu:
                    Show("");
                    break;
            }
        }

        private void RefreshBettingMessage()
        {
            var gm = GameManager.Instance;
            var bc = BettingController.Instance;
            if (gm == null || gm.State != GameState.Betting) return;

            int local = LocalSeatIndex();

            if (local < 0)
            {
                Show(!gm.AllSeatedConfirmed()
                    ? "Place your bets"
                    : "");
                return;
            }

            var me = gm.GetPlayer(local);
            if (me != null && me.TotalBet == 0 && me.Chips < PlayerController.MinBet)
                Show("Out of chips\nWatching this round");
            else if (bc == null || !bc.IsConfirmed(local))
                Show("Place your bet");
            else if (!gm.AllSeatedConfirmed())
                Show("Waiting for other\nplayers to bet...");
            else
                Show("");
        }

        private void OnPlayerTurnStarted(int playerIndex)
        {
            // Follow the hand being played so the list of moves updates (e.g. no Double/Split after a hit)
            var player = GameManager.Instance?.GetPlayer(playerIndex);
            if (player != _turnPlayer)
            {
                if (_turnPlayer != null) _turnPlayer.OnHandChanged.RemoveListener(RefreshTurnMessage);
                _turnPlayer = player;
                if (_turnPlayer != null) _turnPlayer.OnHandChanged.AddListener(RefreshTurnMessage);
            }
            RefreshTurnMessage();
        }

        private void RefreshTurnMessage()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.PlayerTurns || gm.CurrentPlayerIndex < 0) return;

            int seat = gm.CurrentPlayerIndex;
            int local = LocalSeatIndex();
            if (local >= 0 && seat != local)
            {
                Show($"Player {seat + 1}'s turn");
                return;
            }
            string who = local >= 0 ? "Your turn" : $"Player {seat + 1}'s turn";
            Show($"{who}\nYou can {AvailableActions(gm)}");
        }

        // e.g. "Hit, Stand, Double or Split", only the moves possible right now
        private static string AvailableActions(GameManager gm)
        {
            var actions = new System.Collections.Generic.List<string>();
            if (gm.IsActionAvailable(PlayerAction.Hit)) actions.Add("Hit");
            if (gm.IsActionAvailable(PlayerAction.Stand)) actions.Add("Stand");
            if (gm.IsActionAvailable(PlayerAction.DoubleDown)) actions.Add("Double");
            if (gm.IsActionAvailable(PlayerAction.Split)) actions.Add("Split");
            if (actions.Count <= 1) return actions.Count == 1 ? actions[0] : "Stand";
            return string.Join(", ", actions.GetRange(0, actions.Count - 1)) + " or " + actions[^1];
        }

        private void OnRoundResolved(HandResult[] results, int[] payouts)
        {
            int idx = Mathf.Max(LocalSeatIndex(), 0);
            if (idx >= results.Length) { Show(""); return; }

            Show(results[idx] switch
            {
                HandResult.Blackjack       => "Blackjack — you won!",
                HandResult.FiveCard21      => "5-card 21 — bonus 3:2!",
                HandResult.Win             => "You won!",
                HandResult.DealerBust      => "Dealer bust — you won!",
                HandResult.Push            => "Push — it's a draw",
                HandResult.Bust            => "Bust — you lost",
                HandResult.Lose            => "You lost",
                HandResult.DealerBlackjack => "Dealer blackjack — you lost",
                _                          => ""
            });
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void Show(string msg)
        {
            bool visible = !string.IsNullOrEmpty(msg);
            if (_messagePanel != null) _messagePanel.SetActive(visible);
            if (_messageText != null)  _messageText.text = msg;
        }

        private static int LocalSeatIndex() => NetworkConfig.LocalSeat;
    }
}
