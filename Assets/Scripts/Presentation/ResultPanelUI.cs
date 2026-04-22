using System.Collections.Generic;
using GachaDemo.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GachaDemo.Presentation
{
    public sealed class ResultPanelUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private RectTransform cardGridRoot;
        [SerializeField] private GameObject cardTemplate;
        [SerializeField] private float cascadeDelay = 0.04f;

        private readonly List<GameObject> _activeCards = new List<GameObject>(10);
        private readonly List<Sprite> _generatedPortraitSprites = new List<Sprite>(10);
        private Coroutine _showRoutine;
        private GridLayoutGroup _grid;
        private Vector2 _defaultCellSize;
        private Vector2 _defaultSpacing;
        private int _defaultColumnCount;
        private Vector2 _defaultGridSize;
        private Vector2 _defaultGridPos;

        private void Awake()
        {
            _grid = cardGridRoot != null ? cardGridRoot.GetComponent<GridLayoutGroup>() : null;
            if (_grid == null || cardGridRoot == null)
            {
                return;
            }

            _defaultCellSize = _grid.cellSize;
            _defaultSpacing = _grid.spacing;
            _defaultColumnCount = _grid.constraintCount;
            _defaultGridSize = cardGridRoot.sizeDelta;
            _defaultGridPos = cardGridRoot.anchoredPosition;
        }

        public void Show(IReadOnlyList<GachaReward> rewards)
        {
            ClearCards();
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
            }

            ConfigureLayout(rewards.Count);

            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                CreateCard(reward, i);
            }

            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }

            _showRoutine = StartCoroutine(PlayCascadeReveal());
        }

        public void Hide()
        {
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }

            ResetLayout();
            ClearCards();
        }

        private void CreateCard(GachaReward reward, int index)
        {
            if (cardTemplate == null || cardGridRoot == null)
            {
                return;
            }

            var card = Instantiate(cardTemplate, cardGridRoot);
            card.SetActive(true);
            _activeCards.Add(card);
            card.transform.localScale = Vector3.one;

            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = rewardsAreSingle ? new Vector2(420f, 300f) : _defaultCellSize;
            }

            var cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = GetCardColor(reward.Star);
            }

            var stripeImage = FindChildImage(card.transform, "RarityStripe");
            if (stripeImage != null)
            {
                stripeImage.color = GetStripeColor(reward.Star);
            }

            var glowImage = FindChildImage(card.transform, "Glow");
            if (glowImage != null)
            {
                glowImage.color = GetGlowColor(reward.Star);
            }

            var upTag = card.transform.Find("UpTag");
            if (upTag != null)
            {
                upTag.gameObject.SetActive(reward.IsUp);
            }

            var dupTag = card.transform.Find("DupTag");
            if (dupTag != null)
            {
                dupTag.gameObject.SetActive(reward.IsDuplicate);
            }

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text.name == "NameText")
                {
                    text.text = reward.RewardName;
                }
                else if (text.name == "MetaText")
                {
                    var type = reward.IsUp ? "Rate Up" : "Standard";
                    text.text = $"{reward.Star} Star  {type}";
                }
            }

            ApplyPortrait(card.transform, reward);

            var cardOutline = card.GetComponent<Outline>();
            if (cardOutline != null)
            {
                cardOutline.effectColor = reward.Star switch
                {
                    5 => new Color(1f, 0.84f, 0.28f, 0.96f),
                    4 => new Color(0.72f, 0.55f, 1f, 0.92f),
                    _ => new Color(0.68f, 0.8f, 1f, 0.85f)
                };
            }

            card.name = $"RewardCard_{index + 1}_{reward.Star}Star";
        }

        private void ClearCards()
        {
            for (var i = 0; i < _activeCards.Count; i++)
            {
                if (_activeCards[i] != null)
                {
                    Destroy(_activeCards[i]);
                }
            }

            _activeCards.Clear();

            for (var i = 0; i < _generatedPortraitSprites.Count; i++)
            {
                var sprite = _generatedPortraitSprites[i];
                if (sprite == null)
                {
                    continue;
                }

                var texture = sprite.texture;
                Destroy(sprite);
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            _generatedPortraitSprites.Clear();
        }

        private System.Collections.IEnumerator PlayCascadeReveal()
        {
            for (var i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                if (card == null)
                {
                    continue;
                }

                StartCoroutine(PlayCardFlip(card.transform));
                yield return new WaitForSeconds(rewardsAreSingle ? 0f : cascadeDelay);
            }
        }

        private System.Collections.IEnumerator PlayCardFlip(Transform card)
        {
            var duration = rewardsAreSingle ? 0.34f : 0.2f;
            var elapsed = 0f;
            var childrenRevealed = false;
            SetCardChildrenVisible(card, false);
            card.localRotation = Quaternion.identity;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (t < 0.5f)
                {
                    var toBack = 1f - (t / 0.5f);
                    card.localScale = new Vector3(Mathf.Max(0.04f, toBack), 1f, 1f);
                }
                else
                {
                    if (!childrenRevealed)
                    {
                        SetCardChildrenVisible(card, true);
                        childrenRevealed = true;
                    }

                    var toFront = (t - 0.5f) / 0.5f;
                    card.localScale = new Vector3(Mathf.Max(0.04f, toFront), 1f, 1f);
                }
                yield return new WaitForEndOfFrame();
            }

            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
            SetCardChildrenVisible(card, true);
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private bool rewardsAreSingle;

        private void ConfigureLayout(int rewardCount)
        {
            rewardsAreSingle = rewardCount == 1;
            if (_grid == null || cardGridRoot == null)
            {
                return;
            }

            if (rewardsAreSingle)
            {
                _grid.constraintCount = 1;
                _grid.cellSize = new Vector2(420f, 300f);
                _grid.spacing = Vector2.zero;
                cardGridRoot.sizeDelta = new Vector2(440f, 320f);
                cardGridRoot.anchoredPosition = new Vector2(0f, -10f);
                return;
            }

            ResetLayout();
        }

        private void ResetLayout()
        {
            rewardsAreSingle = false;
            if (_grid == null || cardGridRoot == null)
            {
                return;
            }

            _grid.constraintCount = _defaultColumnCount;
            _grid.cellSize = _defaultCellSize;
            _grid.spacing = _defaultSpacing;
            cardGridRoot.sizeDelta = _defaultGridSize;
            cardGridRoot.anchoredPosition = _defaultGridPos;
        }

        private static void SetCardChildrenVisible(Transform card, bool visible)
        {
            for (var i = 0; i < card.childCount; i++)
            {
                card.GetChild(i).gameObject.SetActive(visible);
            }
        }

        private void ApplyPortrait(Transform cardRoot, GachaReward reward)
        {
            var portraitImage = FindChildImage(cardRoot, "PortraitFrame/PortraitImage");
            if (portraitImage == null)
            {
                return;
            }

            var variant = Random.Range(0, 3);
            var sprite = BuildPortraitSprite(reward, variant);
            if (sprite != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.type = Image.Type.Simple;
                portraitImage.preserveAspect = false;
                _generatedPortraitSprites.Add(sprite);
            }

            var mark = cardRoot.Find("PortraitFrame/PortraitImage/PortraitMark");
            if (mark != null && mark.TryGetComponent<TextMeshProUGUI>(out var markText))
            {
                markText.text = $"{GetTypeMark(reward)}{reward.Star}";
                markText.color = new Color(1f, 1f, 1f, 0.88f);
            }
        }

        private static string GetTypeMark(GachaReward reward)
        {
            if (reward.RewardId.StartsWith("c_"))
            {
                return "角";
            }

            if (reward.RewardId.StartsWith("w_"))
            {
                return "武";
            }

            return "物";
        }

        private static Sprite BuildPortraitSprite(GachaReward reward, int variant)
        {
            const int width = 162;
            const int height = 68;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var baseColor = reward.Star switch
            {
                5 => new Color(0.95f, 0.66f, 0.22f, 1f),
                4 => new Color(0.66f, 0.45f, 0.95f, 1f),
                _ => new Color(0.36f, 0.56f, 0.88f, 1f)
            };

            var seed = Mathf.Abs((reward.RewardId + "_" + reward.RewardName + "_" + variant).GetHashCode());
            var noiseA = (seed % 29) / 100f;
            var noiseB = (seed % 17) / 100f;
            var secondary = new Color(
                Mathf.Clamp01(baseColor.r * (0.55f + noiseA)),
                Mathf.Clamp01(baseColor.g * (0.58f + noiseB)),
                Mathf.Clamp01(baseColor.b * 0.62f),
                1f
            );
            var accent = Color.Lerp(Color.white, baseColor, 0.2f);

            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                for (var x = 0; x < width; x++)
                {
                    var c = Color.Lerp(baseColor, secondary, t);
                    if (variant == 0)
                    {
                        var stripe = (x + y) % 16 < 6 ? 0.28f : -0.05f;
                        c *= 1f + stripe;
                    }
                    else if (variant == 1)
                    {
                        var waves = Mathf.Sin((x + seed % 10) * 0.12f) * 0.16f;
                        c *= 1f + waves;
                    }
                    else
                    {
                        var check = ((x / 10) + (y / 10)) % 2 == 0 ? 0.2f : -0.12f;
                        c *= 1f + check;
                    }

                    if (x < 3 || y < 3 || x > width - 4 || y > height - 4)
                    {
                        c = Color.Lerp(c, accent, 0.7f);
                    }

                    texture.SetPixel(x, y, c);
                }
            }

            // Add a bold central emblem for high recognizability.
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var radius = 16 + (seed % 8);
            var emblem = Color.Lerp(Color.white, baseColor, 0.45f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius)
                    {
                        var origin = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, Color.Lerp(origin, emblem, 0.65f));
                    }
                    else if (Mathf.Abs(dist - radius) < 1.8f)
                    {
                        texture.SetPixel(x, y, accent);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Color GetCardColor(int star)
        {
            return star switch
            {
                5 => new Color(0.95f, 0.73f, 0.28f, 0.95f),
                4 => new Color(0.69f, 0.52f, 0.95f, 0.95f),
                _ => new Color(0.43f, 0.58f, 0.82f, 0.92f)
            };
        }

        private static Color GetStripeColor(int star)
        {
            return star switch
            {
                5 => new Color(1f, 0.9f, 0.55f, 0.96f),
                4 => new Color(0.83f, 0.71f, 1f, 0.95f),
                _ => new Color(0.75f, 0.86f, 1f, 0.9f)
            };
        }

        private static Color GetGlowColor(int star)
        {
            return star switch
            {
                5 => new Color(1f, 0.82f, 0.32f, 0.22f),
                4 => new Color(0.72f, 0.56f, 1f, 0.18f),
                _ => new Color(0.52f, 0.7f, 1f, 0.13f)
            };
        }
    }
}
