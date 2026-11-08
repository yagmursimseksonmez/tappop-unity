using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace TapPop
{
    public class StartScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [Tooltip("The logo mark: turns slowly and breathes")]
        [SerializeField] RectTransform mark;
        [SerializeField] LevelPath levelPath;
        [Tooltip("Names the level Play starts")]
        [SerializeField] TMP_Text playLabel;
        [SerializeField] float spinSpeed = 18f;
        [SerializeField] float breathe = 0.03f;

        // unlocked: index of the furthest playable level; Play starts it.
        public void Show(int unlocked, IReadOnlyList<int> stars)
        {
            levelPath.Show(unlocked, stars);
            playLabel.text = "Play level " + (unlocked + 1).ToString(CultureInfo.InvariantCulture);
            gameObject.SetActive(true);
            StopAllCoroutines();
            group.blocksRaycasts = true;
            group.interactable = true;
            StartCoroutine(Tween.Fade(group, 1f, 0.25f));
        }

        public void Hide()
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            group.blocksRaycasts = false;
            group.interactable = false;
            StartCoroutine(HideRoutine());
        }

        void Update()
        {
            float t = Time.time;
            mark.localRotation = Quaternion.Euler(0f, 0f, -t * spinSpeed);
            mark.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f) * breathe);
        }

        IEnumerator HideRoutine()
        {
            yield return Tween.Fade(group, 0f, 0.25f);
            gameObject.SetActive(false);
        }
    }
}
