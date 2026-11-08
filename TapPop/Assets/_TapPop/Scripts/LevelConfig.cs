using UnityEngine;

namespace TapPop
{
    [CreateAssetMenu(menuName = "TapPop/Level Config", fileName = "Level_01")]
    public class LevelConfig : ScriptableObject
    {
        [Min(4)] public int width = 8;
        [Min(4)] public int height = 8;
        [Range(2, 5)] public int colorCount = 5;
        [Min(1)] public int moves = 20;

        [Tooltip("Index into the Tile Set: 0 red, 1 blue, 2 green, 3 yellow, 4 purple")]
        public int goalColor = 0;
        [Min(1)] public int goalCount = 25;

        [Tooltip("After a tap, a group this big that holds a tile that just landed pops by itself. 0 turns chains off. " +
                 "At 2 or 3 the chain rarely stops and the board plays itself.")]
        [Min(0)] public int chainGroupSize = 4;
        [Tooltip("Safety stop: most chain steps after one tap")]
        [Min(1)] public int maxChainSteps = 8;

        [Tooltip("A group of n tiles scores n * n * pointsPerTile")]
        public int pointsPerTile = 10;

        [Tooltip("Reaching the goal gives the first star. These scores give the second and third.")]
        public int twoStarScore = 5000;
        public int threeStarScore = 6500;

        public int ScoreFor(int groupSize) => groupSize * groupSize * pointsPerTile;
    }
}
