using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapPop
{
    public class Hud : MonoBehaviour
    {
        [SerializeField] TMP_Text movesText;
        [SerializeField] TMP_Text goalText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] Image goalIcon;
        [SerializeField] Color textColor = new Color32(0x0A, 0x0A, 0x0A, 0xFF);
        [SerializeField] Color lowMovesColor = new Color32(0xFF, 0x38, 0x48, 0xFF);
        [SerializeField] int lowMoves = 5;

        public RectTransform GoalTarget => goalIcon.rectTransform;

        readonly Dictionary<Transform, Coroutine> punches = new Dictionary<Transform, Coroutine>();
        Coroutine scoreRoutine;
        int shownScore;

        public void Setup(TileSet tileSet, int levelNumber, int goalColor, int goalCount, int moves)
        {
            levelText.text = "Level " + levelNumber.ToString(CultureInfo.InvariantCulture);
            tileSet.Apply(goalIcon, goalColor);
            SetGoal(goalCount, false);
            SetMoves(moves, false);
            SetScore(0, false);
        }

        public void SetMoves(int moves, bool animate = true)
        {
            movesText.text = moves.ToString(CultureInfo.InvariantCulture);
            movesText.color = moves <= lowMoves ? lowMovesColor : textColor;
            if (animate) Punch(movesText.transform);
        }

        public void SetGoal(int left, bool animate = true)
        {
            goalText.text = Mathf.Max(left, 0).ToString(CultureInfo.InvariantCulture);
            if (!animate) return;
            Punch(goalText.transform);
            Punch(goalIcon.transform, 0.35f);
        }

        public void SetScore(int score, bool animate = true)
        {
            if (scoreRoutine != null) StopCoroutine(scoreRoutine);
            if (!animate)
            {
                shownScore = score;
                scoreText.text = Format(score);
                return;
            }
            int from = shownScore;
            scoreRoutine = StartCoroutine(Tween.Run(0.4f, Ease.OutQuad, k =>
            {
                shownScore = Mathf.RoundToInt(Mathf.Lerp(from, score, k));
                scoreText.text = Format(shownScore);
            }));
            Punch(scoreText.transform, 0.15f);
        }

        void Punch(Transform target, float amount = 0.25f)
        {
            if (punches.TryGetValue(target, out Coroutine running) && running != null) StopCoroutine(running);
            target.localScale = Vector3.one;
            punches[target] = StartCoroutine(Tween.Punch(target, amount));
        }

        public static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
