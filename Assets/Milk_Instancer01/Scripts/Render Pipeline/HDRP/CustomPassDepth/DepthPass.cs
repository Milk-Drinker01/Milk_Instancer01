using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
#endif

namespace MilkInstancer
{
#if UNITY_EDITOR
    [CustomPassDrawer(typeof(DepthPass))]
    public class DepthPassDrawer : CustomPassDrawer
    {
        protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
    }
#endif

    public class DepthPass : CustomPass
    {
        //public RenderTexture outputRenderTexture;

        [SerializeField, HideInInspector]
        Shader customCopyShader;
        Material customCopyMaterial;

        protected override bool executeInSceneView => false;

        int depthPass;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (customCopyShader == null)
                customCopyShader = Shader.Find("Hidden/FullScreen/DepthCopy");
            customCopyMaterial = CoreUtils.CreateEngineMaterial(customCopyShader);

            depthPass = customCopyMaterial.FindPass("Depth");
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (ctx.hdCamera.camera != Camera.main)
                return;
            if (RenderPipelineSetup.GetDepthTexture() == null || customCopyMaterial == null)
                return;

            //SyncRenderTextureAspect(RenderPipelineSetup.GetDepthTexture(), ctx.hdCamera.camera);

            var scale = RTHandles.rtHandleProperties.rtHandleScale;
            customCopyMaterial.SetVector("_Scale", scale);

            ctx.cmd.Blit(ctx.cameraNormalBuffer, RenderPipelineSetup.GetDepthTexture(), customCopyMaterial, depthPass);
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(customCopyMaterial);
        }
    }
}