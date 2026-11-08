using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapPop
{
    [RequireComponent(typeof(Image))]
    public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int ColorIndex { get; private set; }
        public RectTransform Rect { get; private set; }

        Image image;
        Board board;
        Coroutine moveRoutine;
        Coroutine scaleRoutine;

        void Awake()
        {
            Rect = (RectTransform)transform;
            image = GetComponent<Image>();
        }

        public void Setup(Board owner, int colorIndex, TileSet tileSet)
        {
            board = owner;
            ColorIndex = colorIndex;
            tileSet.Apply(image, colorIndex);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            name = "Tile_" + tileSet.Id(colorIndex);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (board.AcceptsInput) PlayScale(Tween.Scale(transform, Vector3.one * 0.9f, 0.06f, Ease.OutQuad));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (transform.localScale.x < 1f) PlayScale(Tween.Scale(transform, Vector3.one, 0.15f, Ease.OutBack));
        }

        public void OnPointerClick(PointerEventData eventData) => board.OnTileTapped(this);

        public void MoveTo(Vector2 target, float duration, float delay)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(FallRoutine(target, duration, delay));
        }

        public void Pop(float delay) => PlayScale(PopRoutine(delay));

        public void Shake() => PlayScale(ShakeRoutine());

        IEnumerator FallRoutine(Vector2 target, float duration, float delay)
        {
            yield return Tween.Move(Rect, target, duration, Ease.InQuad, delay);
            PlayScale(LandRoutine());
        }

        IEnumerator LandRoutine()
        {
            yield return Tween.Scale(transform, new Vector3(1.12f, 0.86f, 1f), 0.06f, Ease.OutQuad);
            yield return Tween.Scale(transform, Vector3.one, 0.18f, Ease.OutBack);
        }

        IEnumerator PopRoutine(float delay)
        {
            yield return Tween.Scale(transform, Vector3.one * 1.2f, 0.07f, Ease.OutQuad, delay);
            yield return Tween.Scale(transform, Vector3.zero, 0.12f, Ease.InBack);
        }

        IEnumerator ShakeRoutine()
        {
            transform.localScale = Vector3.one;
            yield return Tween.Run(0.3f, Ease.Linear,
                k => transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 6f) * 12f * (1f - k)));
        }

        // Scale, pop and shake share one slot so a new one cleanly replaces the old one.
        void PlayScale(IEnumerator routine)
        {
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            transform.localRotation = Quaternion.identity;
            scaleRoutine = StartCoroutine(routine);
        }
    }
}
