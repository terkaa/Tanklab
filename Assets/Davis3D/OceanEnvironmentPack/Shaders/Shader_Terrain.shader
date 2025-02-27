// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Davis3D/OceanEnviroment/Shader_Terrain"
{
	Properties
	{
		_Sand_A_Tiling("Sand_A_Tiling", Float) = 1
		_SandDiffuse("Sand Diffuse", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_NormalDetail("NormalDetail", 2D) = "bump" {}
		_NormalDetailIntensity("Normal Detail Intensity", Float) = 1
		_Specular("Specular", Float) = 1
		_Smoothness("Smoothness", Float) = 1
		_Brightness("Brightness", Float) = 1
		_NormalDetailScale("Normal Detail Scale", Float) = 6
		_DiffuseMultiply("Diffuse Multiply", Color) = (1,1,1,0)
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
		#define ASE_VERSION 19701
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Normal;
		uniform float _Sand_A_Tiling;
		uniform sampler2D _NormalDetail;
		uniform float _NormalDetailScale;
		uniform float _NormalDetailIntensity;
		uniform sampler2D _SandDiffuse;
		uniform float4 _DiffuseMultiply;
		uniform float _Brightness;
		uniform float _Specular;
		uniform float _Smoothness;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 temp_cast_0 = (_Sand_A_Tiling).xx;
			float2 uv_TexCoord19 = i.uv_texcoord * temp_cast_0;
			float4 color14 = IsGammaSpace() ? float4(0,0,1,0) : float4(0,0,1,0);
			float2 temp_cast_1 = (_NormalDetailScale).xx;
			float2 uv_TexCoord17 = i.uv_texcoord * temp_cast_1;
			float4 lerpResult10 = lerp( color14 , float4( UnpackNormal( tex2D( _NormalDetail, uv_TexCoord17 ) ) , 0.0 ) , _NormalDetailIntensity);
			o.Normal = BlendNormals( UnpackNormal( tex2D( _Normal, uv_TexCoord19 ) ) , lerpResult10.rgb );
			o.Albedo = ( ( tex2D( _SandDiffuse, uv_TexCoord19 ) * _DiffuseMultiply ) * _Brightness ).rgb;
			float3 temp_cast_5 = (_Specular).xxx;
			o.Specular = temp_cast_5;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.RangedFloatNode;15;-1974.332,460.3434;Inherit;False;Property;_NormalDetailScale;Normal Detail Scale;8;0;Create;True;0;0;0;False;0;False;6;50;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-1950.45,-216.1272;Inherit;False;Property;_Sand_A_Tiling;Sand_A_Tiling;0;0;Create;True;0;0;0;False;0;False;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;17;-1750.332,460.3434;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;16;-1510.332,412.3434;Inherit;True;Property;_NormalDetail;NormalDetail;3;0;Create;True;0;0;0;False;0;False;abc00000000004188447198822879912;027c10b543868e843a9ab4e84064ee7c;True;bump;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-1682.675,-199.6113;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;22;-1059.128,-320.3798;Inherit;True;Property;_SandDiffuse;Sand Diffuse;1;0;Create;True;0;0;0;True;0;False;abc00000000016260969062029517796;abc00000000016260969062029517796;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;13;-726.3317,540.3434;Inherit;False;Property;_NormalDetailIntensity;Normal Detail Intensity;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;9;-1510.332,140.3434;Inherit;True;Property;_Normal;Normal;2;0;Create;True;0;0;0;False;0;False;4d494569b57164f4fbd1651087438618;4d494569b57164f4fbd1651087438618;True;bump;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;21;-755.6224,-171.0681;Inherit;True;Property;_TextureSample3;Texture Sample 3;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;18;-1174.332,412.3434;Inherit;True;Property;_TextureSample16;Texture Sample 16;35;0;Create;True;0;0;0;False;0;False;-1;abc00000000000746726904355842572;abc00000000000746726904355842572;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;25;-678.482,15.83643;Inherit;False;Property;_DiffuseMultiply;Diffuse Multiply;9;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;14;-774.3317,268.3434;Inherit;False;Constant;_Color0;Color 0;23;0;Create;True;0;0;0;False;0;False;0,0,1,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-314.1852,-153.0776;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;12;-1190.332,140.3434;Inherit;True;Property;_T_Rocks_AB_New_Norm;T_Rocks_AB_New_Norm;35;0;Create;True;0;0;0;False;0;False;-1;abc00000000000746726904355842572;abc00000000000746726904355842572;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;10;-502.3317,412.3434;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;238;-302.7179,-52.54486;Inherit;False;Property;_Brightness;Brightness;7;0;Create;True;0;0;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;239;-173.7179,356.4551;Inherit;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;236;-141.0518,254.672;Inherit;False;Property;_Specular;Specular;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;11;-550.3317,220.3434;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;237;-129.7179,-133.5449;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;192,29;Float;False;True;-1;2;ASEMaterialInspector;0;0;StandardSpecular;Davis3D/OceanEnviroment/Shader_Terrain;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;17;0;15;0
WireConnection;19;0;20;0
WireConnection;21;0;22;0
WireConnection;21;1;19;0
WireConnection;18;0;16;0
WireConnection;18;1;17;0
WireConnection;23;0;21;0
WireConnection;23;1;25;0
WireConnection;12;0;9;0
WireConnection;12;1;19;0
WireConnection;10;0;14;0
WireConnection;10;1;18;0
WireConnection;10;2;13;0
WireConnection;11;0;12;0
WireConnection;11;1;10;0
WireConnection;237;0;23;0
WireConnection;237;1;238;0
WireConnection;0;0;237;0
WireConnection;0;1;11;0
WireConnection;0;3;236;0
WireConnection;0;4;239;0
ASEEND*/
//CHKSM=A45C679B60CA45BACC01BAABEF42E2BF060E897A