using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace TapPop
{
    // Win and lose screens: backdrop fades in, content settles in with a small overshoot, stars fill one by one.
    public class ResultPopup : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text primaryLabel;
        [Tooltip("The filled star of each slot; the empty star sits behind it")]
        [SerializeField] RectTransform[] starFills;
        [SerializeField] PopFx fx;
        [Tooltip("One burst colour per star")]
        [SerializeField] Color[] starFxColors =
        {
            new Color32(0xFF, 0x38, 0x48, 0xFF),
            new Color32(0xFF, 0xD4, 0x14, 0xFF),
            new Color32(0x29, 0x7F, 0xFF, 0xFF),
        };
        [SerializeField] float startScale = 0.92f;

        public void Show(int levelNumber, int stars, int score, string primary, string message = null)
        {
            levelText.text = "Level " + levelNumber.ToString(CultureInfo.InvariantCulture);
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
            if (scoreText != null) scoreText.text = "0";
            if (messageText != null && message != null) messageText.text = message;

            StartCoroutine(Tween.Fade(group, 1f, 0.2f));
            yield return Tween.Scale(panel, Vector3.one, 0.35f, Ease.OutBack);
            group.interactable = true;

            for (int i = 0; i < starFills.Length && i < stars; i++)
            {
                yield return new WaitForSeconds(0.12f);
                RectTransform fill = starFills[i];
                yield return Tween.Scale(fill, Vector3.one * 1.25f, 0.14f, Ease.OutQuad);
                fx.Burst(fill.position, starFxColors[i % starFxColors.Length]);
                StartCoroutine(Tween.Scale(fill, Vector3.one, 0.2f, Ease.OutBack));
            }

            if (scoreText != null)
                yield return Tween.Run(0.5f, Ease.OutQuad, k => scoreText.text = Hud.Format(Mathf.RoundToInt(score * k)));
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
