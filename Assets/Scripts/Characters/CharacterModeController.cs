using UnityEngine;
using TheTear.Telemetry;
using TheTear.UI;

namespace TheTear.Characters
{
    public class CharacterModeController : MonoBehaviour
    {
        public Camera arCamera;
        public OverlayController overlayController;
        public TelemetryRecorder telemetry;
        public ErrorPanelController errorPanel;

        public CharacterMode CurrentMode => currentMode;

        public event System.Action<CharacterMode> OnModeChanged;

        private CharacterMode currentMode = CharacterMode.Matter;
        private int voidLayer = -1;
        private int flowLayer = -1;

        private void Start()
        {
            ApplyMode(CharacterMode.Matter);
        }

        public void SetMode(CharacterMode mode)
        {
            if ((mode == CharacterMode.Void || mode == CharacterMode.Flow) && (voidLayer < 0 || flowLayer < 0))
            {
                ShowMissingLayerError();
                return;
            }

            if (currentMode == mode)
            {
                return;
            }

            currentMode = mode;
            ApplyMode(mode);
            OnModeChanged?.Invoke(mode);

            if (telemetry != null)
            {
                telemetry.RecordEvent("character", mode.ToString());
            }
        }

        public bool ValidateLayers(out string message)
        {
            voidLayer = LayerMask.NameToLayer("Void");
            flowLayer = LayerMask.NameToLayer("Flow");

            if (voidLayer < 0 || flowLayer < 0)
            {
                message = "Missing layers for VOID/FLOW. Add in Edit > Project Settings > Tags and Layers: set Layer 6 = \"Void\" and Layer 7 = \"Flow\".";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private void ApplyMode(CharacterMode mode)
        {
            if (arCamera != null)
            {
                int defaultLayer = LayerMask.NameToLayer("Default");
                int mask = defaultLayer >= 0 ? (1 << defaultLayer) : 0;
                if (mode == CharacterMode.Void && voidLayer >= 0)
                {
                    mask |= 1 << voidLayer;
                }
                if (mode == CharacterMode.Flow && flowLayer >= 0)
                {
                    mask |= 1 << flowLayer;
                }
                arCamera.cullingMask = mask;
            }

            if (overlayController != null)
            {
                overlayController.SetMode(mode);
            }
        }

        private void ShowMissingLayerError()
        {
            if (errorPanel != null)
            {
                errorPanel.ShowErrors(new System.Collections.Generic.List<string>
                {
                    "VOID/FLOW layers are missing. Add them in Edit > Project Settings > Tags and Layers: set Layer 6 = \"Void\" and Layer 7 = \"Flow\"."
                }, true);
            }
        }
    }
}
