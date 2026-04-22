using System.Collections;
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
        [SerializeField] private float revealDuration = 0.35f;
        private Outline _fiveStarOutline;

        public IEnumerator PlayReveal(GachaReward reward)
        {
            if (reward.Star < 5)
            {
                // For non-5-star rewards we skip center-stage title to keep pacing concise.
                yield break;
            }

            rarityText.text = $"{reward.Star}* {reward.RewardName}";
            rarityText.color = new Color(1f, 1f, 1f, 0f);
            rarityText.gameObject.SetActive(true);

            var duration = GetDurationByStar(reward.Star);
            var elapsed = 0f;
            rarityText.transform.localScale = Vector3.one;
            if (fiveStarBorder != null)
            {
                fiveStarBorder.gameObject.SetActive(reward.Star == 5);
                _fiveStarOutline ??= fiveStarBorder.GetComponent<Outline>();
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var ease = 1f - Mathf.Pow(1f - t, 2f);
                var alpha = t < 0.25f ? t / 0.25f : (t > 0.8f ? (1f - t) / 0.2f : 1f);
                rarityText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                if (fiveStarBorder != null && reward.Star == 5)
                {
                    var pulse = 0.7f + Mathf.Sin(Time.time * 10f) * 0.3f;
                    if (_fiveStarOutline != null)
                    {
                        _fiveStarOutline.effectColor = new Color(1f, 0.82f, 0.3f, pulse);
                    }
                    fiveStarBorder.transform.localScale = Vector3.one * (1.01f + pulse * 0.03f);
                }
                yield return null;
            }

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
        }

        private float GetDurationByStar(int star)
        {
            return star switch
            {
                5 => revealDuration + 0.3f,
                4 => revealDuration + 0.1f,
                _ => revealDuration
            };
        }

        // The previous meteor rectangle effect is removed because it looked like a blocking blue bar.
    }
}
