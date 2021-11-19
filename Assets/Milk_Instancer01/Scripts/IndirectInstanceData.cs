using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IndirectInstanceData
{
    public GameObject prefab;
    public Mesh[] LODMeshes = new Mesh[3];

    public Material indirectMaterial;
    public int count;
    [HideInInspector] public Vector3[] rotations;
    [HideInInspector] public Vector3[] positions;
    [HideInInspector] public Vector3[] scales;
}
