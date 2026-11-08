using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace TapPop
{
    // Win and lose screens: backdrop fades in, content settles in with a small overshoot, stars fill one by one.
    // The more stars, the bigger the celebration.
    public class ResultPopup : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] TMP_Text titleText;
        [Tooltip("{0} is the level number")]
        [SerializeField] string titleFormat = "Level {0} complete";
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text primaryLabel;
        [Tooltip("The filled star of each slot; the empty star sits behind it")]
        [SerializeField] RectTransform[] starFills;
        [Tooltip("Where the confetti fountain starts")]
        [SerializeField] RectTransform starRow;
        [SerializeField] PopFx fx;
        [SerializeField] Sfx sfx;
        [Tooltip("One burst colour per star")]
        [SerializeField] Color[] starFxColors =
        {
            new Color32(0xFF, 0x38, 0x48, 0xFF),
            new Color32(0xFF, 0xD4, 0x14, 0xFF),
            new Color32(0x29, 0x7F, 0xFF, 0xFF),
        };
        [Tooltip("Confetti mixes the tile colours")]
        [SerializeField] Color[] confettiColors =
        {
            new Color32(0xFF, 0x38, 0x48, 0xFF),
            new Color32(0x29, 0x7F, 0xFF, 0xFF),
            new Color32(0x30, 0xD7, 0x54, 0xFF),
            new Color32(0xFF, 0xD4, 0x14, 0xFF),
            new Color32(0xB6, 0x38, 0xFF, 0xFF),
        };
        [Tooltip("Confetti pieces for 1, 2 and 3 stars")]
        [SerializeField] int[] confettiPerStars = { 12, 30, 56 };
        [SerializeField] float startScale = 0.92f;

        public void Show(int levelNumber, int stars, int score, string primary, string message = null)
        {
            titleText.text = string.Format(CultureInfo.InvariantCulture, titleFormat, levelNumber);
            primaryLabel.text = primary;
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(ShowRoutine(stars, score, message));
        }

        public void Hide()
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(HideRoutine());
        }

        IEnumerator ShowRoutine(int stars, int score, string message)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = true;
            panel.localScale = Vector3.one * startScale;
            foreach (RectTransform fill in starFills) fill.localScale = Vector3.zero;
            if (starRow != null) starRow.localScale = Vector3.one;
            if (scoreText != null) scoreText.text = "0";
            if (messageText != null && message != null) messageText.text = message;

            StartCoroutine(Tween.Fade(group, 1f, 0.2f));
            yield return Tween.Scale(panel, Vector3.one, 0.35f, Ease.OutBack);
            group.interactable = true;

            // Each star lands a little bigger, louder and higher than the one before.
            int earned = Mathf.Min(stars, starFills.Length);
            for (int i = 0; i < earned; i++)
            {
                yield return new WaitForSeconds(0.12f);
                RectTransform fill = starFills[i];
                yield return Tween.Scale(fill, Vector3.one * (1.25f + i * 0.1f), 0.14f, Ease.OutQuad);
                fx.Burst(fill.position, starFxColors[i % starFxColors.Length], 0f, 8 + i * 6, 1f + i * 0.25f);
                if (sfx != null) sfx.Pop(0f, 4 + i * 4);
                StartCoroutine(Tween.Scale(fill, Vector3.one, 0.2f, Ease.OutBack));
            }

            if (earned > 0 && starRow != null)
            {
                int pieces = confettiPerStars[Mathf.Min(earned, confettiPerStars.Length) - 1];
                fx.Confetti(starRow.position, confettiColors, pieces);
                // Three stars get a second wave and a small punch on the row.
                if (earned >= 3)
                {
                    fx.Confetti(starRow.position, confettiColors, pieces / 2, 0.3f);
                    StartCoroutine(Punch(starRow));
                }
            }

            if (scoreText != null)
                yield return Tween.Run(0.5f, Ease.OutQuad, k => scoreText.text = Hud.Format(Mathf.RoundToInt(score * k)));
        }

        static IEnumerator Punch(RectTransform target)
        {
            yield return Tween.Scale(target, Vector3.one * 1.12f, 0.12f, Ease.OutQuad);
            yield return Tween.Scale(target, Vector3.one, 0.3f, Ease.OutBack);
        }

        IEnumerator HideRoutine()
        {
            group.interactable = false;
            StartCoroutine(Tween.Fade(group, 0f, 0.2f));
            yield return Tween.Scale(panel, Vector3.one * startScale, 0.2f, Ease.InQuad);
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
