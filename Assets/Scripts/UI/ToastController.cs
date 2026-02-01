using System.Collections;
using UnityEngine;

namespace TheTear.UI
{
    public class ToastController : MonoBehaviour
    {
        public Component textComponent;
        public CanvasGroup canvasGroup;
        public float defaultDuration = 2.5f;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        public void Show(string message, float duration = -1f)
        {
            if (duration <= 0f)
            {
                duration = defaultDuration;
            }

            UITextHelper.SetText(textComponent, message);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
            }

            StopAllCoroutines();
            StartCoroutine(HideRoutine(duration));
        }

        private IEnumerator HideRoutine(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
            }
        }
    }
}
