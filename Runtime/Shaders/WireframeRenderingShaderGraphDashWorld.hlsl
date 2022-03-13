// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

#ifndef WIREFRAME_RENDERING_SHADERGRAPH_DASH_INCLUDED
#define WIREFRAME_RENDERING_SHADERGRAPH_DASH_INCLUDED

#define WIREFRAME_DASH
#define WIREFRAME_WORLD
#include "WireframeRendering.hlsl"


void GetWireframeFractionDash_World_float(float3 wireframeTexCoords, float width, float falloffWidth, float dashLength, float emptyLength, float3 worldPosition, out float wireframeFraction)
{
	wireframeFraction = GetWireframeFraction(float4(wireframeTexCoords.xyz, 0), width, falloffWidth, dashLength, emptyLength, worldPosition);
}


#endif