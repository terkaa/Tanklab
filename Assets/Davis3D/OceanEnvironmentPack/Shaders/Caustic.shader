// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Davis3D/OceanEnviroment/Shader_CausticDecal"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_Color("Color", Color) = (1,1,1,1)
		_FadeRadius("FadeRadius", Float) = 0.1
		_FadeStrength("FadeStrength", Float) = 0.5
		_Add("Add", Float) = 8
		_Add_Blur("Add_Blur", Float) = 8
		_Scale("Scale", Float) = 1
		_GiantMask_Scale("GiantMask_Scale", Float) = 0.2
		_Waves("Waves", 2D) = "white" {}
		_CausticWave1_Multiply("CausticWave1_Multiply", Float) = 0.75
		_Multiply_Caustics("Multiply_Caustics", Float) = 1.12
		_Caustic_Scale1("Caustic_Scale1", Float) = 2
		_Caustic_Scale2("Caustic_Scale2", Float) = 2
		_Caustic_Scale3("Caustic_Scale3", Float) = 2
		_Caustic_Speed1("Caustic_Speed1", Float) = 2
		_Caustic_Speed2("Caustic_Speed2", Float) = 2
		_Caustic_Speed3("Caustic_Speed3", Float) = 2
		_Flowmap("Flowmap", 2D) = "white" {}
		_Flowmap_Value("Flowmap_Value", Float) = 0
		_Flowmap_Scale1("Flowmap_Scale1", Float) = 3
		_Flowmap_Scale2("Flowmap_Scale2", Float) = 2.3
		_Flowmap_Scale3("Flowmap_Scale3", Float) = 3.34
		[Toggle(_ONLYBLURCAUSTIC_ON)] _OnlyBlurCaustic("OnlyBlurCaustic", Float) = 0
		_BluredWaves("BluredWaves", 2D) = "white" {}
		_Multiply_Blur("Multiply_Blur", Float) = 1.12
		_Blurred_Scale("Blurred_Scale", Float) = 1
		_Caustic_Speed_BLUR("Caustic_Speed_BLUR", Float) = 1


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		[HideInInspector][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Unlit" }

		Cull Off
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Blend DstAlpha One
			ZWrite Off
			ZTest Greater
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma multi_compile_local _ALPHATEST_ON
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#pragma shader_feature_local _ONLYBLURCAUSTIC_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD1;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD2;
				#endif
				#ifdef ASE_FOG
					float fogFactor : TEXCOORD3;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _Waves;
			sampler2D _Flowmap;
			sampler2D _BluredWaves;


			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
			}
			
			float3 InvertDepthDirURP75_g41( float3 In )
			{
				float3 result = In;
				#if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301 || ASE_SRP_VERSION == 70503 || ASE_SRP_VERSION == 70600 || ASE_SRP_VERSION == 70700 || ASE_SRP_VERSION == 70701 || ASE_SRP_VERSION >= 80301
				result *= float3(1,1,-1);
				#endif
				return result;
			}
			
			float EdgeFade56( float FadeRadius, float FadeStrength, float3 PositionOS )
			{
				return 1 - saturate((distance(PositionOS, 0) - FadeRadius) / (1 - FadeStrength));
			}
			
			float AllFunc24( float2 In )
			{
				return all(In);
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = vertexInput.positionWS;
				#endif

				#ifdef ASE_FOG
					output.fogFactor = ComputeFogFactor( vertexInput.positionCS.z );
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
				#ifdef _WRITE_RENDERING_LAYERS
				, out float4 outRenderingLayers : SV_Target1
				#endif
				 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV22_g42 = ase_screenPosNorm.xy;
				float2 localUnStereo22_g42 = UnStereo( UV22_g42 );
				float2 break64_g41 = localUnStereo22_g42;
				float clampDepth69_g41 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch38_g41 = ( 1.0 - clampDepth69_g41 );
				#else
				float staticSwitch38_g41 = clampDepth69_g41;
				#endif
				float3 appendResult39_g41 = (float3(break64_g41.x , break64_g41.y , staticSwitch38_g41));
				float4 appendResult42_g41 = (float4((appendResult39_g41*2.0 + -1.0) , 1.0));
				float4 temp_output_43_0_g41 = mul( unity_CameraInvProjection, appendResult42_g41 );
				float3 temp_output_46_0_g41 = ( (temp_output_43_0_g41).xyz / (temp_output_43_0_g41).w );
				float3 In75_g41 = temp_output_46_0_g41;
				float3 localInvertDepthDirURP75_g41 = InvertDepthDirURP75_g41( In75_g41 );
				float4 appendResult49_g41 = (float4(localInvertDepthDirURP75_g41 , 1.0));
				float4 temp_output_3_0 = mul( unity_CameraToWorld, appendResult49_g41 );
				float4 break97 = temp_output_3_0;
				float3 appendResult96 = (float3(break97.x , break97.y , break97.z));
				float FogMultiplier182 = saturate( exp2( ( unity_FogParams.y * -length( ( appendResult96 - _WorldSpaceCameraPos ) ) ) ) );
				float FadeRadius56 = _FadeRadius;
				float FadeStrength56 = _FadeStrength;
				float3 worldToObj5 = mul( GetWorldToObjectMatrix(), float4( temp_output_3_0.xyz, 1 ) ).xyz;
				float3 PositionOS56 = worldToObj5;
				float localEdgeFade56 = EdgeFade56( FadeRadius56 , FadeStrength56 , PositionOS56 );
				float EdgeFade189 = localEdgeFade56;
				float temp_output_2_0_g40 = ( _TimeParameters.x * _Caustic_Speed1 );
				float temp_output_135_0 = temp_output_2_0_g40;
				float4 break48 = temp_output_3_0;
				float2 appendResult12 = (float2(break48.x , break48.z));
				float2 DecalUV163 = ( appendResult12 * _Scale );
				float4 tex2DNode104 = tex2D( _Flowmap, ( DecalUV163 * _Flowmap_Scale1 ) );
				float2 appendResult108 = (float2(tex2DNode104.r , tex2DNode104.g));
				float2 lerpResult110 = lerp( DecalUV163 , appendResult108 , _Flowmap_Value);
				float2 FlowUV01191 = ( lerpResult110 * _Caustic_Scale1 );
				float2 panner141 = ( temp_output_135_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float temp_output_2_0_g38 = ( _TimeParameters.x * _Caustic_Speed2 );
				float temp_output_137_0 = temp_output_2_0_g38;
				float4 tex2DNode115 = tex2D( _Flowmap, ( DecalUV163 * _Flowmap_Scale2 ) );
				float2 appendResult116 = (float2(tex2DNode115.r , tex2DNode115.g));
				float2 lerpResult119 = lerp( DecalUV163 , appendResult116 , _Flowmap_Value);
				float2 FlowUV02192 = ( lerpResult119 * _Caustic_Scale2 );
				float2 panner145 = ( temp_output_137_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float temp_output_2_0_g37 = ( _TimeParameters.x * _Caustic_Speed3 );
				float temp_output_139_0 = temp_output_2_0_g37;
				float4 tex2DNode127 = tex2D( _Flowmap, ( DecalUV163 * _Flowmap_Scale3 ) );
				float2 appendResult123 = (float2(tex2DNode127.r , tex2DNode127.g));
				float2 lerpResult125 = lerp( DecalUV163 , appendResult123 , _Flowmap_Value);
				float2 FlowUV03193 = ( lerpResult125 * _Caustic_Scale3 );
				float2 panner147 = ( temp_output_139_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 panner152 = ( temp_output_139_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float2 panner155 = ( temp_output_137_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float2 panner161 = ( temp_output_135_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float lerpResult162 = lerp( ( ( ( tex2D( _Waves, panner141 ).g + tex2D( _Waves, panner145 ).g ) * _CausticWave1_Multiply ) * tex2D( _Waves, panner147 ).g ) , ( ( ( tex2D( _Waves, panner152 ).g + tex2D( _Waves, panner155 ).g ) * _CausticWave1_Multiply ) * tex2D( _Waves, panner161 ).g ) , tex2D( _Waves, ( DecalUV163 * _GiantMask_Scale ) ).g);
				float Caustics251 = ( lerpResult162 * _Multiply_Caustics );
				float temp_output_2_0_g35 = ( _TimeParameters.x * ( _Caustic_Speed1 * _Caustic_Speed_BLUR ) );
				float temp_output_208_0 = temp_output_2_0_g35;
				float2 panner212 = ( temp_output_208_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float temp_output_2_0_g34 = ( _TimeParameters.x * ( _Caustic_Speed2 * _Caustic_Speed_BLUR ) );
				float temp_output_203_0 = temp_output_2_0_g34;
				float2 panner210 = ( temp_output_203_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float temp_output_2_0_g39 = ( _TimeParameters.x * ( _Caustic_Speed3 * _Caustic_Speed_BLUR ) );
				float temp_output_204_0 = temp_output_2_0_g39;
				float2 panner219 = ( temp_output_204_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 panner211 = ( temp_output_204_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float2 panner213 = ( temp_output_203_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float2 panner216 = ( temp_output_208_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float lerpResult237 = lerp( ( ( ( tex2D( _BluredWaves, panner212 ).g + tex2D( _BluredWaves, panner210 ).g ) * _CausticWave1_Multiply ) * tex2D( _BluredWaves, panner219 ).g ) , ( ( ( tex2D( _BluredWaves, panner211 ).g + tex2D( _BluredWaves, panner213 ).g ) * _CausticWave1_Multiply ) * tex2D( _BluredWaves, panner216 ).g ) , tex2D( _BluredWaves, ( ( DecalUV163 * _GiantMask_Scale ) * _Blurred_Scale ) ).g);
				float BluredCaustics249 = ( ( _Multiply_Blur * lerpResult237 ) + ( _Add_Blur * 0.01 ) );
				#ifdef _ONLYBLURCAUSTIC_ON
				float staticSwitch178 = BluredCaustics249;
				#else
				float staticSwitch178 = ( Caustics251 * BluredCaustics249 );
				#endif
				float3 temp_cast_2 = (-0.5).xxx;
				float3 temp_cast_3 = (0.5).xxx;
				float2 In24 = ( ( 1.0 - step( worldToObj5 , temp_cast_2 ) ) * step( worldToObj5 , temp_cast_3 ) ).xy;
				float localAllFunc24 = AllFunc24( In24 );
				float DecalBorders185 = localAllFunc24;
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( ( ( FogMultiplier182 * ( ( EdgeFade189 * ( ( 2.0 * staticSwitch178 ) + ( _Add * 0.01 ) ) ) * DecalBorders185 ) ) * _Color ) * _Color.a ).rgb;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( input.positionCS );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, input.fogFactor );
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif

				return half4( Color, Alpha );
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM

			#pragma multi_compile_local _ALPHATEST_ON
			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003


			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_SHADOWCASTER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			float3 _LightDirection;
			float3 _LightPosition;

			PackedVaryings VertexFunction( Attributes input )
			{
				PackedVaryings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = positionWS;
				#endif

				float3 normalWS = TransformObjectToWorldDir( input.normalOS );

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				//code for UNITY_REVERSED_Z is moved into Shadows.hlsl from 6000.0.22 and or higher
				positionCS = ApplyShadowClamping(positionCS);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = positionCS;

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				

				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					#ifdef _ALPHATEST_SHADOW_ON
						clip(Alpha - AlphaClipThresholdShadow);
					#else
						clip(Alpha - AlphaClipThreshold);
					#endif
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( input.positionCS );
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM

			#pragma multi_compile_local _ALPHATEST_ON
			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 positionWS : TEXCOORD1;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD2;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = vertexInput.positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				

				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( input.positionCS );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#pragma multi_compile_local _ALPHATEST_ON
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );

				output.positionCS = TransformWorldToHClip(positionWS);

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				

				surfaceDescription.Alpha = 1;
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#pragma multi_compile_local _ALPHATEST_ON
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			float4 _SelectionID;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
				output.positionCS = TransformWorldToHClip(positionWS);
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				

				surfaceDescription.Alpha = 1;
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = _SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

        	#pragma multi_compile_local _ALPHATEST_ON
        	#pragma multi_compile_instancing
        	#define _SURFACE_TYPE_TRANSPARENT 1
        	#define ASE_VERSION 19701
        	#define ASE_SRP_VERSION 170003


        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				output.normalWS = TransformObjectToWorldNormal( input.normalOS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag( PackedVaryings input
				, out half4 outNormalWS : SV_Target0
			#ifdef _WRITE_RENDERING_LAYERS
				, out float4 outRenderingLayers : SV_Target1
			#endif
				 )
			{
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				

				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float3 normalWS = normalize(input.normalWS);
					float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					float3 normalWS = input.normalWS;
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
				#endif
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "MotionVectors"
			Tags { "LightMode"="MotionVectors" }

			ColorMask RG

			HLSLPROGRAM

			#pragma multi_compile_local _ALPHATEST_ON
			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 positionOld : TEXCOORD4;
				#if _ADD_PRECOMPUTED_VELOCITY
					float3 alembicMotionVector : TEXCOORD5;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 positionCSNoJitter : TEXCOORD0;
				float4 previousPositionCSNoJitter : TEXCOORD1;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Add_Blur;
			float _Blurred_Scale;
			float _Caustic_Speed_BLUR;
			float _Multiply_Blur;
			float _Multiply_Caustics;
			float _GiantMask_Scale;
			float _Caustic_Scale3;
			float _Flowmap_Scale3;
			float _Caustic_Speed3;
			float _CausticWave1_Multiply;
			float _Caustic_Scale2;
			float _Flowmap_Scale2;
			float _Caustic_Speed2;
			float _Caustic_Scale1;
			float _Flowmap_Value;
			float _Flowmap_Scale1;
			float _Scale;
			float _Caustic_Speed1;
			float _FadeStrength;
			float _Add;
			float _FadeRadius;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				// Jittered. Match the frame.
				output.positionCS = vertexInput.positionCS;
				output.positionCSNoJitter = mul( _NonJitteredViewProjMatrix, mul( UNITY_MATRIX_M, input.positionOS ) );

				float4 prevPos = ( unity_MotionVectorsParams.x == 1 ) ? float4( input.positionOld, 1 ) : input.positionOS;

				#if _ADD_PRECOMPUTED_VELOCITY
					prevPos = prevPos - float4(input.alembicMotionVector, 0);
				#endif

				output.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, mul( UNITY_PREV_MATRIX_M, prevPos ) );

				return output;
			}

			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}

			half4 frag(	PackedVaryings input  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				

				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( input.positionCS );
				#endif

				return float4( CalcNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 0, 0 );
			}
			ENDHLSL
		}
		
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback "Hidden/InternalErrorShader"
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;101;-1978.357,133.6523;Inherit;False;659.9619;249.7293;DecalUV;5;30;12;31;48;163;;1,0.964672,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;100;-1979.337,-898.8665;Inherit;False;1327.583;467.7418;FogMultiplier;11;182;98;89;90;88;92;93;94;95;96;97;;0,0.8450127,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;250;-1985.508,3223.648;Inherit;False;2535.213;1636.413;BluredCaustics;43;248;245;247;246;204;209;203;205;206;207;208;212;211;213;210;217;214;215;230;223;225;224;220;222;232;231;219;216;234;233;221;227;228;235;229;236;239;243;237;242;238;241;249;;0.4534977,0.745283,0.5830489,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;252;-1979.243,1569.371;Inherit;False;1979.829;1647.016;Caustics;34;137;198;139;194;135;197;195;152;145;141;155;144;196;143;153;199;154;161;147;158;170;148;172;146;149;159;157;171;150;160;177;162;175;251;;0.3254717,0.7563778,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;134;-1982.273,389.2037;Inherit;False;1351.281;1170.958;CausticFlowmap;31;193;192;191;126;125;129;123;168;120;111;119;127;112;121;110;131;164;116;122;108;167;169;130;104;115;113;105;107;114;166;165;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;102;-1981.135,-408.6868;Inherit;False;768.698;231.8162;DecalFade;4;59;60;56;189;;0.5,0.3963946,0.3089623,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;103;-1981.116,-156.7678;Inherit;False;1150.049;276.679;DecalBorders;8;185;9;8;10;6;11;24;7;;0.3490566,0.06092026,0.1819429,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-1664.601,231.3968;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;163;-1514.901,224.1954;Inherit;False;DecalUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;114;-1904.273,1074.448;Inherit;False;Property;_Flowmap_Scale2;Flowmap_Scale2;19;0;Create;True;0;0;0;False;0;False;2.3;2.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;166;-1888.279,954.7795;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;165;-1884.38,576.4797;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-1901.303,695.5291;Inherit;False;Property;_Flowmap_Scale1;Flowmap_Scale1;18;0;Create;True;0;0;0;False;0;False;3;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;113;-1717.273,960.4485;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1714.303,581.5289;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;169;-1884.377,1330.482;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;130;-1893.171,1449.677;Inherit;False;Property;_Flowmap_Scale3;Flowmap_Scale3;20;0;Create;True;0;0;0;False;0;False;3.34;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-1803.566,290.408;Inherit;False;Property;_Scale;Scale;5;0;Create;True;0;0;0;False;0;False;1;0.075;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;104;-1592.032,551.9824;Inherit;True;Property;_Flowmap;Flowmap;16;0;Create;True;0;0;0;False;0;False;-1;abc00000000006926196262259652773;abc00000000006926196262259652773;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;131;-1544.753,744.6281;Inherit;False;Property;_Flowmap_Value;Flowmap_Value;17;0;Create;True;0;0;0;False;0;False;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;116;-1299.041,960.4595;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;216;-1169.545,4466.833;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;191;-836.1929,546.7066;Inherit;False;FlowUV01;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;115;-1587.34,932.6912;Inherit;True;Property;_TextureSample9;Texture Sample 9;16;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;104;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.DynamicAppendNode;12;-1801.653,194.9462;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;167;-1334.478,835.1802;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-1706.171,1335.677;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-1175.315,631.1837;Inherit;False;Property;_Caustic_Scale1;Caustic_Scale1;10;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;127;-1575.238,1306.921;Inherit;True;Property;_TextureSample8;Texture Sample 8;16;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;104;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;136;-2299.717,2946.888;Inherit;False;Property;_Caustic_Speed1;Caustic_Speed1;13;0;Create;True;0;0;0;False;0;False;2;-1.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-2299.033,3016.839;Inherit;False;Property;_Caustic_Speed2;Caustic_Speed2;14;0;Create;True;0;0;0;False;0;False;2;-2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;108;-1296.07,581.5396;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;248;-1935.508,3898.808;Inherit;False;Property;_Caustic_Speed_BLUR;Caustic_Speed_BLUR;25;0;Create;True;0;0;0;False;0;False;1;2.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;111;-987.3141,550.1834;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;48;-1928.355,183.6523;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;168;-1316.277,1235.58;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;123;-1287.938,1335.689;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;246;-1685.025,3883;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;125;-1146.32,1262.304;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;247;-1685.025,3969.57;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-990.2841,929.103;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;164;-1325.776,468.6571;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;192;-836.6208,922.8696;Inherit;False;FlowUV02;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-2301.853,3084.904;Inherit;False;Property;_Caustic_Speed3;Caustic_Speed3;15;0;Create;True;0;0;0;False;0;False;2;-2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-1167.183,1385.333;Inherit;False;Property;_Caustic_Scale3;Caustic_Scale3;12;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;225;-902.6351,4066.722;Inherit;True;Property;_TextureSample11;Texture Sample 11;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;215;-1368.694,4464.12;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;205;-1349.144,3298.389;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;207;-1363.638,4088.757;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;203;-1531.511,3891.236;Inherit;False;TimeWithSpeedVariable;-1;;34;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.GetLocalVarNode;209;-1371.823,4273.807;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-979.182,1304.333;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;206;-1348.753,3485.582;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;213;-1168.935,4278.085;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;208;-1534.196,3787.285;Inherit;False;TimeWithSpeedVariable;-1;;35;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.GetLocalVarNode;198;-1766.871,2630.227;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;139;-1924.387,2349.709;Inherit;False;TimeWithSpeedVariable;-1;;37;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.PannerNode;211;-1169.652,4092.94;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;195;-1743.801,1842.003;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;197;-1758.685,2445.178;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;212;-1165.098,3304.771;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;194;-1744.192,1654.81;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;137;-1926.559,2247.657;Inherit;False;TimeWithSpeedVariable;-1;;38;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;193;-819.587,1301.304;Inherit;False;FlowUV03;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;204;-1529.34,3993.289;Inherit;False;TimeWithSpeedVariable;-1;;39;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.PannerNode;210;-1164.381,3489.915;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;135;-1929.243,2143.707;Inherit;False;TimeWithSpeedVariable;-1;;40;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.GetLocalVarNode;217;-1365.428,4667.496;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;119;-1157.423,887.0742;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;152;-1564.699,2449.361;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;141;-1560.145,1661.192;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;173;-2301.478,3154.969;Inherit;False;Property;_GiantMask_Scale;GiantMask_Scale;6;0;Create;True;0;0;0;False;0;False;0.2;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;230;-901.3534,3273.648;Inherit;True;Property;_BluredWaves;BluredWaves;22;0;Create;True;0;0;0;False;0;False;-1;f6fca7508871d02489f2a6b17625d2db;f6fca7508871d02489f2a6b17625d2db;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.PannerNode;155;-1563.982,2634.505;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;224;-900.8369,3460.19;Inherit;True;Property;_TextureSample12;Texture Sample 12;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;214;-1354.201,3667.319;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;199;-1763.742,2820.54;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;232;-581.5286,3416.469;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;144;-1351.431,1817.518;Inherit;True;Property;_TextureSample13;Texture Sample 13;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;196;-1749.248,2023.74;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;151;-2339.068,3222.917;Inherit;False;Property;_CausticWave1_Multiply;CausticWave1_Multiply;8;0;Create;True;0;0;0;False;0;False;0.75;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;231;-586.0824,4204.638;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;145;-1559.428,1846.337;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;3;-2567.324,-171.6597;Inherit;False;Reconstruct World Position From Depth;-1;;41;e7094bcbcc80eb140b2a3dbe6a861de8;0;0;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;223;-905.3907,4249.359;Inherit;True;Property;_TextureSample10;Texture Sample 10;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;121;-1178.285,1010.103;Inherit;False;Property;_Caustic_Scale2;Caustic_Scale2;11;0;Create;True;0;0;0;False;0;False;2;2.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;110;-1154.452,508.155;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;245;-1685.025,3795.077;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;148;-1030.128,1766.36;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;170;-1661.384,3012.16;Inherit;False;163;DecalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;147;-1560.517,2028.794;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;233;-436.1054,3529.788;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-1931.116,36.30737;Inherit;False;Constant;_F05;F0.5;0;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;228;-899.926,3643.648;Inherit;True;Property;_TextureSample4;Texture Sample 4;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;221;-1047.326,4659.973;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;227;-904.0009,4438.107;Inherit;True;Property;_TextureSample3;Texture Sample 3;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;158;-1034.682,2554.529;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;157;-1354.595,2794.434;Inherit;True;Property;_TextureSample5;Texture Sample 5;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;146;-1350.52,1999.976;Inherit;True;Property;_TextureSample6;Texture Sample 6;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-884.7041,1879.679;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;236;-272.5014,3671.735;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;-897.8998,2490.691;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;161;-1564.593,2823.254;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;255;2726.713,1124.002;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;234;-449.3009,4140.8;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;94;-1697.358,-667.5251;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;220;-1186.915,4608.595;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;143;-1349.514,1619.371;Inherit;True;Property;_Waves;Waves;7;0;Create;True;0;0;0;False;0;False;-1;ee3f9c278069cdd458daa8b6599e187c;ee3f9c278069cdd458daa8b6599e187c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;177;-573.5305,2409.786;Inherit;False;Property;_Multiply_Caustics;Multiply_Caustics;9;0;Create;True;0;0;0;False;0;False;1.12;20;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;97;-1960.081,-841.644;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;238;39.7928,3939.129;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;171;-1354.884,2986.387;Inherit;True;Property;_TextureSample0;Texture Sample 0;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;242;55.26606,4106.439;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;150;-721.1,2021.626;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;243;-95.03092,4100.616;Inherit;False;Property;_Add_Blur;Add_Blur;4;0;Create;True;0;0;0;False;0;False;8;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;153;-1353.229,2423.049;Inherit;True;Property;_TextureSample2;Texture Sample 2;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;241;202.8185,4004.825;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;239;-135.2072,3897.129;Inherit;False;Property;_Multiply_Blur;Multiply_Blur;23;0;Create;True;0;0;0;False;0;False;1.12;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;160;-746.642,2814.414;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;175;-361.3237,2287.769;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;162;-533.7408,2289.358;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;229;-904.2902,4630.061;Inherit;True;Property;_TextureSample1;Texture Sample 1;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;172;-1493.152,3016.232;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;235;-298.0436,4464.523;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;237;-125.0375,3977.997;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;219;-1165.47,3672.372;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;96;-1834.349,-842.4419;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;189;-1488.857,-329.5948;Inherit;False;EdgeFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;24;-1203.137,-23.75621;Inherit;False;return all(In)@;1;Create;1;True;In;FLOAT2;0,0;In;;Inherit;False;AllFunc;True;False;0;;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;253;1199.195,1211.564;Inherit;False;251;Caustics;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode;93;-1540.84,-663.2234;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;8;-1618.793,-104.0341;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;10;-1497.793,-105.0341;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-1931.135,-286.8695;Inherit;False;Property;_FadeStrength;FadeStrength;2;0;Create;True;0;0;0;False;0;False;0.5;0.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;244;1400.289,1221.169;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;249;325.7055,4000.058;Inherit;False;BluredCaustics;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;6;-1619.922,1.797791;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;59;-1926.216,-358.6868;Inherit;False;Property;_FadeRadius;FadeRadius;1;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FogParamsNode;88;-1514.635,-802.4111;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-1289.539,-775.0873;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;178;1554.201,1295.673;Inherit;False;Property;_OnlyBlurCaustic;OnlyBlurCaustic;21;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;95;-1938.126,-584.7582;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;222;-1213.724,4736.932;Inherit;False;Property;_Blurred_Scale;Blurred_Scale;24;0;Create;True;0;0;0;False;0;False;1;0.32;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;154;-1355.985,2605.687;Inherit;True;Property;_TextureSample7;Texture Sample 7;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.NegateNode;92;-1424.84,-662.2234;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;9;-1774.124,-39.7332;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;254;1167.195,1278.565;Inherit;False;249;BluredCaustics;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;251;-223.4135,2281.977;Inherit;False;Caustics;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;179;1840.872,1185.733;Inherit;False;2;2;0;FLOAT;2;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;176;1989.081,1284.35;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;98;-1036.805,-772.838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;190;2112.474,1117.047;Inherit;False;189;EdgeFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;180;2142.292,1188.393;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformPositionNode;5;-2181.974,-177.7433;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;185;-1048.961,-28.63014;Inherit;False;DecalBorders;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;184;2289.725,1163.758;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Exp2OpNode;89;-1149.782,-773.2595;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;186;2439.735,1163.33;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;183;2374.24,1080.976;Inherit;False;182;FogMultiplier;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;2572.571,1120.621;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;256;2483.713,1262.002;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,0.9888282,0.7877358,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;187;2242.063,1256.767;Inherit;False;185;DecalBorders;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;181;1838.782,1278.527;Inherit;False;Property;_Add;Add;3;0;Create;True;0;0;0;False;0;False;8;20;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-1341.919,-22.93389;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;56;-1742.437,-325.8706;Inherit;False;return 1 - saturate((distance(PositionOS, 0) - FadeRadius) / (1 - FadeStrength))@;1;Create;3;True;FadeRadius;FLOAT;0;In;;Inherit;False;True;FadeStrength;FLOAT;0;In;;Inherit;False;True;PositionOS;FLOAT3;0,0,0;In;;Inherit;False;EdgeFade;True;False;0;;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;182;-868.5375,-775.4979;Inherit;False;FogMultiplier;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;262;2892.789,1179.092;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;259;2927.65,1114.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;260;2927.65,1114.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;261;2927.65,1114.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;257;2927.65,1114.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;258;3066.65,1106.85;Float;False;True;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;Davis3D/OceanEnviroment/Shader_CausticDecal;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;True;True;1;7;False;;1;False;;0;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;True;True;2;False;;True;2;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;25;Surface;1;638193381754121464;  Blend;0;0;Two Sided;0;638193381789558529;Forward Only;0;0;Alpha Clipping;1;0;  Use Shadow Threshold;0;0;Cast Shadows;1;0;Receive Shadows;1;0;Motion Vectors;1;0;  Add Precomputed Velocity;0;0;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;11;False;True;True;True;False;False;True;True;True;False;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;263;3066.65,1156.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;264;3066.65,1156.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;265;3066.65,1156.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;266;3066.65,1156.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;267;3066.65,1156.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;9;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;268;3066.65,1156.85;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;MotionVectors;0;10;MotionVectors;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;False;False;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=MotionVectors;False;False;0;;0;0;Standard;0;False;0
WireConnection;30;0;12;0
WireConnection;30;1;31;0
WireConnection;163;0;30;0
WireConnection;113;0;166;0
WireConnection;113;1;114;0
WireConnection;105;0;165;0
WireConnection;105;1;107;0
WireConnection;104;1;105;0
WireConnection;116;0;115;1
WireConnection;116;1;115;2
WireConnection;216;0;215;0
WireConnection;216;1;208;0
WireConnection;191;0;111;0
WireConnection;115;1;113;0
WireConnection;12;0;48;0
WireConnection;12;1;48;2
WireConnection;122;0;169;0
WireConnection;122;1;130;0
WireConnection;127;1;122;0
WireConnection;108;0;104;1
WireConnection;108;1;104;2
WireConnection;111;0;110;0
WireConnection;111;1;112;0
WireConnection;48;0;3;0
WireConnection;123;0;127;1
WireConnection;123;1;127;2
WireConnection;246;0;138;0
WireConnection;246;1;248;0
WireConnection;125;0;168;0
WireConnection;125;1;123;0
WireConnection;125;2;131;0
WireConnection;247;0;140;0
WireConnection;247;1;248;0
WireConnection;120;0;119;0
WireConnection;120;1;121;0
WireConnection;192;0;120;0
WireConnection;225;1;211;0
WireConnection;203;3;246;0
WireConnection;126;0;125;0
WireConnection;126;1;129;0
WireConnection;213;0;209;0
WireConnection;213;1;203;0
WireConnection;208;3;245;0
WireConnection;139;3;140;0
WireConnection;211;0;207;0
WireConnection;211;1;204;0
WireConnection;212;0;205;0
WireConnection;212;1;208;0
WireConnection;137;3;138;0
WireConnection;193;0;126;0
WireConnection;204;3;247;0
WireConnection;210;0;206;0
WireConnection;210;1;203;0
WireConnection;135;3;136;0
WireConnection;119;0;167;0
WireConnection;119;1;116;0
WireConnection;119;2;131;0
WireConnection;152;0;197;0
WireConnection;152;1;139;0
WireConnection;141;0;194;0
WireConnection;141;1;135;0
WireConnection;230;1;212;0
WireConnection;155;0;198;0
WireConnection;155;1;137;0
WireConnection;224;1;210;0
WireConnection;232;0;230;2
WireConnection;232;1;224;2
WireConnection;144;1;145;0
WireConnection;231;0;225;2
WireConnection;231;1;223;2
WireConnection;145;0;195;0
WireConnection;145;1;137;0
WireConnection;223;1;213;0
WireConnection;110;0;164;0
WireConnection;110;1;108;0
WireConnection;110;2;131;0
WireConnection;245;0;136;0
WireConnection;245;1;248;0
WireConnection;148;0;143;2
WireConnection;148;1;144;2
WireConnection;147;0;196;0
WireConnection;147;1;139;0
WireConnection;233;0;232;0
WireConnection;233;1;151;0
WireConnection;228;1;219;0
WireConnection;221;0;220;0
WireConnection;221;1;222;0
WireConnection;227;1;216;0
WireConnection;158;0;153;2
WireConnection;158;1;154;2
WireConnection;157;1;161;0
WireConnection;146;1;147;0
WireConnection;149;0;148;0
WireConnection;149;1;151;0
WireConnection;236;0;233;0
WireConnection;236;1;228;2
WireConnection;159;0;158;0
WireConnection;159;1;151;0
WireConnection;161;0;199;0
WireConnection;161;1;135;0
WireConnection;255;0;188;0
WireConnection;255;1;256;0
WireConnection;234;0;231;0
WireConnection;234;1;151;0
WireConnection;94;0;96;0
WireConnection;94;1;95;0
WireConnection;220;0;217;0
WireConnection;220;1;173;0
WireConnection;143;1;141;0
WireConnection;97;0;3;0
WireConnection;238;0;239;0
WireConnection;238;1;237;0
WireConnection;171;1;172;0
WireConnection;242;0;243;0
WireConnection;150;0;149;0
WireConnection;150;1;146;2
WireConnection;153;1;152;0
WireConnection;241;0;238;0
WireConnection;241;1;242;0
WireConnection;160;0;159;0
WireConnection;160;1;157;2
WireConnection;175;0;162;0
WireConnection;175;1;177;0
WireConnection;162;0;150;0
WireConnection;162;1;160;0
WireConnection;162;2;171;2
WireConnection;229;1;221;0
WireConnection;172;0;170;0
WireConnection;172;1;173;0
WireConnection;235;0;234;0
WireConnection;235;1;227;2
WireConnection;237;0;236;0
WireConnection;237;1;235;0
WireConnection;237;2;229;2
WireConnection;219;0;214;0
WireConnection;219;1;204;0
WireConnection;96;0;97;0
WireConnection;96;1;97;1
WireConnection;96;2;97;2
WireConnection;189;0;56;0
WireConnection;24;0;11;0
WireConnection;93;0;94;0
WireConnection;8;0;5;0
WireConnection;8;1;9;0
WireConnection;10;0;8;0
WireConnection;244;0;253;0
WireConnection;244;1;254;0
WireConnection;249;0;241;0
WireConnection;6;0;5;0
WireConnection;6;1;7;0
WireConnection;90;0;88;2
WireConnection;90;1;92;0
WireConnection;178;1;244;0
WireConnection;178;0;254;0
WireConnection;154;1;155;0
WireConnection;92;0;93;0
WireConnection;9;0;7;0
WireConnection;251;0;175;0
WireConnection;179;1;178;0
WireConnection;176;0;181;0
WireConnection;98;0;89;0
WireConnection;180;0;179;0
WireConnection;180;1;176;0
WireConnection;5;0;3;0
WireConnection;185;0;24;0
WireConnection;184;0;190;0
WireConnection;184;1;180;0
WireConnection;89;0;90;0
WireConnection;186;0;184;0
WireConnection;186;1;187;0
WireConnection;188;0;183;0
WireConnection;188;1;186;0
WireConnection;11;0;10;0
WireConnection;11;1;6;0
WireConnection;56;0;59;0
WireConnection;56;1;60;0
WireConnection;56;2;5;0
WireConnection;182;0;98;0
WireConnection;262;0;255;0
WireConnection;262;1;256;4
WireConnection;258;2;262;0
ASEEND*/
//CHKSM=FE13ED97081E5DF5546EE3CEF5861317C2FEB1A1