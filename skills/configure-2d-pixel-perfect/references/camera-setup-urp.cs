// PixelPerfect Skill — URP Camera Setup
// ---------------------------------------
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace PixelPerfect
{
    public static class CameraSetupURP
    {
        // orthographicSize formula: (refResY / 2) / PPU
        // e.g. 320x180 at 16 PPU → 90 / 16 = 5.625
        public static PixelPerfectCamera ConfigureCamera(Camera camera, int refResX, int refResY, int targetPPU)
        {
            var pp = camera.gameObject.GetComponent<PixelPerfectCamera>()
                  ?? camera.gameObject.AddComponent<PixelPerfectCamera>();

            camera.orthographic = true;
            camera.orthographicSize = (refResY / 2f) / targetPPU;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

            pp.assetsPPU      = targetPPU;
            pp.refResolutionX = refResX;
            pp.refResolutionY = refResY;
            pp.gridSnapping   = PixelPerfectCamera.GridSnapping.PixelSnapping; // or UpscaleRenderTexture
            pp.cropFrame      = PixelPerfectCamera.CropFrame.Windowbox;        // or None/Pillarbox/Letterbox/StretchFill

            // Read-only properties (for reference):
            //   pp.pixelRatio          (int)   — actual scale factor applied (e.g. 6 on 1080p with 320x180 ref res)
            //   pp.requiresUpscalePass (bool)  — true when gridSnapping = UpscaleRenderTexture
            //   pp.orthographicSize    (float) — PP-calculated ortho size (different from Camera.orthographicSize)

            // Utility methods (for reference):
            //   pp.RoundToPixel(Vector3)              — pixel-grid snapping for custom camera controllers
            //   pp.CorrectCinemachineOrthoSize(float) — used by Cinemachine extension

            return pp;
        }

        // For custom setups that bypass the PP Camera component (e.g. HDRP fallback, custom SRP):
        // set pixelSnapSpacing manually to enable snapping at the renderer level.
        public static void SetManualPixelSnapSpacing(Camera camera)
        {
            PixelPerfectRendering.pixelSnapSpacing = camera.orthographicSize * 2f / camera.pixelHeight;
        }
    }
}
