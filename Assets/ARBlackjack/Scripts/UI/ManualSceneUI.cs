using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ARBlackjack.Network;

namespace ARBlackjack.UI
{
    public class ManualSceneUI : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _manualText;

        private readonly string[] _pages =
        {
            "NORMAL BLACKJACK\n\nGet closer to 21 than the dealer without going over.\n\nNumber cards count their value, face cards 10, Aces 1 or 11.\n\nA win pays 1:1, a Blackjack 3:2, and a tie returns your bet. The dealer draws to 17 and hits a soft 17.",
            "GAME VARIATIONS\n\nClassic: standard rules, 2,000 chips.\n\nDouble Exposure: both dealer cards are face up. The dealer wins ties; Blackjack pays 1:1.\n\nSpanish 21: no 10s in the deck. Your 21 always wins; a 5-card 21 pays 3:2.\n\nHigh Stakes: 10,000 chips; chips and minimum bet are 500.",
            "BETTING PHASE\n\nHold your marker over Bet to add chips, or over Undo to take them back.\n\nHold it over Ready for 3 seconds, or flick your phone sideways, to confirm.\n\nThe cards are dealt once every player is ready.",
            "PLAYING PHASE\n\nHit: flick your phone forward and back.\n\nStand: flick your phone sideways.\n\nDouble: hold your marker over Double to double your bet and get one card.\n\nSplit: hold it over Split to play a pair as two hands.\n\nA sphere marks the hand being played.",
            "END OF THE ROUND\n\nA dealer Blackjack is shown right after the deal; you only lose your bet.\n\nTo play again, hold your marker over Ready (\"Next round?\") for 3 seconds."
        };

        private int _currentPage;
        private Button _previousButton;
        private Button _nextButton;
        private TMP_Text _pageIndicator;

        private void Start()
        {
            if (_backButton == null)
                _backButton = GetComponentInChildren<Button>();

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
                var backLabel = _backButton.GetComponentInChildren<TMP_Text>();
                if (backLabel != null) backLabel.text = "Return";
                _backButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -900f);
            }

            if (_manualText == null)
            {
                foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                {
                    if (text.text.Contains("NORMAL BLACKJACK"))
                    {
                        _manualText = text;
                        break;
                    }
                }
            }

            CreateNavigationButtons();
            ShowPage(0);
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene(NetworkConfig.MainMenuScene);
        }

        private void CreateNavigationButtons()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            _previousButton = CreateArrowButton(canvas.transform, "PreviousPage", "<", -330f);
            _nextButton = CreateArrowButton(canvas.transform, "NextPage", ">", 330f);
            _pageIndicator = CreatePageIndicator(canvas.transform);
            _previousButton.onClick.AddListener(() => ShowPage(_currentPage - 1));
            _nextButton.onClick.AddListener(() => ShowPage(_currentPage + 1));
        }

        private Button CreateArrowButton(Transform parent, string name, string label, float xPosition)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(xPosition, -650f);
            rectTransform.sizeDelta = new Vector2(180f, 140f);

            var sourceImage = _backButton != null ? _backButton.GetComponent<Image>() : null;
            var image = buttonObject.GetComponent<Image>();
            if (sourceImage != null)
            {
                image.sprite = sourceImage.sprite;
                image.type = sourceImage.type;
                image.color = sourceImage.color;
            }
            var button = buttonObject.GetComponent<Button>();
            if (_backButton != null)
            {
                button.colors = _backButton.colors;
                button.transition = _backButton.transition;
            }
            button.targetGraphic = image;
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            var sourceText = _backButton != null ? _backButton.GetComponentInChildren<TMP_Text>() : null;
            if (sourceText != null)
            {
                text.font = sourceText.font;
                text.fontStyle = sourceText.fontStyle;
                text.color = sourceText.color;
            }
            text.fontSize = 65f;
            text.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private TMP_Text CreatePageIndicator(Transform parent)
        {
            var indicatorObject = new GameObject("PageIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
            indicatorObject.transform.SetParent(parent, false);

            var rectTransform = indicatorObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0f, -650f);
            rectTransform.sizeDelta = new Vector2(300f, 100f);

            var indicator = indicatorObject.GetComponent<TextMeshProUGUI>();
            var sourceText = _backButton != null ? _backButton.GetComponentInChildren<TMP_Text>() : null;
            if (sourceText != null)
            {
                indicator.font = sourceText.font;
                indicator.fontStyle = sourceText.fontStyle;
                indicator.color = sourceText.color;
            }
            indicator.fontSize = 45f;
            indicator.alignment = TextAlignmentOptions.Center;
            indicator.color = Color.yellow;
            return indicator;
        }

        private void ShowPage(int page)
        {
            if (_manualText == null || page < 0 || page >= _pages.Length) return;

            _currentPage = page;
            _manualText.text = _pages[_currentPage];
            if (_previousButton != null) _previousButton.interactable = _currentPage > 0;
            if (_nextButton != null) _nextButton.interactable = _currentPage < _pages.Length - 1;
            if (_pageIndicator != null) _pageIndicator.text = $"{_currentPage + 1} of {_pages.Length}";
        }
    }
}
