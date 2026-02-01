using System.Collections;
using UnityEngine;
using TheTear.Characters;
using UnityEngine.UI;

namespace TheTear.UI
{
    public class OverlayController : MonoBehaviour
    {
        public CanvasGroup matterGroup;
        public CanvasGroup voidGroup;
        public CanvasGroup flowGroup;
        public CanvasGroup flashGroup;
        public float tintAlpha = 0.2f;
        public float flashDuration = 0.15f;

        private void Awake()
        {
            ConfigureGroup(matterGroup);
            ConfigureGroup(voidGroup);
            ConfigureGroup(flowGroup);
            ConfigureGroup(flashGroup);
            if (flashGroup != null)
            {
                flashGroup.alpha = 0f;
            }
        }

        public void SetMode(CharacterMode mode)
        {
            if (matterGroup != null) matterGroup.alpha = mode == CharacterMode.Matter ? tintAlpha : 0f;
            if (voidGroup != null) voidGroup.alpha = mode == CharacterMode.Void ? tintAlpha : 0f;
            if (flowGroup != null) flowGroup.alpha = mode == CharacterMode.Flow ? tintAlpha : 0f;

            StopAllCoroutines();
            if (flashGroup != null)
            {
                StartCoroutine(Flash());
            }
        }

        private IEnumerator Flash()
        {
            flashGroup.alpha = 0.6f;
            yield return new WaitForSecondsRealtime(flashDuration);
            flashGroup.alpha = 0f;
        }

        private void ConfigureGroup(CanvasGroup group)
        {
            if (group == null)
            {
                return;
            }
            group.interactable = false;
            group.blocksRaycasts = false;
            group.ignoreParentGroups = true;

            var graphics = group.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
