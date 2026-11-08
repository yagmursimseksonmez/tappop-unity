using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TapPop
{
    // The start screen's level map: one node per level on a winding dotted path, level 1 at the bottom.
    public class LevelPath : MonoBehaviour
    {
        [SerializeField] LevelNode[] nodes;
        [SerializeField] Image[] dots;
        [Tooltip("For each dot, the level it leads away from (0 = level 1)")]
        [SerializeField] int[] dotLevel;
        [SerializeField] Color walkedDot = new Color32(0x0A, 0x0A, 0x0A, 0xFF);
        [SerializeField] Color aheadDot = new Color32(0xCF, 0xCF, 0xCF, 0xFF);

        // unlocked: index of the furthest playable level. stars: best stars per level, 0 if never won.
        public void Show(int unlocked, IReadOnlyList<int> stars)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                LevelNode.State state = i == unlocked ? LevelNode.State.Current
                    : i < unlocked ? LevelNode.State.Done : LevelNode.State.Locked;
                nodes[i].Set(i + 1, state, i < stars.Count ? stars[i] : 0);
            }
            for (int d = 0; d < dots.Length; d++) dots[d].color = dotLevel[d] < unlocked ? walkedDot : aheadDot;
        }
    }
}
