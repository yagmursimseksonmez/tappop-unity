using UnityEngine;

namespace TapPop
{
    // Pop sounds on a small pool of audio sources, so ripples and chains can overlap without cutting each other off.
    public class Sfx : MonoBehaviour
    {
        [Tooltip("Picked at random for each pop so a long ripple does not sound mechanical")]
        [SerializeField] AudioClip[] pops;
        [SerializeField, Range(0f, 1f)] float volume = 0.55f;
        [SerializeField, Min(1)] int voices = 12;
        [Tooltip("Pitch climbs by this much for each ring of a ripple and each chain step")]
        [SerializeField] float pitchStep = 0.05f;
        [SerializeField] float maxPitch = 1.6f;

        AudioSource[] sources;
        int next;

        void Awake()
        {
            sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }
        }

        // step: ring of the ripple plus chain depth; higher steps sound a little higher.
        public void Pop(float delay, int step)
        {
            if (pops == null || pops.Length == 0) return;
            AudioSource source = sources[next];
            next = (next + 1) % sources.Length;
            source.clip = pops[Random.Range(0, pops.Length)];
            source.pitch = Mathf.Min(maxPitch, 1f + step * pitchStep + Random.Range(-0.03f, 0.03f));
            source.volume = volume;
            source.PlayDelayed(delay);
        }
    }
}
