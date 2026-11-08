using System;
using System.Collections;
using UnityEngine;

namespace TapPop
{
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);

        const float Back = 1.70158f;
        public static float InBack(float t) => t * t * ((Back + 1f) * t - Back);
        public static float OutBack(float t)
        {
            t -= 1f;
            return t * t * ((Back + 1f) * t + Back) + 1f;
        }
    }

    // Small coroutine tweens, so the project has no third-party dependencies.
    public static class Tween
    {
        public static IEnumerator Run(float duration, Func<float, float> ease, Action<float> step, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                step(ease(t / duration));
                yield return null;
            }
            step(1f);
        }

        public static IEnumerator Scale(Transform target, Vector3 to, float duration, Func<float, float> ease, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Vector3 from = target.localScale;
            yield return Run(duration, ease, k => target.localScale = Vector3.LerpUnclamped(from, to, k));
        }

        public static IEnumerator Move(RectTransform target, Vector2 to, float duration, Func<float, float> ease, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Vector2 from = target.anchoredPosition;
            yield return Run(duration, ease, k => target.anchoredPosition = Vector2.LerpUnclamped(from, to, k));
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float from = group.alpha;
            yield return Run(duration, Ease.OutQuad, k => group.alpha = Mathf.Lerp(from, to, k));
        }

        public static IEnumerator Punch(Transform target, float amount = 0.25f, float duration = 0.3f)
        {
            yield return Scale(target, Vector3.one * (1f + amount), duration * 0.35f, Ease.OutQuad);
            yield return Scale(target, Vector3.one, duration * 0.65f, Ease.OutBack);
        }
    }
}
