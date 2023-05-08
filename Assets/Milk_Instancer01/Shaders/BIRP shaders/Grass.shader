// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Milk_Instancer/Grass"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("Base Color Map", 2D) = "white" {}
		[Normal]_NormalMap("Normal Map", 2D) = "bump" {}
		_MaskMap("Mask Map", 2D) = "gray" {}
		_BaseColor("Base Color", Color) = (0,0,0,0)
		SmoothnessRemapMin("Smoothness Min", Range( 0 , 1)) = 0
		SmoothnessRemapMax("Smoothness Max", Range( 0 , 1)) = 1
		_AORemapMin("AO Min", Range( 0 , 1)) = 0
		_AORemapMax("AO Max", Range( 0 , 1)) = 1
		_NormalScale("Normal Map Strength", Range( 0 , 10)) = 1
		_Vector1("Vector 1", Vector) = (0,1,0,0)
		_maxHeight("maxHeight", Float) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Off
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#include "Assets/Milk_Instancer01/Shaders/logic/setup.hlsl"
		#pragma instancing_options procedural:setup
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		uniform float4 _wind_angle_strength;
		uniform float _maxHeight;
		uniform float4 _windNoiseUVs;
		uniform float3 _Vector1;
		uniform sampler2D _NormalMap;
		uniform float _NormalScale;
		uniform float4 _BaseColor;
		uniform sampler2D _MainTex;
		uniform sampler2D _MaskMap;
		uniform float SmoothnessRemapMin;
		uniform float SmoothnessRemapMax;
		uniform float _AORemapMin;
		uniform float _AORemapMax;
		uniform float _Cutoff = 0.5;


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


		inline float noise_randomValue (float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453); }

		inline float noise_interpolate (float a, float b, float t) { return (1.0-t)*a + (t*b); }

		inline float valueNoise (float2 uv)
		{
			float2 i = floor(uv);
			float2 f = frac( uv );
			f = f* f * (3.0 - 2.0 * f);
			uv = abs( frac(uv) - 0.5);
			float2 c0 = i + float2( 0.0, 0.0 );
			float2 c1 = i + float2( 1.0, 0.0 );
			float2 c2 = i + float2( 0.0, 1.0 );
			float2 c3 = i + float2( 1.0, 1.0 );
			float r0 = noise_randomValue( c0 );
			float r1 = noise_randomValue( c1 );
			float r2 = noise_randomValue( c2 );
			float r3 = noise_randomValue( c3 );
			float bottomOfGrid = noise_interpolate( r0, r1, f.x );
			float topOfGrid = noise_interpolate( r2, r3, f.x );
			float t = noise_interpolate( bottomOfGrid, topOfGrid, f.y );
			return t;
		}


		float SimpleNoise(float2 UV)
		{
			float t = 0.0;
			float freq = pow( 2.0, float( 0 ) );
			float amp = pow( 0.5, float( 3 - 0 ) );
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(1));
			amp = pow(0.5, float(3-1));
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(2));
			amp = pow(0.5, float(3-2));
			t += valueNoise( UV/freq )*amp;
			return t;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 temp_output_47_0_g32 = ase_vertex3Pos;
			float temp_output_43_0_g32 = _wind_angle_strength.w;
			float3 appendResult25_g32 = (float3(( cos( temp_output_43_0_g32 ) * -1.0 ) , 0.0 , sin( temp_output_43_0_g32 )));
			float3 worldToObjDir41_g32 = normalize( mul( unity_WorldToObject, float4( appendResult25_g32, 0 ) ).xyz );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 worldToObj137 = mul( unity_WorldToObject, float4( ase_worldPos, 1 ) ).xyz;
			float temp_output_61_0_g32 = ( worldToObj137.y / _maxHeight );
			float clampResult3_g32 = clamp( temp_output_61_0_g32 , 0.0 , 1.0 );
			float3 appendResult132 = (float3(worldToObj137.x , 0.0 , worldToObj137.z));
			float3 rotatedValue12_g32 = RotateAroundAxis( appendResult132, temp_output_47_0_g32, normalize( worldToObjDir41_g32 ), radians( ( ( ( pow( clampResult3_g32 , 1.5 ) * 0.85 ) * -1.0 ) * 90.0 ) ) );
			float3 break67_g32 = rotatedValue12_g32;
			float3 appendResult65_g32 = (float3(break67_g32.x , ( break67_g32.y * 0.95 ) , break67_g32.z));
			float2 appendResult83 = (float2(_windNoiseUVs.x , _windNoiseUVs.y));
			float2 noiseUV84 = appendResult83;
			float2 appendResult74 = (float2(ase_worldPos.x , ase_worldPos.z));
			float2 pos75 = appendResult74;
			float simpleNoise203 = SimpleNoise( ( noiseUV84 + pos75 )*_wind_angle_strength.x );
			float temp_output_186_0 = pow( simpleNoise203 , 1.5 );
			float2 appendResult85 = (float2(_windNoiseUVs.z , _windNoiseUVs.w));
			float2 subGustUV86 = appendResult85;
			float simpleNoise204 = SimpleNoise( ( pos75 + subGustUV86 )*_wind_angle_strength.y );
			float temp_output_207_0 = ( ( temp_output_186_0 + temp_output_186_0 + simpleNoise204 ) / 3.0 );
			float3 lerpResult28_g32 = lerp( temp_output_47_0_g32 , appendResult65_g32 , ( temp_output_207_0 * _wind_angle_strength.z ));
			v.vertex.xyz = lerpResult28_g32;
			v.vertex.w = 1;
			v.normal = _Vector1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 tex2DNode17_g31 = UnpackScaleNormal( tex2D( _NormalMap, i.uv_texcoord ), _NormalScale );
			o.Normal = tex2DNode17_g31;
			float4 tex2DNode14_g31 = tex2D( _MainTex, i.uv_texcoord );
			float4 temp_output_160_0 = ( _BaseColor * tex2DNode14_g31 );
			o.Albedo = temp_output_160_0.rgb;
			float4 tex2DNode16_g31 = tex2D( _MaskMap, i.uv_texcoord );
			o.Smoothness = (SmoothnessRemapMin + (tex2DNode16_g31.a - 0.0) * (SmoothnessRemapMax - SmoothnessRemapMin) / (1.0 - 0.0));
			o.Occlusion = (_AORemapMin + (tex2DNode16_g31.g - 0.0) * (_AORemapMax - _AORemapMin) / (1.0 - 0.0));
			o.Alpha = 1;
			clip( ( _BaseColor.a * tex2DNode14_g31.a ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.WorldPosInputsNode;73;-2089.296,463.4031;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector4Node;82;-2154.585,612.6761;Inherit;False;Global;_windNoiseUVs;_windNoiseUVs;1;0;Create;True;0;0;0;False;0;False;0,0,0,0;-173.3333,55.98489,-248.8028,80.36134;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;74;-1893.663,490.9008;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;83;-1912.283,620.778;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-1785.74,612.6989;Inherit;False;noiseUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;75;-1762.034,486.2271;Inherit;False;pos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;85;-1923.266,707.8327;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;86;-1786.677,708.7976;Inherit;False;subGustUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;87;-1529.839,128.405;Inherit;False;84;noiseUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;76;-1530.686,205.0228;Inherit;False;75;pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;210;-1540.964,283.2603;Inherit;False;86;subGustUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;78;-1338.23,160.5286;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;71;-1409.33,374.3966;Inherit;False;Global;_wind_angle_strength;_wind_angle_strength;11;0;Create;True;0;0;0;False;0;False;0,0,0,0;6.99,10,0.49975,1.88321;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;211;-1341.964,255.2603;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;203;-1140.682,184.5123;Inherit;False;Simple;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;204;-1139.038,282.3516;Inherit;False;Simple;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;186;-937.8716,205.1448;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;139;-1095.558,616.9376;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;206;-801.1932,264.7231;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;125;-701.2968,830.8428;Inherit;False;Property;_maxHeight;maxHeight;16;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TransformPositionNode;137;-915.9899,608.6237;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleDivideOpNode;207;-680.1722,238.0039;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;132;-676.0297,590.5869;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode;92;-539.043,199.0995;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;135;-503.3214,721.5941;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-471.5372,372.8941;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;160;-289.8575,9.749381;Inherit;False;FoliageShading;1;;31;327aa30652bb0fd488ed810b7ca6e7b2;0;0;8;COLOR;0;FLOAT3;19;FLOAT3;26;FLOAT;20;FLOAT;21;FLOAT;18;FLOAT;22;FLOAT;23
Node;AmplifyShaderEditor.DynamicAppendNode;185;-586.9713,-31.95514;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StickyNoteNode;129;-1179.963,1333.697;Inherit;False;150;100;New Note;;1,1,1,1;0-1 based off height$;0;0
Node;AmplifyShaderEditor.TransformPositionNode;123;-1664.519,1276.922;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PosVertexDataNode;110;153.9583,-133.3874;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;202;-44.52466,-278.0845;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;3,3,3;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;127;-1195.372,1232.153;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwitchNode;181;-275.9713,-92.95514;Inherit;False;0;2;8;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TransformPositionNode;128;-1237.04,1070.182;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ObjectScaleNode;201;-455.8733,-320.4292;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;126;-1368.08,1101.146;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StickyNoteNode;130;-1653.899,1438.135;Inherit;False;150;100;New Note;;1,1,1,1;object position$;0;0
Node;AmplifyShaderEditor.StickyNoteNode;133;-506.7122,827.039;Inherit;False;150;100;New Note;;1,1,1,1;0-1 based off height$;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;124;-1342.975,1231.877;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;79;-918.3047,13.57542;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;208;136.4095,526.1786;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;111;407.9583,-165.3874;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;121;-1824.398,1277.358;Inherit;False;Constant;_zero;zero;12;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;136;-1357.825,1361.617;Inherit;False;Property;_Float0;Float 0;17;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;134;-688.9244,736.3181;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;122;-1636.608,1130.496;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;138;-875.3483,761.799;Inherit;False;Constant;_Vector0;Vector 0;12;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;183;-297.7214,348.2829;Inherit;False;bendCalculation;-1;;32;5788042df6d75de418f645f58dc6dc15;0;5;61;FLOAT;0;False;47;FLOAT3;0,0,0;False;62;FLOAT3;0,0,0;False;44;FLOAT;1;False;43;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldNormalVector;209;44.49902,729.6948;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;164;-68.70406,495.8503;Inherit;False;Property;_Vector1;Vector 1;15;0;Create;True;0;0;0;False;0;False;0,1,0;0,1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;212;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Milk_Instancer/Grass;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Absolute;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;2;Include;;True;b0e32ed493dd7164582dd2ba66536a16;Custom;False;0;0;;Pragma;instancing_options procedural:setup;False;;Custom;False;0;0;;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;74;0;73;1
WireConnection;74;1;73;3
WireConnection;83;0;82;1
WireConnection;83;1;82;2
WireConnection;84;0;83;0
WireConnection;75;0;74;0
WireConnection;85;0;82;3
WireConnection;85;1;82;4
WireConnection;86;0;85;0
WireConnection;78;0;87;0
WireConnection;78;1;76;0
WireConnection;211;0;76;0
WireConnection;211;1;210;0
WireConnection;203;0;78;0
WireConnection;203;1;71;1
WireConnection;204;0;211;0
WireConnection;204;1;71;2
WireConnection;186;0;203;0
WireConnection;206;0;186;0
WireConnection;206;1;186;0
WireConnection;206;2;204;0
WireConnection;137;0;139;0
WireConnection;207;0;206;0
WireConnection;132;0;137;1
WireConnection;132;2;137;3
WireConnection;135;0;137;2
WireConnection;135;1;125;0
WireConnection;80;0;207;0
WireConnection;80;1;71;3
WireConnection;185;0;207;0
WireConnection;185;1;207;0
WireConnection;185;2;207;0
WireConnection;123;0;121;0
WireConnection;202;0;201;0
WireConnection;127;0;124;0
WireConnection;127;1;136;0
WireConnection;181;0;160;0
WireConnection;181;1;185;0
WireConnection;128;0;126;0
WireConnection;126;0;122;1
WireConnection;126;1;123;2
WireConnection;126;2;122;3
WireConnection;124;0;122;2
WireConnection;124;1;123;2
WireConnection;79;0;203;0
WireConnection;111;0;110;1
WireConnection;111;2;110;3
WireConnection;134;0;137;2
WireConnection;134;1;138;2
WireConnection;183;61;135;0
WireConnection;183;47;92;0
WireConnection;183;62;132;0
WireConnection;183;44;80;0
WireConnection;183;43;71;4
WireConnection;212;0;160;0
WireConnection;212;1;160;19
WireConnection;212;4;160;20
WireConnection;212;5;160;21
WireConnection;212;10;160;18
WireConnection;212;11;183;0
WireConnection;212;12;164;0
ASEEND*/
//CHKSM=5037AF81DF85CB41E026A6B5506ED371A31D4F24