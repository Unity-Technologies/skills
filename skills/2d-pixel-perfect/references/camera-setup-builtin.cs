// PixelPerfect Skill — Built-in Standalone Camera Setup
// -------------------------------------------------------
// Assembly: Unity.2D.PixelPerfect
using UnityEngine;
using UnityEngine.U2D;

namespace PixelPerfect
{
    public static class CameraSetupBuiltIn
    {
        public static PixelPerfectCamera ConfigureCamera(Camera camera, int refResX, int refResY, int targetPPU)
        {
            var pp = camera.gameObject.GetComponent<PixelPerfectCamera>()
                  ?? camera.gameObject.AddComponent<PixelPerfectCamera>();

            // Same base settings as URP
            camera.orthographic = true;
            camera.orthographicSize = (refResY / 2f) / targetPPU;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

            pp.assetsPPU      = targetPPU;
            pp.refResolutionX = refResX;
            pp.refResolutionY = refResY;
            pp.pixelSnapping  = true;    // silently ignored when upscaleRT = true
            pp.upscaleRT      = false;
            pp.cropFrameX     = true;    // black bars left/right
            pp.cropFrameY     = true;    // black bars top/bottom — both true = Windowbox equivalent
            pp.stretchFill    = false;   // only functional when BOTH cropFrameX and cropFrameY are true

            // Read-only: pp.pixelRatio (int) — same semantics as URP pixelRatio
            //
            // Note: No RoundToPixel, CorrectCinemachineOrthoSize, requiresUpscalePass,
            //       or orthographicSize on the standalone component.

            return pp;
        }
    }
}
