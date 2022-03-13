// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

Shader "Hidden/PixelinearAccelerator/Wireframe/URP Wireframe Unlit (From TexCoords)"
{
	Properties
	{
		_Width("Width", Range(0.0, 20.0)) = 10
		_FalloffWidth("Falloff Width", Range(0.0, 1.0)) = 1
		_EdgeColor("Edge Color", Color) = (.75, .75, .75, 1)

		[Toggle(WIREFRAME_CLIP)]
		_WireframeClip("Alpha Clip", Float) = 0
		_WireframeCutoff("Alpha Clip Width", Range(0.0, 20.0)) = 5

		[Toggle(WIREFRAME_DASH)]
		_WireframeDash("Wireframe Dash", Float) = 0
		_DashLength("Dash Length", Range(0.0, 500.)) = 200.
		_EmptyLength("Empty Length", Range(0.0, 500.)) = 200.

		[Toggle(WIREFRAME_TEXTURE)]
		_ApplyTexture("Apply Texture", Float) = 0

		_WireframeTex("Wireframe Texture", 2D) = "white" {}

		_TexLength("Texture Length", Range(0.0, 500.)) = 200.

		[Toggle(WIREFRAME_WORLD)]
		_WorldSpaceReference("World Space Reference", Float) = 0

		[Toggle(WIREFRAME_BEHIND_DEPTH_FADE)]
		_BehindDepthFade("Fade FurtherBehind Objects", Float) = 0

		_DepthFadeDistance("Depth Fade Distance", Range(0.0, 50.)) = 10.

		[Toggle(WIREFRAME_FRESNEL_EFFECT)]
		_WireframeFresnelEffect("Edge Wireframe", Float) = 0

		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
	}

	CustomEditor "PixelinearAccelerator.WireframeRendering.Editor.CustomShaderGUI.WireframeRenderingShaderGUI"

	// The SubShader block containing the Shader code. 
	SubShader
	{
		// SubShader Tags define when and under which conditions a SubShader block or a pass is executed.
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

		Cull [_Cull]
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			// The HLSL code block. Unity SRP uses the HLSL language.
			HLSLPROGRAM

			// This line defines the name of the vertex shader. 
			#pragma vertex vert
			// This line defines the name of the fragment shader. 
			#pragma fragment frag
			#pragma shader_feature_local WIREFRAME_DASH
			#pragma shader_feature_local WIREFRAME_CLIP
			#pragma shader_feature_local WIREFRAME_FRESNEL_EFFECT
			#pragma shader_feature_local WIREFRAME_WORLD
			#pragma shader_feature_local WIREFRAME_TEXTURE
			#pragma shader_feature_local WIREFRAME_BEHIND_DEPTH_FADE
			#pragma shader_feature_local UV3 UV0 UV1 UV2 UV4 UV5 UV6 UV7

			#ifdef WIREFRAME_WORLD
				#define WIREFRAME_NEEDS_POSITION_WS
			#endif

			#if defined(WIREFRAME_DASH) || defined(WIREFRAME_TEXTURE)
				#define WIREFRAME_NEEDS_LENGTH_COORD
				#ifndef WIREFRAME_WORLD
					#define WIREFRAME_NEEDS_SCREENPOS
				#endif
			#endif

			#ifdef WIREFRAME_BEHIND_DEPTH_FADE
				#define WIREFRAME_NEEDS_SCREENPOS
				#define WIREFRAME_NEEDS_DEPTH_DIFFERENCE
			#endif

			#ifdef WIREFRAME_FRESNEL_EFFECT
				#define WIREFRAME_NEEDS_NORMALS
				#define WIREFRAME_NEEDS_VIEW_WS
			#endif

			// The Core.hlsl file contains definitions of frequently used HLSL
			// macros and functions, and also contains #include references to other
			// HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#ifdef WIREFRAME_NEEDS_DEPTH_DIFFERENCE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#endif



			CBUFFER_START(UnityPerMaterial)
			uniform float _Width;
			uniform float _FalloffWidth;
			uniform float _DashLength;
			uniform float _EmptyLength;
			uniform float _TexLength;
			uniform float _DepthFadeDistance;
			uniform float4 _EdgeColor;
			//Note: don't #ifdef out properties here as SRP batcher can not handle different layouts.
			uniform float _WireframeCutoff;
			CBUFFER_END

			#ifdef WIREFRAME_TEXTURE
			TEXTURE2D(_WireframeTex);
			SamplerState sampler_linear_repeat;
			#endif
			#include "WireframeRendering.hlsl"

			// The structure definition defines which variables it contains.
			// This example uses the Attributes structure as an input structure in
			// the vertex shader.
			struct Attributes
			{
				// The positionOS variable contains the vertex positions in object
				// space.
				float3 positionOS   : POSITION;
				#ifdef WIREFRAME_NEEDS_NORMALS
				float3 normalOS        : NORMAL;
				#endif
				#if defined(UV0)
				float4 wireframeUv : TEXCOORD0;
				#elif defined(UV1)
				float4 wireframeUv : TEXCOORD1;
				#elif defined(UV2)
				float4 wireframeUv : TEXCOORD2;
				#elif defined(UV4)
				float4 wireframeUv : TEXCOORD4;
				#elif defined(UV5)
				float4 wireframeUv : TEXCOORD5;
				#elif defined(UV6)
				float4 wireframeUv : TEXCOORD6;
				#elif defined(UV7)
				float4 wireframeUv : TEXCOORD7;
				#else
				float4 wireframeUv : TEXCOORD3;
				#endif
				
			};

			struct Varyings
			{
				// The positions in this struct must have the SV_POSITION semantic.
				float4 positionHCS  : SV_POSITION;
				#ifdef WIREFRAME_NEEDS_NORMALS
				float3 normalWS        : TEXCOORD0;
				#endif
				#ifdef WIREFRAME_NEEDS_VIEW_WS
				float3 viewWS : TEXCOORD1;
				#endif
				#ifdef WIREFRAME_NEEDS_POSITION_WS
				float3 worldPosition : TEXCOORD2;
				#endif
				#ifdef WIREFRAME_NEEDS_SCREENPOS
				float4 screenPosition : TEXCOORD3;
				#endif
				float4 wireframeUv : TEXCOORD4;
			};

			// The vertex shader definition with properties defined in the Varyings 
			// structure. The type of the vert function must match the type (struct)
			// that it returns.
			Varyings vert(Attributes IN)
			{
				// Declaring the output object (OUT) with the Varyings struct.
				Varyings OUT;
				VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionHCS = positions.positionCS;
				#ifdef WIREFRAME_NEEDS_NORMALS
				OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
				#endif
				#if defined(WIREFRAME_NEEDS_VIEW_WS) || defined(WIREFRAME_NEEDS_POSITION_WS)
				float3 positionWS = positions.positionWS;
				#endif
				#ifdef WIREFRAME_NEEDS_VIEW_WS
				OUT.viewWS = GetWorldSpaceNormalizeViewDir(positionWS);
				#endif
				#ifdef WIREFRAME_NEEDS_POSITION_WS
				OUT.worldPosition = positionWS - TransformObjectToWorld(float3(0,0,0));
				#endif
				#ifdef WIREFRAME_NEEDS_SCREENPOS
				OUT.screenPosition = positions.positionNDC;
				#endif
				OUT.wireframeUv = IN.wireframeUv;
				// Returning the output.
				return OUT;
			}

			//NB This is expected to be (eventually) in "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
			float LinearDepthToEyeDepth(float rawDepth)
			{
				#if UNITY_REVERSED_Z
				return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
				#else
				return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
				#endif
			}

			// The fragment shader definition.            
			half4 frag(Varyings IN) : SV_Target
			{
				float4 wireframeTexCoords = IN.wireframeUv;
				#ifdef WIREFRAME_FRESNEL_EFFECT
				float fresnel = saturate(1.0 - saturate(abs(dot(normalize(IN.normalWS), normalize(IN.viewWS)))));
				wireframeTexCoords.w = 1 - wireframeTexCoords.w;
				wireframeTexCoords = SortMinValueToFirstComponent(wireframeTexCoords);
				wireframeTexCoords.x = fresnel;//NB Note that this has minimal effect for flat surfaces, except some slight noise at large line widths.
				wireframeTexCoords.w = 1 - wireframeTexCoords.w;
				#endif
				#ifdef WIREFRAME_NEEDS_SCREENPOS
				float3 screenPosition = IN.screenPosition.xyz / IN.screenPosition.w;
				#endif

				#ifdef WIREFRAME_NEEDS_DEPTH_DIFFERENCE
				float2 uvDepth = IN.positionHCS.xy / (_ScaledScreenParams.xy);
				float rawDepth = SampleSceneDepth(uvDepth).r;
				float sceneZ = (unity_OrthoParams.w == 0) ? LinearEyeDepth(rawDepth, _ZBufferParams) : LinearDepthToEyeDepth(rawDepth); 
				float thisZ = LinearEyeDepth(screenPosition.z, _ZBufferParams);
				float depthDifference = thisZ - sceneZ;
				//NB See, e.g. com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl for similar 
				//See https://www.cyanilux.com/tutorials/depth/#eye-depth as reference
				#endif
				#ifdef WIREFRAME_BEHIND_DEPTH_FADE
				float depthFadeFraction = exp(-max(0, depthDifference) / _DepthFadeDistance);
				_Width *= depthFadeFraction;
				#endif

				_FalloffWidth = max(_FalloffWidth, 0.001);
				#ifdef WIREFRAME_NEEDS_LENGTH_COORD
					#ifdef WIREFRAME_WORLD
					float wireframeFraction = GetWireframeFraction(wireframeTexCoords, _Width, _FalloffWidth, _DashLength, _EmptyLength, IN.worldPosition);
					#else
					float wireframeFraction = GetWireframeFraction(wireframeTexCoords, _Width, _FalloffWidth, _DashLength, _EmptyLength, screenPosition.xy * _ScreenParams.xy);
					#endif
				#else
					#ifdef WIREFRAME_WORLD
					float wireframeFraction = GetWireframeFraction(wireframeTexCoords, _Width, _FalloffWidth, IN.worldPosition);
					#else
					float wireframeFraction = GetWireframeFraction(wireframeTexCoords, _Width, _FalloffWidth);
					#endif
				#endif

				float alpha = lerp(0, _EdgeColor.a, saturate(wireframeFraction));
				#ifdef WIREFRAME_BEHIND_DEPTH_FADE
				alpha *= depthFadeFraction;
				#endif
				float4 color = float4(_EdgeColor.xyz, alpha);
				#ifdef WIREFRAME_CLIP
				clip(wireframeFraction + _WireframeCutoff);
				#endif
				return color;
			}
			ENDHLSL
		}
	}
}