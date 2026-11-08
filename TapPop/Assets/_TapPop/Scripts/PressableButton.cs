using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapPop
{
    // Squash on press, springy release. Sits next to a Button whose transition is None.
    public class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float pressedScale = 0.92f;

        Selectable selectable;
        Coroutine routine;
        bool pressed;

        void Awake() => selectable = GetComponent<Selectable>();

        void OnDisable()
        {
            pressed = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            pressed = true;
            Play(Tween.Scale(transform, Vector3.one * pressedScale, 0.07f, Ease.OutQuad));
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Play(Tween.Scale(transform, Vector3.one, 0.25f, Ease.OutBack));
        }

        void Play(IEnumerator next)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(next);
        }
    }
}
