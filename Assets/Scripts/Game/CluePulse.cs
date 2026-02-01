using System.Collections;
using UnityEngine;

namespace MaskedMurderer.Game
{
    public class CluePulse : MonoBehaviour
    {
        [SerializeField] private float pulseScale = 1.18f;
        [SerializeField] private float pulseDuration = 0.2f;

        private Coroutine routine;
        private Vector3 originalScale;

        void Awake()
        {
            originalScale = transform.localScale;
        }

        public void Pulse()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
            }

            Vector3 targetScale = originalScale * pulseScale;
            float half = pulseDuration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float lerp = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, lerp);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float lerp = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.Lerp(targetScale, originalScale, lerp);
                yield return null;
            }

            transform.localScale = originalScale;
            routine = null;
        }
    }
}
