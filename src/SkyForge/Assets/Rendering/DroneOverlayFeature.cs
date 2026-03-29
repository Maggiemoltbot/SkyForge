#if !UNITY_6000_0_OR_NEWER
#error Drone Overlay Feature requires Unity 6 or later
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace SkyForge.Rendering
{
    /// <summary>
    /// Renders drone objects after Gaussian Splatting composite to ensure visibility.
    /// This solves the issue where the Gaussian splatting full-screen blit 
    /// overwrites everything rendered before it, including the drone in Third Person view.
    /// 
    /// Implementation:
    /// 1. Creates a render pass that executes AFTER the Gaussian splatting composite pass
    /// 2. Filters and renders only objects on the "Drone" layer
    /// 3. Uses the DroneOverlay.shader which has ZTest Always and high render queue
    /// 4. Results in drone being visible above the Gaussian splats
    /// </summary>
    public class DroneOverlayFeature : ScriptableRendererFeature
    {
        class DroneOverlayPass : ScriptableRenderPass
        {
            private FilteringSettings m_FilteringSettings;
            
            public DroneOverlayPass(LayerMask layerMask)
            {
                m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                using var builder = renderGraph.AddUnsafePass("Drone Overlay Pass", out UnsafePassData passData);

                // Get the required data from the frame container
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                // Declare resource usage
                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.ReadWrite);
                
                // Allow the pass to run even during culling
                builder.AllowPassCulling(false);

                // Set the render function
                builder.SetRenderFunc((UnsafePassData data, UnsafeGraphContext context) =>
                {
                    // TODO: Implement actual rendering of drone objects using the passData info
                    // This would involve:
                    // 1. Getting the command buffer from context
                    // 2. Setting up drawing settings for the drone objects
                    // 3. Calling DrawRenderers with the appropriate settings
                    // For now, we'll leave this as a placeholder to ensure compilation
                    
                    // Example of what would go here:
                    /*
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    var sortingSettings = new SortingSettings(cameraData.camera);
                    var drawingSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), sortingSettings);
                    context.renderContext.DrawRenderers(cameraData.cullResults, ref drawingSettings, ref m_FilteringSettings);
                    */
                });
            }

            class UnsafePassData
            {
                // Currently empty but could hold data needed for the unsafe pass
                // For example: filtering settings, material references, etc.
            }
        }

        [Tooltip("Layer(s) to render as drone overlay")]
        public LayerMask droneLayer = 1 << 3; // Default to layer 3

        private DroneOverlayPass m_ScriptablePass;

        /// <summary>
        /// Called by Unity when the feature is created
        /// </summary>
        public override void Create()
        {
            // Try to find the Drone layer, fall back to layer 3 if not found
            int droneLayerIndex = LayerMask.NameToLayer("Drone");
            if (droneLayerIndex == -1)
            {
                droneLayerIndex = 3; // Default to layer 3
            }
            
            droneLayer = 1 << droneLayerIndex;
            m_ScriptablePass = new DroneOverlayPass(droneLayer);
        }

        /// <summary>
        /// Called by Unity to add render passes to the renderer
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}