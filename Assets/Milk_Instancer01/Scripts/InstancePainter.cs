using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MilkInstancer
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MilkInstancer))]
    public class InstancePainter : MonoBehaviour
    {
        #region Variables
        public int numToAdd = 256;
        public float areaSize = 5;
        [HideInInspector] public MilkInstancer indirectRenderer;
        [HideInInspector] public ZoneManager zoneManager;


        //public IndirectInstanceData[] instances;
        //public PaintablePrefab[] prefabParamaters = new PaintablePrefab[0];
        [Header("debug")]
        public bool disableInstancingAtRunTime;

#if UNITY_EDITOR
        public LayerMask rayCastLayerMask;
        [HideInInspector] public GameObject[] _prefabs = new GameObject[0];
        int[] selectionBias;
#endif
        #endregion

        #region non-editor functions
        private void Awake()
        {
            if (disableInstancingAtRunTime && Application.isPlaying)
            {
                //foreach(IndirectInstanceData d in instances)
                //{
                //    for (int i = 0; i < d.positions.Length; i++)
                //    {
                //        Instantiate(d.prefab, d.positions[i], Quaternion.Euler(d.rotations[i]), transform);
                //    }
                //}
                //GetComponent<MilkInstancer>().enabled = false;
            }
        }
        [HideInInspector] public Unity.Mathematics.Random rand = new Unity.Mathematics.Random();
        public void clearInstances()
        {
            zoneManager.ClearInstances();
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
            SceneView.duringSceneGui += OnScene;
#endif
        }
        public void OnDisable()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui -= OnScene;
#endif
        }
#if UNITY_EDITOR
        void eraseInstances(Vector3 point)
        {
            zoneManager.removeData(point, areaSize);
        }
        void paintInstances(Vector3 origin, Vector3 point, float distance, Vector3 normal)
        {
            if (rand.state == 0)
            {
                rand.InitState(1298);
            }
            if (transform.childCount == 0)
            {
                new GameObject("rotation helper").transform.SetParent(transform);
            }
            Transform helper = transform.GetChild(0);

            PaintablePrefab parameters;
            //Vector2 L = Vector2.one;
            int[] paintTypes = new int[zoneManager.instanceTypes.Length];
            Vector3[] points = new Vector3[numToAdd];
            Vector3[] rotations = new Vector3[numToAdd];
            RaycastHit hit;
            Vector3 dir = Vector3.one;
            Vector2 ang = Vector2.zero;
            int k = 0;
            List<Vector3>[] tempBS = new List<Vector3>[zoneManager.instanceTypes.Length];
            for (int i = 0; i < tempBS.Length; i++)
            {
                tempBS[i] = new List<Vector3>();
            }
            for (int i = 0; i < numToAdd; i++)
            {
                ang = UnityEngine.Random.insideUnitCircle * rand.NextFloat(areaSize * .5f);
                dir = point + Vector3.ProjectOnPlane((UnityEngine.Random.insideUnitSphere * areaSize * .5f), normal);
                //dir = Vector3.ProjectOnPlane(new Vector3(ang.x, 0, ang.y), normal) + point;
                dir = dir - origin;
                dir = dir.normalized;
                //Debug.DrawRay(origin, dir * 50, Color.blue, 15);
                if (Physics.Raycast(origin, dir, out hit, (areaSize * areaSize) + distance, rayCastLayerMask))
                {
                    int type = selectionBias[rand.NextInt(selectionBias.Length)];

                    bool bad = zoneManager.checkMinDistance(hit.point, type, zoneManager.instanceTypes[type].minDistanceBetweenInstances);
                    //if (instances[type] != null && instances[type].positions != null)
                    //{
                    //    for (int n = 0; n < instances[type].positions.Length; n++)
                    //    {
                    //        if (Vector3.Distance(hit.point, instances[type].positions[n]) < prefabParamaters[type].minDistance)
                    //        {
                    //            bad = true;
                    //        }
                    //    }
                    //}
                    if (!bad)
                    {
                        for (int n = 0; n < tempBS[type].Count; n++)
                        {
                            if (Vector3.Distance(hit.point, tempBS[type][n]) < zoneManager.instanceTypes[type].minDistanceBetweenInstances)
                            {
                                bad = true;
                            }
                        }
                    }
                    if (!bad)
                    {
                        parameters = zoneManager.instanceTypes[type];

                        tempBS[type].Add(hit.point);
                        paintTypes[type]++;
                        points[k] = hit.point;
                        //Quaternion rot = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);

                        Quaternion bruh = Quaternion.Euler(rand.NextFloat(parameters.xRotationRange.x, parameters.xRotationRange.y), rand.NextFloat(parameters.yRotationRange.x, parameters.yRotationRange.y), rand.NextFloat(parameters.zRotationRange.x, parameters.zRotationRange.y));
                        transform.rotation = Quaternion.Lerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, hit.normal), parameters.RotateToNormalBias);
                        helper.localRotation = bruh;
                        rotations[k] = helper.rotation.eulerAngles;
                        k++;
                    }
                }
            }
            k = 0;

            zoneInstanceData[] data = new zoneInstanceData[zoneManager.instanceTypes.Length];

            for (int n = 0; n < zoneManager.instanceTypes.Length; n++)
            {
                data[n] = new zoneInstanceData();

                parameters = zoneManager.instanceTypes[n];
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
                    rot[i] = rotations[k];
                    //rot[i] = rotations[k];
                    scale[i] = Vector3.one * rand.NextFloat(parameters.scaleRange.x, parameters.scaleRange.y);
                    k++;
                }
                data[n].positions = pos;
                data[n].rotations = rot;
                data[n].scales = scale;

                //addInstanceData(n, pos, rot, scale);
            }

            zoneManager.FilterData(data);
        }
        private void OnValidate()
        {
            zoneManager = GetComponent<ZoneManager>();
        }
        public void countBias()
        {
            if (!zoneManager || zoneManager.instanceTypes == null)
            {
                return;
            }
            List<int> bias = new List<int>();
            for (int i = 0; i < zoneManager.instanceTypes.Length; i++)
            {
                //instances[i].parameters.copyParameters(prefabParamaters[i]);
                for (int n = 0; n < zoneManager.instanceTypes[i].selectionBias; n++)
                {
                    bias.Add(i);
                }
            }
            selectionBias = bias.ToArray();
        }


        bool drawGizmo = false;
        Vector3 gizmoPosition = Vector3.zero;
        Vector3 gizmoNormal = Vector3.up;
        bool gizmoErase = false;
        void OnScene(SceneView scene)
        {
            if (Selection.activeTransform != null && Selection.activeTransform.gameObject != gameObject)
            {
                return;
            }
            Event e = Event.current;
            RaycastHit hit;
            Vector3 mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;
            //Quaternion rot = scene.camera.transform.rotation;

            Ray ray = scene.camera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out hit, 999, rayCastLayerMask))
            {
                drawGizmo = true;
                gizmoPosition = hit.point;
                gizmoNormal = hit.normal;

                if (!e.control)
                {
                    gizmoErase = false;
                    if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
                    {
                        paintInstances(ray.origin, hit.point, hit.distance, hit.normal);
                    }
                }
                else
                {
                    gizmoErase = true;
                    if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
                    {
                        eraseInstances(hit.point);
                    }
                }
            }
            else
            {
                drawGizmo = false;
            }

        }
        void OnDrawGizmosSelected()
        {
            if (drawGizmo)
            {
                if (!gizmoErase)
                {
                    Gizmos.color = new Color(.6f, .6f, 1, .25f);
                    Gizmos.DrawSphere(gizmoPosition, areaSize / 2);
                    Gizmos.DrawLine(gizmoPosition, gizmoPosition + (3 * gizmoNormal));
                }
                else
                {
                    Gizmos.color = new Color(1, .6f, .6f, .25f);
                    Gizmos.DrawSphere(gizmoPosition, areaSize / 2);
                    Gizmos.DrawLine(gizmoPosition, gizmoPosition + (3 * gizmoNormal));
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
            SceneView.duringSceneGui += CustomOnSceneGUI;
        }

        public void OnDisable()
        {
            SceneView.duringSceneGui -= CustomOnSceneGUI;
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
}