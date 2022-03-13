// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

#ifndef WIREFRAME_RENDERING_SHADERGRAPH_DASH_INCLUDED
#define WIREFRAME_RENDERING_SHADERGRAPH_DASH_INCLUDED

#define WIREFRAME_DASH
#include "WireframeRendering.hlsl"


void GetWireframeFractionDash_float(float3 wireframeTexCoords, float width, float falloffWidth, float dashLength, float emptyLength, float2 screenPosition, out float wireframeFraction)
{
	wireframeFraction = GetWireframeFraction(float4(wireframeTexCoords.xyz, 0), width, falloffWidth, dashLength, emptyLength, screenPosition);
}


#endif