using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class HDRPSetup : RenderPipelineSetup
{
    public bool SetShadowDistanceEveryFrame = false;
    private float maxShadowDistance;
    private void Awake()
    {
        SetShadowDistance();
    }
    private void Update()
    {
        if (SetShadowDistanceEveryFrame)
            SetShadowDistance();
    }
    void SetShadowDistance()
    {
        foreach (Volume pr in GameObject.FindObjectsOfType<Volume>())
        {
            HDShadowSettings s;
            pr.sharedProfile.TryGet(out s);
            if (s != null)
            {
                maxShadowDistance = (float)s.maxShadowDistance;
            }
        }
    }
    protected override float _GetShadowDistance()
    {
        return maxShadowDistance;
    }
}
