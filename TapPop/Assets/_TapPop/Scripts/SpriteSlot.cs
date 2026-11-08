using UnityEngine;

namespace TapPop
{
    // Names the art this Image expects. TapPop > Relink Art fills every slot from Assets/_TapPop/Art by file name.
    [DisallowMultipleComponent]
    public class SpriteSlot : MonoBehaviour
    {
        [Tooltip("Sprite file name without extension, e.g. btn_primary")]
        public string spriteName;
        public bool preserveAspect = true;
        [Tooltip("Hidden once the real art is linked, e.g. the placeholder logo text")]
        public GameObject placeholder;
    }
}
