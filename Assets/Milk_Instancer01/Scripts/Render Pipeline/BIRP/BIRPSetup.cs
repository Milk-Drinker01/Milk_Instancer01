using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MilkInstancer
{
    public class BIRPSetup : RenderPipelineSetup
    {
        protected override float _GetShadowDistance()
        {
            return QualitySettings.shadowDistance;
        }
    }
}
