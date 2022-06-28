// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

Shader "Hidden/PixelinearAccelerator/Wireframe/URP Wireframe Unlit (Using Geometry Shader)"
{
	Properties
	{
		[Header(Size)]
		[Space]
		_Width("Width", Range(0.0, 20.0)) = 10
		_FalloffWidth("Falloff Width", Range(0.0, 1.0)) = 1
		[Toggle(WIREFRAME_OVERSHOOT)]
		_Overshoot("Use Overshoot", Float) = 0
		_OvershootLength("Overshoot", Range(0.0, 20.0)) = 1

		[Header(Space)]
		[Space]
		[Toggle(WIREFRAME_WORLD)]
		_WorldSpaceReference("World Space Reference", Float) = 0
		[Toggle(WIREFRAME_USE_OBJECT_NORMALS)]
		_UseObjectNormals("Use Object Normals", Float) = 0

		[Header(Appearance)]
		[Space]
		_EdgeColor("Edge Color", Color) = (.75, .75, .75, 1)
		[Toggle(WIREFRAME_DASH)]
		_WireframeDash("Wireframe Dash", Float) = 0
		_DashLength("Dash Length", Range(0.0, 500.)) = 200.
		_EmptyLength("Empty Length", Range(0.0, 500.)) = 200.
		[Toggle(WIREFRAME_TEXTURE)]
		_ApplyTexture("Apply Texture", Float) = 0
		_WireframeTex("Wireframe Texture", 2D) = "white" {}
		_TexLength("Texture Length", Range(0.0, 500.)) = 200.
		[Toggle(WIREFRAME_BEHIND_DEPTH_FADE)]
		_BehindDepthFade("Fade Further Behind Objects", Float) = 0
		_DepthFadeDistance("Depth Fade Distance", Range(0.0, 50.)) = 10.
		[KeywordEnum(NotImported,Shown,NotShown)]
		_Wireframe_Contour_Edges("Contour Edges Mode", Float) = 0

		[Header(Culling)]
		[Space]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
		[Toggle(WIREFRAME_CLIP)]
		_WireframeClip("Alpha Clip", Float) = 0
		_WireframeCutoff("Alpha Clip Width", Range(0.0, 20.0)) = 5
		[KeywordEnum(InFront, Behind, All)] _Wireframe_Depth("Wireframe Depth Mode", Float) = 0

		_InFrontDepthCutoff("Depth Cutoff Between In Front & Behind", Float) = 0.005
		_NdcMinMaxCutoffForWidthAndAlpha("Viewport Edge Min/Max Fractions For Tapering Width (x, y) and Alpha (z, w)", Vector) = (-0.01, 0.01, -0.001, 0.001)
	}

	CustomEditor "PixelinearAccelerator.WireframeRendering.Editor.CustomShaderGUI.WireframeRenderingShaderGUI"

	// The SubShader block containing the Shader code. 
	SubShader
	{
		// SubShader Tags define when and under which conditions a SubShader block or a pass is executed.
		Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Transparent-10" }

		Cull [_Cull]
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest Always

		Pass
		{
			// The HLSL code block. Unity SRP uses the HLSL language.
			HLSLPROGRAM

			// Signal this shader requires geometry programs
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 5.0
			#pragma require geometry

			#pragma shader_feature_local WIREFRAME_DASH
			#pragma shader_feature_local WIREFRAME_CLIP
			#pragma shader_feature_local WIREFRAME_WORLD
			#pragma shader_feature_local WIREFRAME_USE_OBJECT_NORMALS
			#pragma shader_feature_local WIREFRAME_TEXTURE
			#pragma shader_feature_local WIREFRAME_BEHIND_DEPTH_FADE
			#pragma shader_feature_local WIREFRAME_OVERSHOOT
			#pragma shader_feature_local _WIREFRAME_DEPTH_INFRONT _WIREFRAME_DEPTH_BEHIND _WIREFRAME_DEPTH_ALL
			#pragma shader_feature_local _WIREFRAME_CONTOUR_EDGES_NOTIMPORTED _WIREFRAME_CONTOUR_EDGES_SHOWN _WIREFRAME_CONTOUR_EDGES_NOTSHOWN

			// Register our functions
			#pragma vertex Vertex
			#pragma geometry Geometry
			#pragma fragment Fragment

			// Include our logic file
			#include "WireframeGeometry.hlsl"
			ENDHLSL
		}
	}
}