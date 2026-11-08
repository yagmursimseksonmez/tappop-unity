using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapPop
{
    public class GameController : MonoBehaviour
    {
        [Tooltip("Played in order; each win unlocks the next")]
        [SerializeField] LevelConfig[] levels;
        [SerializeField] TileSet tileSet;
        [SerializeField] Board board;
        [SerializeField] Hud hud;
        [SerializeField] PopFx fx;
        [SerializeField] StartScreen startScreen;
        [SerializeField] ResultPopup winPopup;
        [SerializeField] ResultPopup losePopup;

        // Saved in the browser, so a reload continues where the player left off.
        const string UnlockedKey = "TapPop.Level";
        const string StarsKey = "TapPop.Stars.";

        int unlocked;      // furthest playable level
        int[] bestStars;   // per level, 0 until won
        int levelIndex;    // level being played
        int movesLeft;
        int score;
        int goalLeft;
        int goalShown;
        bool playing;

        LevelConfig Level => levels[levelIndex];
        int LevelNumber => levelIndex + 1;
        bool IsLastLevel => levelIndex == levels.Length - 1;

        void Awake()
        {
            board.GroupPopped += OnGroupPopped;
            board.Settled += OnSettled;
            unlocked = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, levels.Length - 1);
            bestStars = new int[levels.Length];
            for (int i = 0; i < levels.Length; i++) bestStars[i] = PlayerPrefs.GetInt(StarsKey + i, 0);
            levelIndex = unlocked;
        }

        void Start()
        {
            hud.Setup(tileSet, LevelNumber, Level.goalColor, Level.goalCount, Level.moves);
            winPopup.gameObject.SetActive(false);
            losePopup.gameObject.SetActive(false);
            startScreen.Show(unlocked, bestStars);
        }

        // Button targets, wired in the scene.
        public void Play() => PlayLevel(unlocked);

        // Level path nodes: any level up to the furthest unlocked one.
        public void PlayLevel(int index)
        {
            if (index < 0 || index > unlocked) return;
            levelIndex = index;
            startScreen.Hide();
            StartLevel();
        }

        // Primary popup button: after a win the next level is already current, after a loss it is the same one.
        public void Restart()
        {
            winPopup.Hide();
            losePopup.Hide();
            StartLevel();
        }

        public void BackToMenu()
        {
            winPopup.Hide();
            losePopup.Hide();
            board.InputEnabled = false;
            startScreen.Show(unlocked, bestStars);
        }

        void StartLevel()
        {
            StopAllCoroutines();
            fx.Clear();
            movesLeft = Level.moves;
            score = 0;
            goalLeft = goalShown = Level.goalCount;
            playing = true;
            hud.Setup(tileSet, LevelNumber, Level.goalColor, Level.goalCount, Level.moves);
            board.Build(Level);
            board.InputEnabled = true;
        }

        // Step 0 is the tap and costs a move; chain steps are free.
        void OnGroupPopped(int color, IReadOnlyList<TileView> tiles, int chainStep)
        {
            if (chainStep == 0)
            {
                movesLeft--;
                hud.SetMoves(movesLeft);
            }
            score += Level.ScoreFor(tiles.Count);
            hud.SetScore(score);

            if (color != Level.goalColor || goalLeft <= 0) return;
            int collected = Mathf.Min(tiles.Count, goalLeft);
            goalLeft -= collected;
            for (int i = 0; i < collected; i++)
            {
                fx.Fly(tileSet.SpriteFor(color), tileSet.TintFor(color), tiles[i].transform.position, hud.GoalTarget,
                    0.05f + i * 0.05f, () => hud.SetGoal(--goalShown));
            }
        }

        void OnSettled()
        {
            if (!playing) return;
            if (goalLeft <= 0) StartCoroutine(End(true));
            else if (movesLeft <= 0) StartCoroutine(End(false));
        }

        IEnumerator End(bool won)
        {
            playing = false;
            board.InputEnabled = false;
            yield return new WaitForSeconds(0.6f);

            if (!won)
            {
                losePopup.Show(LevelNumber, 0, score, "Try again", $"Level {LevelNumber} · Goal {Level.goalCount - goalLeft}/{Level.goalCount}");
                yield break;
            }

            int completed = LevelNumber;
            int stars = Stars();
            bool last = IsLastLevel;
            if (stars > bestStars[levelIndex])
            {
                bestStars[levelIndex] = stars;
                PlayerPrefs.SetInt(StarsKey + levelIndex, stars);
            }
            if (!last)
            {
                levelIndex++;
                unlocked = Mathf.Max(unlocked, levelIndex);
                PlayerPrefs.SetInt(UnlockedKey, unlocked);
            }
            PlayerPrefs.Save();
            winPopup.Show(completed, stars, score, last ? "Play again" : "Next level");
        }

        int Stars() => 1 + (score >= Level.twoStarScore ? 1 : 0) + (score >= Level.threeStarScore ? 1 : 0);
    }
}
