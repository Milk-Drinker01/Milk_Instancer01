using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MilkInstancer.demos
{
    public class OcclusionTest : MonoBehaviour
    {
        public RawImage DepthImageComponent;
        public RawImage OverHeadImageComponent;
        public int OverHeadResolution = 256;
        public Camera OverheadCamera;

        private void Awake()
        {
            RenderTexture rt = new RenderTexture(OverHeadResolution, OverHeadResolution, 0);
            rt.Create();
            OverHeadImageComponent.texture = rt;
            OverheadCamera.targetTexture = rt;

            DepthImageComponent.texture = RenderPipelineSetup.GetDepthTexture();
        }

    }
}