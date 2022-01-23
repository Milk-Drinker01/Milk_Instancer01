using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sun : MonoBehaviour
{
    public float rotationSpeed = 5;
    public AnimationCurve curve;
    public AnimationCurve brightnessCurve;
    public float lowIntensity;
    public float highIntensity;

    public float evalTime;
    private void Update()
    {
        evalTime += Time.deltaTime * rotationSpeed;
        if (evalTime > 1)
        {
            evalTime -= 1;
        }

        transform.GetChild(0).GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>().intensity = Mathf.Lerp(lowIntensity, highIntensity, brightnessCurve.Evaluate(evalTime));
        transform.eulerAngles = new Vector3(-30, 0, curve.Evaluate(evalTime));
        transform.GetChild(0).LookAt(transform);
    }
}
