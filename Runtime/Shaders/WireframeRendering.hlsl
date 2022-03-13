// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

#ifndef WIREFRAME_RENDERING_INCLUDED
#define WIREFRAME_RENDERING_INCLUDED

//Note that we need _WireframeTex, sampler_linear_repeat, _TexLength defined before importing WireframeRendering.hlsl if WIREFRAME_TEXTURE is defined.

#include "WireframeUtil.hlsl"

static const float LARGE_NEGATIVE_NUMBER = -2e24;

float4 GetDerivativesAndLengthPerCoordinate(float4 coords, out float4 xDeriv, out float4 yDeriv)
{
	xDeriv = ddx(coords);
	yDeriv = ddy(coords);
	float4 xDeriv2 = xDeriv * xDeriv;
	float4 yDeriv2 = yDeriv * yDeriv;
	float4 derivLen = sqrt(xDeriv2 + yDeriv2);
	derivLen = max(derivLen, 0.00000000001);
	return derivLen;
}

float4 GetDerivativeLengthPerCoordinate(float4 coords)
{
	float4 xDeriv, yDeriv;
	return GetDerivativesAndLengthPerCoordinate(coords, xDeriv, yDeriv);
}

#ifdef WIREFRAME_NEEDS_LENGTH_COORD
	#ifdef WIREFRAME_WORLD
	float GetWireframeFraction(float4 wireframeTexCoords, float width, float falloffWidth, float dashLength, float emptyLength, float3 worldPosition)
	#else
	float GetWireframeFraction(float4 wireframeTexCoords, float width, float falloffWidth, float dashLength, float emptyLength, float2 screenPositionPx)
	#endif
#else
	#ifdef WIREFRAME_WORLD
	float GetWireframeFraction(float4 wireframeTexCoords, float width, float falloffWidth, float3 worldPosition)
	#else
	float GetWireframeFraction(float4 wireframeTexCoords, float width, float falloffWidth)
	#endif
#endif
{
	wireframeTexCoords.w = 1 - wireframeTexCoords.w;//Since 1 - wireframeTextureCoordinate is stored in the w component due to default behaviour of vertex shader float4.

	float4 xDeriv, yDeriv;
	float4 derivLen = GetDerivativesAndLengthPerCoordinate(wireframeTexCoords, xDeriv, yDeriv);
	float4 wireframePx = (1.0 - wireframeTexCoords) / derivLen;

#ifdef WIREFRAME_WORLD
	float3 xDerivWorld = ddx(worldPosition);
	float3 yDerivWorld = ddy(worldPosition);

	float2 wireframe1DirectionSS = float2(xDeriv.x, yDeriv.x);
	float2 wireframe2DirectionSS = float2(xDeriv.y, yDeriv.y);
	float2 wireframe3DirectionSS = float2(xDeriv.z, yDeriv.z);
	float2 wireframe4DirectionSS = float2(xDeriv.w, yDeriv.w);
	wireframe1DirectionSS = SafeNormalize2(wireframe1DirectionSS);
	wireframe2DirectionSS = SafeNormalize2(wireframe2DirectionSS);
	wireframe3DirectionSS = SafeNormalize2(wireframe3DirectionSS);
	wireframe4DirectionSS = SafeNormalize2(wireframe4DirectionSS);

	float3 wireframeWorldDelta1 = xDerivWorld * wireframe1DirectionSS.x + yDerivWorld * wireframe1DirectionSS.y;
	float3 wireframeWorldDelta2 = xDerivWorld * wireframe2DirectionSS.x + yDerivWorld * wireframe2DirectionSS.y;
	float3 wireframeWorldDelta3 = xDerivWorld * wireframe3DirectionSS.x + yDerivWorld * wireframe3DirectionSS.y;
	float3 wireframeWorldDelta4 = xDerivWorld * wireframe4DirectionSS.x + yDerivWorld * wireframe4DirectionSS.y;

	float4 wireframeWorldDeltaLengths = float4(length(wireframeWorldDelta1), length(wireframeWorldDelta2), length(wireframeWorldDelta3), length(wireframeWorldDelta4));

	float4 wireframeWorld = wireframePx * wireframeWorldDeltaLengths;
	float4 wireframeFinal = wireframeWorld;
#else
	float4 wireframeFinal = wireframePx;
#endif

#ifdef WIREFRAME_NEEDS_LENGTH_COORD
	float2 dash1DirectionSS = float2(-yDeriv.x, xDeriv.x);
	float2 dash2DirectionSS = float2(-yDeriv.y, xDeriv.y);
	float2 dash3DirectionSS = float2(-yDeriv.z, xDeriv.z);
	float2 dash4DirectionSS = float2(-yDeriv.w, xDeriv.w);
	dash1DirectionSS = ToForwardQuadrants(dash1DirectionSS);
	dash2DirectionSS = ToForwardQuadrants(dash2DirectionSS);
	dash3DirectionSS = ToForwardQuadrants(dash3DirectionSS);
	dash4DirectionSS = ToForwardQuadrants(dash4DirectionSS);
	dash1DirectionSS = SafeNormalize2(dash1DirectionSS);
	dash2DirectionSS = SafeNormalize2(dash2DirectionSS);
	dash3DirectionSS = SafeNormalize2(dash3DirectionSS);
	dash4DirectionSS = SafeNormalize2(dash4DirectionSS);

	#ifdef WIREFRAME_WORLD
		float3 dashWorldDelta1 = xDerivWorld * dash1DirectionSS.x + yDerivWorld * dash1DirectionSS.y;
		float3 dashWorldDelta2 = xDerivWorld * dash2DirectionSS.x + yDerivWorld * dash2DirectionSS.y;
		float3 dashWorldDelta3 = xDerivWorld * dash3DirectionSS.x + yDerivWorld * dash3DirectionSS.y;
		float3 dashWorldDelta4 = xDerivWorld * dash4DirectionSS.x + yDerivWorld * dash4DirectionSS.y;
		float3 dashWorldDirection1 = SafeNormalize(dashWorldDelta1);
		float3 dashWorldDirection2 = SafeNormalize(dashWorldDelta2);
		float3 dashWorldDirection3 = SafeNormalize(dashWorldDelta3);
		float3 dashWorldDirection4 = SafeNormalize(dashWorldDelta4);
		float4 dashWorldDeltaLengths = float4(length(dashWorldDelta1), length(dashWorldDelta2), length(dashWorldDelta3), length(dashWorldDelta4));

		float dashInputW1 = dot(worldPosition, dashWorldDirection1);
		float dashInputW2 = dot(worldPosition, dashWorldDirection2);
		float dashInputW3 = dot(worldPosition, dashWorldDirection3);
		float dashInputW4 = dot(worldPosition, dashWorldDirection4);
		float4 dashInputsW = float4(dashInputW1, dashInputW2, dashInputW3, dashInputW4);
		float4 dashInputsFinal = dashInputsW;
	#else
	
		float dashInputPx1 = dot(screenPositionPx, dash1DirectionSS);
		float dashInputPx2 = dot(screenPositionPx, dash2DirectionSS);
		float dashInputPx3 = dot(screenPositionPx, dash3DirectionSS);
		float dashInputPx4 = dot(screenPositionPx, dash4DirectionSS);
		float4 dashInputsPx = float4(dashInputPx1, dashInputPx2, dashInputPx3, dashInputPx4);
		float4 dashInputsFinal = dashInputsPx;
	#endif

	float dashLengthTotal = dashLength + emptyLength;
	float4 Radius = width;//Even if width is larger than dashLength/2, we still want the line width to be what controls the radius
	float4 dashInputsFromCenter = (frac(dashInputsFinal / dashLengthTotal) - 0.5) * (dashLengthTotal);
	float4 wireframeOffset = max(0, wireframeFinal);//Note that this should be max(0, wireframeFinal - width + Radius), but here we will just take Radius = width.
	#ifdef WIREFRAME_DASH
		float4 dashInputOffset = max(0, abs(dashInputsFromCenter) - dashLength * 0.5);//Note that don't add Radius, which makes the rounding cap start at the desired width. This will cause dashes to 'link' together when width is large compared to dash width.
		float4 sdf = -Radius + sqrt(dashInputOffset*dashInputOffset + wireframeOffset * wireframeOffset);
	#else
		float4 sdf = -Radius + wireframeOffset;
	#endif

#else
	float4 sdf = -width + wireframeFinal;//This way, SDF will start at -width and hit 0 at width away from the edge.

#endif//WIREFRAME_NEEDS_LENGTH_COORD


#if defined(WIREFRAME_WORLD)

	#ifdef WIREFRAME_DASH
	float4 sdfDdx, sdfDdy;
	float4 sdfDerivLength = GetDerivativesAndLengthPerCoordinate(sdf, sdfDdx, sdfDdy);
	float4 remapped = sdf / sdfDerivLength;

	#else
	float4 remapped = sdf / wireframeWorldDeltaLengths;
	#endif
	float4 inverseSdfComponents = remapped < falloffWidth ? InverseLerpUnclamped(falloffWidth, 0, remapped) : falloffWidth - remapped;

	//Note that dashWorldDeltaLengths will be zero when wireframeWorldDeltaLengths are (due to x and y world derivatives of texture coordinates both being zero), so this one test suffices to cover the dash case as well.
	inverseSdfComponents = wireframeWorldDeltaLengths > 0 ? inverseSdfComponents : LARGE_NEGATIVE_NUMBER;
	float inverseSdf = max(inverseSdfComponents.x, max(inverseSdfComponents.y, max(inverseSdfComponents.z, inverseSdfComponents.w)));
#else
	float4 inverseSdfComponents = InverseLerpUnclamped(falloffWidth, 0, sdf);
	float inverseSdf = max(inverseSdfComponents.x, max(inverseSdfComponents.y, max(inverseSdfComponents.z, inverseSdfComponents.w)));
	#ifdef WIREFRAME_CLIP
	inverseSdf = inverseSdf > 0 ? inverseSdf : inverseSdf * falloffWidth;
	#endif
#endif

#ifdef WIREFRAME_TEXTURE
	float2 uv1 = float2(dashInputsFinal.x, wireframeOffset.x);
	float2 uv2 = float2(dashInputsFinal.y, wireframeOffset.y);
	float2 uv3 = float2(dashInputsFinal.z, wireframeOffset.z);
	float2 uv4 = float2(dashInputsFinal.w, wireframeOffset.w);
	float2 uvFinal = step(inverseSdf, inverseSdfComponents.x) * uv1 + step(inverseSdf, inverseSdfComponents.y) * uv2 + step(inverseSdf, inverseSdfComponents.z) * uv3 + step(inverseSdf, inverseSdfComponents.w) * uv4;
#ifdef WIREFRAME_WORLD
	uvFinal.x /= _TexLength;
#else
	uvFinal.x /= _TexLength;
#endif
	uvFinal.y /= width;
	float texR = saturate(_WireframeTex.Sample(sampler_linear_repeat, uvFinal).r);
	inverseSdf = inverseSdf > 0 ? saturate(inverseSdf) * texR : inverseSdf;
#endif

	return inverseSdf;
}


#endif