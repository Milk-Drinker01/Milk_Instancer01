using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEngine.UI;
#endif

// based off eliomans indirect isntancer using compute shaders. 

// Preferrably want to have all buffer structs in power of 2...
// 6 * 4 bytes = 24 bytes
[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct IndirectInstanceCSInput
{
    public Vector3 boundsCenter;       // 3
    public Vector3 boundsExtents;      // 6
}

// 8 * 4 bytes = 32 bytes
[StructLayout(LayoutKind.Sequential)]
public struct Indirect2x2Matrix
{
    public Vector4 row0;    // 4
    public Vector4 row1;    // 8
};

// 2 * 4 bytes = 8 bytes
[StructLayout(LayoutKind.Sequential)]
public struct SortingData
{
    public uint drawCallInstanceIndex; // 1
    public float distanceToCam;         // 2
};

[ExecuteInEditMode]
//[ExecuteAlways]
public class MilkInstancer : MonoBehaviour
{
    public static MilkInstancer instance;
    [Header("Settings")]
    public bool runCompute = true;
    public bool drawInstances = true;
    public bool drawInstanceShadows = true;
    public bool enableFrustumCulling = true;
    public bool enableOcclusionCulling = true;
    public bool enableDetailCulling = true;
    public bool enableLOD = true;
    public bool isVR;   //this does nothing but make sure people dont use single pass instanced when not using vr
    public bool vrRenderingModeIsSinglePassInstanced;
    [Range(00.00f, .05f)] public float detailCullingPercentage = 0.005f;

    private Camera InstancerCamera;
    public Camera GetInstancerCamera() { return InstancerCamera; }

    [Header("Compute Shaders")]
    public ComputeShader createDrawDataBufferCS;
    public ComputeShader sortingCS;
    public ComputeShader occlusionCS;
    public ComputeShader scanInstancesCS;
    public ComputeShader scanGroupSumsCS;
    public ComputeShader copyInstanceDataCS;

    [Header("Data")]
    [ReadOnly] List<IndirectInstanceCSInput> instancesInputData = new List<IndirectInstanceCSInput>();
    [ReadOnly] IndirectRenderingMesh[] indirectMeshes;

    [Header("debug")]
#if UNITY_EDITOR
    public bool scanDebug;
    public bool groupScanDebug;
    public bool sortDebug;
    
#endif

#region compute buffers
    private ComputeBuffer m_instancesIsVisibleBuffer;
    private ComputeBuffer m_instancesGroupSumArrayBuffer;
    private ComputeBuffer m_instancesScannedGroupSumBuffer;
    private ComputeBuffer m_instancesScannedPredicates;
    private ComputeBuffer m_instanceDataBuffer;
    private ComputeBuffer m_instancesSortingData;
    private ComputeBuffer m_instancesSortingDataTemp;
    private ComputeBuffer m_instancesMatrixRows01;
    private ComputeBuffer m_instancesMatrixRows23;
    private ComputeBuffer m_instancesMatrixRows45;
    private ComputeBuffer m_instancesCulledMatrixRows01;
    private ComputeBuffer m_instancesCulledMatrixRows23;
    private ComputeBuffer m_instancesCulledMatrixRows45;
    private ComputeBuffer m_instancesArgsBuffer;
    private ComputeBuffer m_shadowArgsBuffer;
    private ComputeBuffer m_shadowsIsVisibleBuffer;
    private ComputeBuffer m_shadowGroupSumArrayBuffer;
    private ComputeBuffer m_shadowsScannedGroupSumBuffer;
    private ComputeBuffer m_shadowScannedInstancePredicates;
    private ComputeBuffer m_shadowCulledMatrixRows01;
    private ComputeBuffer m_shadowCulledMatrixRows23;
    private ComputeBuffer m_shadowCulledMatrixRows45;

    private ComputeBuffer lod0RangePerTypeBuffer;
    private ComputeBuffer lod1RangePerTypeBuffer;
    private ComputeBuffer distancePerTypeBuffer;
    private ComputeBuffer occCulPerType;
    private ComputeBuffer frustCulPerType;
#endregion
#region command buffers
    private CommandBuffer m_sortingCommandBuffer;
#endregion
#region kernel IDS
    private int m_createDrawDataBufferKernelID;
    private int m_sortingCSKernelID;
    private int m_sortingTransposeKernelID;
    private int m_occlusionKernelID;
    private int m_scanInstancesKernelID;
    private int m_scanGroupSumsKernelID;
    private int m_copyInstanceDataKernelID;
    private int m_setupSinglePassInstancedID;
    #endregion
    #region shader property IDs
    private static readonly int _Data = Shader.PropertyToID("_Data");
    private static readonly int _Input = Shader.PropertyToID("_Input");
    private static readonly int _ShouldFrustumCull = Shader.PropertyToID("_ShouldFrustumCull");
    private static readonly int _ShouldOcclusionCull = Shader.PropertyToID("_ShouldOcclusionCull");
    private static readonly int _ShouldLOD = Shader.PropertyToID("_ShouldLOD");
    private static readonly int _ShouldDetailCull = Shader.PropertyToID("_ShouldDetailCull");
    private static readonly int _ShouldOnlyUseLOD02Shadows = Shader.PropertyToID("_ShouldOnlyUseLOD02Shadows");
    private static readonly int _UNITY_MATRIX_MVP = Shader.PropertyToID("_UNITY_MATRIX_MVP");
    private static readonly int _CamPosition = Shader.PropertyToID("_CamPosition");
    private static readonly int _HiZTextureSize = Shader.PropertyToID("_HiZTextureSize");
    private static readonly int _Level = Shader.PropertyToID("_Level");
    private static readonly int _LevelMask = Shader.PropertyToID("_LevelMask");
    private static readonly int _Width = Shader.PropertyToID("_Width");
    private static readonly int _Height = Shader.PropertyToID("_Height");
    private static readonly int _ShadowDistance = Shader.PropertyToID("_ShadowDistance");
    private static readonly int _DetailCullingScreenPercentage = Shader.PropertyToID("_DetailCullingScreenPercentage");
    private static readonly int _HiZMap = Shader.PropertyToID("_HiZMap");
    private static readonly int _NumOfGroups = Shader.PropertyToID("_NumOfGroups");
    private static readonly int _NumOfDrawcalls = Shader.PropertyToID("_NumOfDrawcalls");
    private static readonly int _ArgsOffset = Shader.PropertyToID("_ArgsOffset");
    private static readonly int _Positions = Shader.PropertyToID("_Positions");
    private static readonly int _Scales = Shader.PropertyToID("_Scales");
    private static readonly int _Rotations = Shader.PropertyToID("_Rotations");
    private static readonly int _ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");
    private static readonly int _ShadowArgsBuffer = Shader.PropertyToID("_ShadowArgsBuffer");
    private static readonly int _IsVisibleBuffer = Shader.PropertyToID("_IsVisibleBuffer");
    private static readonly int _ShadowIsVisibleBuffer = Shader.PropertyToID("_ShadowIsVisibleBuffer");
    private static readonly int _GroupSumArray = Shader.PropertyToID("_GroupSumArray");
    private static readonly int _ScannedInstancePredicates = Shader.PropertyToID("_ScannedInstancePredicates");
    private static readonly int _GroupSumArrayIn = Shader.PropertyToID("_GroupSumArrayIn");
    private static readonly int _GroupSumArrayOut = Shader.PropertyToID("_GroupSumArrayOut");
    private static readonly int _DrawcallDataOut = Shader.PropertyToID("_DrawcallDataOut");
    private static readonly int _SortingData = Shader.PropertyToID("_SortingData");
    private static readonly int _InstanceDataBuffer = Shader.PropertyToID("_InstanceDataBuffer");
    private static readonly int _InstancePredicatesIn = Shader.PropertyToID("_InstancePredicatesIn");
    private static readonly int _InstancesDrawMatrixRows01 = Shader.PropertyToID("_InstancesDrawMatrixRows01");
    private static readonly int _InstancesDrawMatrixRows23 = Shader.PropertyToID("_InstancesDrawMatrixRows23");
    private static readonly int _InstancesDrawMatrixRows45 = Shader.PropertyToID("_InstancesDrawMatrixRows45");
    private static readonly int _InstancesCulledMatrixRows01 = Shader.PropertyToID("_InstancesCulledMatrixRows01");
    private static readonly int _InstancesCulledMatrixRows23 = Shader.PropertyToID("_InstancesCulledMatrixRows23");
    private static readonly int _InstancesCulledMatrixRows45 = Shader.PropertyToID("_InstancesCulledMatrixRows45");

    private static readonly int _OcclusionCullingPerType = Shader.PropertyToID("_perTypeOcclusionCull");
    private static readonly int _FrustumCullingPerType = Shader.PropertyToID("_perTypeFrustumCull");
    private static readonly int _Lod0RangePerType = Shader.PropertyToID("_perTypeLod0Range");
    private static readonly int _Lod1RangePerType = Shader.PropertyToID("_perTypeLod1Range");
    private static readonly int _DistancePerType = Shader.PropertyToID("_perTypeRenderDistance");
    #endregion

    private RenderPipelineSetup _RenderPipelineSetup;
    private RenderTexture HiZDepthTexture;
    private int m_numberOfInstanceTypes;
    private int m_numberOfInstances;
    private int m_occlusionGroupX;
    private int m_scanInstancesGroupX;
    private int m_scanThreadGroupsGroupX;
    private int m_copyInstanceDataGroupX;
    private bool m_debugLastDrawLOD = false;
    private uint[] m_args;
    private Bounds m_bounds;

    private const int NUMBER_OF_DRAW_CALLS = 3; // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5; // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW; // 3draws * 5args = 15args
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = NUMBER_OF_ARGS_PER_DRAW * sizeof(uint); // 5args * 4bytes = 20 bytes
    private const int ARGS_BYTE_SIZE_PER_INSTANCE_TYPE = NUMBER_OF_ARGS_PER_INSTANCE_TYPE * sizeof(uint); // 15args * 4bytes = 60bytes
    private const int SCAN_THREAD_GROUP_SIZE = 64;

#if UNITY_EDITOR
    private void OnValidate()
    {
        CheckIfOnlyInstance();
    }
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        CheckIfOnlyInstance();
#endif
        //if (QualitySettings.renderPipeline == null)
        //{
        //    maxShadowDistance = QualitySettings.shadowDistance;
        //}
        //else
        //{
        //    foreach (Volume pr in GameObject.FindObjectsOfType<Volume>())
        //    {
        //        HDShadowSettings s;
        //        pr.sharedProfile.TryGet(out s);
        //        if (s != null)
        //        {
        //            maxShadowDistance = (float)s.maxShadowDistance;
        //        }
        //    }
        //}
    }
    
    private void OnEnable()
    {
        if (GetComponent<ZoneManager>())
        {
            GetComponent<ZoneManager>().ResetPosition();
        }
        //UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += renderingDone;
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += PreRender;
#if UNITY_EDITOR
        //SceneView.onSceneGUIDelegate += OnScene;
        SceneView.duringSceneGui += OnScene;
#endif
    }
    private void OnDisable()
    {
        ReleaseBuffers();
        //UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= renderingDone;
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= PreRender;
        if (HiZDepthTexture != null)
        {
            HiZDepthTexture.Release();
            HiZDepthTexture = null;
        }
#if UNITY_EDITOR
        //SceneView.onSceneGUIDelegate -= OnScene;
        SceneView.duringSceneGui -= OnScene;
#endif
    }
#if UNITY_EDITOR
    void OnScene(SceneView scene)
    {
        //force unity to repaint the scene view, and trigger an update in edit mode
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
    }
#endif
    void PreRender(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
    {
        if (Camera.main)
            InstancerCamera = Camera.main;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (camera.cameraType == CameraType.SceneView)
                InstancerCamera = camera;
        }
        else
        {
            if (camera == Camera.main)
                InstancerCamera = camera;
            else if (camera.cameraType == CameraType.SceneView)
                InstancerCamera = Camera.main;
        }
#endif
    }
    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void LateUpdate()
    {
        RenderInstances(InstancerCamera);
    }

#region draw
    public void RenderInstances(Camera cam)
    {
        //Debug.Log(cam.name);
        if (indirectMeshes == null || indirectMeshes.Length == 0 || !initialized)
        {
            return;
        }

        if (runCompute)
        {
            Profiler.BeginSample("CalculateVisibleInstances()");
            CalculateVisibleInstances(cam);
            Profiler.EndSample();
        }
        //checkDebugLOD();
        if (drawInstances)
        {
            Profiler.BeginSample("DrawInstances()");
            DrawInstances();
            Profiler.EndSample();
        }

        if (drawInstanceShadows)
        {
            Profiler.BeginSample("DrawInstanceShadows()");
            DrawInstanceShadows();
            Profiler.EndSample();
        }
    }
    //private const string DEBUG_SHADER_LOD_KEYWORD = "_INDIRECT_DEBUG_LOD_ON";
    //public bool drawLodDebug;
    //void checkDebugLOD()
    //{
    //    if (drawLodDebug != m_debugLastDrawLOD)
    //    {
    //        m_debugLastDrawLOD = drawLodDebug;

    //        if (drawLodDebug)
    //        {
    //            for (int i = 0; i < indirectMeshes.Length; i++)
    //            {
    //                indirectMeshes[i].material[0].EnableKeyword(DEBUG_SHADER_LOD_KEYWORD);
    //            }
    //        }
    //        else
    //        {
    //            for (int i = 0; i < indirectMeshes.Length; i++)
    //            {
    //                indirectMeshes[i].material[0].DisableKeyword(DEBUG_SHADER_LOD_KEYWORD);
    //            }
    //        }
    //    }
    //}
    //[HideInInspector] public ShadowCastingMode[] instanceShadowCastingModes;
    [HideInInspector] public bool[] instanceShadowCastingModes;
    private void DrawInstances()
    {
        for (int i = 0; i < indirectMeshes.Length; i++)
        {
            int argsIndex = i * ARGS_BYTE_SIZE_PER_INSTANCE_TYPE;
            IndirectRenderingMesh irm = indirectMeshes[i];

            if (enableLOD)
            {
                //Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material, m_bounds, m_instancesArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 0, irm.lod00MatPropBlock, instanceShadowCastingModes[i]);
                Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material[irm.lod0Material], m_bounds, m_instancesArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 0, irm.lod00MatPropBlock, ShadowCastingMode.Off);
                //Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material, m_bounds, m_instancesArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 1, irm.lod01MatPropBlock, instanceShadowCastingModes[i]);
                Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material[irm.lod1Material], m_bounds, m_instancesArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 1, irm.lod01MatPropBlock, ShadowCastingMode.Off);
            }
            //Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material, m_bounds, m_instancesArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 2, irm.lod02MatPropBlock, instanceShadowCastingModes[i]);
            Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material[irm.lod2Material], m_bounds, m_instancesArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 2, irm.lod02MatPropBlock, ShadowCastingMode.Off);
        }
    }
    private void DrawInstanceShadows()
    {
        for (int i = 0; i < indirectMeshes.Length; i++)
        {
            if (instanceShadowCastingModes[i])
            {
                int argsIndex = i * ARGS_BYTE_SIZE_PER_INSTANCE_TYPE;
                IndirectRenderingMesh irm = indirectMeshes[i];

                if (enableLOD)
                {
                    Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material[irm.lod0Material], m_bounds, m_shadowArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 0, irm.shadowLod00MatPropBlock, ShadowCastingMode.ShadowsOnly);
                    Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material[irm.lod1Material], m_bounds, m_shadowArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 1, irm.shadowLod01MatPropBlock, ShadowCastingMode.ShadowsOnly);
                }
                Graphics.DrawMeshInstancedIndirect(irm.mesh, 0, irm.material[irm.lod2Material], m_bounds, m_shadowArgsBuffer, argsIndex + ARGS_BYTE_SIZE_PER_DRAW_CALL * 2, irm.shadowLod02MatPropBlock, ShadowCastingMode.ShadowsOnly);
            }
        }
    }

    Vector3 cameraPosition;
    Matrix4x4 m_MVP;
    private void CalculateVisibleInstances(Camera cam)
    {
        if (cam == null)
        {
            return;
        }
        // Global data
        cameraPosition = cam.transform.position;
        m_bounds.center = cameraPosition;

        //Matrix4x4 m = mainCamera.transform.localToWorldMatrix;
        Matrix4x4 v = cam.worldToCameraMatrix;
        Matrix4x4 p = cam.projectionMatrix;
        m_MVP = p * v;

        //////////////////////////////////////////////////////
        // Reset the arguments buffer
        //////////////////////////////////////////////////////
        Profiler.BeginSample("Resetting args buffer");
        {
            m_instancesArgsBuffer.SetData(m_args);
            m_shadowArgsBuffer.SetData(m_args);
        }
        Profiler.EndSample();



        //////////////////////////////////////////////////////
        // Set up compute shader to perform the occlusion culling
        //////////////////////////////////////////////////////
        Profiler.BeginSample("02 Occlusion");
        {
            // Input
            occlusionCS.SetFloat(_ShadowDistance, _RenderPipelineSetup == null ? 150 : _RenderPipelineSetup.GetShadowDistance());
            occlusionCS.SetMatrix(_UNITY_MATRIX_MVP, m_MVP);
            occlusionCS.SetVector(_CamPosition, cameraPosition);

            // Dispatch
            occlusionCS.Dispatch(m_occlusionKernelID, m_occlusionGroupX, 1, 1);
        }
        Profiler.EndSample();

        //////////////////////////////////////////////////////
        // Perform scan of instance predicates
        //////////////////////////////////////////////////////
        Profiler.BeginSample("03 Scan Instances");
        {
            // Normal
            scanInstancesCS.SetBuffer(m_scanInstancesKernelID, _InstancePredicatesIn, m_instancesIsVisibleBuffer);
            scanInstancesCS.SetBuffer(m_scanInstancesKernelID, _GroupSumArray, m_instancesGroupSumArrayBuffer);
            scanInstancesCS.SetBuffer(m_scanInstancesKernelID, _ScannedInstancePredicates, m_instancesScannedPredicates);
            scanInstancesCS.Dispatch(m_scanInstancesKernelID, m_scanInstancesGroupX, 1, 1);
#if UNITY_EDITOR
            if (scanDebug)
            {
                scanDebug = false;
                uint[] data = new uint[m_instancesScannedPredicates.count];
                m_instancesScannedPredicates.GetData(data);
                for (int i = 0; i < data.Length; i++)
                {
                    Debug.Log(data[i]);
                }
            }
#endif

            // Shadows
            scanInstancesCS.SetBuffer(m_scanInstancesKernelID, _InstancePredicatesIn, m_shadowsIsVisibleBuffer);
            scanInstancesCS.SetBuffer(m_scanInstancesKernelID, _GroupSumArray, m_shadowGroupSumArrayBuffer);
            scanInstancesCS.SetBuffer(m_scanInstancesKernelID, _ScannedInstancePredicates, m_shadowScannedInstancePredicates);
            scanInstancesCS.Dispatch(m_scanInstancesKernelID, m_scanInstancesGroupX, 1, 1);
        }
        Profiler.EndSample();

        //////////////////////////////////////////////////////
        // Perform scan of group sums
        //////////////////////////////////////////////////////
        Profiler.BeginSample("Scan Thread Groups");
        {
            // Normal
            scanGroupSumsCS.SetBuffer(m_scanGroupSumsKernelID, _GroupSumArrayIn, m_instancesGroupSumArrayBuffer);
            scanGroupSumsCS.SetBuffer(m_scanGroupSumsKernelID, _GroupSumArrayOut, m_instancesScannedGroupSumBuffer);
            scanGroupSumsCS.Dispatch(m_scanGroupSumsKernelID, m_scanThreadGroupsGroupX, 1, 1);
#if UNITY_EDITOR
            if (groupScanDebug)
            {
                groupScanDebug = false;
                uint[] data = new uint[m_instancesScannedGroupSumBuffer.count];
                m_instancesScannedGroupSumBuffer.GetData(data);
                for (int i = 0; i < data.Length; i++)
                {
                    Debug.Log(data[i]);
                }
            }
#endif
            // Shadows
            scanGroupSumsCS.SetBuffer(m_scanGroupSumsKernelID, _GroupSumArrayIn, m_shadowGroupSumArrayBuffer);
            scanGroupSumsCS.SetBuffer(m_scanGroupSumsKernelID, _GroupSumArrayOut, m_shadowsScannedGroupSumBuffer);
            scanGroupSumsCS.Dispatch(m_scanGroupSumsKernelID, m_scanThreadGroupsGroupX, 1, 1);
        }
        Profiler.EndSample();

        //////////////////////////////////////////////////////
        // Perform stream compaction 
        // Calculate instance offsets and store in drawcall arguments buffer
        //////////////////////////////////////////////////////
        Profiler.BeginSample("Copy Instance Data");
        {
            // Normal
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancePredicatesIn, m_instancesIsVisibleBuffer);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _GroupSumArray, m_instancesScannedGroupSumBuffer);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _ScannedInstancePredicates, m_instancesScannedPredicates);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesCulledMatrixRows01, m_instancesCulledMatrixRows01);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesCulledMatrixRows23, m_instancesCulledMatrixRows23);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesCulledMatrixRows45, m_instancesCulledMatrixRows45);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _DrawcallDataOut, m_instancesArgsBuffer);
            copyInstanceDataCS.Dispatch(m_copyInstanceDataKernelID, m_copyInstanceDataGroupX, 1, 1);

            // Shadows
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancePredicatesIn, m_shadowsIsVisibleBuffer);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _GroupSumArray, m_shadowsScannedGroupSumBuffer);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _ScannedInstancePredicates, m_shadowScannedInstancePredicates);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesCulledMatrixRows01, m_shadowCulledMatrixRows01);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesCulledMatrixRows23, m_shadowCulledMatrixRows23);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesCulledMatrixRows45, m_shadowCulledMatrixRows45);
            copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _DrawcallDataOut, m_shadowArgsBuffer);
            copyInstanceDataCS.Dispatch(m_copyInstanceDataKernelID, m_copyInstanceDataGroupX, 1, 1);
        }
        Profiler.EndSample();

        if (vrRenderingModeIsSinglePassInstanced && Application.isPlaying && isVR)
        {
            copyInstanceDataCS.SetBuffer(m_setupSinglePassInstancedID, _DrawcallDataOut, m_instancesArgsBuffer);
            copyInstanceDataCS.Dispatch(m_setupSinglePassInstancedID, Mathf.Max(1, m_numberOfInstanceTypes - 1), 1, 1);

            copyInstanceDataCS.SetBuffer(m_setupSinglePassInstancedID, _DrawcallDataOut, m_shadowArgsBuffer);
            copyInstanceDataCS.Dispatch(m_setupSinglePassInstancedID, Mathf.Max(1, m_numberOfInstanceTypes - 1), 1, 1);
            //uint[] args = new uint[m_instancesArgsBuffer.count];
            //m_instancesArgsBuffer.GetData(args);
            //for (int i = 0; i < (args.Length / 5) - 1; i++)
            //{
            //    args[(i * 5) + 1] *= 2;
            //}
            //m_instancesArgsBuffer.SetData(args);
        }

        //temp solution for single pass instanced
        //uint[] args = new uint[m_instancesArgsBuffer.count];
        //m_instancesArgsBuffer.GetData(args);
        //for (int i = 0; i < (args.Length / 5) - 1; i++)
        //{
        //    args[(i * 5) + 1] *= 2;
        //}
        //m_instancesArgsBuffer.SetData(args);

        //////////////////////////////////////////////////////
        // Sort the position buffer based on distance from camera
        //////////////////////////////////////////////////////
        Profiler.BeginSample("LOD Sorting");
        {
            //m_lastCamPosition = camPosition;

            m_paddingInput[0] = int.MinValue;
            m_paddingInput[1] = 0;
            m_paddingBuffer.SetData(m_paddingInput);
            Graphics.ExecuteCommandBufferAsync(m_sortingCommandBuffer, ComputeQueueType.Background);
#if UNITY_EDITOR
            if (sortDebug)
            {
                sortDebug = false;
                SortingData[] data = new SortingData[m_instancesSortingData.count];
                m_instancesSortingData.GetData(data);
                for (int i = 0; i < data.Length; i++)
                {
                    Debug.Log(data[i].distanceToCam);
                }
            }
#endif
        }
        Profiler.EndSample();
    }
    
    private bool TryGetKernels()
    {
        return TryGetKernel("CSMain", ref createDrawDataBufferCS, ref m_createDrawDataBufferKernelID)
            //&& TryGetKernel("BitonicSort", ref sortingCS, ref m_sortingCSKernelID)
            //&& TryGetKernel("MatrixTranspose", ref sortingCS, ref m_sortingTransposeKernelID)
            && TryGetKernel("CSMain", ref occlusionCS, ref m_occlusionKernelID)
            && TryGetKernel("CSMain", ref scanInstancesCS, ref m_scanInstancesKernelID)
            && TryGetKernel("CSMain", ref scanGroupSumsCS, ref m_scanGroupSumsKernelID)
            && TryGetKernel("CSMain", ref copyInstanceDataCS, ref m_copyInstanceDataKernelID)
            && TryGetKernel("SinglePassInstancedSetup", ref copyInstanceDataCS, ref m_setupSinglePassInstancedID)
        ;
    }
    private static bool TryGetKernel(string kernelName, ref ComputeShader cs, ref int kernelID)
    {
        if (!cs.HasKernel(kernelName))
        {
            Debug.LogError(kernelName + " kernel not found in " + cs.name + "!");
            return false;
        }

        kernelID = cs.FindKernel(kernelName);
        return true;
    }
#endregion
#region initialization

    public void StopDrawing(bool shouldReleaseBuffers = false)
    {
        if (shouldReleaseBuffers)
        {
            ReleaseBuffers();
            initialized = false;
        }
    }
    [HideInInspector] public bool initialized;
    public void Initialize(ref PaintablePrefab[] _instances)
    {
        initialized = InitializeRenderer(ref _instances);
    }
    [HideInInspector] public uint[] FrustumCullingPerType;
    [HideInInspector] public uint[] OcclusionCullingPerType;
    [HideInInspector] public float[] lod0RangePerType;
    [HideInInspector] public float[] lod1RangePerType;
    [HideInInspector] public float[] distancePerType;
    public bool InitializeRenderer(ref PaintablePrefab[] _instances)
    {
        if (!TryGetKernels())
        {
            return false;
        }
        ReleaseBuffers();
        instancesInputData.Clear();
        m_numberOfInstanceTypes = 0;
        for (int i = 0; i < _instances.Length; i++)
        {
            m_numberOfInstanceTypes += Mathf.Max(_instances[i].lodMaterialIndexes[0].workaround.Length, _instances[i].lodMaterialIndexes[1].workaround.Length, _instances[i].lodMaterialIndexes[2].workaround.Length);
        }
        instanceShadowCastingModes = new bool[m_numberOfInstanceTypes];
        m_numberOfInstances = 0;
        m_bounds.center = Vector3.zero;
        m_bounds.extents = Vector3.one * 10000;
        if (!TryGetComponent<RenderPipelineSetup>(out _RenderPipelineSetup))
        {
            Debug.Log("---------------------------------------------------");
            Debug.Log("You are not using a Render Pipeline Setup component.");
            Debug.Log("Occlusion culling will not be used, due to lack of a depth texture.");
            Debug.Log("A default shadow distance of 150 will be used.");
            Debug.Log("---------------------------------------------------");
        }
        if (enableOcclusionCulling)
        {
            HiZDepthTexture = RenderPipelineSetup.GetDepthTexture();
        }
        else
        {
            HiZDepthTexture = RenderPipelineSetup.CreateDepthTexture(1,1);
        }
        indirectMeshes = new IndirectRenderingMesh[m_numberOfInstanceTypes];
        
        m_args = new uint[m_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> scales = new List<Vector3>();
        List<Vector3> rotations = new List<Vector3>();
        List<SortingData> sortingData = new List<SortingData>();

        lod0RangePerType = new float[m_numberOfInstanceTypes];
        lod1RangePerType = new float[m_numberOfInstanceTypes];
        distancePerType = new float[m_numberOfInstanceTypes];
        FrustumCullingPerType = new uint[m_numberOfInstanceTypes];
        OcclusionCullingPerType = new uint[m_numberOfInstanceTypes];
        uint forceoccCullingOff = (uint)(enableOcclusionCulling ? 1 : 0);
        uint forcefrusCullingOff = (uint)(enableFrustumCulling ? 1 : 0);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            forceoccCullingOff *= 0;
            forcefrusCullingOff *= 0;
        }
#endif
        //for (int i = 0; i < cullingPerType.Length; i++)
        //{
        //    cullingPerType[i] = 1 * forceCullingOff;
        //}

        int currentInstanceType = 0;
        for (int i = 0; i < _instances.Length; i++)
        {
            int max = Mathf.Max(_instances[i].lodMaterialIndexes[0].workaround.Length, _instances[i].lodMaterialIndexes[1].workaround.Length, _instances[i].lodMaterialIndexes[2].workaround.Length);
            PaintablePrefab iid = _instances[i];
            for (int t = 0; t < max; t++)
            {
                instanceShadowCastingModes[currentInstanceType] = iid.instanceShadowCastingMode;
                FrustumCullingPerType[currentInstanceType] = (uint)(iid.EnableFrustumCulling ? 1 : 0) * forcefrusCullingOff;
                OcclusionCullingPerType[currentInstanceType] = (uint)(iid.EnableOcclusionCulling ? 1 : 0) * forceoccCullingOff;
                lod0RangePerType[currentInstanceType] = iid.lod0Range;
                lod1RangePerType[currentInstanceType] = iid.lod1Range;
                distancePerType[currentInstanceType] = iid.maxRenderRange;

                IndirectRenderingMesh irm = new IndirectRenderingMesh();

                irm.lod0Material = iid.lodMaterialIndexes[0].workaround[0];
                irm.lod1Material = iid.lodMaterialIndexes[1].workaround[0];
                irm.lod2Material = iid.lodMaterialIndexes[2].workaround[0];

                // Initialize Mesh
                irm.numOfVerticesLod00 = (uint)iid.LODMeshes[0].vertexCount;
                irm.numOfVerticesLod01 = (uint)iid.LODMeshes[1].vertexCount;
                irm.numOfVerticesLod02 = (uint)iid.LODMeshes[2].vertexCount;
                if (iid.LODMeshes[0].subMeshCount > t)
                {
                    irm.numOfIndicesLod00 = iid.LODMeshes[0].GetIndexCount(t);
                    irm.lod0Material = iid.lodMaterialIndexes[0].workaround[t];
                }
                if (iid.LODMeshes[1].subMeshCount > t)
                {
                    irm.numOfIndicesLod01 = iid.LODMeshes[1].GetIndexCount(t);
                    irm.lod1Material = iid.lodMaterialIndexes[1].workaround[t];
                }
                if (iid.LODMeshes[2].subMeshCount > t)
                {
                    irm.numOfIndicesLod02 = iid.LODMeshes[2].GetIndexCount(t);
                    irm.lod2Material = iid.lodMaterialIndexes[2].workaround[t];
                }

                irm.mesh = new Mesh();
                irm.mesh.name = iid.prefabToPaint.name;
                irm.mesh.CombineMeshes(
                    new CombineInstance[] {
                    new CombineInstance() { mesh = iid.LODMeshes[0], subMeshIndex = t},
                    new CombineInstance() { mesh = iid.LODMeshes[1], subMeshIndex = t},
                    new CombineInstance() { mesh = iid.LODMeshes[2], subMeshIndex = t}
                    },
                    true,       // Merge Submeshes 
                    false,      // Use Matrices
                    false       // Has lightmap data
                );

                // Arguments
                int argsIndex = currentInstanceType * NUMBER_OF_ARGS_PER_INSTANCE_TYPE;

                // Buffer with arguments has to have five integer numbers
                // LOD00
                m_args[argsIndex + 0] = irm.numOfIndicesLod00;                          // 0 - index count per instance, 
                m_args[argsIndex + 1] = 0;                                              // 1 - instance count
                m_args[argsIndex + 2] = 0;                                              // 2 - start index location
                m_args[argsIndex + 3] = 0;                                              // 3 - base vertex location
                m_args[argsIndex + 4] = 0;                                              // 4 - start instance location

                // LOD01
                m_args[argsIndex + 5] = irm.numOfIndicesLod01;                          // 0 - index count per instance, 
                m_args[argsIndex + 6] = 0;                                              // 1 - instance count
                m_args[argsIndex + 7] = m_args[argsIndex + 0] + m_args[argsIndex + 2];  // 2 - start index location
                m_args[argsIndex + 8] = 0;                                              // 3 - base vertex location
                m_args[argsIndex + 9] = 0;                                              // 4 - start instance location

                // LOD02
                m_args[argsIndex + 10] = irm.numOfIndicesLod02;                         // 0 - index count per instance, 
                m_args[argsIndex + 11] = 0;                                             // 1 - instance count
                m_args[argsIndex + 12] = m_args[argsIndex + 5] + m_args[argsIndex + 7]; // 2 - start index location
                m_args[argsIndex + 13] = 0;                                             // 3 - base vertex location
                m_args[argsIndex + 14] = 0;                                             // 4 - start instance location

                // Materials
                irm.material = iid.indirectMaterial;//new Material(iid.indirectMaterial);
                Bounds originalBounds = CalculateBounds(iid.prefabToPaint);

                // Add the instance data (positions, rotations, scaling, bounds...)
                for (int j = 0; j < iid.transformData.positions.Length; j++)
                {
                    positions.Add(iid.transformData.positions[j]);
                    rotations.Add(iid.transformData.rotations[j]);
                    scales.Add(iid.transformData.scales[j]);

                    //Debug.Log(((((uint)i * NUMBER_OF_ARGS_PER_INSTANCE_TYPE) << 16) + ((uint)m_numberOfInstances)) >> 16);
                    sortingData.Add(new SortingData()
                    {
                        drawCallInstanceIndex = ((((uint)currentInstanceType * NUMBER_OF_ARGS_PER_INSTANCE_TYPE) << 16) + ((uint)m_numberOfInstances)),
                        //distanceToCam = Vector3.Distance(iid.transformData.positions[j], camPosition)
                        distanceToCam = 0
                    });

                    // Calculate the renderer bounds
                    Bounds b = new Bounds();
                    b.center = iid.transformData.positions[j] + originalBounds.center;
                    //b.center = originalBounds.center;
                    Vector3 s = originalBounds.size;
                    s.Scale(iid.transformData.scales[j]);
                    b.size = s;

                    instancesInputData.Add(new IndirectInstanceCSInput()
                    {
                        boundsCenter = b.center,
                        boundsExtents = b.extents,
                    });

                    m_numberOfInstances++;
                }

                // Add the data to the renderer list
                indirectMeshes[currentInstanceType] = irm;

                currentInstanceType++;
            }
        }

        int nextPowerOfTwo = Mathf.NextPowerOfTwo(m_numberOfInstances);
        //nextPowerOfTwo = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(m_numberOfInstances, 2)));

        int computeShaderInputSize = Marshal.SizeOf(typeof(IndirectInstanceCSInput));
        int computeShaderDrawMatrixSize = Marshal.SizeOf(typeof(Indirect2x2Matrix));
        int computeSortingDataSize = Marshal.SizeOf(typeof(SortingData));
        int numScannedGroups = 1 + Mathf.Max(1, Mathf.CeilToInt((float)m_numberOfInstances / (SCAN_THREAD_GROUP_SIZE * 2)));

        m_instancesArgsBuffer = new ComputeBuffer(m_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE, sizeof(uint), ComputeBufferType.IndirectArguments);
        m_instanceDataBuffer = new ComputeBuffer(m_numberOfInstances, computeShaderInputSize, ComputeBufferType.Default);
        m_instancesSortingData = new ComputeBuffer(m_numberOfInstances, computeSortingDataSize, ComputeBufferType.Default);
        //m_instancesSortingDataTemp = new ComputeBuffer(m_numberOfInstances, computeSortingDataSize, ComputeBufferType.Default);
        m_instancesMatrixRows01 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_instancesMatrixRows23 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_instancesMatrixRows45 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_instancesCulledMatrixRows01 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_instancesCulledMatrixRows23 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_instancesCulledMatrixRows45 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_instancesIsVisibleBuffer = new ComputeBuffer(m_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        m_instancesScannedPredicates = new ComputeBuffer(nextPowerOfTwo, sizeof(uint), ComputeBufferType.Default);
        m_instancesGroupSumArrayBuffer = new ComputeBuffer(nextPowerOfTwo, sizeof(uint), ComputeBufferType.Default);
        m_instancesScannedGroupSumBuffer = new ComputeBuffer(Mathf.NextPowerOfTwo(numScannedGroups), sizeof(uint), ComputeBufferType.Default);

        m_shadowArgsBuffer = new ComputeBuffer(m_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE, sizeof(uint), ComputeBufferType.IndirectArguments);
        m_shadowCulledMatrixRows01 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_shadowCulledMatrixRows23 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_shadowCulledMatrixRows45 = new ComputeBuffer(m_numberOfInstances, computeShaderDrawMatrixSize, ComputeBufferType.Default);
        m_shadowsIsVisibleBuffer = new ComputeBuffer(m_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        m_shadowScannedInstancePredicates = new ComputeBuffer(nextPowerOfTwo, sizeof(uint), ComputeBufferType.Default);
        m_shadowGroupSumArrayBuffer = new ComputeBuffer(nextPowerOfTwo, sizeof(uint), ComputeBufferType.Default);
        m_shadowsScannedGroupSumBuffer = new ComputeBuffer(Mathf.NextPowerOfTwo(numScannedGroups), sizeof(uint), ComputeBufferType.Default);

        m_instancesArgsBuffer.SetData(m_args);
        m_shadowArgsBuffer.SetData(m_args);
        m_instancesSortingData.SetData(sortingData);
        //m_instancesSortingDataTemp.SetData(sortingData);
        m_instanceDataBuffer.SetData(instancesInputData);

        // Setup the Material Property blocks for our meshes...
        //int _Whatever = Shader.PropertyToID("_Whatever");
        //int _DebugLODEnabled = Shader.PropertyToID("_DebugLODEnabled");
        for (int i = 0; i < indirectMeshes.Length; i++)
        {
            IndirectRenderingMesh irm = indirectMeshes[i];
            int argsIndex = i * NUMBER_OF_ARGS_PER_INSTANCE_TYPE;

            irm.lod00MatPropBlock = new MaterialPropertyBlock();
            irm.lod01MatPropBlock = new MaterialPropertyBlock();
            irm.lod02MatPropBlock = new MaterialPropertyBlock();
            irm.shadowLod00MatPropBlock = new MaterialPropertyBlock();
            irm.shadowLod01MatPropBlock = new MaterialPropertyBlock();
            irm.shadowLod02MatPropBlock = new MaterialPropertyBlock();

            irm.lod00MatPropBlock.SetInt(_ArgsOffset, argsIndex + 4);
            irm.lod01MatPropBlock.SetInt(_ArgsOffset, argsIndex + 9);
            irm.lod02MatPropBlock.SetInt(_ArgsOffset, argsIndex + 14);

            irm.shadowLod00MatPropBlock.SetInt(_ArgsOffset, argsIndex + 4);
            irm.shadowLod01MatPropBlock.SetInt(_ArgsOffset, argsIndex + 9);
            irm.shadowLod02MatPropBlock.SetInt(_ArgsOffset, argsIndex + 14);

            irm.lod00MatPropBlock.SetBuffer(_ArgsBuffer, m_instancesArgsBuffer);
            irm.lod01MatPropBlock.SetBuffer(_ArgsBuffer, m_instancesArgsBuffer);
            irm.lod02MatPropBlock.SetBuffer(_ArgsBuffer, m_instancesArgsBuffer);

            irm.shadowLod00MatPropBlock.SetBuffer(_ArgsBuffer, m_shadowArgsBuffer);
            irm.shadowLod01MatPropBlock.SetBuffer(_ArgsBuffer, m_shadowArgsBuffer);
            irm.shadowLod02MatPropBlock.SetBuffer(_ArgsBuffer, m_shadowArgsBuffer);

            irm.lod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_instancesCulledMatrixRows01);
            irm.lod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_instancesCulledMatrixRows01);
            irm.lod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_instancesCulledMatrixRows01);

            irm.lod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_instancesCulledMatrixRows23);
            irm.lod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_instancesCulledMatrixRows23);
            irm.lod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_instancesCulledMatrixRows23);

            irm.lod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_instancesCulledMatrixRows45);
            irm.lod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_instancesCulledMatrixRows45);
            irm.lod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_instancesCulledMatrixRows45);

            irm.shadowLod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_shadowCulledMatrixRows01);
            irm.shadowLod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_shadowCulledMatrixRows01);
            irm.shadowLod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_shadowCulledMatrixRows01);

            irm.shadowLod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_shadowCulledMatrixRows23);
            irm.shadowLod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_shadowCulledMatrixRows23);
            irm.shadowLod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_shadowCulledMatrixRows23);

            irm.shadowLod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_shadowCulledMatrixRows45);
            irm.shadowLod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_shadowCulledMatrixRows45);
            irm.shadowLod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_shadowCulledMatrixRows45);
        }

        //-----------------------------------
        // InitializeDrawData
        //-----------------------------------

        // Create the buffer containing draw data for all instances
        ComputeBuffer positionsBuffer = new ComputeBuffer(m_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        ComputeBuffer scaleBuffer = new ComputeBuffer(m_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        ComputeBuffer rotationBuffer = new ComputeBuffer(m_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);

        positionsBuffer.SetData(positions);
        scaleBuffer.SetData(scales);
        rotationBuffer.SetData(rotations);

        createDrawDataBufferCS.SetInt(Shader.PropertyToID("_count"), m_numberOfInstances);
        createDrawDataBufferCS.SetBuffer(m_createDrawDataBufferKernelID, _Positions, positionsBuffer);
        createDrawDataBufferCS.SetBuffer(m_createDrawDataBufferKernelID, _Scales, scaleBuffer);
        createDrawDataBufferCS.SetBuffer(m_createDrawDataBufferKernelID, _Rotations, rotationBuffer);
        createDrawDataBufferCS.SetBuffer(m_createDrawDataBufferKernelID, _InstancesDrawMatrixRows01, m_instancesMatrixRows01);
        createDrawDataBufferCS.SetBuffer(m_createDrawDataBufferKernelID, _InstancesDrawMatrixRows23, m_instancesMatrixRows23);
        createDrawDataBufferCS.SetBuffer(m_createDrawDataBufferKernelID, _InstancesDrawMatrixRows45, m_instancesMatrixRows45);

        int groupX = Mathf.Max(1, Mathf.CeilToInt((float)m_numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE)));

        createDrawDataBufferCS.Dispatch(m_createDrawDataBufferKernelID, groupX, 1, 1);

        ReleaseComputeBuffer(ref positionsBuffer);
        ReleaseComputeBuffer(ref scaleBuffer);
        ReleaseComputeBuffer(ref rotationBuffer);

        //-----------------------------------
        // InitConstantComputeVariables
        //-----------------------------------

        m_occlusionGroupX = Mathf.Max(1, nextPowerOfTwo / 64);
        m_scanInstancesGroupX = Mathf.Max(1, nextPowerOfTwo / (2 * SCAN_THREAD_GROUP_SIZE));
        m_scanThreadGroupsGroupX = 1;
        m_copyInstanceDataGroupX = Mathf.Max(1, nextPowerOfTwo / (2 * SCAN_THREAD_GROUP_SIZE));

        createDrawDataBufferCS.SetInt(Shader.PropertyToID("_count"), m_numberOfInstances);
        occlusionCS.SetInt(Shader.PropertyToID("_count"), m_numberOfInstances);
        scanInstancesCS.SetInt(Shader.PropertyToID("_num"), m_numberOfInstances);
        copyInstanceDataCS.SetInt(Shader.PropertyToID("_count"), m_numberOfInstances);
        

        lod0RangePerTypeBuffer = new ComputeBuffer(m_numberOfInstanceTypes, sizeof(float), ComputeBufferType.Default);
        lod0RangePerTypeBuffer.SetData(lod0RangePerType);
        occlusionCS.SetBuffer(m_occlusionKernelID, _Lod0RangePerType, lod0RangePerTypeBuffer);

        lod1RangePerTypeBuffer = new ComputeBuffer(m_numberOfInstanceTypes, sizeof(float), ComputeBufferType.Default);
        lod1RangePerTypeBuffer.SetData(lod1RangePerType);
        occlusionCS.SetBuffer(m_occlusionKernelID, _Lod1RangePerType, lod1RangePerTypeBuffer);

        distancePerTypeBuffer = new ComputeBuffer(m_numberOfInstanceTypes, sizeof(float), ComputeBufferType.Default);
        distancePerTypeBuffer.SetData(distancePerType);
        occlusionCS.SetBuffer(m_occlusionKernelID, _DistancePerType, distancePerTypeBuffer);

        frustCulPerType = new ComputeBuffer(m_numberOfInstanceTypes, sizeof(uint), ComputeBufferType.Default);
        frustCulPerType.SetData(FrustumCullingPerType);
        occlusionCS.SetBuffer(m_occlusionKernelID, _FrustumCullingPerType, frustCulPerType);

        occCulPerType = new ComputeBuffer(m_numberOfInstanceTypes, sizeof(uint), ComputeBufferType.Default);
        occCulPerType.SetData(OcclusionCullingPerType);
        occlusionCS.SetBuffer(m_occlusionKernelID, _OcclusionCullingPerType, occCulPerType);

        occlusionCS.SetInt(_ShouldDetailCull, enableDetailCulling ? 1 : 0);
        occlusionCS.SetInt(_ShouldLOD, enableLOD ? 1 : 0);
        //occlusionCS.SetInt(_ShouldOnlyUseLOD02Shadows, enableOnlyLOD02Shadows ? 1 : 0);
        //occlusionCS.SetFloat(_ShadowDistance, QualitySettings.shadowDistance);
        occlusionCS.SetFloat(_DetailCullingScreenPercentage, detailCullingPercentage);
        occlusionCS.SetBuffer(m_occlusionKernelID, _InstanceDataBuffer, m_instanceDataBuffer);
        occlusionCS.SetBuffer(m_occlusionKernelID, _ArgsBuffer, m_instancesArgsBuffer);
        occlusionCS.SetBuffer(m_occlusionKernelID, _ShadowArgsBuffer, m_shadowArgsBuffer);
        occlusionCS.SetBuffer(m_occlusionKernelID, _IsVisibleBuffer, m_instancesIsVisibleBuffer);
        occlusionCS.SetBuffer(m_occlusionKernelID, _ShadowIsVisibleBuffer, m_shadowsIsVisibleBuffer);
        occlusionCS.SetBuffer(m_occlusionKernelID, _SortingData, m_instancesSortingData);

        occlusionCS.SetVector(_HiZTextureSize, new Vector2(HiZDepthTexture.width, HiZDepthTexture.height));
        occlusionCS.SetTexture(m_occlusionKernelID, _HiZMap, HiZDepthTexture);
//        if (Application.isPlaying)
//        {
//            if (enableOcclusionCulling && HiZDepthTexture != null)
//            {
//                occlusionCS.SetVector(_HiZTextureSize, new Vector2(HiZDepthTexture.width, HiZDepthTexture.height));
//                occlusionCS.SetTexture(m_occlusionKernelID, _HiZMap, HiZDepthTexture);
//            }
//            else if (blankDepthTexture)
//            {
//                occlusionCS.SetVector(_HiZTextureSize, Vector2.one);
//                occlusionCS.SetTexture(m_occlusionKernelID, _HiZMap, blankDepthTexture);
//            }
//        }
//        else
//        {
//#if UNITY_EDITOR

//            //occlusionCS.SetInt(_ShouldFrustumCull, 0);
//            if (blankDepthTexture != null)
//            {
//                occlusionCS.SetVector(_HiZTextureSize, new Vector2(blankDepthTexture.width, blankDepthTexture.height));
//                occlusionCS.SetTexture(m_occlusionKernelID, _HiZMap, blankDepthTexture);
//            }
//#endif
//        }
        scanGroupSumsCS.SetInt(_NumOfGroups, Mathf.NextPowerOfTwo(numScannedGroups));

        copyInstanceDataCS.SetInt(_NumOfDrawcalls, m_numberOfInstanceTypes * NUMBER_OF_DRAW_CALLS);
        copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstanceDataBuffer, m_instanceDataBuffer);
        copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesDrawMatrixRows01, m_instancesMatrixRows01);
        copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesDrawMatrixRows23, m_instancesMatrixRows23);
        copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _InstancesDrawMatrixRows45, m_instancesMatrixRows45);
        copyInstanceDataCS.SetBuffer(m_copyInstanceDataKernelID, _SortingData, m_instancesSortingData);

        CreateCommandBuffers();

        if (InstancerCamera)
        {
            if (runCompute)
            {
                //Calculate an extra time to avoid incorrect sorting when switching zones
                //theres gotta be a better way to fix the issue than this shit. ill look into it eventually
                for (int i = 0; i < 30; i++)
                {
                    CalculateVisibleInstances(InstancerCamera);
                }
            }
        }
        return true;
    }

    #region INSTANCE SORTING
    private Kernels m_kernels;
    private void CreateCommandBuffers()
    {
        m_kernels = new Kernels(sortingCS);
        CreateSortingCommandBuffer();
    }
    private class Kernels
    {
        public int Init { get; private set; }
        public int Sort { get; private set; }
        public int PadBuffer { get; private set; }
        public int OverwriteAndTruncate { get; private set; }
        public int SetMin { get; private set; }
        public int SetMax { get; private set; }
        public int GetPaddingIndex { get; private set; }
        public int CopyBuffer { get; private set; }

        public Kernels(ComputeShader cs)
        {
            Init = cs.FindKernel("InitKeys");
            Sort = cs.FindKernel("BitonicSort");
            PadBuffer = cs.FindKernel("PadBuffer");
            OverwriteAndTruncate = cs.FindKernel("OverwriteAndTruncate");
            SetMin = cs.FindKernel("SetMin");
            SetMax = cs.FindKernel("SetMax");
            GetPaddingIndex = cs.FindKernel("GetPaddingIndex");
            CopyBuffer = cs.FindKernel("CopyBuffer");
        }
    }
    private void CreateSortingCommandBuffer()
    {
        Sort(m_instancesSortingData);
    }
    public void Disposetwo()
    {
        m_keysBuffer?.Dispose();
        m_tempBuffer?.Dispose();
        m_valuesBuffer?.Dispose();
        m_paddingBuffer?.Dispose();
    }
    private static class Properties
    {
        public static int Block { get; private set; } = Shader.PropertyToID("_Block");
        public static int Dimension { get; private set; } = Shader.PropertyToID("_Dimension");
        public static int Count { get; private set; } = Shader.PropertyToID("_Count");
        public static int NextPowerOfTwo { get; private set; } = Shader.PropertyToID("_NextPowerOfTwo");

        public static int KeysBuffer { get; private set; } = Shader.PropertyToID("_Keys");
        public static int ValuesBuffer { get; private set; } = Shader.PropertyToID("_Values");
        public static int TempBuffer { get; private set; } = Shader.PropertyToID("_Temp");
        public static int PaddingBuffer { get; private set; } = Shader.PropertyToID("_PaddingBuffer");

        public static int ExternalValuesBuffer { get; private set; } = Shader.PropertyToID("_ExternalValues");
        public static int ExternalKeysBuffer { get; private set; } = Shader.PropertyToID("_ExternalKeys");

        public static int FromBuffer { get; private set; } = Shader.PropertyToID("_Input");
        public static int ToBuffer { get; private set; } = Shader.PropertyToID("_Data");
    }
    private void Init(out int x, out int y, out int z)
    {
        Disposetwo();

        // initializing local buffers
        m_paddingBuffer = new ComputeBuffer(2, sizeof(int));
        m_keysBuffer = new ComputeBuffer(m_paddedCount, sizeof(uint));
        m_tempBuffer = new ComputeBuffer(m_paddedCount, Marshal.SizeOf(typeof(SortingData)));
        m_valuesBuffer = new ComputeBuffer(m_paddedCount, Marshal.SizeOf(typeof(SortingData)));

        m_tempBuffer.SetCounterValue(0);
        m_valuesBuffer.SetCounterValue(0);

        m_sortingCommandBuffer.SetComputeIntParam(sortingCS, Properties.Count, m_originalCount);
        m_sortingCommandBuffer.SetComputeIntParam(sortingCS, Properties.NextPowerOfTwo, m_paddedCount);

        //int minMaxKernel = m_isReverseSort ? m_kernels.SetMin : m_kernels.SetMax;
        int minMaxKernel = m_kernels.SetMax;

        //m_paddingInput[0] = m_isReverseSort ? int.MaxValue : int.MinValue;
        m_paddingInput[0] = int.MinValue;
        m_paddingInput[1] = 0;

        m_paddingBuffer.SetData(m_paddingInput);

        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, minMaxKernel, Properties.ExternalKeysBuffer, m_externalKeysBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, minMaxKernel, Properties.PaddingBuffer, m_paddingBuffer);

        // first determine either the minimum value or maximum value of the given data, depending on whether it's a reverse sort or not, 
        // to serve as the padding value for non-power-of-two sized inputs
        m_sortingCommandBuffer.DispatchCompute(sortingCS, minMaxKernel, Mathf.CeilToInt((float)m_originalCount / Util.GROUP_SIZE), 1, 1);

        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.GetPaddingIndex, Properties.ExternalKeysBuffer, m_externalKeysBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.GetPaddingIndex, Properties.PaddingBuffer, m_paddingBuffer);
        m_sortingCommandBuffer.DispatchCompute(sortingCS, m_kernels.GetPaddingIndex, Mathf.CeilToInt((float)m_originalCount / Util.GROUP_SIZE), 1, 1);

        // setting up the second kernel, the padding kernel. because the sort only works on power of two sized buffers,
        // this will pad the buffer with duplicates of the greatest (or least, if reverse sort) integer to be truncated later
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.PadBuffer, Properties.ExternalKeysBuffer, m_externalKeysBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.PadBuffer, Properties.ExternalValuesBuffer, m_externalValuesBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.PadBuffer, Properties.ValuesBuffer, m_valuesBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.PadBuffer, Properties.PaddingBuffer, m_paddingBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.PadBuffer, Properties.TempBuffer, m_tempBuffer);

        m_sortingCommandBuffer.DispatchCompute(sortingCS, m_kernels.PadBuffer, Mathf.CeilToInt((float)m_paddedCount / Util.GROUP_SIZE), 1, 1);

        // initialize the keys buffer for use with the sort algorithm proper
        Util.CalculateWorkSize(m_paddedCount, out x, out y, out z);

        m_sortingCommandBuffer.SetComputeIntParam(sortingCS, Properties.Count, m_paddedCount);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.Init, Properties.KeysBuffer, m_keysBuffer);
        m_sortingCommandBuffer.DispatchCompute(sortingCS, m_kernels.Init, x, y, z);
    }
    public void Sort(ComputeBuffer values, int length = -1)
    {
        m_sortingCommandBuffer = new CommandBuffer { name = "AsyncGPUSorting" };
        m_sortingCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        m_instancesSortingDataTemp = new ComputeBuffer(values.count, Marshal.SizeOf(typeof(SortingData)));
        //ComputeBuffer copyBuff = new ComputeBuffer(values.count, Marshal.SizeOf(typeof(SortingData)));

        m_sortingCommandBuffer.SetComputeIntParam(sortingCS, Properties.Count, values.count);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.CopyBuffer, Properties.FromBuffer, values);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.CopyBuffer, Properties.ToBuffer, m_instancesSortingDataTemp);

        m_sortingCommandBuffer.DispatchCompute(sortingCS, m_kernels.CopyBuffer, Mathf.CeilToInt((float)values.count / Util.GROUP_SIZE), 1, 1);

        Sort(values, m_instancesSortingDataTemp, length);
    }
    private ComputeBuffer m_keysBuffer;
    private ComputeBuffer m_tempBuffer;
    private ComputeBuffer m_valuesBuffer;
    private ComputeBuffer m_paddingBuffer;

    private ComputeBuffer m_externalValuesBuffer;
    private ComputeBuffer m_externalKeysBuffer;

    private bool m_mustTruncateValueBuffer = false;
    //private bool m_isReverseSort = false;
    private int m_originalCount = 0;
    private int m_paddedCount = 0;

    private readonly int[] m_paddingInput = new int[] { 0, 0 };


    public void Sort(ComputeBuffer values, ComputeBuffer keys, int length = -1)
    {
        Debug.Assert(values.count == keys.count, "Value and key buffers must be of the same size.");

        m_originalCount = length < 0 ? values.count : length;
        m_paddedCount = Mathf.NextPowerOfTwo(m_originalCount);
        m_mustTruncateValueBuffer = !Mathf.IsPowerOfTwo(m_originalCount);
        m_externalValuesBuffer = values;
        m_externalKeysBuffer = keys;

        // initialize the buffers to be used by the sorting algorithm
        Init(out int x, out int y, out int z);

        // run the bitonic merge sort algorithm
        for (int dim = 2; dim <= m_paddedCount; dim <<= 1)
        {
            m_sortingCommandBuffer.SetComputeIntParam(sortingCS, Properties.Dimension, dim);

            for (int block = dim >> 1; block > 0; block >>= 1)
            {
                m_sortingCommandBuffer.SetComputeIntParam(sortingCS, Properties.Block, block);
                m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.Sort, Properties.KeysBuffer, m_keysBuffer);
                m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.Sort, Properties.ValuesBuffer, m_valuesBuffer);

                m_sortingCommandBuffer.DispatchCompute(sortingCS, m_kernels.Sort, x, y, z);
            }
        }

        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.OverwriteAndTruncate, Properties.KeysBuffer, m_keysBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.OverwriteAndTruncate, Properties.ExternalValuesBuffer, m_externalValuesBuffer);
        m_sortingCommandBuffer.SetComputeBufferParam(sortingCS, m_kernels.OverwriteAndTruncate, Properties.TempBuffer, m_tempBuffer);

        m_sortingCommandBuffer.DispatchCompute(sortingCS, m_kernels.OverwriteAndTruncate, Mathf.CeilToInt((float)m_originalCount / Util.GROUP_SIZE), 1, 1);
    }
    private static class Util
    {
        public const int GROUP_SIZE = 256;
        public const int MAX_DIM_GROUPS = 1024;
        public const int MAX_DIM_THREADS = (GROUP_SIZE * MAX_DIM_GROUPS);

        public static void CalculateWorkSize(int length, out int x, out int y, out int z)
        {
            if (length <= MAX_DIM_THREADS)
            {
                x = (length - 1) / GROUP_SIZE + 1;
                y = z = 1;
            }
            else
            {
                x = MAX_DIM_GROUPS;
                y = (length - 1) / MAX_DIM_THREADS + 1;
                z = 1;
            }
        }
    }
    #endregion

    public void ReleaseBuffers()
    {
        ReleaseCommandBuffer(ref m_sortingCommandBuffer);
        //ReleaseCommandBuffer(ref visibleInstancesCB);

        ReleaseComputeBuffer(ref m_instancesIsVisibleBuffer);
        ReleaseComputeBuffer(ref m_instancesGroupSumArrayBuffer);
        ReleaseComputeBuffer(ref m_instancesScannedGroupSumBuffer);
        ReleaseComputeBuffer(ref m_instancesScannedPredicates);
        ReleaseComputeBuffer(ref m_instanceDataBuffer);
        ReleaseComputeBuffer(ref m_instancesSortingData);
        ReleaseComputeBuffer(ref m_instancesSortingDataTemp);
        ReleaseComputeBuffer(ref m_instancesMatrixRows01);
        ReleaseComputeBuffer(ref m_instancesMatrixRows23);
        ReleaseComputeBuffer(ref m_instancesMatrixRows45);
        ReleaseComputeBuffer(ref m_instancesCulledMatrixRows01);
        ReleaseComputeBuffer(ref m_instancesCulledMatrixRows23);
        ReleaseComputeBuffer(ref m_instancesCulledMatrixRows45);
        ReleaseComputeBuffer(ref m_instancesArgsBuffer);

        ReleaseComputeBuffer(ref m_shadowArgsBuffer);
        ReleaseComputeBuffer(ref m_shadowsIsVisibleBuffer);
        ReleaseComputeBuffer(ref m_shadowGroupSumArrayBuffer);
        ReleaseComputeBuffer(ref m_shadowsScannedGroupSumBuffer);
        ReleaseComputeBuffer(ref m_shadowScannedInstancePredicates);
        ReleaseComputeBuffer(ref m_shadowCulledMatrixRows01);
        ReleaseComputeBuffer(ref m_shadowCulledMatrixRows23);
        ReleaseComputeBuffer(ref m_shadowCulledMatrixRows45);

        ReleaseComputeBuffer(ref lod0RangePerTypeBuffer);
        ReleaseComputeBuffer(ref lod1RangePerTypeBuffer);
        ReleaseComputeBuffer(ref distancePerTypeBuffer);
        ReleaseComputeBuffer(ref occCulPerType);
        ReleaseComputeBuffer(ref frustCulPerType);
        ReleaseComputeBuffer(ref m_paddingBuffer); 
        ReleaseComputeBuffer(ref m_keysBuffer); 
        ReleaseComputeBuffer(ref m_tempBuffer); 
        ReleaseComputeBuffer(ref m_valuesBuffer);
    }
    private static void ReleaseComputeBuffer(ref ComputeBuffer _buffer)
    {
        if (_buffer == null)
        {
            return;
        }

        _buffer.Release();
        _buffer = null;
    }

    private static void ReleaseCommandBuffer(ref CommandBuffer _buffer)
    {
        if (_buffer == null)
        {
            return;
        }

        _buffer.Release();
        _buffer = null;
    }
    private Bounds CalculateBounds(GameObject obj)
    {
        //GameObject obj = Instantiate(_prefab);
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.Euler(Vector3.zero);
        obj.transform.localScale = Vector3.one;
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds();
        if (rends.Length > 0)
        {
            b = new Bounds(rends[0].bounds.center, rends[0].bounds.size);
            for (int r = 1; r < rends.Length; r++)
            {
                b.Encapsulate(rends[r].bounds);
            }
        }
        //b.center = Vector3.zero;
        //Debug.Log(b.size);
        //Debug.Log(b.center);

        return b;
    }
    IEnumerator Destroy(GameObject obj)
    {
        yield return new WaitForSeconds(.1f);
        DestroyImmediate(obj);
    }
#endregion
    
#if UNITY_EDITOR
    public void CheckIfOnlyInstance()
    {
        if (gameObject.scene.name != "Null")
        {
            return;
        }
        if (instance != this)
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogError("you should only have 1 instance of the instancer");
                DestroyImmediate(this);
            }
        }
    }
#endif
}
[System.Serializable]
public class IndirectRenderingMesh
{
    public Mesh mesh;
    public Material[] material;
    public int lod0Material;
    public int lod1Material;
    public int lod2Material;
    public MaterialPropertyBlock lod00MatPropBlock;
    public MaterialPropertyBlock lod01MatPropBlock;
    public MaterialPropertyBlock lod02MatPropBlock;
    public MaterialPropertyBlock shadowLod00MatPropBlock;
    public MaterialPropertyBlock shadowLod01MatPropBlock;
    public MaterialPropertyBlock shadowLod02MatPropBlock;
    public uint numOfVerticesLod00;
    public uint numOfVerticesLod01;
    public uint numOfVerticesLod02;
    public uint numOfIndicesLod00 = 0;
    public uint numOfIndicesLod01 = 0;
    public uint numOfIndicesLod02 = 0;
}

#if UNITY_EDITOR
[CustomEditor(typeof(MilkInstancer))]
class InstancingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MilkInstancer comp = (MilkInstancer)target;
        //Debug.Log(comp.gameObject.scene);
        //Debug.Log(comp.gameObject.scene.name);
        if (comp.gameObject.scene.name != "Null")
        {
            //comp.checkIfOnlyInstance();
        }
    }
}
#endif