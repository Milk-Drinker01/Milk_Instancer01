using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            DepthRenderTexture = CreateDepthTexture(512,512);
        return DepthRenderTexture;
    }
    public static RenderTexture CreateDepthTexture(int resX, int resY)
    {
        RenderTexture DepthRT = new RenderTexture(resX, resY, 0);
        DepthRT.Create();
        RenderTexture.active = DepthRT;
        GL.Begin(GL.TRIANGLES);
        GL.Clear(true, true, new Color(0, 0, 0, 1));
        GL.End();
        return DepthRT;
    }
}
