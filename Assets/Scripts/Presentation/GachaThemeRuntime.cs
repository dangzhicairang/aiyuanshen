using System;
using UnityEngine;

namespace GachaDemo.Presentation
{
    [Serializable]
    public struct ThemeColorValue
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color ToColor(Color fallback)
        {
            if (A <= 0f && R <= 0f && G <= 0f && B <= 0f)
            {
                return fallback;
            }

            return new Color(
                Mathf.Clamp01(R),
                Mathf.Clamp01(G),
                Mathf.Clamp01(B),
                Mathf.Clamp01(A)
            );
        }
    }

    [Serializable]
    public sealed class GachaThemeButtonSprites
    {
        public string SingleDraw;
        public string TenDraw;
        public string Skip;
    }

    [Serializable]
    public sealed class GachaThemeCardColors
    {
        public ThemeColorValue ThreeStar;
        public ThemeColorValue FourStar;
        public ThemeColorValue FiveStar;
    }

    [Serializable]
    public sealed class GachaThemeConfig
    {
        public string Title;
        public ThemeColorValue PanelBackground;
        public ThemeColorValue InfoStripBackground;
        public ThemeColorValue TitleColor;
        public ThemeColorValue CurrencyColor;
        public ThemeColorValue StateColor;
        public ThemeColorValue ResultColor;
        public ThemeColorValue FiveStarOutlineColor;
        public ThemeColorValue ThreeStarRevealColor;
        public ThemeColorValue FourStarRevealColor;
        public ThemeColorValue FiveStarRevealColor;
        public string PanelSprite;
        public string CardBackgroundSprite;
        public GachaThemeButtonSprites ButtonSprites;
        public GachaThemeCardColors CardBaseColors;
        public GachaThemeCardColors CardStripeColors;
        public GachaThemeCardColors CardGlowColors;
    }

    public static class GachaThemeRuntime
    {
        private const string ThemeConfigPath = "Configs/gacha_theme";
        private static GachaThemeConfig _cachedConfig;

        public static GachaThemeConfig Get()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var text = Resources.Load<TextAsset>(ThemeConfigPath);
            if (text == null)
            {
                _cachedConfig = new GachaThemeConfig();
                return _cachedConfig;
            }

            _cachedConfig = JsonUtility.FromJson<GachaThemeConfig>(text.text);
            if (_cachedConfig == null)
            {
                _cachedConfig = new GachaThemeConfig();
            }

            return _cachedConfig;
        }

        public static Sprite TryLoadSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            return Resources.Load<Sprite>(resourcePath);
        }
    }
}
