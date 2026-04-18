using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

namespace WorldOfVictoria.Rendering
{
    public sealed class VoxelVolumetricFogFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader shader;
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        private Material runtimeMaterial;
        private VoxelVolumetricFogPass runtimePass;

        public override void Create()
        {
            if (shader == null)
            {
                shader = Shader.Find("Hidden/WorldOfVictoria/VoxelVolumetricFog");
            }

            if (shader != null)
            {
                runtimeMaterial = CoreUtils.CreateEngineMaterial(shader);
            }

            runtimePass = new VoxelVolumetricFogPass
            {
                renderPassEvent = injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (runtimeMaterial == null || renderingData.cameraData.isPreviewCamera)
            {
                return;
            }

            runtimePass.Setup(runtimeMaterial);
            renderer.EnqueuePass(runtimePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(runtimeMaterial);
        }

        private sealed class VoxelVolumetricFogPass : ScriptableRenderPass
        {
            private Material material;
            private bool fetchActiveColor = true;

            public void Setup(Material passMaterial)
            {
                material = passMaterial;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    return;
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                if (!resourceData.activeColorTexture.IsValid())
                {
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                TextureHandle destination = resourceData.activeColorTexture;

                if (fetchActiveColor)
                {
                    var targetDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
                    targetDesc.name = "_WovVolumetricFogColor";
                    targetDesc.clearBuffer = false;
                    destination = renderGraph.CreateTexture(targetDesc);

                    renderGraph.AddBlitPass(source, destination, Vector2.one, Vector2.zero, passName: "Voxel Volumetric Fog Copy");
                    source = destination;
                    destination = resourceData.activeColorTexture;
                }

                var blitParameters = new BlitMaterialParameters(source, destination, material, 0);
                renderGraph.AddBlitPass(blitParameters, passName: "Voxel Volumetric Fog");
            }
        }
    }
}
