using UnityEngine;

namespace TapPop
{
    // Keeps its children inside the phone's safe area (notch, home indicator).
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screenSize;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied || Screen.width != screenSize.x || Screen.height != screenSize.y) Apply();
        }

        void Apply()
        {
            Rect safe = Screen.safeArea;
            applied = safe;
            screenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
