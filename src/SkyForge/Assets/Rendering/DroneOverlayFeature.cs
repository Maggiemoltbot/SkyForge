// SPDX-License-Identifier: MIT
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
            private LayerMask m_DroneLayerMask;
            
            public DroneOverlayPass(LayerMask droneLayerMask)
            {
                m_DroneLayerMask = droneLayerMask;
                // MUST render after the Gaussian Splatting composite pass
                // which is RenderPassEvent.BeforeRenderingTransparents
                // So we go after post-processing to be safe
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }

            private class PassData
            {
                public RendererListHandle rendererListHandle;
            }

            private void SetupRendererList(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
            {
                // Get the frame data needed for renderer list creation
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                // Create filtering settings to only include objects on the Drone layer
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, m_DroneLayerMask);

                // Create sorting settings based on the camera
                SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
                SortingSettings sortingSettings = new SortingSettings(cameraData.camera) { criteria = sortFlags };

                // Create drawing settings for UniversalForward shader tag
                // This ensures we use the standard forward rendering path
                ShaderTagId shaderTag = new ShaderTagId("UniversalForward");
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTag, renderingData, cameraData, lightData, sortFlags);

                // Create the renderer list parameters
                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings
                );

                // Create the renderer list handle
                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Get the resource data for render target access
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Create and configure the pass data
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Drone Overlay Pass", out var passData))
                {
                    // Setup the renderer list with our filtering criteria
                    SetupRendererList(frameData, ref passData, renderGraph);
                    
                    // Validate the renderer list handle
                    if (!passData.rendererListHandle.IsValid())
                        return;

                    // Declare that this pass uses the renderer list
                    builder.UseRendererList(passData.rendererListHandle);
                    
                    // Set the render target to the active camera color texture
                    // This ensures we're drawing directly to the main camera output
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    
                    // We also need to write to the depth texture if the drone shader uses depth
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                    // Configure the render function that will execute the draw call
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        // Draw all renderers in the list (only drone objects due to our filtering)
                        context.cmd.DrawRendererList(data.rendererListHandle);
                    });
                }
            }
        }

        [Tooltip("Layer(s) to render as drone overlay")]
        public LayerMask droneLayer = 1 << 3; // Default to layer 3

        private DroneOverlayPass m_Pass;

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
            m_Pass = new DroneOverlayPass(droneLayer);
        }

        /// <summary>
        /// Called by Unity to add render passes to the renderer
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
}