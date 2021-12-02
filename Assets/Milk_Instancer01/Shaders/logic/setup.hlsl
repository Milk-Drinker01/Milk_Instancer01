
#ifndef _INDIRECTSETUP_
#define _INDIRECTSETUP_

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

#include "ShaderInclude_IndirectStructs.cginc"
uniform uint _ArgsOffset;
StructuredBuffer<uint> _ArgsBuffer;
StructuredBuffer<Indirect2x2Matrix> _InstancesDrawMatrixRows01;
StructuredBuffer<Indirect2x2Matrix> _InstancesDrawMatrixRows23;
StructuredBuffer<Indirect2x2Matrix> _InstancesDrawMatrixRows45;

void setup()
{
#if defined(SHADER_API_METAL)
    uint index = unity_InstanceID;
#else
    uint index = unity_InstanceID + _ArgsBuffer[_ArgsOffset];
#endif
    Indirect2x2Matrix rows01 = _InstancesDrawMatrixRows01[index];
    Indirect2x2Matrix rows23 = _InstancesDrawMatrixRows23[index];
    Indirect2x2Matrix rows45 = _InstancesDrawMatrixRows45[index];

#define unity_ObjectToWorld unity_ObjectToWorld
#define unity_WorldToObject unity_WorldToObject

    unity_ObjectToWorld = float4x4(rows01.row0, rows01.row1, rows23.row0, float4(0, 0, 0, 1));
    unity_WorldToObject = float4x4(rows23.row1, rows45.row0, rows45.row1, float4(0, 0, 0, 1));
}
#endif

void Instancing_float(float3 Position, out float3 Out) {
    Out = 0;
#ifndef SHADERGRAPH_PREVIEW
    Out = Position;
#endif
}

#endif