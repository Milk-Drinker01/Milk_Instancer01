using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ZoneManager : MonoBehaviour
{
    public PaintablePrefab[] instanceTypes = new PaintablePrefab[0];
    public float zoneSize = 15f;

    [Header("DEBUG")]
    public bool countTotalInstances;
    public bool countActiveInstances;
    [HideInInspector] public ulong totalInstances = 0;
    [HideInInspector] public ulong totalActiveInstances = 0;

    [Header("DEBUG, DO NOT MANUALLY CHANGE THESE VALUES")]
    //public IndirectInstanceData[] instances;
    [HideInInspector] public float oldZoneSize = 15;
    [HideInInspector] public GameObject[] _prefabs = new GameObject[0];
    [HideInInspector] public List<int> positionsx;
    [HideInInspector] public List<zoneAxisData> xAxisData;

    MilkInstancer instancer;

    int lastX = int.MinValue;
    int lastZ = int.MinValue;
    void resetPos()
    {
        lastX = int.MinValue;
        lastZ = int.MinValue;
    }
    private void Update()
    {
        if (instanceTypes == null)
        {
            return;
        }
        if (!instancer)
        {
            TryGetComponent<MilkInstancer>(out instancer);
            return;
        }
        if (!instancer.mainCam)
        {
            return;
        }
        int zonePositionX = getZone(instancer.mainCam.transform.position.x);
        int zonePositionZ = getZone(instancer.mainCam.transform.position.z);
        if (zonePositionX != lastX || zonePositionZ != lastZ)
        {
            enterNewZone(zonePositionX, zonePositionZ);
            lastX = zonePositionX;
            lastZ = zonePositionZ;
        }
    }
    void enterNewZone(int zonePositionX, int zonePositionZ)
    {
        totalActiveInstances = 0;
        zoneInstanceData[] newData = new zoneInstanceData[instanceTypes.Length];

        List<zoneData> zoneData = new List<zoneData>();
        int zoneCount = 0;
        int totalCount = 0;
        //Debug.Log(zonePositionZ);
        for (int i = 0; i < instanceTypes.Length; i++)
        {
            if (instanceTypes[i].prefabToPaint == null)
            {
                continue;
            }
            newData[i] = new zoneInstanceData();

            int zoneRange = Mathf.CeilToInt(instanceTypes[i].maxRenderRange / oldZoneSize);
            for (int x = zonePositionX - zoneRange; x <= zonePositionX + zoneRange; x++)
            {
                if (!positionsx.Contains(x))
                {
                    continue;
                }
                int xIndex = positionsx.IndexOf(x);
                for (int z = zonePositionZ - zoneRange; z <= zonePositionZ + zoneRange; z++)
                {
                    if (!xAxisData[xIndex].positionsz.Contains(z))
                    {
                        continue;
                    }
                    if (Mathf.Pow(x - zonePositionX, 2) + Mathf.Pow(z - zonePositionZ, 2) > zoneRange * zoneRange)
                    {
                        continue;
                    }
                    int zIndex = xAxisData[xIndex].positionsz.IndexOf(z);
                    //Debug.Log(xIndex);
                    //Debug.Log(zIndex);

                    zoneData.Add(xAxisData[xIndex].zAxisData[zIndex]);
                    zoneCount++;
                    if (xAxisData[xIndex].zAxisData[zIndex].data[i].positions != null)
                    {
                        totalCount += xAxisData[xIndex].zAxisData[zIndex].data[i].positions.Length;
                    }
                }
            }
            newData[i].positions = new Vector3[totalCount];
            newData[i].rotations = new Vector3[totalCount];
            newData[i].scales = new Vector3[totalCount];
            int ind = 0;
            for (int z = 0; z < zoneCount; z++)
            {
                if (zoneData[z].data[i].positions != null)
                {
                    for (int k = 0; k < zoneData[z].data[i].positions.Length; k++)
                    {
                        newData[i].positions[ind] = zoneData[z].data[i].positions[k];
                        newData[i].rotations[ind] = zoneData[z].data[i].rotations[k];
                        newData[i].scales[ind] = zoneData[z].data[i].scales[k];
                        ind++;
                    }
                }
            }

            totalActiveInstances += (ulong)totalCount;

            zoneData.Clear();
            zoneCount = 0;
            totalCount = 0;

            //Debug.Log(newData[i].positions.Length);
            GC.SuppressFinalize(instanceTypes[i].transformData);
            instanceTypes[i].transformData = newData[i];
        }
        GC.SuppressFinalize(zoneData);
        init();
    }
    int getZone(float position)
    {
        return Mathf.CeilToInt((position - (oldZoneSize / 2f)) / oldZoneSize);
    }
    public void init()
    {
        if (instancer == null)
        {
            instancer = GetComponent<MilkInstancer>();
        }
        //if (!AssertInstanceData())
        //{
        //    enabled = false;
        //    return;
        //}


        //for (int i = 0; i < instances.Length; i++)
        //{
        //    if (instances[i].positions == null || instances[i].positions.Length == 0)
        //    {
        //        indirectRenderer.initialized = false;
        //        indirectRenderer.ReleaseBuffers();
        //        return;
        //    }
        //}
        //indirectRenderer.Initialize(ref instances);
        //return;

        Profiler.BeginSample("removing empty arrays");
        List<PaintablePrefab> nonEmptyData = new List<PaintablePrefab>();
        for (int i = 0; i < instanceTypes.Length; i++)
        {
            if (instanceTypes[i].prefabToPaint != null && instanceTypes[i].transformData != null && instanceTypes[i].transformData.positions != null && instanceTypes[i].transformData.positions.Length > 0)
            {
                //instances[i].shadowCastingMode = GetComponent<InstancePainter>().prefabParamaters[i].instanceShadowCastingMode;
                nonEmptyData.Add(instanceTypes[i]);
            }
        }
        Profiler.EndSample();
        if (nonEmptyData.Count > 0)
        {
            PaintablePrefab[] nonEmptyArray = nonEmptyData.ToArray();
            //indirectRenderer.instanceShadowCastingModes = new ShadowCastingMode[nonEmptyArray.Length];
            instancer.Initialize(ref nonEmptyArray);
        }
        else
        {
            instancer.initialized = false;
            instancer.ReleaseBuffers();
        }
    }
    //private bool AssertInstanceData()
    //{
    //    for (int i = 0; i < instanceTypes.Length; i++)
    //    {
    //        if (instanceTypes[i]._oldPrefab == null)
    //        {
    //            Debug.LogError("Missing Prefab on instance at index: " + i + "! Aborting.");
    //            return false;
    //        }

    //        if (instanceTypes[i].indirectMaterial == null)
    //        {
    //            Debug.LogError("Missing indirectMaterial on instance at index: " + i + "! Aborting.");
    //            return false;
    //        }

    //        if (instanceTypes[i].LODMeshes[0] == null)
    //        {
    //            Debug.LogError("Missing lod00Mesh on instance at index: " + i + "! Aborting.");
    //            return false;
    //        }

    //        if (instanceTypes[i].LODMeshes[1] == null)
    //        {
    //            Debug.LogError("Missing lod01Mesh on instance at index: " + i + "! Aborting.");
    //            return false;
    //        }

    //        if (instanceTypes[i].LODMeshes[2] == null)
    //        {
    //            Debug.LogError("Missing lod02Mesh on instance at index: " + i + "! Aborting.");
    //            return false;
    //        }
    //    }

    //    return true;
    //}
    public void ClearInstances()
    {
        totalActiveInstances = 0;
        totalInstances = 0;
        if (positionsx != null)
        {
            positionsx.Clear();
        }
        xAxisData.Clear();
        resetPos();
    }
    public void countAllInstances()
    {
        totalInstances = 0;

        for (int x = 0; x < positionsx.Count; x++)
        {
            int xIndex = x;
            for (int z = 0; z < xAxisData[xIndex].positionsz.Count; z++)
            {
                int zIndex = z;

                for (int i = 0; i < xAxisData[xIndex].zAxisData[zIndex].data.Length; i++)
                {
                    if (xAxisData[xIndex].zAxisData[zIndex].data[i].positions != null)
                    {
                        totalInstances += (ulong)xAxisData[xIndex].zAxisData[zIndex].data[i].positions.Length;
                    }
                }
            }
        }
    }
    public void removeData(Vector3 point, float areaSize)
    {
        for (int x = 0; x < positionsx.Count; x++)
        {
            int xIndex = x;
            for (int z = 0; z < xAxisData[xIndex].positionsz.Count; z++)
            {
                int zIndex = z;

                xAxisData[xIndex].zAxisData[zIndex].removeData(point, areaSize);
            }
        }
        resetPos();
        if (countTotalInstances)
        {
            countAllInstances();
        }
    }
    public bool checkMinDistance(Vector3 point, int type, float minDist)
    {
        int zonePositionX = getZone(point.x);
        int zonePositionZ = getZone(point.z);
        int zoneRange = 1;
        for (int x = zonePositionX - zoneRange; x <= zonePositionX + zoneRange; x++)
        {
            if (!positionsx.Contains(x))
            {
                continue;
            }
            int xIndex = positionsx.IndexOf(x);
            for (int z = zonePositionZ - zoneRange; z <= zonePositionZ + zoneRange; z++)
            {
                if (!xAxisData[xIndex].positionsz.Contains(z))
                {
                    continue;
                }
                int zIndex = xAxisData[xIndex].positionsz.IndexOf(z);

                if (instanceTypes[type] != null && xAxisData[xIndex].zAxisData[zIndex].data[type].positions != null)
                {
                    for (int n = 0; n < xAxisData[xIndex].zAxisData[zIndex].data[type].positions.Length; n++)
                    {
                        if (Vector3.Distance(point, xAxisData[xIndex].zAxisData[zIndex].data[type].positions[n]) < minDist)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }
    public void FilterData(zoneInstanceData[] data)
    {
        Dictionary<int, Dictionary<int, newZoneData>> newZoneData = new Dictionary<int, Dictionary<int, newZoneData>>();
        int xZone = 0;
        int zZone = 0;
        for (int i = 0; i < data.Length; i++)
        {
            for (int n = 0; n < data[i].positions.Length; n++)
            {
                xZone = getZone(data[i].positions[n].x);
                zZone = getZone(data[i].positions[n].z);

                if (!newZoneData.ContainsKey(xZone))
                {
                    newZoneData.Add(xZone, new Dictionary<int, newZoneData>());
                }
                if (!newZoneData[xZone].ContainsKey(zZone))
                {
                    newZoneData[xZone].Add(zZone, new newZoneData(data.Length));
                }
                //Debug.Log(newZoneData[xZone].Count);
                //Debug.Log(newZoneData[xZone][zZone].data.Length);
                //Debug.Log(newZoneData[xZone][zZone].data[i].positions.Count);
                newZoneData[xZone][zZone].data[i].positions.Add(data[i].positions[n]);
                newZoneData[xZone][zZone].data[i].rotations.Add(data[i].rotations[n]);
                newZoneData[xZone][zZone].data[i].scales.Add(data[i].scales[n]);
            }
        }
        foreach(KeyValuePair<int, Dictionary<int, newZoneData>> xData in newZoneData)
        {
            foreach(KeyValuePair<int, newZoneData> zData in xData.Value)
            {
                zoneInstanceData[] sortedData = new zoneInstanceData[data.Length];

                for (int i = 0; i < data.Length; i++)
                {
                    sortedData[i] = new zoneInstanceData();

                    sortedData[i].positions = zData.Value.data[i].positions.ToArray();
                    sortedData[i].rotations = zData.Value.data[i].rotations.ToArray();
                    sortedData[i].scales = zData.Value.data[i].scales.ToArray();
                }

                addData(xData.Key, zData.Key, sortedData);
                GC.SuppressFinalize(sortedData);
            }
        }
        GC.SuppressFinalize(newZoneData);
    }
    void addData(int xAxis, int zAxis, zoneInstanceData[] data)
    {
        if (positionsx == null)
        {
            positionsx = new List<int>();
            xAxisData = new List<zoneAxisData>();
        }
        if (!positionsx.Contains(xAxis))
        {
            positionsx.Add(xAxis);
            xAxisData.Add(new zoneAxisData());
        }
        int xIndex = positionsx.IndexOf(xAxis);
        if (!xAxisData[xIndex].positionsz.Contains(zAxis))
        {
            xAxisData[xIndex].positionsz.Add(zAxis);
            xAxisData[xIndex].zAxisData.Add(new zoneData(data.Length));
        }
        int zIndex = xAxisData[xIndex].positionsz.IndexOf(zAxis);
        xAxisData[xIndex].zAxisData[zIndex].addData(data);

        if (countTotalInstances)
        {
            countAllInstances();
        }
        resetPos();
    }

    [HideInInspector] public bool oldCountTotalInstances;
    private void OnValidate()
    {
        for (int i = 0; i < instanceTypes.Length; i++)
        {
            if (!instanceTypes[i].initialized)
            {
                instanceTypes[i] = new PaintablePrefab();
            }
        }
        checkPrefabList();
        if (GetComponent<InstancePainter>())
        {
            GetComponent<InstancePainter>().countBias();
        }
        if (!Mathf.Approximately(zoneSize, oldZoneSize))
        {
            //zoneSizeChanged();
        }
        resetPos();
        if (countTotalInstances && !oldCountTotalInstances)
        {
            countAllInstances();
        }
    }
    public void zoneSizeChanged()
    {
        if (Mathf.Approximately(zoneSize, oldZoneSize))
        {
            Debug.Log("size hasnt changed");
            return;
        }
        zoneInstanceData[] newData = new zoneInstanceData[instanceTypes.Length];

        List<zoneData> zoneData = new List<zoneData>();
        int zoneCount = 0;
        int totalCount = 0;
        //Debug.Log(zonePositionZ);
        for (int i = 0; i < instanceTypes.Length; i++)
        {
            if (instanceTypes[i].prefabToPaint == null)
            {
                continue;
            }
            newData[i] = new zoneInstanceData();

            for (int x = 0; x < positionsx.Count; x++)
            {
                int xIndex = x;
                for (int z = 0; z < xAxisData[xIndex].positionsz.Count; z++)
                {
                    int zIndex = z;

                    zoneData.Add(xAxisData[xIndex].zAxisData[zIndex]);
                    zoneCount++;
                    if (xAxisData[xIndex].zAxisData[zIndex].data[i].positions != null)
                    {
                        totalCount += xAxisData[xIndex].zAxisData[zIndex].data[i].positions.Length;
                    }
                }
            }

            newData[i].positions = new Vector3[totalCount];
            newData[i].rotations = new Vector3[totalCount];
            newData[i].scales = new Vector3[totalCount];
            int ind = 0;
            for (int z = 0; z < zoneCount; z++)
            {
                if (zoneData[z].data[i].positions != null)
                {
                    for (int k = 0; k < zoneData[z].data[i].positions.Length; k++)
                    {
                        newData[i].positions[ind] = zoneData[z].data[i].positions[k];
                        newData[i].rotations[ind] = zoneData[z].data[i].rotations[k];
                        newData[i].scales[ind] = zoneData[z].data[i].scales[k];
                        ind++;
                    }
                }
            }

            totalActiveInstances += (ulong)totalCount;

            zoneData.Clear();
            zoneCount = 0;
            totalCount = 0;
        }
        oldZoneSize = zoneSize;
        ClearInstances();
        FilterData(newData);
    }
    void checkPrefabList()
    {
        int l = instanceTypes.Length;
        bool isANull = false;
        for (int i = 0; i < l; i++)
        {
            if (instanceTypes[i].prefabToPaint == null)
            {
                isANull = true;
            }
        }
        for (int i = 0; i < l; i++)
        {
            for (int n = i; n < l; n++)
            {
                if (i != n)
                {
                    if (instanceTypes[i].prefabToPaint == instanceTypes[n].prefabToPaint)
                    {
                        if (instanceTypes[i].prefabToPaint == null || isANull)
                        {
                            PaintablePrefab[] newArray = new PaintablePrefab[l - 1];

                            int tot = 0;
                            for (int k = 0; k < l; k++)
                            {
                                if (k != n)
                                {
                                    newArray[tot] = instanceTypes[k];
                                    tot++;
                                }
                            }

                            instanceTypes = newArray;
                            checkPrefabList();
                            return;
                        }
                        else
                        {
                            instanceTypes[n].prefabToPaint = null;
                        }
                    }
                }
            }
        }
        if (l != _prefabs.Length)
        {
            rebuildInstanceDatabase();
        }
        else
        {
            for (int i = 0; i < l; i++)
            {
                if (instanceTypes[i].prefabToPaint != _prefabs[i])
                {
                    rebuildInstanceDatabase();
                    break;
                }
            }
        }
    }
    void rebuildInstanceDatabase()
    {

        int numOutOfOrder = 0;
        int[] gay = new int[_prefabs.Length];
        for (int i = 0; i < _prefabs.Length; i++)
        {
            gay[i] = -1;
            for (int n = 0; n < instanceTypes.Length; n++)
            {
                if (_prefabs[i] == instanceTypes[n].prefabToPaint)
                {
                    numOutOfOrder++;
                    gay[i] = n;
                }
            }
        }

        for (int i = 0; i < instanceTypes.Length; i++)
        {
            if (instanceTypes[i].prefabToPaint == null)
            {
                instanceTypes[i]._oldPrefab = null;
                continue;
            }
            if (instanceTypes[i].prefabToPaint == instanceTypes[i]._oldPrefab)
            {
                setInstanceMaterials(i);
            }
            else
            {
                instanceTypes[i]._oldPrefab = instanceTypes[i].prefabToPaint;
                //instanceTypes[i]._oldId = (uint)i;
                for (int lod = 0; lod < 3; lod++)
                {
                    int child = lod;
                    if (instanceTypes[i].prefabToPaint.transform.childCount <= lod)
                    {
                        child = instanceTypes[i].prefabToPaint.transform.childCount - 1;
                    }

                    Dictionary<Material, List<subMeshInstance>> baseMaterials = new Dictionary<Material, List<subMeshInstance>>();

                    MeshRenderer[] rends = instanceTypes[i].prefabToPaint.transform.GetChild(child).GetComponentsInChildren<MeshRenderer>();
                    for (int r = 0; r < rends.Length; r++)
                    {
                        for (int m = 0; m < rends[r].sharedMaterials.Length; m++)
                        {
                            if (!baseMaterials.ContainsKey(rends[r].sharedMaterials[m]))
                            {
                                baseMaterials.Add(rends[r].sharedMaterials[m], new List<subMeshInstance>());
                            }
                            baseMaterials[rends[r].sharedMaterials[m]].Add(new subMeshInstance(rends[r].GetComponent<MeshFilter>(), m));
                        }
                    }

                    List<Mesh> newMeshes = new List<Mesh>();
                    foreach (KeyValuePair<Material, List<subMeshInstance>> pair in baseMaterials)
                    {
                        List<CombineInstance> matcombine = new List<CombineInstance>();
                        for (int j = 0; j < pair.Value.Count; j++)
                        {
                            CombineInstance ci = new CombineInstance();

                            ci.mesh = pair.Value[j].mesh.sharedMesh;
                            ci.subMeshIndex = pair.Value[j].index;
                            //ci.transform = filters[i].transform.localToWorldMatrix;
                            ci.transform = pair.Value[j].mesh.transform.localToWorldMatrix;

                            matcombine.Add(ci);
                        }
                        Mesh matMesh = new Mesh();
                        matMesh.CombineMeshes(matcombine.ToArray(), true, true);
                        newMeshes.Add(matMesh);
                    }

                    List<CombineInstance> combine = new List<CombineInstance>();
                    for (int m = 0; m < newMeshes.Count; m++)
                    {
                        CombineInstance ci = new CombineInstance();

                        ci.mesh = newMeshes[m];
                        ci.subMeshIndex = 0;
                        //ci.transform = filters[i].transform.localToWorldMatrix;
                        ci.transform = instanceTypes[i].prefabToPaint.transform.localToWorldMatrix;

                        combine.Add(ci);
                    }
                    Mesh lodMesh = new Mesh();
                    lodMesh.CombineMeshes(combine.ToArray(), false, true);
                    //List<MeshFilter> filters = new List<MeshFilter>();
                    //List<Matrix4x4> transforms = new List<Matrix4x4>();
                    //foreach (MeshFilter filt in prefabs[i].transform.GetChild(child).GetComponentsInChildren<MeshFilter>())
                    //{
                    //    filters.Add(filt);
                    //    transforms.Add(filt.transform.localToWorldMatrix);
                    //}
                    //MeshFilter[] meshFilters = filters.ToArray();
                    //CombineInstance[] combine = new CombineInstance[meshFilters.Length];
                    //int c = 0;
                    //while (c < meshFilters.Length)
                    //{
                    //    combine[c].mesh = meshFilters[c].sharedMesh;
                    //    combine[c].transform = transforms[c];

                    //    c++;
                    //}

                    //Mesh lodMesh = new Mesh();
                    //lodMesh.CombineMeshes(combine, false);
                    instanceTypes[i].LODMeshes[lod] = lodMesh;
                }

                setInstanceMaterials(i);
            }
        }
        if (numOutOfOrder > 1)
        {
            checkListChanged(gay);
        }
        _prefabs = new GameObject[instanceTypes.Length];
        for (int i = 0; i < instanceTypes.Length; i++)
        {
            _prefabs[i] = instanceTypes[i].prefabToPaint;
        }
    }
    void setInstanceMaterials(int i)
    {
        instanceTypes[i].lodMaterialIndexes = new gayWorkAround[3];
        instanceTypes[i].lodMaterialIndexes[0] = new gayWorkAround();
        instanceTypes[i].lodMaterialIndexes[1] = new gayWorkAround();
        instanceTypes[i].lodMaterialIndexes[2] = new gayWorkAround();

        List<Material> baseMaterials = new List<Material>();
        List<Material> newMaterials = new List<Material>();
        List<int> materialIndexes = new List<int>();
        for (int c = 0; c < Mathf.Min(3, instanceTypes[i].prefabToPaint.transform.childCount); c++)
        {
            materialIndexes.Clear();
            MeshRenderer[] rends = instanceTypes[i].prefabToPaint.transform.GetChild(c).GetComponentsInChildren<MeshRenderer>();
            for (int r = 0; r < rends.Length; r++)
            {
                Material[] mats = rends[r].sharedMaterials;
                for (int m = 0; m < rends[r].sharedMaterials.Length; m++)
                {
                    if (!baseMaterials.Contains(rends[r].sharedMaterials[m]))
                    {
                        baseMaterials.Add(rends[r].sharedMaterials[m]);

                        Material newMat = new Material(rends[r].sharedMaterials[m].shader);
                        newMat.CopyPropertiesFromMaterial(rends[r].sharedMaterials[m]);
                        newMaterials.Add(newMat);
                    }
                    materialIndexes.Add(baseMaterials.IndexOf(rends[r].sharedMaterials[m]));
                }
            }

            instanceTypes[i].lodMaterialIndexes[c].workaround = materialIndexes.ToArray();
        }
        instanceTypes[i].indirectMaterial = newMaterials.ToArray();
    }
    public void checkListChanged(int[] newOrder)
    {
        for (int x = 0; x < positionsx.Count; x++)
        {
            int xIndex = x;
            for (int z = 0; z < xAxisData[xIndex].positionsz.Count; z++)
            {
                int zIndex = z;
                xAxisData[xIndex].zAxisData[zIndex].correct(instanceTypes.Length, newOrder);
            }
        }
    }
}

[CustomEditor(typeof(ZoneManager))]
public class ZoneManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ZoneManager t = (ZoneManager)target;
        if (GUILayout.Button("Confirm New Zone Size (could be slow)"))
        {
            t.zoneSizeChanged();
        }
        if (t.countTotalInstances)
        {
            GUILayout.Label("Total Instances - " + t.totalInstances.ToString());
        }
        if (t.countActiveInstances)
        {
            GUILayout.Label("Total Active Instances - " + t.totalActiveInstances.ToString());
        }
    }
}
[System.Serializable]
public class PaintablePrefab
{
    public GameObject prefabToPaint;

    public PaintablePrefab()
    {
        prefabToPaint = null;
    }

    [HideInInspector] public bool initialized = true;
    public float maxRenderRange = 50;

    [Range(0, 100)]
    public int selectionBias = 1;
    [Range(0, 15)]
    public float minDistanceBetweenInstances = 1;
    public Vector2 xRotationRange = new Vector2(0, 0);
    public Vector2 yRotationRange = new Vector2(-180, 180);
    public Vector2 zRotationRange = new Vector2(0, 0);

    public Vector2 scaleRange = new Vector2(.5f, 1);

    [Range(0,1)]
    public float RotateToNormalBias;
    //public ShadowCastingMode instanceShadowCastingMode = ShadowCastingMode.Off;
    public bool instanceShadowCastingMode = false;
    public bool EnableOcclusionCulling = true;

    [HideInInspector] public zoneInstanceData transformData;

    [HideInInspector] public GameObject _oldPrefab;
    //[HideInInspector] public uint id;
    //[HideInInspector] public uint _oldId;
    [HideInInspector] public Mesh[] LODMeshes = new Mesh[3];

    [HideInInspector] public Material[] indirectMaterial;

    [HideInInspector] public gayWorkAround[] lodMaterialIndexes = new gayWorkAround[3];
}
[System.Serializable]
public class gayWorkAround
{
    public int[] workaround = new int[1];
}
public class newZoneData
{
    public newZoneInstanceData[] data;
    public newZoneData(int n)
    {
        data = new newZoneInstanceData[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = new newZoneInstanceData();
            data[i].positions = new List<Vector3>();
            data[i].rotations = new List<Vector3>();
            data[i].scales = new List<Vector3>();
        }
    }
}
public class newZoneInstanceData
{
    public List<Vector3> positions;
    public List<Vector3> rotations;
    public List<Vector3> scales;
}


[System.Serializable]
public class zoneAxisData
{
    public List<int> positionsz;
    public List<zoneData> zAxisData;
    public zoneAxisData()
    {
        positionsz = new List<int>();
        zAxisData = new List<zoneData>();
    }
}
[System.Serializable]
public class zoneData
{
    public zoneInstanceData[] data;
    public zoneData(int n)
    {
        data = new zoneInstanceData[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = new zoneInstanceData();
        }
    }
    public void addData(zoneInstanceData[] newData)
    {
        int count;
        for (int i = 0; i < newData.Length; i++)
        {
            //Debug.Log(data.)
            if (data[i].positions == null)
            {
                data[i].positions = new Vector3[newData[i].positions.Length];
                data[i].rotations = new Vector3[newData[i].positions.Length];
                data[i].scales = new Vector3[newData[i].positions.Length];
            }
            else
            {
                count = data[i].positions.Length;
                Vector3[] temp = new Vector3[count];

                cloneArray(ref temp, ref data[i].positions);
                data[i].positions = new Vector3[count + newData[i].positions.Length];
                cloneArray(ref data[i].positions, ref temp);

                cloneArray(ref temp, ref data[i].rotations);
                data[i].rotations = new Vector3[count + newData[i].positions.Length];
                cloneArray(ref data[i].rotations, ref temp);

                cloneArray(ref temp, ref data[i].scales);
                data[i].scales = new Vector3[count + newData[i].positions.Length];
                cloneArray(ref data[i].scales, ref temp);


                GC.SuppressFinalize(temp);
            }

            count = data[i].positions.Length;
            for (int n = 1; n < newData[i].positions.Length + 1; n++)
            {
                data[i].positions[count - n] = newData[i].positions[n - 1];
                data[i].rotations[count - n] = newData[i].rotations[n - 1];
                data[i].scales[count - n] = newData[i].scales[n - 1];
            }
        }
    }
    public void removeData(Vector3 point, float areaSize)
    {
        int removed = 0;
        int length;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].positions == null)
            {
                continue;
            }
            length = data[i].positions.Length;
            for (int n = 0; n < length; n++)
            {
                if (Vector3.Distance(data[i].positions[n], point) < areaSize / 2)
                {
                    removed++;
                    data[i].positions[n] = data[i].positions[length - removed];
                    data[i].rotations[n] = data[i].rotations[length - removed];
                    data[i].scales[n] = data[i].scales[length - removed];
                }
            }
            Vector3[] temp = new Vector3[length - removed];

            cloneArray(ref temp, ref data[i].positions, true);
            data[i].positions = new Vector3[temp.Length];
            cloneArray(ref data[i].positions, ref temp);

            cloneArray(ref temp, ref data[i].rotations, true);
            data[i].rotations = new Vector3[temp.Length];
            cloneArray(ref data[i].rotations, ref temp);

            cloneArray(ref temp, ref data[i].scales, true);
            data[i].scales = new Vector3[temp.Length];
            cloneArray(ref data[i].scales, ref temp);

            GC.SuppressFinalize(temp);

            removed = 0;
        }
    }
    public void correct(int newLength, int[] targets)
    {
        zoneInstanceData[] newData = new zoneInstanceData[newLength];
        for (int i = 0; i < newLength; i++)
        {
            newData[i] = new zoneInstanceData();
        }
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != -1)
            {
                newData[i] = data[targets[i]];
            }
        }

        data = newData;
    }
    public void clearInstances()
    {
        int removed = 0;
        int length;
        for (int i = 0; i < data.Length; i++)
        {
            data[i].positions = null;
        }
    }
    void cloneArray(ref Vector3[] to, ref Vector3[] from, bool erase = false)
    {
        int range = from.Length;
        if (erase)
        {
            range = to.Length;
        }
        for (int i = 0; i < range; i++)
        {
            to[i] = from[i];
        }
    }
}
[System.Serializable]
public class zoneInstanceData
{
    public Vector3[] positions;
    public Vector3[] rotations;
    public Vector3[] scales;
}