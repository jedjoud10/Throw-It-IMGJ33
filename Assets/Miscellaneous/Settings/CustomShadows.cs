using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
public class CustomShadows : ScriptableRendererFeature {
    class CustomPass1 : ScriptableRenderPass {
        public Material material;
        public float generalShadowStrength;

        public CustomPass1(float generalShadowStrength) {
            this.generalShadowStrength = generalShadowStrength;
        }

        private class PassData {
            internal TextureHandle target;
            internal Material material;
            internal int shadowmapID;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalLightData lights = frameData.Get<UniversalLightData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();



            if (lights.mainLightIndex == -1) {
                return;
            }

            var desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.graphicsFormat = GraphicsFormat.R8_UNorm;
            var tempTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ScreenSpaceShadowmapTexture", true);

            using (var builder = renderGraph.AddComputePass<PassData>("Custom shadows pass 1 amogus", out var passData)) {
                builder.SetRenderFunc((PassData data, ComputeGraphContext rgContext) => {
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Custom shadows pass 1", out var passData)) {
                passData.target = tempTexture;
                builder.AllowPassCulling(false);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                passData.material = material;
                passData.shadowmapID = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");

                builder.UseAllGlobalTextures(true);
                builder.AllowGlobalStateModification(true);
                builder.UseGlobalTexture(passData.shadowmapID);
                builder.SetGlobalTextureAfterPass(tempTexture, passData.shadowmapID);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                    data.material.SetFloat("_generalShadowStrength", generalShadowStrength);
                    Matrix4x4 matrix = lights.visibleLights[lights.mainLightIndex].localToWorldMatrix;
                    data.material.SetMatrix("_sunMatrix", matrix);
                    data.material.SetMatrix("_invSunMatrix", matrix.inverse);
                    data.material.SetFloat("_generalShadowStrength", generalShadowStrength);
                    Blitter.BlitTexture(context.cmd, data.target, Vector2.one, data.material, 0);
                    /*
                    context.cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS"), false);
                    context.cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE"), false);
                    context.cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN"), true);
                    */
                });
            }
        }
    }

    class CustomPass2 : ScriptableRenderPass {
        private static void ExecutePass(RasterCommandBuffer cmd, UniversalShadowData shadowData) {
            int cascadesCount = shadowData.mainLightShadowCascadesCount;
            bool mainLightShadows = shadowData.supportsMainLightShadows;
            bool receiveShadowsNoCascade = mainLightShadows && cascadesCount == 1;
            bool receiveShadowsCascades = mainLightShadows && cascadesCount > 1;

            // Before transparent object pass, force to disable screen space shadow of main light
            cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN"), false);

            // then enable main light shadows with or without cascades
            cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS"), receiveShadowsNoCascade);
            cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE"), receiveShadowsCascades);
        }

        internal class PassData {
            internal CustomPass2 pass;
            internal UniversalShadowData shadowData;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            using (var builder = renderGraph.AddComputePass<PassData>("Custom shadows pass 2 amogus", out var passData)) {
                builder.SetRenderFunc((PassData data, ComputeGraphContext rgContext) => {
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Custom shadows pass 2", out var passData)) {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                TextureHandle color = resourceData.activeColorTexture;
                builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                passData.shadowData = frameData.Get<UniversalShadowData>();
                passData.pass = this;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data.shadowData);
                });
            }
        }
    }

    CustomPass1 customPass;
    CustomPass2 customPass2;
    public Material material;
    public float generalShadowStrength;

    // TODO: find fix for this pls
    [Tooltip("Decided to make this toggable since the screen space shadows effect seems fucked up in prefabs??")]
    public bool allowInSceneView;

    public override void Create() {
        customPass = new CustomPass1(generalShadowStrength);
        //customPass2 = new CustomPass2();

        //customPass.material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/ScreenSpaceShadows"));
        customPass.ConfigureInput(ScriptableRenderPassInput.Depth);
        customPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses+2;
        //customPass2.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents+2;
    }

    public Material GetInternalMat() {
        return material;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;


        bool allowMainLightShadows = renderingData.shadowData.supportsMainLightShadows && renderingData.lightData.mainLightIndex != -1;
        bool shouldEnqueue = allowMainLightShadows;

        if (shouldEnqueue) {
            customPass.material = material;
            renderer.EnqueuePass(customPass);
            //renderer.EnqueuePass(customPass2);
        }

        /*
        if (renderingData.cameraData.cameraType != CameraType.SceneView || (allowInSceneView || Application.isPlaying)) {
            renderer.EnqueuePass(customPass);
        }
        */
    }


    protected override void Dispose(bool disposing) {
    }
}

/*
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal {
    [Serializable]
    internal class ScreenSpaceShadowsSettings {
    }

    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Screen Space Shadows")]
    [Tooltip("Screen Space Shadows")]
    internal class CustomShadows : ScriptableRendererFeature {
#if UNITY_EDITOR
        [UnityEditor.ShaderKeywordFilter.SelectIf(true, keywordNames: ShaderKeywordStrings.MainLightShadowScreen)]
        private const bool k_RequiresScreenSpaceShadowsKeyword = true;
#endif

        // Serialized Fields
        private Shader m_Shader;
        [SerializeField] private ScreenSpaceShadowsSettings m_Settings = new ScreenSpaceShadowsSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceShadowsPass m_SSShadowsPass = null;
        private ScreenSpaceShadowsPostPass m_SSShadowsPostPass = null;

        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceShadows";

        /// <inheritdoc/>
        public override void Create() {
            if (m_SSShadowsPass == null)
                m_SSShadowsPass = new ScreenSpaceShadowsPass();
            if (m_SSShadowsPostPass == null)
                m_SSShadowsPostPass = new ScreenSpaceShadowsPostPass();

            LoadMaterial();

            m_SSShadowsPass.renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            m_SSShadowsPostPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

            if (!LoadMaterial()) {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool allowMainLightShadows = renderingData.shadowData.supportsMainLightShadows && renderingData.lightData.mainLightIndex != -1;
            bool shouldEnqueue = allowMainLightShadows && m_SSShadowsPass.Setup(m_Settings, m_Material);

            if (shouldEnqueue) {
                bool isDeferredRenderingMode = false;

                m_SSShadowsPass.renderPassEvent = isDeferredRenderingMode
                    ? RenderPassEvent.AfterRenderingGbuffer
                    : RenderPassEvent.AfterRenderingPrePasses + 1; // We add 1 to ensure this happens after depth priming depth copy pass that might be scheduled

                renderer.EnqueuePass(m_SSShadowsPass);
                renderer.EnqueuePass(m_SSShadowsPostPass);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            m_SSShadowsPass?.Dispose();
            m_SSShadowsPass = null;
            CoreUtils.Destroy(m_Material);
        }

        private bool LoadMaterial() {
            if (m_Material != null) {
                return true;
            }

            if (m_Shader == null) {
                m_Shader = Shader.Find(k_ShaderName);
                if (m_Shader == null) {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

            return m_Material != null;
        }

        private class ScreenSpaceShadowsPass : ScriptableRenderPass {
            // Private Variables
            private Material m_Material;
            private ScreenSpaceShadowsSettings m_CurrentSettings;
            private RTHandle m_RenderTarget;
            private int m_ScreenSpaceShadowmapTextureID;
            private PassData m_PassData;

            internal ScreenSpaceShadowsPass() {
                profilingSampler = new ProfilingSampler("Blit Screen Space Shadows");
                m_CurrentSettings = new ScreenSpaceShadowsSettings();
                m_ScreenSpaceShadowmapTextureID = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");
                m_PassData = new PassData();
            }

            public void Dispose() {
                m_RenderTarget?.Release();
            }

            internal bool Setup(ScreenSpaceShadowsSettings featureSettings, Material material) {
                m_CurrentSettings = featureSettings;
                m_Material = material;
                ConfigureInput(ScriptableRenderPassInput.Depth);

                return m_Material != null;
            }

            private class PassData {
                internal TextureHandle target;
                internal Material material;
                internal int shadowmapID;
            }

            /// <summary>
            /// Initialize the shared pass data.
            /// </summary>
            /// <param name="passData"></param>
            private void InitPassData(ref PassData passData) {
                passData.material = m_Material;
                passData.shadowmapID = m_ScreenSpaceShadowmapTextureID;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                if (m_Material == null) {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceShadows pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                    return;
                }
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                var desc = cameraData.cameraTargetDescriptor;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.msaaSamples = 1;
                // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
                // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
                desc.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Blend)
                    ? GraphicsFormat.R8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm;
                TextureHandle color = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ScreenSpaceShadowmapTexture", true);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler)) {
                    passData.target = color;
                    builder.SetRenderAttachment(color, 0, AccessFlags.Write);

                    InitPassData(ref passData);
                    builder.AllowGlobalStateModification(true);

                    if (color.IsValid())
                        builder.SetGlobalTextureAfterPass(color, m_ScreenSpaceShadowmapTextureID);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => {
                        ExecutePass(rgContext.cmd, data, data.target);
                    });
                }
            }

            private static void ExecutePass(RasterCommandBuffer cmd, PassData data, RTHandle target) {
                Blitter.BlitTexture(cmd, target, Vector2.one, data.material, 0);
                //cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadows, false);
                //cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowCascades, false);
                //cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowScreen, true);
                
                cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS"), false);
                cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE"), false);
                cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN"), true);
            }
        }

        private class ScreenSpaceShadowsPostPass : ScriptableRenderPass {
            private static readonly RTHandle k_CurrentActive = RTHandles.Alloc(BuiltinRenderTextureType.CurrentActive);

            internal ScreenSpaceShadowsPostPass() {
                profilingSampler = new ProfilingSampler("Set Screen Space Shadow Keywords");
            }

            private static void ExecutePass(RasterCommandBuffer cmd, UniversalShadowData shadowData) {
                int cascadesCount = shadowData.mainLightShadowCascadesCount;
                bool mainLightShadows = shadowData.supportsMainLightShadows;
                bool receiveShadowsNoCascade = mainLightShadows && cascadesCount == 1;
                bool receiveShadowsCascades = mainLightShadows && cascadesCount > 1;

                cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS"), true);
                cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE"), true);
                cmd.SetKeyword(GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN"), false);
            }

            internal class PassData {
                internal ScreenSpaceShadowsPostPass pass;
                internal UniversalShadowData shadowData;
            }
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler)) {
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                    TextureHandle color = resourceData.activeColorTexture;
                    builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                    passData.shadowData = frameData.Get<UniversalShadowData>();
                    passData.pass = this;

                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => {
                        ExecutePass(rgContext.cmd, data.shadowData);
                    });
                }
            }
        }
    }
}
*/