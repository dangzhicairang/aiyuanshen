using System.Collections;
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
        private Coroutine _showRoutine;
        private GridLayoutGroup _grid;
        private Vector2 _defaultCellSize;
        private Vector2 _defaultSpacing;
        private int _defaultColumnCount;
        private Vector2 _defaultGridSize;
        private Vector2 _defaultGridPos;
        private bool _skipReveal;
        private bool _singleMode;
        private readonly List<Sprite> _runtimePortraitSprites = new List<Sprite>(12);

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
            _skipReveal = false;
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
            }

            ConfigureLayout(rewards.Count);

            for (var i = 0; i < rewards.Count; i++)
            {
                CreateCard(rewards[i], i);
            }

            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }

            _showRoutine = StartCoroutine(PlayCascadeReveal());
        }

        public void Hide()
        {
            _skipReveal = false;
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }

            ResetLayout();
            ClearCards();
        }

        public void RequestSkipReveal()
        {
            _skipReveal = true;
            for (var i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                if (card == null)
                {
                    continue;
                }

                SetCardChildrenVisible(card.transform, true);
                card.transform.localScale = Vector3.one;
                card.transform.localRotation = Quaternion.identity;
            }
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
                cardRect.sizeDelta = _singleMode ? new Vector2(420f, 300f) : _defaultCellSize;
            }

            var theme = GachaThemeRuntime.Get();
            var cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = GetCardColor(theme, reward.Star);
            }

            var stripeImage = FindChildImage(card.transform, "RarityStripe");
            if (stripeImage != null)
            {
                stripeImage.color = GetStripeColor(theme, reward.Star);
            }

            var frameOverlay = FindChildImage(card.transform, "CardFrameOverlay");
            if (frameOverlay != null)
            {
                frameOverlay.color = GetFrameOverlayColor(reward.Star);
            }

            var glowImage = FindChildImage(card.transform, "Glow");
            if (glowImage != null)
            {
                glowImage.color = GetGlowColor(theme, reward.Star);
            }

            var namePlate = FindChildImage(card.transform, "NamePlate");
            if (namePlate != null)
            {
                namePlate.color = GetNamePlateColor(reward.Star);
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

            var typeBadge = card.transform.Find("TypeBadge");
            if (typeBadge != null)
            {
                var badgeImage = typeBadge.GetComponent<Image>();
                if (badgeImage != null)
                {
                    badgeImage.color = GetTypeBadgeColor(reward.RewardId);
                }
            }

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text.name == "NameText")
                {
                    text.text = reward.RewardName;
                }
                else if (text.name == "MetaText")
                {
                    text.text = BuildMetaLine(reward, GachaCardCatalogRuntime.Find(reward.RewardId));
                }
                else if (text.name == "StarText")
                {
                    text.text = BuildStarText(reward.Star);
                }
                else if (text.name == "TypeBadgeText")
                {
                    text.text = reward.RewardId.StartsWith("c_") ? "角色" : "武器";
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

            for (var i = 0; i < _runtimePortraitSprites.Count; i++)
            {
                var sprite = _runtimePortraitSprites[i];
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

            _runtimePortraitSprites.Clear();
        }

        private IEnumerator PlayCascadeReveal()
        {
            for (var i = 0; i < _activeCards.Count; i++)
            {
                if (_skipReveal)
                {
                    break;
                }

                var card = _activeCards[i];
                if (card == null)
                {
                    continue;
                }

                StartCoroutine(PlayCardFlip(card.transform));
                yield return new WaitForSeconds(_singleMode ? 0f : cascadeDelay);
            }

            RequestSkipReveal();
        }

        private IEnumerator PlayCardFlip(Transform card)
        {
            if (_skipReveal)
            {
                SetCardChildrenVisible(card, true);
                yield break;
            }

            var duration = _singleMode ? 0.34f : 0.2f;
            var elapsed = 0f;
            var childrenRevealed = false;
            SetCardChildrenVisible(card, false);
            card.localRotation = Quaternion.identity;

            while (elapsed < duration)
            {
                if (_skipReveal)
                {
                    break;
                }

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

        private void ConfigureLayout(int rewardCount)
        {
            _singleMode = rewardCount == 1;
            if (_grid == null || cardGridRoot == null)
            {
                return;
            }

            if (_singleMode)
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
            _singleMode = false;
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

        private static string BuildMetaLine(GachaReward reward, GachaCardVisualDefinition visual)
        {
            if (visual != null && !string.IsNullOrWhiteSpace(visual.Subtitle))
            {
                return visual.Subtitle;
            }

            var type = reward.RewardId.StartsWith("c_") ? "角色" : "武器";
            var upTag = reward.IsUp ? "UP" : "常驻";
            return $"{reward.Star} 星 · {type} · {upTag}";
        }

        private void ApplyPortrait(Transform cardRoot, GachaReward reward)
        {
            var portraitImage = FindChildImage(cardRoot, "PortraitFrame/PortraitImage");
            if (portraitImage == null)
            {
                return;
            }

            var visual = GachaCardCatalogRuntime.Find(reward.RewardId);
            var catalog = GachaCardCatalogRuntime.Get();
            var preferredPath = visual?.PortraitSprite;
            if (string.IsNullOrWhiteSpace(preferredPath))
            {
                preferredPath = GetDefaultPortraitPath(catalog, reward.RewardId);
            }

            var sprite = GachaThemeRuntime.TryLoadSprite(preferredPath);
            if (sprite != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.type = Image.Type.Simple;
                portraitImage.color = Color.white;
            }
            else
            {
                var generatedSprite = BuildCartoonPortraitSprite(reward);
                if (generatedSprite != null)
                {
                    portraitImage.sprite = generatedSprite;
                    portraitImage.type = Image.Type.Simple;
                    portraitImage.color = Color.white;
                    _runtimePortraitSprites.Add(generatedSprite);
                    sprite = generatedSprite;
                }
                else
                {
                    portraitImage.color = GetFallbackPortraitColor(reward.Star);
                }
            }

            var mark = cardRoot.Find("PortraitFrame/PortraitImage/PortraitMark");
            if (mark != null && mark.TryGetComponent<TextMeshProUGUI>(out var markText))
            {
                markText.text = string.IsNullOrWhiteSpace(visual?.BadgeText) ? GetTypeMark(reward) : visual.BadgeText;
                markText.color = new Color(1f, 1f, 1f, 0.9f);
                markText.fontSize = sprite != null ? 14f : 20f;
                markText.alignment = TextAlignmentOptions.Bottom;
            }
        }

        private static string GetDefaultPortraitPath(GachaCardCatalogConfig catalog, string rewardId)
        {
            if (catalog == null)
            {
                return null;
            }

            if (rewardId.StartsWith("c_"))
            {
                return catalog.DefaultCharacterPortrait;
            }

            if (rewardId.StartsWith("w_"))
            {
                return catalog.DefaultWeaponPortrait;
            }

            return catalog.DefaultMaterialPortrait;
        }

        private static string GetTypeMark(GachaReward reward)
        {
            if (reward.RewardId.StartsWith("c_"))
            {
                return "角色";
            }

            if (reward.RewardId.StartsWith("w_"))
            {
                return "武器";
            }

            return "材料";
        }

        private static Color GetFallbackPortraitColor(int star)
        {
            return star switch
            {
                5 => new Color(0.95f, 0.66f, 0.22f, 1f),
                4 => new Color(0.66f, 0.45f, 0.95f, 1f),
                _ => new Color(0.36f, 0.56f, 0.88f, 1f)
            };
        }

        private static Color GetFrameOverlayColor(int star)
        {
            return star switch
            {
                5 => new Color(1f, 0.9f, 0.6f, 0.18f),
                4 => new Color(0.86f, 0.74f, 1f, 0.16f),
                _ => new Color(0.8f, 0.9f, 1f, 0.14f)
            };
        }

        private static Color GetTypeBadgeColor(string rewardId)
        {
            if (rewardId.StartsWith("c_"))
            {
                return new Color(0.29f, 0.62f, 0.89f, 0.92f);
            }

            if (rewardId.StartsWith("w_"))
            {
                return new Color(0.46f, 0.5f, 0.64f, 0.92f);
            }

            return new Color(0.4f, 0.45f, 0.52f, 0.9f);
        }

        private static Color GetNamePlateColor(int star)
        {
            return star switch
            {
                5 => new Color(0.36f, 0.26f, 0.1f, 0.75f),
                4 => new Color(0.24f, 0.18f, 0.34f, 0.74f),
                _ => new Color(0.11f, 0.17f, 0.27f, 0.72f)
            };
        }

        private static string BuildStarText(int star)
        {
            star = Mathf.Clamp(star, 1, 5);
            return new string('★', star);
        }

        private static Sprite BuildCartoonPortraitSprite(GachaReward reward)
        {
            const int width = 192;
            const int height = 112;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var seed = Mathf.Abs((reward.RewardId + reward.RewardName).GetHashCode());
            var palette = BuildPalette(seed, reward.Star);

            FillGradient(texture, palette.BackgroundTop, palette.BackgroundBottom);
            DrawEllipse(texture, new Vector2(width * 0.5f, height * 0.15f), 80f, 22f, palette.Ribbon, 1f);
            DrawEllipse(texture, new Vector2(width * 0.5f, height * 0.88f), 74f, 46f, palette.Clothes, 1f);
            DrawRect(texture, (int)(width * 0.44f), (int)(height * 0.43f), 22, 18, palette.Skin);
            DrawEllipse(texture, new Vector2(width * 0.5f, height * 0.58f), 36f, 30f, palette.Skin, 1f);

            var hairLift = 5f + (seed % 6);
            DrawEllipse(texture, new Vector2(width * 0.5f, height * 0.68f + hairLift), 44f, 28f, palette.Hair, 1f);
            DrawEllipse(texture, new Vector2(width * 0.36f, height * 0.60f), 14f, 18f, palette.Hair, 1f);
            DrawEllipse(texture, new Vector2(width * 0.64f, height * 0.60f), 14f, 18f, palette.Hair, 1f);
            DrawRect(texture, (int)(width * 0.34f), (int)(height * 0.56f), 62, 8, palette.Hair);

            DrawEllipse(texture, new Vector2(width * 0.43f, height * 0.58f), 5f, 4f, Color.white, 1f);
            DrawEllipse(texture, new Vector2(width * 0.57f, height * 0.58f), 5f, 4f, Color.white, 1f);
            DrawEllipse(texture, new Vector2(width * 0.43f, height * 0.58f), 2.2f, 2.2f, palette.Eye, 1f);
            DrawEllipse(texture, new Vector2(width * 0.57f, height * 0.58f), 2.2f, 2.2f, palette.Eye, 1f);
            DrawRect(texture, (int)(width * 0.475f), (int)(height * 0.50f), 10, 2, palette.Mouth);

            if (reward.Star >= 5)
            {
                DrawDiamond(texture, new Vector2(width * 0.5f, height * 0.9f), 10, new Color(1f, 0.9f, 0.55f, 0.95f));
            }
            else if (reward.Star == 4)
            {
                DrawDiamond(texture, new Vector2(width * 0.5f, height * 0.9f), 8, new Color(0.78f, 0.64f, 1f, 0.92f));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static PortraitPalette BuildPalette(int seed, int star)
        {
            var hueBase = (seed % 100) / 100f;
            var saturation = Mathf.Lerp(0.35f, 0.65f, (seed % 17) / 16f);
            var value = Mathf.Lerp(0.55f, 0.88f, star / 5f);

            var hair = Color.HSVToRGB(hueBase, saturation, value);
            var bgTop = Color.HSVToRGB((hueBase + 0.08f) % 1f, 0.32f, 0.78f);
            var bgBottom = Color.HSVToRGB((hueBase + 0.16f) % 1f, 0.45f, 0.44f);
            var cloth = Color.HSVToRGB((hueBase + 0.5f) % 1f, 0.4f, 0.46f);
            var eye = Color.HSVToRGB((hueBase + 0.2f) % 1f, 0.7f, 0.28f);
            var ribbon = star switch
            {
                5 => new Color(0.96f, 0.82f, 0.46f, 0.92f),
                4 => new Color(0.76f, 0.62f, 0.98f, 0.9f),
                _ => new Color(0.58f, 0.74f, 0.95f, 0.88f)
            };

            return new PortraitPalette
            {
                BackgroundTop = bgTop,
                BackgroundBottom = bgBottom,
                Hair = hair,
                Skin = new Color(1f, 0.87f, 0.76f, 1f),
                Eye = eye,
                Mouth = new Color(0.72f, 0.35f, 0.4f, 1f),
                Clothes = cloth,
                Ribbon = ribbon
            };
        }

        private static void FillGradient(Texture2D texture, Color top, Color bottom)
        {
            var width = texture.width;
            var height = texture.height;
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                var c = Color.Lerp(bottom, top, t);
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawRect(Texture2D texture, int startX, int startY, int rectWidth, int rectHeight, Color color)
        {
            var xMin = Mathf.Clamp(startX, 0, texture.width - 1);
            var yMin = Mathf.Clamp(startY, 0, texture.height - 1);
            var xMax = Mathf.Clamp(startX + rectWidth, 0, texture.width);
            var yMax = Mathf.Clamp(startY + rectHeight, 0, texture.height);
            for (var y = yMin; y < yMax; y++)
            {
                for (var x = xMin; x < xMax; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawEllipse(Texture2D texture, Vector2 center, float radiusX, float radiusY, Color color, float blend)
        {
            var xMin = Mathf.Clamp(Mathf.FloorToInt(center.x - radiusX), 0, texture.width - 1);
            var xMax = Mathf.Clamp(Mathf.CeilToInt(center.x + radiusX), 0, texture.width - 1);
            var yMin = Mathf.Clamp(Mathf.FloorToInt(center.y - radiusY), 0, texture.height - 1);
            var yMax = Mathf.Clamp(Mathf.CeilToInt(center.y + radiusY), 0, texture.height - 1);

            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var normalizedX = (x - center.x) / radiusX;
                    var normalizedY = (y - center.y) / radiusY;
                    if (normalizedX * normalizedX + normalizedY * normalizedY > 1f)
                    {
                        continue;
                    }

                    var current = texture.GetPixel(x, y);
                    texture.SetPixel(x, y, Color.Lerp(current, color, Mathf.Clamp01(blend)));
                }
            }
        }

        private static void DrawDiamond(Texture2D texture, Vector2 center, int radius, Color color)
        {
            var xMin = Mathf.Clamp(Mathf.FloorToInt(center.x - radius), 0, texture.width - 1);
            var xMax = Mathf.Clamp(Mathf.CeilToInt(center.x + radius), 0, texture.width - 1);
            var yMin = Mathf.Clamp(Mathf.FloorToInt(center.y - radius), 0, texture.height - 1);
            var yMax = Mathf.Clamp(Mathf.CeilToInt(center.y + radius), 0, texture.height - 1);

            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var dx = Mathf.Abs(x - center.x);
                    var dy = Mathf.Abs(y - center.y);
                    if (dx + dy <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private struct PortraitPalette
        {
            public Color BackgroundTop;
            public Color BackgroundBottom;
            public Color Hair;
            public Color Skin;
            public Color Eye;
            public Color Mouth;
            public Color Clothes;
            public Color Ribbon;
        }

        private static Color GetCardColor(GachaThemeConfig theme, int star)
        {
            return star switch
            {
                5 => theme.CardBaseColors != null ? theme.CardBaseColors.FiveStar.ToColor(new Color(0.95f, 0.73f, 0.28f, 0.95f)) : new Color(0.95f, 0.73f, 0.28f, 0.95f),
                4 => theme.CardBaseColors != null ? theme.CardBaseColors.FourStar.ToColor(new Color(0.69f, 0.52f, 0.95f, 0.95f)) : new Color(0.69f, 0.52f, 0.95f, 0.95f),
                _ => theme.CardBaseColors != null ? theme.CardBaseColors.ThreeStar.ToColor(new Color(0.43f, 0.58f, 0.82f, 0.92f)) : new Color(0.43f, 0.58f, 0.82f, 0.92f)
            };
        }

        private static Color GetStripeColor(GachaThemeConfig theme, int star)
        {
            return star switch
            {
                5 => theme.CardStripeColors != null ? theme.CardStripeColors.FiveStar.ToColor(new Color(1f, 0.9f, 0.55f, 0.96f)) : new Color(1f, 0.9f, 0.55f, 0.96f),
                4 => theme.CardStripeColors != null ? theme.CardStripeColors.FourStar.ToColor(new Color(0.83f, 0.71f, 1f, 0.95f)) : new Color(0.83f, 0.71f, 1f, 0.95f),
                _ => theme.CardStripeColors != null ? theme.CardStripeColors.ThreeStar.ToColor(new Color(0.75f, 0.86f, 1f, 0.9f)) : new Color(0.75f, 0.86f, 1f, 0.9f)
            };
        }

        private static Color GetGlowColor(GachaThemeConfig theme, int star)
        {
            return star switch
            {
                5 => theme.CardGlowColors != null ? theme.CardGlowColors.FiveStar.ToColor(new Color(1f, 0.82f, 0.32f, 0.22f)) : new Color(1f, 0.82f, 0.32f, 0.22f),
                4 => theme.CardGlowColors != null ? theme.CardGlowColors.FourStar.ToColor(new Color(0.72f, 0.56f, 1f, 0.18f)) : new Color(0.72f, 0.56f, 1f, 0.18f),
                _ => theme.CardGlowColors != null ? theme.CardGlowColors.ThreeStar.ToColor(new Color(0.52f, 0.7f, 1f, 0.13f)) : new Color(0.52f, 0.7f, 1f, 0.13f)
            };
        }
    }
}
