using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace TapPop
{
    // UI particles: pop sparks and goal tiles flying to the HUD. Pooled images, so nothing is created mid-game.
    public class PopFx : MonoBehaviour
    {
        [Tooltip("Draw it white: it is tinted to the tile colour")]
        [SerializeField] Sprite spark;
        [SerializeField] int sparksPerTile = 6;
        [SerializeField] float sparkSize = 34f;
        [SerializeField] float speedMin = 500f;
        [SerializeField] float speedMax = 1100f;
        [SerializeField] float gravity = 2600f;
        [SerializeField] float lifetime = 0.55f;
        [SerializeField] float flySize = 100f;
        [SerializeField] float flyDuration = 0.55f;

        struct Spark
        {
            public Image image;
            public Vector2 velocity;
            public float age, life, spin;
        }

        readonly List<Spark> sparks = new List<Spark>();
        readonly Stack<Image> pool = new Stack<Image>();
        RectTransform rect;

        void Awake() => rect = (RectTransform)transform;

        public void Burst(Vector3 worldPosition, Color color, float delay = 0f) =>
            StartCoroutine(BurstRoutine(worldPosition, color, delay));

        public void Fly(Sprite sprite, Color tint, Vector3 fromWorld, RectTransform target, float delay, Action onArrive) =>
            StartCoroutine(FlyRoutine(sprite, tint, fromWorld, target, delay, onArrive));

        public void Clear()
        {
            StopAllCoroutines();
            foreach (Spark s in sparks) Release(s.image);
            sparks.Clear();
        }

        IEnumerator BurstRoutine(Vector3 worldPosition, Color color, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Vector2 local = rect.InverseTransformPoint(worldPosition);
            for (int i = 0; i < sparksPerTile; i++)
            {
                Image image = Get(spark, color, sparkSize);
                image.rectTransform.anchoredPosition = local;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(speedMin, speedMax);
                sparks.Add(new Spark
                {
                    image = image,
                    velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed + Vector2.up * 300f,
                    life = lifetime * Random.Range(0.7f, 1.1f),
                    spin = Random.Range(-720f, 720f),
                });
            }
        }

        IEnumerator FlyRoutine(Sprite sprite, Color tint, Vector3 fromWorld, RectTransform target, float delay, Action onArrive)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Image image = Get(sprite, tint, flySize);
            RectTransform rt = image.rectTransform;
            Vector2 from = rect.InverseTransformPoint(fromWorld);
            Vector2 to = rect.InverseTransformPoint(target.position);
            Vector2 control = Vector2.Lerp(from, to, 0.3f) + new Vector2(Random.Range(-260f, 260f), 260f);
            yield return Tween.Run(flyDuration, Ease.InOutQuad, k =>
            {
                Vector2 a = Vector2.Lerp(from, control, k);
                Vector2 b = Vector2.Lerp(control, to, k);
                rt.anchoredPosition = Vector2.Lerp(a, b, k);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k);
            });
            Release(image);
            onArrive?.Invoke();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark s = sparks[i];
                s.age += dt;
                if (s.age >= s.life)
                {
                    Release(s.image);
                    sparks.RemoveAt(i);
                    continue;
                }
                s.velocity.y -= gravity * dt;
                RectTransform rt = s.image.rectTransform;
                rt.anchoredPosition += s.velocity * dt;
                rt.Rotate(0f, 0f, s.spin * dt);
                float k = s.age / s.life;
                rt.localScale = Vector3.one * (1f - k * k);
                sparks[i] = s;
            }
        }

        Image Get(Sprite sprite, Color color, float size)
        {
            Image image = pool.Count > 0 ? pool.Pop() : CreateImage();
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            RectTransform rt = image.rectTransform;
            rt.sizeDelta = Vector2.one * size;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            return image;
        }

        Image CreateImage()
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        void Release(Image image)
        {
            image.gameObject.SetActive(false);
            pool.Push(image);
        }
    }
}
