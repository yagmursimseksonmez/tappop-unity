using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapPop
{
    // One stop on the start screen's level path: done (white with stars), current (ink, breathes) or locked (faded).
    public class LevelNode : MonoBehaviour
    {
        public enum State { Done, Current, Locked }

        [SerializeField] Image disc;
        [SerializeField] TMP_Text number;
        [SerializeField] Button button;
        [SerializeField] GameObject starRow;
        [Tooltip("The filled star of each slot; the empty star sits behind it")]
        [SerializeField] GameObject[] starFills;
        [SerializeField] Color ink = new Color32(0x0A, 0x0A, 0x0A, 0xFF);
        [SerializeField] Color surface = Color.white;
        [SerializeField] Color muted = new Color32(0x8E, 0x8E, 0x8E, 0xFF);
        [SerializeField, Range(0f, 1f)] float lockedAlpha = 0.5f;
        [SerializeField] float currentScale = 1.25f;
        [SerializeField] float breathe = 0.04f;

        State state;

        public void Set(int levelNumber, State newState, int stars)
        {
            state = newState;
            number.text = levelNumber.ToString(CultureInfo.InvariantCulture);
            disc.color = state == State.Current ? ink : state == State.Done ? surface : new Color(surface.r, surface.g, surface.b, lockedAlpha);
            number.color = state == State.Current ? surface : state == State.Done ? ink : muted;
            button.interactable = state != State.Locked;
            starRow.SetActive(stars > 0);
            for (int i = 0; i < starFills.Length; i++) starFills[i].SetActive(i < stars);
            transform.localScale = Vector3.one * (state == State.Current ? currentScale : 1f);
        }

        void Update()
        {
            if (state != State.Current) return;
            transform.localScale = Vector3.one * (currentScale * (1f + Mathf.Sin(Time.time * 2.5f) * breathe));
        }
    }
}
