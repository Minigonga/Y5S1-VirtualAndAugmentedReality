# AR Multiplayer Blackjack

## Students

- **Gonçalo Pinto** — 202204943 — up202204943@up.pt
- **Leonardo Ribeiro** — 202205144 — up202205144@up.pt

## Project overview

This project is an augmented-reality multiplayer Blackjack game developed with
Unity 6 (`6000.6.6f1`). Players sit around a physical table and use printed
Vuforia image markers and phone gestures to interact with the game.

The game supports up to five players. Photon PUN 2 is used for room creation,
joining, and synchronization of the shared game state.

## How to run the project

1. Open the project in Unity Hub using Unity 6 (`6000.6.6f1`).
2. Open `Assets/Scenes/MainMenuScene.unity`, or build and run the project using
   the configured Android build profile.
3. Ensure that the Vuforia database is available in
   `Assets/StreamingAssets/Vuforia/` and that Photon PUN 2 is configured in
   `PhotonServerSettings`.
4. Print the six markers from [markers.pdf](markers.pdf) at 100% scale. Each marker must be
   exactly 10 × 10 cm:
   - one `table` marker;
   - five hand markers: `hand1` through `hand5`.
5. Run the application, create a room, choose a game mode, and wait in the
   lobby. Other players can join using the room ID.
6. Point the camera at the table marker.

## Implemented features

### Augmented reality

- Vuforia image tracking for the table and five player hand markers.
- The table marker anchors the AR table and game area.
- Marker tracking controls the visibility of the associated scene objects.
- Betting, ready confirmation, double, and split actions are performed by
  hovering a hand marker over the corresponding zones.

### Blackjack gameplay

- Complete round flow: setup, betting, dealing, player turns, dealer turn,
  resolution, and next round.
- Six-deck shoe with Fisher-Yates shuffling and automatic reshuffling.
- Blackjack detection, ace handling, bust detection, and dealer soft-17 rules.
- Chip betting, payouts, double, split, and multiple hands.
- Casino split/double rules: the dealer checks for blackjack before players act
  (so only the original bet is lost), doubling is allowed on any first two cards
  and after a split, up to three splits, split Aces get one card each and stand,
  and a 21 made after a split pays 1:1 instead of as a blackjack.
- Players can start another round by confirming at the ready zone.

### Game modes

- **Classic Blackjack**: standard rules, 2,000 starting chips, and 3:2
  Blackjack payout.
- **Double Exposure**: both dealer cards are visible, with adjusted rules and
  payouts.
- **Spanish 21**: tens are removed from the shoe and special bonuses apply.
- **High Stakes**: higher-value chips and minimum bets.

### Betting and player actions

- Move the hand marker over the bet zone to place chips.
- Move it over the undo zone to remove the last chip.
- Hold the marker over the Ready zone for three seconds to confirm the bet.
- Use a forward-and-back phone movement to perform **Hit**.
- Use a side-to-side phone movement to perform **Stand** or confirm a bet.
- Point the marker at **Double** to double the bet and receive one final card.
- Point the marker at **Split** when the first two cards have the same value.
- Double and Split are only offered while they are possible (for example, not
  after a hit), and only on the phone of the player whose turn it is.
- The hand being played is marked by a sphere floating above its cards,
  visible to every player; after a split it moves to the hand in play.
- On your turn, the message box lists the moves you can make right now.

### Multiplayer networking

- Photon PUN 2 room creation and joining.
- Rooms support up to five players.
- Seats are assigned according to join order and remain stable if a player
  leaves.
- Host-authoritative game logic.
- Synchronization of state changes, bets, confirmations, cards, player turns,
  actions, payouts, and new rounds through Photon RPCs.

### User interface and visual features

- Main menu, game-mode selection, room creation, room joining, and lobby.
- Four-page **How to Play** manual with previous and next page buttons.
- Manual page indicator and Return button.
- Animated card dealing from the camera to the player's hand.
- Chip stacks displayed at each betting zone.
- Hand chip display showing the selected chip denomination.
- Game status and round result messages.

## Technologies

- **Unity 6** — application and scene development.
- **Vuforia** — augmented-reality image target tracking.
- **Photon PUN 2** — multiplayer rooms and game-state networking.

### 3D models

The project uses the following Sketchfab models:

- [Blackjack table](https://sketchfab.com/3d-models/blackjack-table-baf7e3cbd14140ca81ea1a79d28fb635)
- [Poker chips](https://sketchfab.com/3d-models/poker-chips-lowpoly-286c87f6c24a4e298830f2edfbf915f5)
- [Cards](https://sketchfab.com/3d-models/playing-cards-793274af15df4f20848d83ab6d127493)