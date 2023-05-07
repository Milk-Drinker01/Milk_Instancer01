using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MilkInstancer
{
    public abstract class RenderPipelineSetup : MonoBehaviour
    {
        public static RenderPipelineSetup instance;

        static RenderTexture DepthRenderTexture;

        public float GetShadowDistance()
        {
            return _GetShadowDistance();
        }
        protected abstract float _GetShadowDistance();

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }

        public static RenderTexture GetDepthTexture()
        {
            if (DepthRenderTexture == null)
                CreateDepthTexture(1, 1);
            return DepthRenderTexture;
        }

        static void CreateDepthTexture(int resX, int resY)
        {
            DepthRenderTexture = new RenderTexture(resX, resY, 0);
            DepthRenderTexture.Create();
            RenderTexture.active = DepthRenderTexture;
            GL.Begin(GL.TRIANGLES);
            GL.Clear(true, true, new Color(0, 0, 0, 1));
            GL.End();
            //return DepthRenderTexture;
        }

        public void CheckRTSize(Camera camera)
        {
            if (SyncRenderTextureResolution(DepthRenderTexture, camera))
                GetComponent<MilkInstancer>().SetHiZSize();
        }

        bool SyncRenderTextureResolution(RenderTexture rt, Camera camera)
        {
            float aspect = rt.width / (float)rt.height;

            if (!Mathf.Approximately(aspect, camera.aspect))
            {
                rt.Release();
                rt.width = camera.pixelWidth;
                rt.height = camera.pixelHeight;
                rt.Create();
                return true;
            }
            return false;
        }
    }
}
