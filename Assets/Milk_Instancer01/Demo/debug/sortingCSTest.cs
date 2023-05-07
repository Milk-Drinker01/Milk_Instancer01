using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace MilkInstancer.tests
{
    public class sortingCSTest : MonoBehaviour
    {
        public ComputeShader sortingCS;
        public uint[] sortingKeys;
        public float[] sortingValues;

        SortingData[] dataToSort;
        private CommandBuffer m_sortingCommandBuffer;
        private ComputeBuffer m_instancesSortingData;
        private ComputeBuffer m_instancesSortingDataTemp;

        [ContextMenu("setup shit")]
        public void setupTest()
        {
            dataToSort = new SortingData[sortingKeys.Length];
            for (int i = 0; i < sortingKeys.Length; i++)
            {
                dataToSort[i] = new SortingData { distanceToCam = sortingValues[i], drawCallInstanceIndex = sortingKeys[i] };
            }
            int computeSortingDataSize = Marshal.SizeOf(typeof(SortingData));
            m_instancesSortingData = new ComputeBuffer(sortingKeys.Length, computeSortingDataSize, ComputeBufferType.Default);
            m_instancesSortingData.SetData(dataToSort);
            CreateCommandBuffers();

            //ComputeBuffer gay = new ComputeBuffer(1, 4);
            //uint[] bruh = new uint[1];
            //gay.GetData(bruh);
            //Debug.Log(bruh[0]);
        }
        [ContextMenu("sort shit")]
        public void sortTest()
        {
            //uint powTwo = sortingCS.
            //m_paddingInput[0] = int.MinValue;
            //m_paddingInput[1] = 0;
            //m_paddingBuffer.SetData(m_paddingInput);
            //Graphics.ExecuteCommandBufferAsync(m_sortingCommandBuffer, ComputeQueueType.Background);
            Graphics.ExecuteCommandBuffer(m_sortingCommandBuffer);
            SortingData[] data = new SortingData[m_instancesSortingData.count];
            m_instancesSortingData.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Log(data[i].distanceToCam);
            }
        }

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
            //m_sortingCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

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
            //m_originalCount = nextPowerOfTwo;
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
    }

}