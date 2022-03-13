// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

#ifndef WIREFRAME_RENDERING_SHADERGRAPH_INCLUDED
#define WIREFRAME_RENDERING_SHADERGRAPH_INCLUDED

#define WIREFRAME_WORLD
#include "WireframeRendering.hlsl"

void GetWireframeFractionNoDash_World_float(float3 wireframeTexCoords, float width, float falloffWidth, float3 worldPosition, out float wireframeFraction)
{
	wireframeFraction = GetWireframeFraction(float4(wireframeTexCoords.xyz, 0), width, falloffWidth, worldPosition);
}

#endif