using System.Collections;
using System.Collections.Generic;
using GachaDemo.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GachaDemo.Presentation
{
    public sealed class RevealTimelineController : MonoBehaviour
    {
        [SerializeField] private Image flashImage;
        [SerializeField] private Image fiveStarBorder;
        [SerializeField] private Image meteorTrail;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private GameObject previewCard;
        [SerializeField] private float revealDuration = 0.3f;

        private Outline _fiveStarOutline;
        private bool _skipRequested;
        private CanvasGroup _previewCardCanvasGroup;
        private bool _previewFrontVisible;
        private readonly List<Sprite> _runtimePreviewSprites = new List<Sprite>(8);

        public IEnumerator PlayReveal(GachaReward reward)
        {
            if (_skipRequested)
            {
                _skipRequested = false;
                yield break;
            }

            var theme = GachaThemeRuntime.Get();
            var revealColor = GetRevealColor(theme, reward.Star);
            var duration = GetDurationByStar(reward.Star);

            rarityText.text = $"{reward.Star}★  {reward.RewardName}";
            rarityText.color = new Color(1f, 1f, 1f, 0f);
            rarityText.gameObject.SetActive(true);
            rarityText.transform.localScale = Vector3.one;

            SetupRevealVisuals(reward, revealColor);
            SetupPreviewCard(reward, theme);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (_skipRequested)
                {
                    _skipRequested = false;
                    break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var ease = 1f - Mathf.Pow(1f - t, 2f);
                var alpha = t < 0.25f ? t / 0.25f : (t > 0.8f ? (1f - t) / 0.2f : 1f);
                var safeAlpha = Mathf.Clamp01(alpha);

                rarityText.color = new Color(revealColor.r, revealColor.g, revealColor.b, safeAlpha);
                rarityText.transform.localScale = Vector3.one * (1f + ease * 0.07f);

                if (flashImage != null && flashImage.gameObject.activeSelf)
                {
                    var flashBase = reward.Star switch
                    {
                        5 => 0.04f,
                        4 => 0.028f,
                        _ => 0f
                    };
                    var flashBoost = reward.Star switch
                    {
                        5 => 0.07f,
                        4 => 0.04f,
                        _ => 0f
                    };
                    flashImage.color = new Color(revealColor.r, revealColor.g, revealColor.b, flashBase + safeAlpha * flashBoost);
                }

                if (meteorTrail != null)
                {
                    meteorTrail.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-420f, 420f, t), Mathf.Lerp(160f, -100f, t));
                    meteorTrail.color = new Color(revealColor.r, revealColor.g, revealColor.b, Mathf.Lerp(0.85f, 0f, t));
                }

                if (fiveStarBorder != null && reward.Star >= 4)
                {
                    var pulse = 0.7f + Mathf.Sin(Time.time * 10f) * 0.3f;
                    if (_fiveStarOutline != null)
                    {
                        _fiveStarOutline.effectColor = new Color(revealColor.r, revealColor.g, revealColor.b, pulse);
                    }
                    fiveStarBorder.transform.localScale = Vector3.one * (1.01f + pulse * 0.04f);
                }

                if (previewCard != null)
                {
                    var baseScale = 0.86f + ease * 0.14f;
                    var flipX = 1f;
                    const float flipStart = 0.28f;
                    const float flipEnd = 0.62f;
                    if (t < flipStart)
                    {
                        SetPreviewFace(false, reward, theme);
                    }
                    else if (t <= flipEnd)
                    {
                        var flipProgress = Mathf.InverseLerp(flipStart, flipEnd, t);
                        if (flipProgress < 0.5f)
                        {
                            SetPreviewFace(false, reward, theme);
                            flipX = Mathf.Lerp(1f, 0.05f, flipProgress / 0.5f);
                        }
                        else
                        {
                            SetPreviewFace(true, reward, theme);
                            flipX = Mathf.Lerp(0.05f, 1f, (flipProgress - 0.5f) / 0.5f);
                        }
                    }
                    else
                    {
                        SetPreviewFace(true, reward, theme);
                    }

                    var shakeStrength = t < flipStart ? GetPreFlipShakeStrength(t, flipStart, reward.Star) : 0f;
                    var shakeX = Mathf.Sin(Time.time * 46f) * shakeStrength;
                    var shakeY = Mathf.Cos(Time.time * 35f) * shakeStrength * 0.55f;
                    if (previewCard.TryGetComponent<RectTransform>(out var previewRect))
                    {
                        previewRect.anchoredPosition = new Vector2(shakeX, -10f + shakeY);
                    }

                    UpdateBackFaceShimmer(t, flipStart, reward.Star);
                    previewCard.transform.localScale = new Vector3(baseScale * flipX, baseScale, 1f);
                    previewCard.transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-8f, 0f, ease), 0f);
                    if (_previewCardCanvasGroup != null)
                    {
                        _previewCardCanvasGroup.alpha = safeAlpha;
                    }
                }

                yield return null;
            }

            CleanupVisuals();
        }

        public void RequestSkip()
        {
            _skipRequested = true;
        }

        private void SetupRevealVisuals(GachaReward reward, Color revealColor)
        {
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(true);
                flashImage.color = new Color(revealColor.r, revealColor.g, revealColor.b, 0f);
                flashImage.rectTransform.sizeDelta = reward.Star switch
                {
                    5 => new Vector2(440f, 190f),
                    4 => new Vector2(380f, 160f),
                    _ => new Vector2(0f, 0f)
                };
                flashImage.gameObject.SetActive(reward.Star >= 4);
            }

            if (meteorTrail != null)
            {
                meteorTrail.gameObject.SetActive(true);
                meteorTrail.rectTransform.sizeDelta = reward.Star switch
                {
                    5 => new Vector2(380f, 22f),
                    4 => new Vector2(320f, 18f),
                    _ => new Vector2(260f, 14f)
                };
                meteorTrail.rectTransform.anchoredPosition = new Vector2(-420f, 160f);
                meteorTrail.color = new Color(revealColor.r, revealColor.g, revealColor.b, 0.8f);
            }

            if (fiveStarBorder != null)
            {
                fiveStarBorder.gameObject.SetActive(reward.Star >= 4);
                _fiveStarOutline ??= fiveStarBorder.GetComponent<Outline>();
                if (_fiveStarOutline != null)
                {
                    _fiveStarOutline.effectColor = new Color(revealColor.r, revealColor.g, revealColor.b, 0.8f);
                }
            }
        }

        private void SetupPreviewCard(GachaReward reward, GachaThemeConfig theme)
        {
            if (previewCard == null)
            {
                return;
            }

            previewCard.SetActive(true);
            if (_previewCardCanvasGroup == null)
            {
                _previewCardCanvasGroup = previewCard.GetComponent<CanvasGroup>();
            }
            if (_previewCardCanvasGroup == null)
            {
                _previewCardCanvasGroup = previewCard.AddComponent<CanvasGroup>();
            }
            _previewCardCanvasGroup.alpha = 0f;
            EnsurePreviewBackFace();
            _previewFrontVisible = true;

            var cardRect = previewCard.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = new Vector2(320f, 230f);
            }

            var cardImage = previewCard.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = GetCardColor(theme, reward.Star);
            }

            var stripeImage = FindChildImage(previewCard.transform, "RarityStripe");
            if (stripeImage != null)
            {
                stripeImage.color = GetStripeColor(theme, reward.Star);
            }

            var glowImage = FindChildImage(previewCard.transform, "Glow");
            if (glowImage != null)
            {
                glowImage.color = GetGlowColor(theme, reward.Star);
            }

            var frameOverlay = FindChildImage(previewCard.transform, "CardFrameOverlay");
            if (frameOverlay != null)
            {
                frameOverlay.color = GetFrameOverlayColor(reward.Star);
            }

            var namePlate = FindChildImage(previewCard.transform, "NamePlate");
            if (namePlate != null)
            {
                namePlate.color = GetNamePlateColor(reward.Star);
            }

            var upTag = previewCard.transform.Find("UpTag");
            if (upTag != null)
            {
                upTag.gameObject.SetActive(reward.IsUp);
            }

            var dupTag = previewCard.transform.Find("DupTag");
            if (dupTag != null)
            {
                dupTag.gameObject.SetActive(reward.IsDuplicate);
            }

            var typeBadge = previewCard.transform.Find("TypeBadge");
            if (typeBadge != null && typeBadge.TryGetComponent<Image>(out var badgeImage))
            {
                badgeImage.color = GetTypeBadgeColor(reward.RewardId);
            }

            var texts = previewCard.GetComponentsInChildren<TextMeshProUGUI>(true);
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

            ApplyPortrait(previewCard.transform, reward);
            SetPreviewFace(false, reward, theme);
        }

        private void CleanupVisuals()
        {
            rarityText.gameObject.SetActive(false);
            rarityText.transform.localScale = Vector3.one;

            if (fiveStarBorder != null)
            {
                fiveStarBorder.gameObject.SetActive(false);
                fiveStarBorder.transform.localScale = Vector3.one;
            }

            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(false);
            }

            if (meteorTrail != null)
            {
                meteorTrail.gameObject.SetActive(false);
            }

            if (previewCard != null)
            {
                previewCard.SetActive(false);
                previewCard.transform.localScale = Vector3.one;
                previewCard.transform.localRotation = Quaternion.identity;
                if (previewCard.TryGetComponent<RectTransform>(out var previewRect))
                {
                    previewRect.anchoredPosition = new Vector2(0f, -10f);
                }
            }

            for (var i = 0; i < _runtimePreviewSprites.Count; i++)
            {
                var sprite = _runtimePreviewSprites[i];
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

            _runtimePreviewSprites.Clear();
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
                    _runtimePreviewSprites.Add(generatedSprite);
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

        private static Image FindChildImage(Transform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private void EnsurePreviewBackFace()
        {
            if (previewCard == null || previewCard.transform.Find("BackFace") != null)
            {
                return;
            }

            var backFaceGo = new GameObject("BackFace", typeof(RectTransform), typeof(Image));
            backFaceGo.transform.SetParent(previewCard.transform, false);
            var backFaceRect = backFaceGo.GetComponent<RectTransform>();
            backFaceRect.anchorMin = new Vector2(0.5f, 0.5f);
            backFaceRect.anchorMax = new Vector2(0.5f, 0.5f);
            backFaceRect.pivot = new Vector2(0.5f, 0.5f);
            backFaceRect.sizeDelta = new Vector2(162f, 120f);
            var backFaceImage = backFaceGo.GetComponent<Image>();
            backFaceImage.color = new Color(0.2f, 0.25f, 0.36f, 0.96f);
            backFaceImage.raycastTarget = false;

            var shimmerGo = new GameObject("BackFaceShimmer", typeof(RectTransform), typeof(Image));
            shimmerGo.transform.SetParent(backFaceGo.transform, false);
            var shimmerRect = shimmerGo.GetComponent<RectTransform>();
            shimmerRect.anchorMin = new Vector2(0.5f, 0.5f);
            shimmerRect.anchorMax = new Vector2(0.5f, 0.5f);
            shimmerRect.pivot = new Vector2(0.5f, 0.5f);
            shimmerRect.sizeDelta = new Vector2(48f, 164f);
            shimmerRect.localRotation = Quaternion.Euler(0f, 0f, -18f);
            shimmerRect.anchoredPosition = new Vector2(-140f, 0f);
            var shimmerImage = shimmerGo.GetComponent<Image>();
            shimmerImage.color = new Color(1f, 1f, 1f, 0f);
            shimmerImage.raycastTarget = false;

            var backTextGo = new GameObject("BackFaceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            backTextGo.transform.SetParent(backFaceGo.transform, false);
            var backTextRect = backTextGo.GetComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;
            var backText = backTextGo.GetComponent<TextMeshProUGUI>();
            backText.text = "WISH";
            backText.alignment = TextAlignmentOptions.Center;
            backText.fontSize = 30f;
            backText.fontStyle = FontStyles.Bold;
            backText.color = new Color(0.92f, 0.95f, 1f, 0.85f);
            backText.font = rarityText != null ? rarityText.font : TMP_Settings.defaultFontAsset;
            backText.raycastTarget = false;
        }

        private void SetPreviewFace(bool showFront, GachaReward reward, GachaThemeConfig theme)
        {
            if (previewCard == null || _previewFrontVisible == showFront)
            {
                return;
            }

            _previewFrontVisible = showFront;
            var backFace = previewCard.transform.Find("BackFace");
            for (var i = 0; i < previewCard.transform.childCount; i++)
            {
                var child = previewCard.transform.GetChild(i);
                if (backFace != null && child == backFace)
                {
                    child.gameObject.SetActive(!showFront);
                }
                else
                {
                    child.gameObject.SetActive(showFront);
                }
            }

            var rootImage = previewCard.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = showFront ? GetCardColor(theme, reward.Star) : GetBackCardColor(reward.Star);
            }

            if (backFace != null && backFace.TryGetComponent<Image>(out var backFaceImage))
            {
                backFaceImage.color = GetBackFaceInnerColor(reward.Star);
            }
        }

        private static Color GetRevealColor(GachaThemeConfig theme, int star)
        {
            return star switch
            {
                5 => theme.FiveStarRevealColor.ToColor(new Color(1f, 0.84f, 0.32f, 1f)),
                4 => theme.FourStarRevealColor.ToColor(new Color(0.72f, 0.56f, 1f, 1f)),
                _ => theme.ThreeStarRevealColor.ToColor(new Color(0.54f, 0.78f, 1f, 1f))
            };
        }

        private float GetDurationByStar(int star)
        {
            return star switch
            {
                5 => revealDuration + 0.4f,
                4 => revealDuration + 0.2f,
                _ => revealDuration + 0.04f
            };
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

        private static Color GetBackCardColor(int star)
        {
            return star switch
            {
                5 => new Color(0.42f, 0.33f, 0.14f, 0.97f),
                4 => new Color(0.28f, 0.22f, 0.4f, 0.9f),
                _ => new Color(0.17f, 0.24f, 0.36f, 0.82f)
            };
        }

        private static Color GetBackFaceInnerColor(int star)
        {
            return star switch
            {
                5 => new Color(0.66f, 0.55f, 0.27f, 0.95f),
                4 => new Color(0.49f, 0.38f, 0.66f, 0.95f),
                _ => new Color(0.35f, 0.45f, 0.62f, 0.95f)
            };
        }

        private void UpdateBackFaceShimmer(float t, float flipStart, int star)
        {
            if (previewCard == null || _previewFrontVisible)
            {
                return;
            }

            var shimmer = previewCard.transform.Find("BackFace/BackFaceShimmer");
            if (shimmer == null || !shimmer.TryGetComponent<Image>(out var shimmerImage))
            {
                return;
            }

            var shimmerRect = shimmer.GetComponent<RectTransform>();
            var sweepT = Mathf.Clamp01(t / Mathf.Max(0.01f, flipStart));
            shimmerRect.anchoredPosition = new Vector2(Mathf.Lerp(-140f, 140f, sweepT), 0f);
            var brightness = star switch
            {
                5 => 0.32f,
                4 => 0.26f,
                _ => 0.22f
            };
            var pulse = 0.5f + Mathf.Sin(Time.time * 12f) * 0.5f;
            shimmerImage.color = new Color(1f, 1f, 1f, brightness * pulse);
        }

        private static float GetPreFlipShakeStrength(float t, float flipStart, int star)
        {
            var phase = Mathf.Clamp01(t / Mathf.Max(0.01f, flipStart));
            var ramp = 1f - Mathf.Abs(phase - 0.58f) / 0.58f;
            var maxShake = star switch
            {
                5 => 4.8f,
                4 => 3.6f,
                _ => 2.8f
            };
            return Mathf.Max(0f, ramp) * maxShake;
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
    }
}
