// PixelPerfect Skill — Pipeline Detection
// ----------------------------------------
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace PixelPerfect
{
    public enum PipelineType { BuiltIn, URP, HDRP, Custom }

    public static class PipelineDetection
    {
        // GraphicsSettings.currentRenderPipeline is always up to date — safe to call on startup.
        // Do NOT use RenderPipelineManager.currentPipeline: it is null until at least one frame renders.
        public static PipelineType DetectPipeline()
        {
            var rpa = GraphicsSettings.currentRenderPipeline;
            if (rpa == null) return PipelineType.BuiltIn;

            // Use FullName (includes namespace) — more stable than Name, which strips the namespace.
            // URP:  UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset
            // HDRP: UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset
            var fullName = rpa.GetType().FullName ?? string.Empty;
            if (fullName.Contains("Universal"))      return PipelineType.URP;
            if (fullName.Contains("HighDefinition")) return PipelineType.HDRP;
            return PipelineType.Custom;
        }

        public static System.Type GetPixelPerfectCameraType(PipelineType pipeline)
        {
            if (pipeline == PipelineType.URP)
            {
                var t = System.Type.GetType(
                    "UnityEngine.Rendering.Universal.PixelPerfectCamera, " +
                    "Unity.RenderPipelines.Universal.2D.Runtime");
                if (t != null) return t;
                Debug.LogWarning("URP detected but PixelPerfectCamera not found. Is the 2D Renderer installed?");
            }
            // Assembly name confirmed from asmdef — no "Runtime" suffix
            return System.Type.GetType("UnityEngine.U2D.PixelPerfectCamera, Unity.2D.PixelPerfect");
        }

        // Migration diagnostic: check for the wrong component type on a URP camera
        public static bool HasPipelineMismatch(Camera camera)
        {
            return DetectPipeline() == PipelineType.URP
                && camera.GetComponent<PixelPerfectCamera>() != null;
        }
    }
}
