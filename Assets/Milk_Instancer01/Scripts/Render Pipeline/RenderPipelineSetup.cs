using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderPipelineSetup : MonoBehaviour
{
    public static RenderPipelineSetup instance;

    RenderTexture DepthRenderTexture;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }
    public RenderTexture GetDepthTexture()
    {
        if (DepthRenderTexture == null)
            CreateDepthTexture();
        return DepthRenderTexture;
    }
    public void CreateDepthTexture()
    {
        DepthRenderTexture = new RenderTexture(512, 512, 0);
        DepthRenderTexture.Create();
        RenderTexture.active = DepthRenderTexture;
        GL.Begin(GL.TRIANGLES);
        GL.Clear(true, true, new Color(0, 0, 0, 1));
        GL.End();
    }
}
