using System;
using UnityEngine;

namespace GachaDemo.Presentation
{
    [Serializable]
    public sealed class GachaCardVisualDefinition
    {
        public string RewardId;
        public string PortraitSprite;
        public string BadgeText;
        public string Subtitle;
    }

    [Serializable]
    public sealed class GachaCardCatalogConfig
    {
        public string DefaultCharacterPortrait;
        public string DefaultWeaponPortrait;
        public string DefaultMaterialPortrait;
        public GachaCardVisualDefinition[] Cards;
    }

    public static class GachaCardCatalogRuntime
    {
        private const string CatalogPath = "Configs/gacha_card_catalog";
        private static GachaCardCatalogConfig _cached;

        public static GachaCardCatalogConfig Get()
        {
            if (_cached != null)
            {
                return _cached;
            }

            var text = Resources.Load<TextAsset>(CatalogPath);
            if (text == null)
            {
                _cached = new GachaCardCatalogConfig();
                return _cached;
            }

            _cached = JsonUtility.FromJson<GachaCardCatalogConfig>(text.text);
            if (_cached == null)
            {
                _cached = new GachaCardCatalogConfig();
            }

            return _cached;
        }

        public static GachaCardVisualDefinition Find(string rewardId)
        {
            var catalog = Get();
            if (catalog.Cards == null || string.IsNullOrWhiteSpace(rewardId))
            {
                return null;
            }

            for (var i = 0; i < catalog.Cards.Length; i++)
            {
                var entry = catalog.Cards[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.RewardId))
                {
                    continue;
                }

                if (string.Equals(entry.RewardId, rewardId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
