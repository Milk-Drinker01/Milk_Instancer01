using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class windZoneShaderProperties : MonoBehaviour
{
    [Range(0.0f, 10.0f)]
    public float strength;

    public float gustScale = 1;
    public float gustSpeed = 5;
    public float subGustScale = 10;
    public float subGustSpeed = 3;

    public float shiverScalee = 5;
    public float shiverStrength = 10;
    public float shiverSpeed = 1;

    public bool changeWindDirection;
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
    //float windTime;
    float2 subUV = new float2(0,0);
    float rad;
    float strengthRemap;
    void Update()
    {
        rad = transform.eulerAngles.y * Mathf.Deg2Rad;
        strengthRemap = Remap(strength, 0, 10, .25f, 1);
        mainUV.x += Time.deltaTime * (-Mathf.Sin(rad) * gustSpeed * strengthRemap);
        mainUV.y += Time.deltaTime * (-Mathf.Cos(rad) * gustSpeed * strengthRemap);
        //windTime += Time.deltaTime * gustSpeed * strengthRemap;

        subUV.x += Time.deltaTime * (-Mathf.Sin(rad) * subGustSpeed * strengthRemap);
        subUV.y += Time.deltaTime * (-Mathf.Cos(rad) * subGustSpeed * strengthRemap);

        Shader.SetGlobalVector("_wind_angle_strength", new Vector4(gustScale, subGustScale, strengthRemap, rad));
        Shader.SetGlobalVector("_windNoiseUVs", new Vector4(mainUV.x, mainUV.y, subUV.x, subUV.y));
        //Shader.SetGlobalVector("_windNoiseUVs", new Vector4(windTime, 0, shiverUV.x, shiverUV.y));
        Shader.SetGlobalVector("_wind_shiver", new Vector4(shiverScalee, shiverStrength, shiverSpeed, 1));

        if (changeWindDirection && Application.isPlaying)
        {
            transform.eulerAngles = new Vector3(0, initialRotation + (Mathf.Sin(Time.time * windChangeDirectionSpeed) * windChangeDirectionAmplitude), 0);
        }
    }
    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
