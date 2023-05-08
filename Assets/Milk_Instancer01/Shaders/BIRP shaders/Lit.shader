// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Milk_Instancer/Lit"
{
	Properties
	{
		_MainTex("Base Color Map", 2D) = "white" {}
		[Normal]_NormalMap("Normal Map", 2D) = "bump" {}
		_NormalScale("Normal Map Strength", Float) = 1
		_MaskMap("Mask Map", 2D) = "gray" {}
		_BaseColor("Base Color", Color) = (1,1,1,1)
		_MetalicMin("Metalic Min", Range( 0 , 1)) = 0
		_MetalicMax("Metalic Max", Range( 0 , 1)) = 1
		_SmoothnessRemapMin("Smoothness Min", Range( 0 , 1)) = 0
		_SmoothnessRemapMax("Smoothness Max", Range( 0 , 1)) = 1
		_AORemapMin("AO Min", Range( 0 , 1)) = 0
		_AORemapMax("AO Max", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#include "Assets/Milk_Instancer01/Shaders/logic/setup.hlsl"
		#pragma instancing_options procedural:setup
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _NormalMap;
		uniform float _NormalScale;
		uniform float4 _BaseColor;
		uniform sampler2D _MainTex;
		uniform sampler2D _MaskMap;
		uniform float _MetalicMin;
		uniform float _MetalicMax;
		uniform float _SmoothnessRemapMin;
		uniform float _SmoothnessRemapMax;
		uniform float _AORemapMin;
		uniform float _AORemapMax;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = UnpackScaleNormal( tex2D( _NormalMap, i.uv_texcoord ), _NormalScale );
			float4 tex2DNode11_g2 = tex2D( _MainTex, i.uv_texcoord );
			o.Albedo = ( _BaseColor * tex2DNode11_g2 ).rgb;
			float4 tex2DNode12_g2 = tex2D( _MaskMap, i.uv_texcoord );
			o.Metallic = (_MetalicMin + (tex2DNode12_g2.r - 0.0) * (_MetalicMax - _MetalicMin) / (1.0 - 0.0));
			o.Smoothness = (_SmoothnessRemapMin + (tex2DNode12_g2.a - 0.0) * (_SmoothnessRemapMax - _SmoothnessRemapMin) / (1.0 - 0.0));
			o.Occlusion = (_AORemapMin + (tex2DNode12_g2.g - 0.0) * (_AORemapMax - _AORemapMin) / (1.0 - 0.0));
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;12;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Milk_Instancer/Lit;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;2;Include;;True;b0e32ed493dd7164582dd2ba66536a16;Custom;False;0;0;;Pragma;instancing_options procedural:setup;False;;Custom;False;0;0;;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.FunctionNode;13;-287.8757,58.93889;Inherit;False;StandardShading;0;;2;5e7e2ae5299f0b54ea8330c3cad1f8cb;0;0;6;COLOR;24;FLOAT3;23;FLOAT;20;FLOAT;19;FLOAT;18;FLOAT;22
WireConnection;12;0;13;24
WireConnection;12;1;13;23
WireConnection;12;3;13;20
WireConnection;12;4;13;19
WireConnection;12;5;13;18
ASEEND*/
//CHKSM=049E1BB9100D2C9DAFB814774134C7B68CA8C1A3