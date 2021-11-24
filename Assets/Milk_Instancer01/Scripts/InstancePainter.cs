using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(MilkInstancer))]
public class InstancePainter : MonoBehaviour
{
    #region Variables
    public GameObject[] prefabs = new GameObject[0];
    public int numToAdd = 256;
    public float areaSize = 5;
    [HideInInspector] public MilkInstancer indirectRenderer;

    //[Header("debug")]
    [HideInInspector] public IndirectInstanceData[] instances;
    public PaintablePrefab[] prefabParamaters = new PaintablePrefab[0];

#if UNITY_EDITOR
    public LayerMask rayCastLayerMask;
    [HideInInspector] public GameObject[] _prefabs = new GameObject[0];
    int[] selectionBias;
    public int totalCount = 0;
#endif
    #endregion

    #region non-editor functions
    private void Awake()
    {
        init();
    }
    public void init()
    {
        if (indirectRenderer == null)
        {
            indirectRenderer = GetComponent<MilkInstancer>();
        }
        if (!AssertInstanceData())
        {
            enabled = false;
            return;
        }


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

        List<IndirectInstanceData> nonEmptyData = new List<IndirectInstanceData>();
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i].positions != null && instances[i].positions.Length > 0)
            {
                instances[i].shadowCastingMode = prefabParamaters[i].instanceShadowCastingMode;
                nonEmptyData.Add(instances[i]);
            }
        }
        if (nonEmptyData.Count > 0)
        {
            IndirectInstanceData[] nonEmptyArray = nonEmptyData.ToArray();
            indirectRenderer.instanceShadowCastingModes = new ShadowCastingMode[nonEmptyArray.Length];
            for (int i = 0; i < nonEmptyArray.Length; i++)
            {
                indirectRenderer.instanceShadowCastingModes[i] = nonEmptyArray[i].shadowCastingMode;
            }
            indirectRenderer.Initialize(ref nonEmptyArray);
        }
        else
        {
            indirectRenderer.initialized = false;
            indirectRenderer.ReleaseBuffers();
        }
    }

    private bool AssertInstanceData()
    {
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i].prefab == null)
            {
                Debug.LogError("Missing Prefab on instance at index: " + i + "! Aborting.");
                return false;
            }

            if (instances[i].indirectMaterial == null)
            {
                Debug.LogError("Missing indirectMaterial on instance at index: " + i + "! Aborting.");
                return false;
            }

            if (instances[i].LODMeshes[0] == null)
            {
                Debug.LogError("Missing lod00Mesh on instance at index: " + i + "! Aborting.");
                return false;
            }

            if (instances[i].LODMeshes[1] == null)
            {
                Debug.LogError("Missing lod01Mesh on instance at index: " + i + "! Aborting.");
                return false;
            }

            if (instances[i].LODMeshes[2] == null)
            {
                Debug.LogError("Missing lod02Mesh on instance at index: " + i + "! Aborting.");
                return false;
            }
        }

        return true;
    }
    //public bool indirectRenderingEnabled = true;
    //private bool lastIndirectRenderingEnabled = false;
    //private bool lastIndirectDrawShadows = false;
    //private void Update()
    //{
    //    if (lastIndirectRenderingEnabled != indirectRenderingEnabled)
    //    {
    //        lastIndirectRenderingEnabled = indirectRenderingEnabled;

    //        if (indirectRenderingEnabled)
    //        {
    //            indirectRenderer.Initialize(ref instances);
    //        }
    //        else
    //        {
    //            indirectRenderer.StopDrawing(true);
    //        }
    //    }

    //    if (lastIndirectDrawShadows != indirectRenderer.drawInstanceShadows)
    //    {
    //        lastIndirectDrawShadows = indirectRenderer.drawInstanceShadows;
    //    }
    //}
    [HideInInspector] public Unity.Mathematics.Random rand = new Unity.Mathematics.Random();
    public void clearInstances()
    {
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i].positions = null;
            instances[i].rotations = null;
            instances[i].scales = null;
            prefabParamaters[i].currentCount = 0;
        }
#if UNITY_EDITOR
        checkPrefabList();
        totalCount = 0;
#endif
        init();
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

    // Taken from:
    // http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/
    // https://www.shadertoy.com/view/4dtBWH
    private Vector2 Nth_weyl(Vector2 p0, float n)
    {
        Vector2 res = p0 + n * new Vector2(0.754877669f, 0.569840296f);
        res.x %= 1;
        res.y %= 1;
        return res;
    }
    #endregion
    private void OnEnable()
    {
        if (!Application.isEditor)
        {
            Destroy(this);
        }
#if UNITY_EDITOR
        SceneView.onSceneGUIDelegate += OnScene;
#endif
    }
    public void OnDisable()
    {
#if UNITY_EDITOR
        SceneView.onSceneGUIDelegate -= OnScene;
#endif
    }
#if UNITY_EDITOR
    void eraseInstances(Vector3 point)
    {
        int removed = 0;
        int length;
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i].positions == null)
            {
                continue;
            }
            length = instances[i].positions.Length;
            for (int n = 0; n < length; n++)
            {
                if (Vector3.Distance(instances[i].positions[n], point) < areaSize)
                {
                    removed++;
                    instances[i].positions[n] = instances[i].positions[length - removed];
                    instances[i].rotations[n] = instances[i].rotations[length - removed];
                    instances[i].scales[n] = instances[i].scales[length - removed];
                }
            }
            Vector3[] temp = new Vector3[length - removed];

            cloneArray(ref temp, ref instances[i].positions, true);
            instances[i].positions = new Vector3[temp.Length];
            cloneArray(ref instances[i].positions, ref temp);

            cloneArray(ref temp, ref instances[i].rotations, true);
            instances[i].rotations = new Vector3[temp.Length];
            cloneArray(ref instances[i].rotations, ref temp);

            cloneArray(ref temp, ref instances[i].scales, true);
            instances[i].scales = new Vector3[temp.Length];
            cloneArray(ref instances[i].scales, ref temp);


            prefabParamaters[i].currentCount = temp.Length;
            GC.SuppressFinalize(temp);

            removed = 0;
        }
        totalCount = 0;
        init();
    }
    void paintInstances(Vector3 origin, Vector3 point, float distance)
    {
        if (rand.state == 0)
        {
            rand.InitState(1298);
        }

        PaintablePrefab parameters;
        //Vector2 L = Vector2.one;
        int[] paintTypes = new int[prefabParamaters.Length];
        Vector3[] points = new Vector3[numToAdd];
        RaycastHit hit;
        Vector3 dir = Vector3.one;
        //Vector2 ang = Vector2.zero;
        int k = 0;
        for (int i = 0; i < numToAdd; i++)
        {
            //ang = UnityEngine.Random.insideUnitCircle * rand.NextFloat(areaSize);
            dir = point + (UnityEngine.Random.insideUnitSphere * areaSize * .5f);
            dir = dir - origin;
            dir = dir.normalized;
            //dir.x += rand.NextFloat(-areaSize, areaSize);
            //dir.y += rand.NextFloat(-areaSize, areaSize);
            //dir.z += rand.NextFloat(-areaSize, areaSize);
            if (Physics.Raycast(origin, dir, out hit, (areaSize * areaSize) + distance, rayCastLayerMask))
            {
                paintTypes[selectionBias[rand.NextInt(selectionBias.Length)]]++;
                points[k] = hit.point;
                k++;
            }
        }
        k = 0;
        for (int n = 0; n < prefabParamaters.Length; n++)
        {
            parameters = prefabParamaters[n];
            Vector3[] pos = new Vector3[paintTypes[n]];
            Vector3[] rot = new Vector3[paintTypes[n]];
            Vector3[] scale = new Vector3[paintTypes[n]];
            for (int i = 0; i < paintTypes[n]; i++)
            {
                //pos[i] = Nth_weyl(L * i, i) * areaSize;
                //pos[i] = new Vector3(pos[i].x - areaSize * 0.5f, 0f, pos[i].y - areaSize * 0.5f) + point;

                //pos[i] = new Vector3(rand.NextFloat(-1, 1), 0f, rand.NextFloat(-1, 1)).normalized * rand.NextFloat(0, areaSize) * .5f + point;
                //Vector2 circle = UnityEngine.Random.insideUnitCircle;
                //pos[i] = new Vector3(circle.x, 0f, circle.y) * areaSize * .5f + point;
                pos[i] = points[k];
                rot[i] = new Vector3(rand.NextFloat(parameters.xRotationRange.x, parameters.xRotationRange.y), rand.NextFloat(parameters.yRotationRange.x, parameters.yRotationRange.y), rand.NextFloat(parameters.zRotationRange.x, parameters.zRotationRange.y));
                scale[i] = Vector3.one * rand.NextFloat(parameters.scaleRange.x, parameters.scaleRange.y);
                k++;
            }
            addInstanceData(n, pos, rot, scale);
        }
        init();
        totalCount = 0;
        for (int i = 0; i < prefabParamaters.Length; i++)
        {
            totalCount += prefabParamaters[i].currentCount;
        }
    }
    private void addInstanceData(int instanceType, Vector3[] positions, Vector3[] rotations, Vector3[] scales)
    {
        int count;
        if (instances[instanceType].positions == null)
        {
            instances[instanceType].positions = new Vector3[positions.Length];
            instances[instanceType].rotations = new Vector3[positions.Length];
            instances[instanceType].scales = new Vector3[positions.Length];
        }
        else
        {
            count = instances[instanceType].positions.Length;
            Vector3[] temp = new Vector3[count];

            cloneArray(ref temp, ref instances[instanceType].positions);
            instances[instanceType].positions = new Vector3[count + positions.Length];
            cloneArray(ref instances[instanceType].positions, ref temp);

            cloneArray(ref temp, ref instances[instanceType].rotations);
            instances[instanceType].rotations = new Vector3[count + positions.Length];
            cloneArray(ref instances[instanceType].rotations, ref temp);

            cloneArray(ref temp, ref instances[instanceType].scales);
            instances[instanceType].scales = new Vector3[count + positions.Length];
            cloneArray(ref instances[instanceType].scales, ref temp);

            GC.SuppressFinalize(temp);
        }

        count = instances[instanceType].positions.Length;
        for (int i = 1; i < positions.Length + 1; i++)
        {
            instances[instanceType].positions[count - i] = positions[i - 1];
            instances[instanceType].rotations[count - i] = rotations[i - 1];
            instances[instanceType].scales[count - i] = scales[i - 1];
        }
        prefabParamaters[instanceType].currentCount = count;
    }
    void checkPrefabList()
    {
        int l = prefabs.Length;
        if (prefabs.Length != _prefabs.Length)
        {
            rebuildInstanceDatabase();
        }
        else
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != _prefabs[i])
                {
                    rebuildInstanceDatabase();
                    break;
                }
            }
        }
    }
    private void OnValidate()
    {
        //if (paintType < 0)
        //{
        //    paintType = 0;
        //}
        //if (paintType >= prefabs.Length)
        //{
        //    paintType = prefabs.Length - 1;
        //}
        checkPrefabList();
        List<int> bias = new List<int>();
        for (int i = 0; i < prefabParamaters.Length; i++)
        {
            //instances[i].parameters.copyParameters(prefabParamaters[i]);
            for (int n = 0; n < prefabParamaters[i].selectionBias; n++)
            {
                bias.Add(i);
            }
        }
        selectionBias = bias.ToArray();

        if (!Application.isPlaying)
        {
            init();
        }
    }
    void rebuildInstanceDatabase()
    {
        Dictionary<GameObject, IndirectInstanceData> data = new Dictionary<GameObject, IndirectInstanceData>();
        PaintablePrefab[] tempParamaters = new PaintablePrefab[prefabs.Length];
        for (int i = 0; i < instances.Length; i++)
        {
            for (int n = 0; n < prefabs.Length; n++)
            {
                if (prefabs[n] == instances[i].prefab)
                {
                    instances[i].index = i;
                    data.Add(instances[i].prefab, instances[i]);
                    tempParamaters[i] = new PaintablePrefab(prefabParamaters[i].prefab);
                    tempParamaters[i].copyParameters(prefabParamaters[i]);
                    break;
                }
            }
        }

        instances = new IndirectInstanceData[prefabs.Length];
        prefabParamaters = new PaintablePrefab[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (data.ContainsKey(prefabs[i]))
            {
                instances[i] = data[prefabs[i]];

                Material reference = prefabs[i].GetComponentInChildren<MeshRenderer>().sharedMaterial;
                instances[i].indirectMaterial = new Material(reference.shader);
                instances[i].indirectMaterial.CopyPropertiesFromMaterial(reference);
                prefabParamaters[i] = new PaintablePrefab(prefabs[i]);
                prefabParamaters[i].copyParameters(tempParamaters[instances[i].index]);
            }
            else
            {
                instances[i] = new IndirectInstanceData();
                prefabParamaters[i] = new PaintablePrefab(prefabs[i]);
                instances[i].prefab = prefabs[i];

                for (int lod = 0; lod < 3; lod++)
                {
                    int child = lod;
                    if (prefabs[i].transform.childCount <= lod)
                    {
                        child = prefabs[i].transform.childCount - 1;
                    }
                    List<MeshFilter> filters = new List<MeshFilter>();
                    List<Matrix4x4> transforms = new List<Matrix4x4>();
                    foreach (MeshFilter filt in prefabs[i].transform.GetChild(child).GetComponentsInChildren<MeshFilter>())
                    {
                        filters.Add(filt);
                        transforms.Add(filt.transform.localToWorldMatrix);
                    }
                    MeshFilter[] meshFilters = filters.ToArray();
                    CombineInstance[] combine = new CombineInstance[meshFilters.Length];
                    int c = 0;
                    while (c < meshFilters.Length)
                    {
                        combine[c].mesh = meshFilters[c].sharedMesh;
                        combine[c].transform = transforms[c];

                        c++;
                    }

                    Mesh lodMesh = new Mesh();
                    lodMesh.CombineMeshes(combine);
                    instances[i].LODMeshes[lod] = lodMesh;
                }
                Material reference = prefabs[i].GetComponentInChildren<MeshRenderer>().sharedMaterial;
                instances[i].indirectMaterial = new Material(reference.shader);
                instances[i].indirectMaterial.CopyPropertiesFromMaterial(reference);
            }
        }

        _prefabs = new GameObject[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            _prefabs[i] = prefabs[i];
        }
    }
    void OnScene(SceneView scene)
    {
        if (Selection.activeTransform != null && Selection.activeTransform.gameObject != gameObject)
        {
            return;
        }
        Event e = Event.current;
        if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
        {
            RaycastHit hit;
            Vector3 mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;

            Ray ray = scene.camera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out hit, rayCastLayerMask))
            {
                if (!e.control)
                {
                    paintInstances(ray.origin, hit.point, hit.distance);
                }
                else
                {
                    eraseInstances(hit.point);
                }
            }
        }
    }
#endif
}
#if UNITY_EDITOR
[CustomEditor(typeof(InstancePainter))]
class InstancePainterEditor : Editor
{
    public void OnEnable()
    {
        SceneView.onSceneGUIDelegate += CustomOnSceneGUI;
    }

    public void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= CustomOnSceneGUI;
    }

    private void CustomOnSceneGUI(SceneView s)
    {
        Selection.activeGameObject = ((Component)target).gameObject; //Here! Manually assign the selection to be your object
    }
    private void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        InstancePainter t = (InstancePainter)target;
        if (GUILayout.Button("clear instances"))
        {
            t.clearInstances();
        }
    }
}
#endif
[System.Serializable]
public class PaintablePrefab
{
    public GameObject prefab;
    public PaintablePrefab(GameObject _prefab)
    {
        prefab = _prefab;
    }
    [Range(0, 100)]
    public int selectionBias = 1;

    public Vector2 xRotationRange = new Vector2(0, 0);
    public Vector2 yRotationRange = new Vector2(-180, 180);
    public Vector2 zRotationRange = new Vector2(0, 0);

    public Vector2 scaleRange = new Vector2(.5f, 1);

    public ShadowCastingMode instanceShadowCastingMode = ShadowCastingMode.Off;

    public int currentCount = 0;

    public void copyParameters(PaintablePrefab from)
    {
        selectionBias = from.selectionBias;
        xRotationRange = from.xRotationRange;
        yRotationRange = from.yRotationRange;
        zRotationRange = from.zRotationRange;
        scaleRange = from.scaleRange;
        currentCount = from.currentCount;
        instanceShadowCastingMode = from.instanceShadowCastingMode;
    }
}