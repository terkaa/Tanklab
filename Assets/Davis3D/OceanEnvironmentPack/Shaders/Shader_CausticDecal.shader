// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Shader_CausticDecal"
{
	Properties
    {
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_Color("Color", Color) = (1,1,1,1)
		_Add("Add", Float) = 8
		_Add_Blur("Add_Blur", Float) = 8
		_GiantMask_Scale("GiantMask_Scale", Float) = 0.2
		_Scale("Scale", Float) = 1
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
		_FadeRadius("FadeRadius", Float) = 0
		_FadeStrength("FadeStrength", Float) = 0.8

		[HideInInspector] _DrawOrder("_DrawOrder", Int) = 0
		[HideInInspector][Enum(Depth Bias, 0, View Bias, 1)] _DecalMeshBiasType("_DecalMeshBiasType", Int) = 0
		[HideInInspector] _DecalMeshDepthBias("_DecalMeshDepthBias", Float) = 0.0
		[HideInInspector] _DecalMeshViewBias("_DecalMeshViewBias", Float) = 0.0
		[HideInInspector] _DecalStencilWriteMask("_DecalStencilWriteMask", Int) = 16
		[HideInInspector] _DecalStencilRef("_DecalStencilRef", Int) = 16
		[HideInInspector][ToggleUI] _AffectAlbedo("Boolean", Float) = 1
		//[HideInInspector][ToggleUI] _AffectNormal("Boolean", Float) = 1
        //[HideInInspector][ToggleUI] _AffectAO("Boolean", Float) = 1
        //[HideInInspector][ToggleUI] _AffectMetal("Boolean", Float) = 1
        //[HideInInspector][ToggleUI] _AffectSmoothness("Boolean", Float) = 1
        //[HideInInspector][ToggleUI] _AffectEmission("Boolean", Float) = 1
		[HideInInspector] _DecalColorMask0("_DecalColorMask0", Int) = 0
		[HideInInspector] _DecalColorMask1("_DecalColorMask1", Int) = 0
		[HideInInspector] _DecalColorMask2("_DecalColorMask2", Int) = 0
		[HideInInspector] _DecalColorMask3("_DecalColorMask3", Int) = 0

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		[HideInInspector] _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _NormalBlendSrc("_NormalBlendSrc", Float) = 0.0
		[HideInInspector] _MaskBlendSrc("_MaskBlendSrc", Float) = 1.0
		[HideInInspector] _DecalMaskMapBlueScale("_DecalMaskMapBlueScale", Range(0.0, 1.0)) = 1.0

		//[HideInInspector]_Unity_Identify_HDRP_Decal("_Unity_Identify_HDRP_Decal", Float) = 1.0
	}

    SubShader
    {
		LOD 0

		
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		HLSLINCLUDE
		#pragma target 4.5
		#pragma exclude_renderers glcore gles gles3 ps4 ps5 
		#pragma multi_compile_instancing
		#pragma instancing_options renderinglayer

		#define SUPPORT_GLOBAL_MIP_BIAS

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float3 NormalTS;
            float NormalAlpha;
            float Metallic;
            float Occlusion;
            float Smoothness;
            float MAOSAlpha;
			float3 Emission;
        };
		ENDHLSL

		
        Pass
		{
			
			Name "DBufferProjector"
			Tags { "LightMode"="DBufferProjector" }

            Stencil
            {
            	Ref [_DecalStencilRef]
            	WriteMask [_DecalStencilWriteMask]
            	Comp Always
            	Pass Replace
            }


			Cull Front
			ZWrite Off
			ZTest Greater

			Blend 0 DstAlpha One
			Blend 1 DstAlpha One
			Blend 2 DstAlpha One
			Blend 3 Zero OneMinusSrcColor

			ColorMask[_DecalColorMask0]
			ColorMask[_DecalColorMask1] 1
			ColorMask[_DecalColorMask2] 2
			ColorMask[_DecalColorMask3] 3

			HLSLPROGRAM

            #pragma shader_feature_local_fragment _MATERIAL_AFFECTS_ALBEDO
            #pragma shader_feature_local_fragment _COLORMAP
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #define ASE_VERSION 19701
            #define ASE_SRP_VERSION 170003


            #pragma vertex Vert
            #pragma fragment Frag

			#pragma multi_compile_fragment DECALS_3RT DECALS_4RT
			#pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#define SUPPORT_GLOBAL_MIP_BIAS

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"

			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#pragma shader_feature_local _ONLYBLURCAUSTIC_ON


            struct AttributesMesh
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
				
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

      		struct PackedVaryingsToPS
			{
				float4 positionCS : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_RELATIVE_WORLD_POS)
				float3 positionRWS : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _FadeRadius;
            float _Add;
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
            float _FadeStrength;
            float _Caustic_Speed1;
            float _DrawOrder;
			float _NormalBlendSrc;
			float _MaskBlendSrc;
			float _DecalBlend;
			int   _DecalMeshBiasType;
            float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
            float _DecalStencilWriteMask;
            float _DecalStencilRef;
			#ifdef _MATERIAL_AFFECTS_ALBEDO
            float _AffectAlbedo;
			#endif
			#ifdef _MATERIAL_AFFECTS_NORMAL
            float _AffectNormal;
			#endif
            #ifdef _MATERIAL_AFFECTS_MASKMAP
            float _AffectAO;
			float _AffectMetal;
            float _AffectSmoothness;
			#endif
			#ifdef _MATERIAL_AFFECTS_EMISSION
            float _AffectEmission;
			#endif
            float _DecalColorMask0;
            float _DecalColorMask1;
            float _DecalColorMask2;
            float _DecalColorMask3;
            CBUFFER_END

			sampler2D _Waves;
			sampler2D _Flowmap;
			sampler2D _BluredWaves;
			float4x4 unity_CameraProjection;
			float4x4 unity_CameraInvProjection;
			float4x4 unity_WorldToCamera;
			float4x4 unity_CameraToWorld;


            #if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT // Always present as we require it also in case of anisotropic lighting
            #define ATTRIBUTES_NEED_TEXCOORD0

            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #define VARYINGS_NEED_TEXCOORD0
            #endif

			float2 TransformUVs1_g35( float2 UV )
			{
				#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
					float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
					float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
					float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
					UV.xy = UV.xy * scale + offset;
				#endif
				return UV;
			}
			
			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
			}
			
			float3 InvertDepthDirHD73_g36( float3 In )
			{
				float3 result = In;
				#if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301 || ASE_SRP_VERSION == 70503 || ASE_SRP_VERSION == 70600 || ASE_SRP_VERSION == 70700 || ASE_SRP_VERSION == 70701 || ASE_SRP_VERSION >= 80301
				result *= float3(1,1,-1);
				#endif
				return result;
			}
			
			float EdgeFade306( float FadeRadius, float FadeStrength, float3 PositionOS )
			{
				return 1 - saturate((distance(PositionOS, 0) - FadeRadius) / (1 - FadeStrength));
			}
			

            void GetSurfaceData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, PositionInputs posInput, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
                    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                    float fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
                    float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                    float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
                    fragInputs.texCoord0.xy = fragInputs.texCoord0.xy * scale + offset;
                    fragInputs.texCoord1.xy = fragInputs.texCoord1.xy * scale + offset;
                    fragInputs.texCoord2.xy = fragInputs.texCoord2.xy * scale + offset;
                    fragInputs.texCoord3.xy = fragInputs.texCoord3.xy * scale + offset;
                    fragInputs.positionRWS = posInput.positionWS;
                    fragInputs.tangentToWorld[2].xyz = TransformObjectToWorldDir(float3(0, 1, 0));
                    fragInputs.tangentToWorld[1].xyz = TransformObjectToWorldDir(float3(0, 0, 1));
                #else
                    #ifdef LOD_FADE_CROSSFADE
                    LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                    #endif

                    float fadeFactor = 1.0;
                #endif

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);

                #ifdef _MATERIAL_AFFECTS_EMISSION
                #endif

                #ifdef _MATERIAL_AFFECTS_ALBEDO
                    surfaceData.baseColor.xyz = surfaceDescription.BaseColor;
                    surfaceData.baseColor.w = surfaceDescription.Alpha * fadeFactor;
                #endif

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    #ifdef DECAL_SURFACE_GRADIENT
                        #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
                            float3x3 tangentToWorld = transpose((float3x3)normalToWorld);
                        #else
                            float3x3 tangentToWorld = fragInputs.tangentToWorld;
                        #endif

                        surfaceData.normalWS.xyz = SurfaceGradientFromTangentSpaceNormalAndFromTBN(surfaceDescription.NormalTS.xyz, tangentToWorld[0], tangentToWorld[1]);
                    #else
                        #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
                            surfaceData.normalWS.xyz = mul((float3x3)normalToWorld, surfaceDescription.NormalTS);
                        #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_PREVIEW)

                            surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, fragInputs.tangentToWorld));
                        #endif
                    #endif

                    surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;
                #else
                    #if (SHADERPASS == SHADERPASS_FORWARD_PREVIEW)
                        #ifdef DECAL_SURFACE_GRADIENT
                            surfaceData.normalWS.xyz = float3(0.0, 0.0, 0.0);
                        #else
                            surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(float3(0.0, 0.0, 0.1), fragInputs.tangentToWorld));
                        #endif
                    #endif
                #endif

                #ifdef _MATERIAL_AFFECTS_MASKMAP
                    surfaceData.mask.z = surfaceDescription.Smoothness;
                    surfaceData.mask.w = surfaceDescription.MAOSAlpha * fadeFactor;

                    #ifdef DECALS_4RT
                        surfaceData.mask.x = surfaceDescription.Metallic;
                        surfaceData.mask.y = surfaceDescription.Occlusion;
                        surfaceData.MAOSBlend.x = surfaceDescription.MAOSAlpha * fadeFactor;
                        surfaceData.MAOSBlend.y = surfaceDescription.MAOSAlpha * fadeFactor;
                    #endif

                #endif
            }

			PackedVaryingsToPS Vert(AttributesMesh inputMesh  )
			{
				PackedVaryingsToPS output;

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				inputMesh.tangentOS = float4( 1, 0, 0, -1);
				inputMesh.normalOS = float3( 0, 1, 0 );

				float4 ase_clipPos = TransformWorldToHClip( TransformObjectToWorld(inputMesh.positionOS));
				float4 screenPos = ComputeScreenPos( ase_clipPos , _ProjectionParams.x );
				output.ase_texcoord1 = screenPos;
				

				inputMesh.normalOS = inputMesh.normalOS;
				inputMesh.tangentOS = inputMesh.tangentOS;

				float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);

				output.positionCS = TransformWorldToHClip(positionRWS);
				#if defined(ASE_NEEDS_FRAG_RELATIVE_WORLD_POS)
				output.positionRWS = positionRWS;
				#endif

				return output;
			}

			void Frag( PackedVaryingsToPS packedInput,
			#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
				OUTPUT_DBUFFER(outDBuffer)
			#else
				out float4 outEmissive : SV_Target0
			#endif
			
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

				FragInputs input;
                ZERO_INITIALIZE(FragInputs, input);
                input.tangentToWorld = k_identity3x3;
				#if defined(ASE_NEEDS_FRAG_RELATIVE_WORLD_POS)
				input.positionRWS = packedInput.positionRWS;
				#endif

                input.positionSS = packedInput.positionCS;

				DecalSurfaceData surfaceData;
				float clipValue = 1.0;
				float angleFadeFactor = 1.0;

				PositionInputs posInput;
			#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)

				float depth = LoadCameraDepth(input.positionSS.xy);
				posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

				DecalPrepassData material;
				ZERO_INITIALIZE(DecalPrepassData, material);
				if (_EnableDecalLayers)
				{
					uint decalLayerMask = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal).x);

					DecodeFromDecalPrepass(posInput.positionSS, material);

					if ((decalLayerMask & material.renderingLayerMask) == 0)
						clipValue -= 2.0;
				}


				float3 positionDS = TransformWorldToObject(posInput.positionWS);
				positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5, 0.5);
				if (!(all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
				{
					clipValue -= 2.0;
				}

			#ifndef SHADER_API_METAL
				clip(clipValue);
			#else
				if (clipValue > 0.0)
				{
			#endif

				float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
				float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
				float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
				positionDS.xz = positionDS.xz * scale + offset;

				input.texCoord0.xy = positionDS.xz;
				input.texCoord1.xy = positionDS.xz;
				input.texCoord2.xy = positionDS.xz;
				input.texCoord3.xy = positionDS.xz;

				float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
				if (_EnableDecalLayers)
				{
					float2 angleFade = float2(normalToWorld[1][3], normalToWorld[2][3]);

					if (angleFade.x > 0.0f)
					{
						float3 decalNormal = float3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
                        angleFadeFactor = DecodeAngleFade(dot(material.geomNormalWS, decalNormal), angleFade);
					}
				}

			#else
				posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, uint2(0, 0));
				#if defined(ASE_NEEDS_FRAG_RELATIVE_WORLD_POS)
					float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
				#else
					float3 V = float3(1.0, 1.0, 1.0);
				#endif
			#endif

				float3 positionWS = GetAbsolutePositionWS( posInput.positionWS );
				float3 positionRWS = posInput.positionWS;

				float3 worldTangent = TransformObjectToWorldDir(float3(1, 0, 0));
				float3 worldNormal = TransformObjectToWorldDir(float3(0, 1, 0));
				float3 worldBitangent = TransformObjectToWorldDir(float3(0, 0, 1));

				float4 texCoord0 = input.texCoord0;
				float4 texCoord1 = input.texCoord1;
				float4 texCoord2 = input.texCoord2;
				float4 texCoord3 = input.texCoord3;

				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float temp_output_2_0_g33 = ( _TimeParameters.x * _Caustic_Speed1 );
				float temp_output_135_0 = temp_output_2_0_g33;
				float2 texCoord3_g35 = texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 UV1_g35 = texCoord3_g35;
				float2 localTransformUVs1_g35 = TransformUVs1_g35( UV1_g35 );
				float2 temp_output_301_0 = ( localTransformUVs1_g35 * _Scale );
				float4 tex2DNode104 = tex2D( _Flowmap, ( temp_output_301_0 * _Flowmap_Scale1 ) );
				float2 appendResult108 = (float2(tex2DNode104.r , tex2DNode104.g));
				float2 lerpResult110 = lerp( temp_output_301_0 , appendResult108 , _Flowmap_Value);
				float2 FlowUV01191 = ( lerpResult110 * _Caustic_Scale1 );
				float2 panner141 = ( temp_output_135_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float temp_output_2_0_g34 = ( _TimeParameters.x * _Caustic_Speed2 );
				float temp_output_137_0 = temp_output_2_0_g34;
				float4 tex2DNode115 = tex2D( _Flowmap, ( temp_output_301_0 * _Flowmap_Scale2 ) );
				float2 appendResult116 = (float2(tex2DNode115.r , tex2DNode115.g));
				float2 lerpResult119 = lerp( temp_output_301_0 , appendResult116 , _Flowmap_Value);
				float2 FlowUV02192 = ( lerpResult119 * _Caustic_Scale2 );
				float2 panner145 = ( temp_output_137_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float temp_output_2_0_g29 = ( _TimeParameters.x * _Caustic_Speed3 );
				float temp_output_139_0 = temp_output_2_0_g29;
				float4 tex2DNode127 = tex2D( _Flowmap, ( temp_output_301_0 * _Flowmap_Scale3 ) );
				float2 appendResult123 = (float2(tex2DNode127.r , tex2DNode127.g));
				float2 lerpResult125 = lerp( temp_output_301_0 , appendResult123 , _Flowmap_Value);
				float2 FlowUV03193 = ( lerpResult125 * _Caustic_Scale3 );
				float2 panner147 = ( temp_output_139_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 panner152 = ( temp_output_139_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float2 panner155 = ( temp_output_137_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float2 panner161 = ( temp_output_135_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 temp_cast_0 = (( (0) * _GiantMask_Scale )).xx;
				float lerpResult162 = lerp( ( ( ( tex2D( _Waves, panner141 ).g + tex2D( _Waves, panner145 ).g ) * _CausticWave1_Multiply ) * tex2D( _Waves, panner147 ).g ) , ( ( ( tex2D( _Waves, panner152 ).g + tex2D( _Waves, panner155 ).g ) * _CausticWave1_Multiply ) * tex2D( _Waves, panner161 ).g ) , tex2D( _Waves, temp_cast_0 ).g);
				float Caustics251 = ( lerpResult162 * _Multiply_Caustics );
				float temp_output_2_0_g32 = ( _TimeParameters.x * ( _Caustic_Speed1 * _Caustic_Speed_BLUR ) );
				float temp_output_208_0 = temp_output_2_0_g32;
				float2 panner212 = ( temp_output_208_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float temp_output_2_0_g31 = ( _TimeParameters.x * ( _Caustic_Speed2 * _Caustic_Speed_BLUR ) );
				float temp_output_203_0 = temp_output_2_0_g31;
				float2 panner210 = ( temp_output_203_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float temp_output_2_0_g30 = ( _TimeParameters.x * ( _Caustic_Speed3 * _Caustic_Speed_BLUR ) );
				float temp_output_204_0 = temp_output_2_0_g30;
				float2 panner219 = ( temp_output_204_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 panner211 = ( temp_output_204_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float2 panner213 = ( temp_output_203_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float2 panner216 = ( temp_output_208_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 temp_cast_1 = (( ( (0) * _GiantMask_Scale ) * _Blurred_Scale )).xx;
				float lerpResult237 = lerp( ( ( ( tex2D( _BluredWaves, panner212 ).g + tex2D( _BluredWaves, panner210 ).g ) * _CausticWave1_Multiply ) * tex2D( _BluredWaves, panner219 ).g ) , ( ( ( tex2D( _BluredWaves, panner211 ).g + tex2D( _BluredWaves, panner213 ).g ) * _CausticWave1_Multiply ) * tex2D( _BluredWaves, panner216 ).g ) , tex2D( _BluredWaves, temp_cast_1 ).g);
				float BluredCaustics249 = ( ( _Multiply_Blur * lerpResult237 ) + ( _Add_Blur * 0.01 ) );
				#ifdef _ONLYBLURCAUSTIC_ON
				float staticSwitch178 = BluredCaustics249;
				#else
				float staticSwitch178 = ( Caustics251 * BluredCaustics249 );
				#endif
				float FadeRadius306 = _FadeRadius;
				float FadeStrength306 = _FadeStrength;
				float4 screenPos = packedInput.ase_texcoord1;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV22_g37 = ase_screenPosNorm.xy;
				float2 localUnStereo22_g37 = UnStereo( UV22_g37 );
				float2 break64_g36 = localUnStereo22_g37;
				float clampDepth69_g36 = SampleCameraDepth( ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch38_g36 = ( 1.0 - clampDepth69_g36 );
				#else
				float staticSwitch38_g36 = clampDepth69_g36;
				#endif
				float3 appendResult39_g36 = (float3(break64_g36.x , break64_g36.y , staticSwitch38_g36));
				float4 appendResult42_g36 = (float4((appendResult39_g36*2.0 + -1.0) , 1.0));
				float4 temp_output_43_0_g36 = mul( unity_CameraInvProjection, appendResult42_g36 );
				float3 temp_output_46_0_g36 = ( (temp_output_43_0_g36).xyz / (temp_output_43_0_g36).w );
				float3 In73_g36 = temp_output_46_0_g36;
				float3 localInvertDepthDirHD73_g36 = InvertDepthDirHD73_g36( In73_g36 );
				float4 appendResult49_g36 = (float4(localInvertDepthDirHD73_g36 , 1.0));
				float3 worldToObj312 = mul( GetWorldToObjectMatrix(), float4( GetCameraRelativePositionWS(mul( unity_CameraToWorld, appendResult49_g36 ).xyz), 1 ) ).xyz;
				float3 PositionOS306 = worldToObj312;
				float localEdgeFade306 = EdgeFade306( FadeRadius306 , FadeStrength306 , PositionOS306 );
				

				surfaceDescription.BaseColor = ( ( ( ( 2.0 * staticSwitch178 ) + ( _Add * 0.01 ) ) * localEdgeFade306 ) * _Color ).rgb;
				surfaceDescription.Alpha = _Color.a;
				surfaceDescription.NormalTS = float3( 0, 0, 1 );
				surfaceDescription.NormalAlpha = 1;
				surfaceDescription.Metallic = 0;
				surfaceDescription.Occlusion = 1;
				surfaceDescription.Smoothness = 0.5;
				surfaceDescription.MAOSAlpha = 1;
				surfaceDescription.Emission = float3( 0, 0, 0 );

				GetSurfaceData(surfaceDescription, input, V, posInput, angleFadeFactor, surfaceData);

			#if ((SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)) && defined(SHADER_API_METAL)
				} // if (clipValue > 0.0)

				clip(clipValue);
			#endif

			#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
				ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
			#else
				// Emissive need to be pre-exposed
				outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
				outEmissive.a = 1.0;
			#endif
			}

            ENDHLSL
        }

		
        Pass
		{
			
			Name "DBufferMesh"
			Tags { "LightMode"="DBufferMesh" }


			Stencil
			{
				Ref [_DecalStencilRef]
				WriteMask [_DecalStencilWriteMask]
				Comp Always
				Pass Replace
			}


			ZWrite Off
			ZTest LEqual

			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask[_DecalColorMask0]
			ColorMask[_DecalColorMask1] 1
			ColorMask[_DecalColorMask2] 2
			ColorMask[_DecalColorMask3] 3

            HLSLPROGRAM

            #pragma shader_feature_local_fragment _MATERIAL_AFFECTS_ALBEDO
            #pragma shader_feature_local_fragment _COLORMAP
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #define ASE_VERSION 19701
            #define ASE_SRP_VERSION 170003


            #pragma vertex Vert
            #pragma fragment Frag

			#pragma multi_compile_fragment DECALS_3RT DECALS_4RT
			#pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#define SUPPORT_GLOBAL_MIP_BIAS

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/DecalMeshBiasTypeEnum.cs.hlsl"

			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#pragma shader_feature_local _ONLYBLURCAUSTIC_ON


            struct AttributesMesh
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv0 : TEXCOORD0;
				
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			struct PackedVaryingsToPS
			{
				float4 positionCS : SV_POSITION;
                float3 positionRWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float4 uv0 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _FadeRadius;
            float _Add;
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
            float _FadeStrength;
            float _Caustic_Speed1;
            float _DrawOrder;
			float _NormalBlendSrc;
			float _MaskBlendSrc;
			float _DecalBlend;
			int   _DecalMeshBiasType;
            float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
            float _DecalStencilWriteMask;
            float _DecalStencilRef;
            #ifdef _MATERIAL_AFFECTS_ALBEDO
            float _AffectAlbedo;
			#endif
            #ifdef _MATERIAL_AFFECTS_NORMAL
            float _AffectNormal;
			#endif
            #ifdef _MATERIAL_AFFECTS_MASKMAP
            float _AffectAO;
			float _AffectMetal;
            float _AffectSmoothness;
			#endif
            #ifdef _MATERIAL_AFFECTS_EMISSION
            float _AffectEmission;
			#endif
            float _DecalColorMask0;
            float _DecalColorMask1;
            float _DecalColorMask2;
            float _DecalColorMask3;
            CBUFFER_END

	   		sampler2D _Waves;
	   		sampler2D _Flowmap;
	   		sampler2D _BluredWaves;
	   		float4x4 unity_CameraProjection;
	   		float4x4 unity_CameraInvProjection;
	   		float4x4 unity_WorldToCamera;
	   		float4x4 unity_CameraToWorld;


			float2 TransformUVs1_g35( float2 UV )
			{
				#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
					float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
					float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
					float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
					UV.xy = UV.xy * scale + offset;
				#endif
				return UV;
			}
			
			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
			}
			
			float3 InvertDepthDirHD73_g36( float3 In )
			{
				float3 result = In;
				#if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301 || ASE_SRP_VERSION == 70503 || ASE_SRP_VERSION == 70600 || ASE_SRP_VERSION == 70700 || ASE_SRP_VERSION == 70701 || ASE_SRP_VERSION >= 80301
				result *= float3(1,1,-1);
				#endif
				return result;
			}
			
			float EdgeFade306( float FadeRadius, float FadeStrength, float3 PositionOS )
			{
				return 1 - saturate((distance(PositionOS, 0) - FadeRadius) / (1 - FadeStrength));
			}
			

            void GetSurfaceData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, PositionInputs posInput, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
                    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                    float fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
                    float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                    float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
                    fragInputs.texCoord0.xy = fragInputs.texCoord0.xy * scale + offset;
                    fragInputs.texCoord1.xy = fragInputs.texCoord1.xy * scale + offset;
                    fragInputs.texCoord2.xy = fragInputs.texCoord2.xy * scale + offset;
                    fragInputs.texCoord3.xy = fragInputs.texCoord3.xy * scale + offset;
                    fragInputs.positionRWS = posInput.positionWS;
                    fragInputs.tangentToWorld[2].xyz = TransformObjectToWorldDir(float3(0, 1, 0));
                    fragInputs.tangentToWorld[1].xyz = TransformObjectToWorldDir(float3(0, 0, 1));
                #else
                    #ifdef LOD_FADE_CROSSFADE
                    LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                    #endif

                    float fadeFactor = 1.0;
                #endif

                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);

                #ifdef _MATERIAL_AFFECTS_EMISSION
                #endif

                #ifdef _MATERIAL_AFFECTS_ALBEDO
                    surfaceData.baseColor.xyz = surfaceDescription.BaseColor;
                    surfaceData.baseColor.w = surfaceDescription.Alpha * fadeFactor;
                #endif

                #ifdef _MATERIAL_AFFECTS_NORMAL
                    #ifdef DECAL_SURFACE_GRADIENT
                        #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
                            float3x3 tangentToWorld = transpose((float3x3)normalToWorld);
                        #else
                            float3x3 tangentToWorld = fragInputs.tangentToWorld;
                        #endif

                        surfaceData.normalWS.xyz = SurfaceGradientFromTangentSpaceNormalAndFromTBN(surfaceDescription.NormalTS.xyz, tangentToWorld[0], tangentToWorld[1]);
                    #else
                        #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
                            surfaceData.normalWS.xyz = mul((float3x3)normalToWorld, surfaceDescription.NormalTS);
                        #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_PREVIEW)

                            surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(surfaceDescription.NormalTS, fragInputs.tangentToWorld));
                        #endif
                    #endif

                    surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;
                #else
                    #if (SHADERPASS == SHADERPASS_FORWARD_PREVIEW)
                        #ifdef DECAL_SURFACE_GRADIENT
                            surfaceData.normalWS.xyz = float3(0.0, 0.0, 0.0);
                        #else
                            surfaceData.normalWS.xyz = normalize(TransformTangentToWorld(float3(0.0, 0.0, 0.1), fragInputs.tangentToWorld));
                        #endif
                    #endif
                #endif

                #ifdef _MATERIAL_AFFECTS_MASKMAP
                    surfaceData.mask.z = surfaceDescription.Smoothness;
                    surfaceData.mask.w = surfaceDescription.MAOSAlpha * fadeFactor;

                    #ifdef DECALS_4RT
                        surfaceData.mask.x = surfaceDescription.Metallic;
                        surfaceData.mask.y = surfaceDescription.Occlusion;
                        surfaceData.MAOSBlend.x = surfaceDescription.MAOSAlpha * fadeFactor;
                        surfaceData.MAOSBlend.y = surfaceDescription.MAOSAlpha * fadeFactor;
                    #endif

                #endif
            }

			PackedVaryingsToPS Vert(AttributesMesh inputMesh  )
			{
				PackedVaryingsToPS output;

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				inputMesh.tangentOS = float4( 1, 0, 0, -1);
				inputMesh.normalOS = float3( 0, 1, 0 );

				float4 ase_clipPos = TransformWorldToHClip( TransformObjectToWorld(inputMesh.positionOS));
				float4 screenPos = ComputeScreenPos( ase_clipPos , _ProjectionParams.x );
				output.ase_texcoord4 = screenPos;
				

				inputMesh.normalOS = inputMesh.normalOS;
				inputMesh.tangentOS = inputMesh.tangentOS;

				float3 worldSpaceBias = 0.0f;

				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
				{
					float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
					float3 V = GetWorldSpaceNormalizeViewDir(positionRWS);
					worldSpaceBias = V * (_DecalMeshViewBias);
				}

				float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS) + worldSpaceBias;
				float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(inputMesh.tangentOS.xyz), inputMesh.tangentOS.w);

				output.positionRWS.xyz = positionRWS;
				output.positionCS = TransformWorldToHClip(positionRWS);
				output.normalWS.xyz = normalWS;
				output.tangentWS.xyzw = tangentWS;
				output.uv0.xyzw = inputMesh.uv0;

				if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
				{
					#if UNITY_REVERSED_Z
						output.positionCS.z -= _DecalMeshDepthBias;
					#else
						output.positionCS.z += _DecalMeshDepthBias;
					#endif
				}


				return output;
			}

			void Frag(  PackedVaryingsToPS packedInput,
			#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
				OUTPUT_DBUFFER(outDBuffer)
			#else
				out float4 outEmissive : SV_Target0
			#endif
			
			)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				UNITY_SETUP_INSTANCE_ID(packedInput);

                FragInputs input;
                ZERO_INITIALIZE(FragInputs, input);

                input.tangentToWorld = k_identity3x3;
                input.positionSS = packedInput.positionCS;

                input.positionRWS = packedInput.positionRWS.xyz;

                input.tangentToWorld = BuildTangentToWorld(packedInput.tangentWS.xyzw, packedInput.normalWS.xyz);
                input.texCoord0 = packedInput.uv0.xyzw;

				DecalSurfaceData surfaceData;
				float clipValue = 1.0;
				float angleFadeFactor = 1.0;

				PositionInputs posInput;
			#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)

				float depth = LoadCameraDepth(input.positionSS.xy);
				posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

				DecalPrepassData material;
				ZERO_INITIALIZE(DecalPrepassData, material);
				if (_EnableDecalLayers)
				{
					uint decalLayerMask = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal).x);

					DecodeFromDecalPrepass(posInput.positionSS, material);

					if ((decalLayerMask & material.renderingLayerMask) == 0)
						clipValue -= 2.0;
				}

				float3 positionDS = TransformWorldToObject(posInput.positionWS);
				positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5, 0.5);
				if (!(all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
				{
					clipValue -= 2.0;
				}

			#ifndef SHADER_API_METAL
				clip(clipValue);
			#else
				if (clipValue > 0.0)
				{
			#endif

				float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
				float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
				float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
				positionDS.xz = positionDS.xz * scale + offset;

				input.texCoord0.xy = positionDS.xz;
				input.texCoord1.xy = positionDS.xz;
				input.texCoord2.xy = positionDS.xz;
				input.texCoord3.xy = positionDS.xz;

				float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

				if (_EnableDecalLayers)
				{
					float2 angleFade = float2(normalToWorld[1][3], normalToWorld[2][3]);
					if (angleFade.x > 0.0f)
					{
						float3 decalNormal = float3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
                        angleFadeFactor = DecodeAngleFade(dot(material.geomNormalWS, decalNormal), angleFade);
					}
				}

			#else
				posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, uint2(0, 0));
				float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
			#endif

				float3 positionWS = GetAbsolutePositionWS( posInput.positionWS );
				float3 positionRWS = posInput.positionWS;

				float3 worldTangent = TransformObjectToWorldDir(float3(1, 0, 0));
				float3 worldNormal = TransformObjectToWorldDir(float3(0, 1, 0));
				float3 worldBitangent = TransformObjectToWorldDir(float3(0, 0, 1));

				float4 texCoord0 = input.texCoord0;
				float4 texCoord1 = input.texCoord1;
				float4 texCoord2 = input.texCoord2;
				float4 texCoord3 = input.texCoord3;

				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float temp_output_2_0_g33 = ( _TimeParameters.x * _Caustic_Speed1 );
				float temp_output_135_0 = temp_output_2_0_g33;
				float2 texCoord3_g35 = texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 UV1_g35 = texCoord3_g35;
				float2 localTransformUVs1_g35 = TransformUVs1_g35( UV1_g35 );
				float2 temp_output_301_0 = ( localTransformUVs1_g35 * _Scale );
				float4 tex2DNode104 = tex2D( _Flowmap, ( temp_output_301_0 * _Flowmap_Scale1 ) );
				float2 appendResult108 = (float2(tex2DNode104.r , tex2DNode104.g));
				float2 lerpResult110 = lerp( temp_output_301_0 , appendResult108 , _Flowmap_Value);
				float2 FlowUV01191 = ( lerpResult110 * _Caustic_Scale1 );
				float2 panner141 = ( temp_output_135_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float temp_output_2_0_g34 = ( _TimeParameters.x * _Caustic_Speed2 );
				float temp_output_137_0 = temp_output_2_0_g34;
				float4 tex2DNode115 = tex2D( _Flowmap, ( temp_output_301_0 * _Flowmap_Scale2 ) );
				float2 appendResult116 = (float2(tex2DNode115.r , tex2DNode115.g));
				float2 lerpResult119 = lerp( temp_output_301_0 , appendResult116 , _Flowmap_Value);
				float2 FlowUV02192 = ( lerpResult119 * _Caustic_Scale2 );
				float2 panner145 = ( temp_output_137_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float temp_output_2_0_g29 = ( _TimeParameters.x * _Caustic_Speed3 );
				float temp_output_139_0 = temp_output_2_0_g29;
				float4 tex2DNode127 = tex2D( _Flowmap, ( temp_output_301_0 * _Flowmap_Scale3 ) );
				float2 appendResult123 = (float2(tex2DNode127.r , tex2DNode127.g));
				float2 lerpResult125 = lerp( temp_output_301_0 , appendResult123 , _Flowmap_Value);
				float2 FlowUV03193 = ( lerpResult125 * _Caustic_Scale3 );
				float2 panner147 = ( temp_output_139_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 panner152 = ( temp_output_139_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float2 panner155 = ( temp_output_137_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float2 panner161 = ( temp_output_135_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 temp_cast_0 = (( (0) * _GiantMask_Scale )).xx;
				float lerpResult162 = lerp( ( ( ( tex2D( _Waves, panner141 ).g + tex2D( _Waves, panner145 ).g ) * _CausticWave1_Multiply ) * tex2D( _Waves, panner147 ).g ) , ( ( ( tex2D( _Waves, panner152 ).g + tex2D( _Waves, panner155 ).g ) * _CausticWave1_Multiply ) * tex2D( _Waves, panner161 ).g ) , tex2D( _Waves, temp_cast_0 ).g);
				float Caustics251 = ( lerpResult162 * _Multiply_Caustics );
				float temp_output_2_0_g32 = ( _TimeParameters.x * ( _Caustic_Speed1 * _Caustic_Speed_BLUR ) );
				float temp_output_208_0 = temp_output_2_0_g32;
				float2 panner212 = ( temp_output_208_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float temp_output_2_0_g31 = ( _TimeParameters.x * ( _Caustic_Speed2 * _Caustic_Speed_BLUR ) );
				float temp_output_203_0 = temp_output_2_0_g31;
				float2 panner210 = ( temp_output_203_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float temp_output_2_0_g30 = ( _TimeParameters.x * ( _Caustic_Speed3 * _Caustic_Speed_BLUR ) );
				float temp_output_204_0 = temp_output_2_0_g30;
				float2 panner219 = ( temp_output_204_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 panner211 = ( temp_output_204_0 * float2( -0.012,-0.0134 ) + FlowUV01191);
				float2 panner213 = ( temp_output_203_0 * float2( 0.015,0.02 ) + FlowUV02192);
				float2 panner216 = ( temp_output_208_0 * float2( 0.01,-0.04 ) + FlowUV03193);
				float2 temp_cast_1 = (( ( (0) * _GiantMask_Scale ) * _Blurred_Scale )).xx;
				float lerpResult237 = lerp( ( ( ( tex2D( _BluredWaves, panner212 ).g + tex2D( _BluredWaves, panner210 ).g ) * _CausticWave1_Multiply ) * tex2D( _BluredWaves, panner219 ).g ) , ( ( ( tex2D( _BluredWaves, panner211 ).g + tex2D( _BluredWaves, panner213 ).g ) * _CausticWave1_Multiply ) * tex2D( _BluredWaves, panner216 ).g ) , tex2D( _BluredWaves, temp_cast_1 ).g);
				float BluredCaustics249 = ( ( _Multiply_Blur * lerpResult237 ) + ( _Add_Blur * 0.01 ) );
				#ifdef _ONLYBLURCAUSTIC_ON
				float staticSwitch178 = BluredCaustics249;
				#else
				float staticSwitch178 = ( Caustics251 * BluredCaustics249 );
				#endif
				float FadeRadius306 = _FadeRadius;
				float FadeStrength306 = _FadeStrength;
				float4 screenPos = packedInput.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV22_g37 = ase_screenPosNorm.xy;
				float2 localUnStereo22_g37 = UnStereo( UV22_g37 );
				float2 break64_g36 = localUnStereo22_g37;
				float clampDepth69_g36 = SampleCameraDepth( ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch38_g36 = ( 1.0 - clampDepth69_g36 );
				#else
				float staticSwitch38_g36 = clampDepth69_g36;
				#endif
				float3 appendResult39_g36 = (float3(break64_g36.x , break64_g36.y , staticSwitch38_g36));
				float4 appendResult42_g36 = (float4((appendResult39_g36*2.0 + -1.0) , 1.0));
				float4 temp_output_43_0_g36 = mul( unity_CameraInvProjection, appendResult42_g36 );
				float3 temp_output_46_0_g36 = ( (temp_output_43_0_g36).xyz / (temp_output_43_0_g36).w );
				float3 In73_g36 = temp_output_46_0_g36;
				float3 localInvertDepthDirHD73_g36 = InvertDepthDirHD73_g36( In73_g36 );
				float4 appendResult49_g36 = (float4(localInvertDepthDirHD73_g36 , 1.0));
				float3 worldToObj312 = mul( GetWorldToObjectMatrix(), float4( GetCameraRelativePositionWS(mul( unity_CameraToWorld, appendResult49_g36 ).xyz), 1 ) ).xyz;
				float3 PositionOS306 = worldToObj312;
				float localEdgeFade306 = EdgeFade306( FadeRadius306 , FadeStrength306 , PositionOS306 );
				

				surfaceDescription.BaseColor = ( ( ( ( 2.0 * staticSwitch178 ) + ( _Add * 0.01 ) ) * localEdgeFade306 ) * _Color ).rgb;
				surfaceDescription.Alpha = _Color.a;
				surfaceDescription.NormalTS = float3( 0, 0, 1 );
				surfaceDescription.NormalAlpha = 1;
				surfaceDescription.Metallic = 0;
				surfaceDescription.Occlusion = 1;
				surfaceDescription.Smoothness = 0.5;
				surfaceDescription.MAOSAlpha = 1;
				surfaceDescription.Emission = float3( 0, 0, 0 );

				GetSurfaceData(surfaceDescription, input, V, posInput, angleFadeFactor, surfaceData);

			#if ((SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)) && defined(SHADER_API_METAL)
				}

				clip(clipValue);
			#endif

			#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
				ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
			#else
				outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
				outEmissive.a = 1.0;
			#endif
			}
            ENDHLSL
        }

		
        Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

            Cull Back

            HLSLPROGRAM
		    #pragma shader_feature_local_fragment _MATERIAL_AFFECTS_ALBEDO
		    #pragma shader_feature_local_fragment _COLORMAP
		    #pragma multi_compile _ LOD_FADE_CROSSFADE
		    #define ASE_VERSION 19701
		    #define ASE_SRP_VERSION 170003

		    #pragma exclude_renderers glcore gles gles3 ps4 ps5 
			#pragma editor_sync_compilation

			#pragma vertex Vert
			#pragma fragment Frag

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENEPICKINGPASS 1
			#define SUPPORT_GLOBAL_MIP_BIAS

            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

			// Require _SelectionID variable
            float4 _SelectionID;

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"



			

            struct AttributesMesh
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				
                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryingsToPS
			{
				float4 positionCS : SV_POSITION;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _FadeRadius;
            float _Add;
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
            float _FadeStrength;
            float _Caustic_Speed1;
            float _DrawOrder;
			float _NormalBlendSrc;
			float _MaskBlendSrc;
			float _DecalBlend;
			int   _DecalMeshBiasType;
            float _DecalMeshDepthBias;
			float _DecalMeshViewBias;
            float _DecalStencilWriteMask;
            float _DecalStencilRef;
            #ifdef _MATERIAL_AFFECTS_ALBEDO
            float _AffectAlbedo;
			#endif
            #ifdef _MATERIAL_AFFECTS_NORMAL
            float _AffectNormal;
			#endif
            #ifdef _MATERIAL_AFFECTS_MASKMAP
            float _AffectAO;
			float _AffectMetal;
            float _AffectSmoothness;
			#endif
            #ifdef _MATERIAL_AFFECTS_EMISSION
            float _AffectEmission;
			#endif
            float _DecalColorMask0;
            float _DecalColorMask1;
            float _DecalColorMask2;
            float _DecalColorMask3;
            CBUFFER_END

	   		

			
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"

			PackedVaryingsToPS Vert(AttributesMesh inputMesh )
			{
				PackedVaryingsToPS output;

				UNITY_SETUP_INSTANCE_ID(inputMesh);
				UNITY_TRANSFER_INSTANCE_ID(inputMesh, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				inputMesh.tangentOS = float4( 1, 0, 0, -1);
				inputMesh.normalOS = float3( 0, 1, 0 );

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
				float3 defaultVertexValue = inputMesh.positionOS.xyz;
				#else
				float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
				inputMesh.positionOS.xyz = vertexValue;
				#else
				inputMesh.positionOS.xyz += vertexValue;
				#endif

				float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS) ;
				output.positionCS = TransformWorldToHClip(positionRWS);

				return output;
			}

			void Frag(  PackedVaryingsToPS packedInput,
						out float4 outColor : SV_Target0
						
						)
			{
				

				//This port is needed as templates always require fragment ports to correctly work...this will be discarded by the compiler
				float3 baseColor = float3( 0,0,0);
				outColor = _SelectionID;
			}

            ENDHLSL
        }
		
    }
    CustomEditor "Rendering.HighDefinition.DecalShaderGraphGUI"
	
	Fallback Off
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;252;-1979.243,1569.371;Inherit;False;1979.829;1647.016;Caustics;34;137;198;139;194;135;197;195;152;145;141;155;144;196;143;153;199;154;161;147;158;170;148;172;146;149;159;157;171;150;160;177;162;175;251;;0.3254717,0.7563778,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;134;-1982.273,389.2037;Inherit;False;1351.281;1170.958;CausticFlowmap;25;193;192;191;126;125;129;123;120;111;119;127;112;121;110;131;116;122;108;130;104;115;113;105;107;114;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;250;-1985.508,3223.648;Inherit;False;2535.213;1636.413;BluredCaustics;43;248;245;247;246;204;209;203;205;206;207;208;212;211;213;210;217;214;215;230;223;225;224;220;222;232;231;219;216;234;233;221;227;228;235;229;236;239;243;237;242;238;241;249;;0.4534977,0.745283,0.5830489,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;151;-2339.068,3222.917;Inherit;False;Property;_CausticWave1_Multiply;CausticWave1_Multiply;6;0;Create;True;0;0;0;False;0;False;0.75;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;242;55.26606,4106.439;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;148;-1030.128,1766.36;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;160;-746.642,2814.414;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;146;-1350.52,1999.976;Inherit;True;Property;_Waves2;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-884.7041,1879.679;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;143;-1349.514,1619.371;Inherit;True;Property;_Waves;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;238;39.7928,3939.129;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;162;-533.7408,2289.358;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;177;-573.5305,2409.786;Inherit;False;Property;_Multiply_Caustics;Multiply_Caustics;7;0;Create;True;0;0;0;False;0;False;1.12;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;171;-1354.884,2986.387;Inherit;True;Property;_Waves6;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;235;-298.0436,4464.523;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;237;-125.0375,3977.997;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;-897.8998,2490.691;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;150;-721.1,2021.626;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;219;-1165.47,3672.372;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;241;202.8185,4004.825;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;147;-1560.517,2028.794;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;154;-1355.985,2605.687;Inherit;True;Property;_Waves4;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;234;-449.3009,4140.8;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;161;-1564.593,2823.254;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;221;-1047.326,4659.973;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;251;-223.4135,2281.977;Inherit;False;Caustics;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;158;-1034.682,2554.529;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;229;-904.2902,4630.061;Inherit;True;Property;_Waves12;Waves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;110;-1154.452,508.155;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;239;-135.2072,3897.129;Inherit;False;Property;_Multiply_Blur;Multiply_Blur;21;0;Create;True;0;0;0;False;0;False;1.12;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;136;-2299.717,2946.888;Inherit;False;Property;_Caustic_Speed1;Caustic_Speed1;11;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;243;-95.03092,4100.616;Inherit;False;Property;_Add_Blur;Add_Blur;2;0;Create;True;0;0;0;False;0;False;8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;111;-987.3141,550.1834;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;157;-1354.595,2794.434;Inherit;True;Property;_Waves5;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.DynamicAppendNode;116;-1299.041,960.4595;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;170;-1661.384,3012.16;Inherit;False;-1;;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;233;-436.1054,3529.788;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;228;-899.926,3643.648;Inherit;True;Property;_Waves11;Waves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;249;325.7055,4000.058;Inherit;False;BluredCaustics;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;227;-904.0009,4438.107;Inherit;True;Property;_Waves10;Waves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;220;-1186.915,4608.595;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;216;-1169.545,4466.833;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.01,-0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;175;-361.3237,2287.769;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;192;-836.6208,922.8696;Inherit;False;FlowUV02;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;131;-1544.753,744.6281;Inherit;False;Property;_Flowmap_Value;Flowmap_Value;15;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;127;-1575.238,1306.921;Inherit;True;Property;_Flowmap2;Flowmap;14;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;104;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.DynamicAppendNode;108;-1296.07,581.5396;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-1175.315,631.1837;Inherit;False;Property;_Caustic_Scale1;Caustic_Scale1;8;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;245;-1685.025,3795.077;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-1167.183,1385.333;Inherit;False;Property;_Caustic_Scale3;Caustic_Scale3;10;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-2301.853,3084.904;Inherit;False;Property;_Caustic_Speed3;Caustic_Speed3;13;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-2299.033,3016.839;Inherit;False;Property;_Caustic_Speed2;Caustic_Speed2;12;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;196;-1749.248,2023.74;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;199;-1763.742,2820.54;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;172;-1493.152,3016.232;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;236;-272.5014,3671.735;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;125;-1146.32,1262.304;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;225;-902.6351,4066.722;Inherit;True;Property;_Waves9;Waves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;230;-901.3534,3273.648;Inherit;True;Property;_BluredWaves;BluredWaves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;113;-1717.273,960.4485;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;104;-1592.032,551.9824;Inherit;True;Property;_Flowmap;Flowmap;14;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.DynamicAppendNode;123;-1287.938,1335.689;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;144;-1351.431,1817.518;Inherit;True;Property;_Waves1;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;246;-1685.025,3883;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1714.303,581.5289;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;114;-1904.273,1074.448;Inherit;False;Property;_Flowmap_Scale2;Flowmap_Scale2;17;0;Create;True;0;0;0;False;0;False;2.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;115;-1587.34,932.6912;Inherit;True;Property;_Flowmap1;Flowmap;14;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;104;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;130;-1893.171,1449.677;Inherit;False;Property;_Flowmap_Scale3;Flowmap_Scale3;18;0;Create;True;0;0;0;False;0;False;3.34;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-1706.171,1335.677;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;191;-836.1929,546.7066;Inherit;False;FlowUV01;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-1901.303,695.5291;Inherit;False;Property;_Flowmap_Scale1;Flowmap_Scale1;16;0;Create;True;0;0;0;False;0;False;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;217;-1365.428,4667.496;Inherit;False;-1;;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-979.182,1304.333;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;210;-1164.381,3489.915;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;119;-1157.423,887.0742;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;215;-1368.694,4464.12;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-1178.285,1010.103;Inherit;False;Property;_Caustic_Scale2;Caustic_Scale2;9;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;223;-905.3907,4249.359;Inherit;True;Property;_Waves7;Waves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;248;-1935.508,3898.808;Inherit;False;Property;_Caustic_Speed_BLUR;Caustic_Speed_BLUR;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;145;-1559.428,1846.337;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-990.2841,929.103;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;214;-1354.201,3667.319;Inherit;False;193;FlowUV03;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;224;-900.8369,3460.19;Inherit;True;Property;_Waves8;Waves;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;230;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.PannerNode;152;-1564.699,2449.361;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;222;-1213.724,4736.932;Inherit;False;Property;_Blurred_Scale;Blurred_Scale;22;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;232;-581.5286,3416.469;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;153;-1353.229,2423.049;Inherit;True;Property;_Waves3;Waves;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;143;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;231;-586.0824,4204.638;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;173;-2301.478,3154.969;Inherit;False;Property;_GiantMask_Scale;GiantMask_Scale;3;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;139;-1924.387,2349.709;Inherit;False;TimeWithSpeedVariable;-1;;29;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.PannerNode;213;-1168.935,4278.085;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;141;-1560.145,1661.192;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;209;-1371.823,4273.807;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;204;-1529.34,3993.289;Inherit;False;TimeWithSpeedVariable;-1;;30;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;247;-1685.025,3969.57;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;197;-1758.685,2445.178;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;155;-1563.982,2634.505;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.015,0.02;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;206;-1348.753,3485.582;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;203;-1531.511,3891.236;Inherit;False;TimeWithSpeedVariable;-1;;31;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.GetLocalVarNode;205;-1349.144,3298.389;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;208;-1534.196,3787.285;Inherit;False;TimeWithSpeedVariable;-1;;32;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.FunctionNode;135;-1929.243,2143.707;Inherit;False;TimeWithSpeedVariable;-1;;33;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.GetLocalVarNode;195;-1743.801,1842.003;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;211;-1169.652,4092.94;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;194;-1744.192,1654.81;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;212;-1165.098,3304.771;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.012,-0.0134;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;137;-1926.559,2247.657;Inherit;False;TimeWithSpeedVariable;-1;;34;6c6258ddca69992488e84eb5db994d7b;0;1;3;FLOAT;0;False;2;FLOAT;0;FLOAT;5
Node;AmplifyShaderEditor.GetLocalVarNode;198;-1766.871,2630.227;Inherit;False;192;FlowUV02;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;193;-819.587,1301.304;Inherit;False;FlowUV03;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;207;-1363.638,4088.757;Inherit;False;191;FlowUV01;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;181;2203.878,1272.339;Inherit;False;Property;_Add;Add;1;0;Create;True;0;0;0;False;0;False;8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;180;2507.387,1182.205;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;301;-2347.199,932.4617;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;296;-2531.044,928.5302;Inherit;False;HDRP Decal UVs;-1;;35;5e2643b1c5c9c214d8c438e23555b8af;1,4,1;1;12;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;302;-2487.76,1003.624;Inherit;False;Property;_Scale;Scale;4;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;306;1961.904,1725.608;Inherit;False;return 1 - saturate((distance(PositionOS, 0) - FadeRadius) / (1 - FadeStrength))@;1;Create;3;False;FadeRadius;FLOAT;0;In;;Inherit;False;False;FadeStrength;FLOAT;0;In;;Inherit;False;False;PositionOS;FLOAT3;0,0,0;In;;Inherit;False;EdgeFade;True;False;0;;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;308;1742.134,1669.939;Inherit;False;Property;_FadeRadius;FadeRadius;24;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;309;1737.134,1738.939;Inherit;False;Property;_FadeStrength;FadeStrength;25;0;Create;True;0;0;0;False;0;False;0.8;0.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;307;1280.42,1897.604;Inherit;False;Reconstruct World Position From Depth;-1;;36;e7094bcbcc80eb140b2a3dbe6a861de8;0;0;1;FLOAT4;0
Node;AmplifyShaderEditor.TransformPositionNode;312;1613.157,1896.611;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;310;2860.012,1241.383;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;179;2192.968,1171.745;Inherit;False;2;2;0;FLOAT;2;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;176;2352.878,1262.562;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;303;3023.728,1229.404;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;304;2780.728,1369.404;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;254;1518.15,1173.228;Inherit;False;249;BluredCaustics;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;244;1751.244,1115.832;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;178;1922.156,1200.336;Inherit;False;Property;_OnlyBlurCaustic;OnlyBlurCaustic;19;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;253;1521.607,1072.927;Inherit;False;251;Caustics;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;298;2715.65,1119.85;Float;False;False;-1;2;Rendering.HighDefinition.DecalShaderGraphGUI;0;16;New Amplify Shader;d345501910c196f4a81c9eff8a0a5ad7;True;DecalProjectorForwardEmissive;0;1;DecalProjectorForwardEmissive;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;True;7;d3d11;metal;vulkan;xboxone;xboxseries;playstation;switch;0;False;True;8;5;False;;1;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;False;False;False;False;False;False;False;True;True;0;True;_DecalStencilRef;255;False;;255;True;_DecalStencilWriteMask;7;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;2;False;;False;True;1;LightMode=DecalProjectorForwardEmissive;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;299;2715.65,1119.85;Float;False;False;-1;2;Rendering.HighDefinition.DecalShaderGraphGUI;0;16;New Amplify Shader;d345501910c196f4a81c9eff8a0a5ad7;True;DBufferMesh;0;2;DBufferMesh;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;True;7;d3d11;metal;vulkan;xboxone;xboxseries;playstation;switch;0;False;True;2;5;False;;10;False;;1;0;False;;10;False;;False;False;True;2;5;False;;10;False;;1;0;False;;10;False;;False;False;True;2;5;False;;10;False;;1;0;False;;10;False;;False;False;True;1;0;False;;6;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;0;True;_DecalStencilRef;255;False;;255;True;_DecalStencilWriteMask;7;False;;3;False;;0;False;;0;False;;7;False;;3;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;False;True;1;LightMode=DBufferMesh;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;300;2715.65,1119.85;Float;False;False;-1;2;Rendering.HighDefinition.DecalShaderGraphGUI;0;16;New Amplify Shader;d345501910c196f4a81c9eff8a0a5ad7;True;DecalMeshForwardEmissive;0;3;DecalMeshForwardEmissive;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;True;7;d3d11;metal;vulkan;xboxone;xboxseries;playstation;switch;0;False;True;8;5;False;;1;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;0;True;_DecalStencilRef;255;False;;255;True;_DecalStencilWriteMask;7;False;;3;False;;0;False;;0;False;;7;False;;3;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;False;True;1;LightMode=DecalMeshForwardEmissive;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;297;3466.511,1191.311;Float;False;True;-1;2;Rendering.HighDefinition.DecalShaderGraphGUI;0;22;Shader_CausticDecal;d345501910c196f4a81c9eff8a0a5ad7;True;DBufferProjector;0;0;DBufferProjector;11;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;True;7;d3d11;metal;vulkan;xboxone;xboxseries;playstation;switch;0;True;True;1;7;False;;1;False;;0;0;False;;10;False;;False;True;True;1;7;False;;1;False;;0;0;False;;10;False;;False;True;True;1;7;False;;1;False;;0;0;False;;10;False;;False;True;True;1;0;False;;6;False;;0;1;False;;0;False;;False;False;True;True;1;False;;False;False;False;False;False;False;False;False;False;True;True;0;True;_DecalStencilRef;255;False;;255;True;_DecalStencilWriteMask;7;False;;3;False;;0;False;;0;False;;7;False;;3;False;;0;False;;0;False;;True;True;2;False;;True;2;False;;False;True;1;LightMode=DBufferProjector;False;False;0;;0;0;Standard;7;Affect BaseColor;1;0;Affect Normal;0;638183706942115755;Affect Metal;0;638183706951661490;Affect AO;0;638183706958222587;Affect Smoothness;0;638183706964458528;Affect Emission;0;638183722965928819;Support LOD CrossFade;1;0;0;5;True;False;True;False;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;313;3466.511,1231.311;Float;False;False;-1;2;Rendering.HighDefinition.DecalShaderGraphGUI;0;1;New Amplify Shader;d345501910c196f4a81c9eff8a0a5ad7;True;ScenePickingPass;0;4;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;True;7;d3d11;metal;vulkan;xboxone;xboxseries;playstation;switch;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;True;7;d3d11;metal;vulkan;xboxone;xboxseries;playstation;switch;0;;0;0;Standard;0;False;0
WireConnection;242;0;243;0
WireConnection;148;0;143;2
WireConnection;148;1;144;2
WireConnection;160;0;159;0
WireConnection;160;1;157;2
WireConnection;146;1;147;0
WireConnection;149;0;148;0
WireConnection;149;1;151;0
WireConnection;143;1;141;0
WireConnection;238;0;239;0
WireConnection;238;1;237;0
WireConnection;162;0;150;0
WireConnection;162;1;160;0
WireConnection;162;2;171;2
WireConnection;171;1;172;0
WireConnection;235;0;234;0
WireConnection;235;1;227;2
WireConnection;237;0;236;0
WireConnection;237;1;235;0
WireConnection;237;2;229;2
WireConnection;159;0;158;0
WireConnection;159;1;151;0
WireConnection;150;0;149;0
WireConnection;150;1;146;2
WireConnection;219;0;214;0
WireConnection;219;1;204;0
WireConnection;241;0;238;0
WireConnection;241;1;242;0
WireConnection;147;0;196;0
WireConnection;147;1;139;0
WireConnection;154;1;155;0
WireConnection;234;0;231;0
WireConnection;234;1;151;0
WireConnection;161;0;199;0
WireConnection;161;1;135;0
WireConnection;221;0;220;0
WireConnection;221;1;222;0
WireConnection;251;0;175;0
WireConnection;158;0;153;2
WireConnection;158;1;154;2
WireConnection;229;1;221;0
WireConnection;110;0;301;0
WireConnection;110;1;108;0
WireConnection;110;2;131;0
WireConnection;111;0;110;0
WireConnection;111;1;112;0
WireConnection;157;1;161;0
WireConnection;116;0;115;1
WireConnection;116;1;115;2
WireConnection;233;0;232;0
WireConnection;233;1;151;0
WireConnection;228;1;219;0
WireConnection;249;0;241;0
WireConnection;227;1;216;0
WireConnection;220;0;217;0
WireConnection;220;1;173;0
WireConnection;216;0;215;0
WireConnection;216;1;208;0
WireConnection;175;0;162;0
WireConnection;175;1;177;0
WireConnection;192;0;120;0
WireConnection;127;1;122;0
WireConnection;108;0;104;1
WireConnection;108;1;104;2
WireConnection;245;0;136;0
WireConnection;245;1;248;0
WireConnection;172;0;170;0
WireConnection;172;1;173;0
WireConnection;236;0;233;0
WireConnection;236;1;228;2
WireConnection;125;0;301;0
WireConnection;125;1;123;0
WireConnection;125;2;131;0
WireConnection;225;1;211;0
WireConnection;230;1;212;0
WireConnection;113;0;301;0
WireConnection;113;1;114;0
WireConnection;104;1;105;0
WireConnection;123;0;127;1
WireConnection;123;1;127;2
WireConnection;144;1;145;0
WireConnection;246;0;138;0
WireConnection;246;1;248;0
WireConnection;105;0;301;0
WireConnection;105;1;107;0
WireConnection;115;1;113;0
WireConnection;122;0;301;0
WireConnection;122;1;130;0
WireConnection;191;0;111;0
WireConnection;126;0;125;0
WireConnection;126;1;129;0
WireConnection;210;0;206;0
WireConnection;210;1;203;0
WireConnection;119;0;301;0
WireConnection;119;1;116;0
WireConnection;119;2;131;0
WireConnection;223;1;213;0
WireConnection;145;0;195;0
WireConnection;145;1;137;0
WireConnection;120;0;119;0
WireConnection;120;1;121;0
WireConnection;224;1;210;0
WireConnection;152;0;197;0
WireConnection;152;1;139;0
WireConnection;232;0;230;2
WireConnection;232;1;224;2
WireConnection;153;1;152;0
WireConnection;231;0;225;2
WireConnection;231;1;223;2
WireConnection;139;3;140;0
WireConnection;213;0;209;0
WireConnection;213;1;203;0
WireConnection;141;0;194;0
WireConnection;141;1;135;0
WireConnection;204;3;247;0
WireConnection;247;0;140;0
WireConnection;247;1;248;0
WireConnection;155;0;198;0
WireConnection;155;1;137;0
WireConnection;203;3;246;0
WireConnection;208;3;245;0
WireConnection;135;3;136;0
WireConnection;211;0;207;0
WireConnection;211;1;204;0
WireConnection;212;0;205;0
WireConnection;212;1;208;0
WireConnection;137;3;138;0
WireConnection;193;0;126;0
WireConnection;180;0;179;0
WireConnection;180;1;176;0
WireConnection;301;0;296;0
WireConnection;301;1;302;0
WireConnection;306;0;308;0
WireConnection;306;1;309;0
WireConnection;306;2;312;0
WireConnection;312;0;307;0
WireConnection;310;0;180;0
WireConnection;310;1;306;0
WireConnection;179;1;178;0
WireConnection;176;0;181;0
WireConnection;303;0;310;0
WireConnection;303;1;304;0
WireConnection;244;0;253;0
WireConnection;244;1;254;0
WireConnection;178;1;244;0
WireConnection;178;0;254;0
WireConnection;297;0;303;0
WireConnection;297;1;304;4
ASEEND*/
//CHKSM=EA491C07976708BE704E251D6F204167BB128BA9