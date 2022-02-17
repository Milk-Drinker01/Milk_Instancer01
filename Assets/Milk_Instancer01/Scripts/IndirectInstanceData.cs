using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class IndirectInstanceData
{
    public GameObject prefab;
    public Mesh[] LODMeshes = new Mesh[3];

    public Material[] indirectMaterial;


    public gayWorkAround[] lodMaterialIndexes = new gayWorkAround[3];
    [HideInInspector] public Vector3[] rotations;
    [HideInInspector] public Vector3[] positions;
    [HideInInspector] public Vector3[] scales;

    //public ShadowCastingMode shadowCastingMode;
    public bool shadowCastingMode;

#if UNITY_EDITOR
    public int index;
#endif
}
[System.Serializable]
public class gayWorkAround
{
    public int[] workaround = new int[1];
}