using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class windZoneShaderProperties : MonoBehaviour
{
    [Range(0.0f, 10.0f)]
    public float strength;

    public float gustScale = 25;
    public float gustSpeed = 5;

    public float shiverScalee = 5;
    public float shiverStrength = 10;
    public float shiverSpeed = 1;

    public float windChangeDirectionSpeed;
    public float windChangeDirectionAmplitude;
    float initialRotation;

    public float grassColorVariationScale = 1;
    public Color grassVariationColor;
    public float grassVariationStrength;

    private void Awake()
    {
        initialRotation = transform.eulerAngles.y;
        Shader.SetGlobalFloat("noise1Scale", grassColorVariationScale);
        //Shader.SetGlobalColor("varColor", grassVariationColor);
        Shader.SetGlobalFloat("min", grassVariationStrength);
    }
    float2 mainUV = new float2(0,0);
    float2 shiverUV = new float2(0,0);
    void Update()
    {
        
        float rad = transform.eulerAngles.y * Mathf.Deg2Rad;
        mainUV.x += Time.deltaTime * (-Mathf.Sin(rad) * gustSpeed * Remap(strength, 0, 10, .25f, 1));
        mainUV.y += Time.deltaTime * (-Mathf.Cos(rad) * gustSpeed * Remap(strength, 0, 10, .25f, 1));

        shiverUV.x += Time.deltaTime * (-Mathf.Sin(rad) * shiverSpeed);
        shiverUV.y += Time.deltaTime * (-Mathf.Cos(rad) * shiverSpeed);

        Shader.SetGlobalVector("_wind_angle_strength", new Vector4(rad, strength, gustScale, 1));
        Shader.SetGlobalVector("_windNoiseUVs", new Vector4(mainUV.x, mainUV.y, shiverUV.x, shiverUV.y));
        Shader.SetGlobalVector("_wind_shiver", new Vector4(shiverScalee, shiverStrength, shiverSpeed, 1));

        if (Application.isPlaying)
        {
            transform.eulerAngles = new Vector3(0, initialRotation + (Mathf.Sin(Time.time * windChangeDirectionSpeed) * windChangeDirectionAmplitude), 0);
        }
    }
    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
