// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

#ifndef WIREFRAME_RENDERING_UTIL_INCLUDED
#define WIREFRAME_RENDERING_UTIL_INCLUDED

#pragma warning( disable: 4008 )

//Safely normalize a float2. Will yield length < 1 for very small lengths, and 0 for value of (0,0).
inline float2 SafeNormalize2(float2 value)
{
	float dotSafe = max(0.000000001f, dot(value, value));
	return value * rsqrt(dotSafe);
}

//Swaps order of components so the smallest value is first
float4 SortMinValueToFirstComponent(float4 vec4)
{
	float2 mins = min(vec4.xz, vec4.yw);
	float2 maxs = max(vec4.xz, vec4.yw);
	float4 orderComponentsXyAndZw = float4(mins.x, maxs.x, mins.y, maxs.y);
	float minV = min(orderComponentsXyAndZw.x, orderComponentsXyAndZw.z);
	float maxV = max(orderComponentsXyAndZw.x, orderComponentsXyAndZw.z);
	float4 final = float4(minV, orderComponentsXyAndZw.y, maxV, orderComponentsXyAndZw.w);
	return final;
}

//Sorts the components of a float3
float3 CuteSort(float3 vec3)
{
	float a = min(min(vec3.x, vec3.y), vec3.z);
	float b = max(max(vec3.x, vec3.y), vec3.z);
	return float3(a, vec3.x + vec3.y + vec3.z - a - b, b);
}

inline float4 InverseLerpUnclamped(float4 from, float4 to, float4 value)
{
	return (value - from) / (to - from);
}

inline float3 InverseLerpUnclamped(float3 from, float3 to, float3 value)
{
	return (value - from) / (to - from);
}

inline float2 InverseLerpUnclamped(float2 from, float2 to, float2 value)
{
	return (value - from) / (to - from);
}

inline float InverseLerpUnclamped(float from, float to, float value) 
{
	return (value - from) / (to - from);
}

//Performs 180 degree rotation on a float2, if needed, so that x-component is positive
inline float2 ToForwardQuadrants(float2 vec2)
{
	return vec2 * sign(vec2.x);
}
//Performs 180 degree rotation on a float2, if needed, so that x-component is positive; if y-component is larger, does the same for y instead
inline float2 ToForwardQuadrantsSafe(float2 vec2)
{
	float2 absv = abs(vec2);
	return vec2 * (absv.x > absv.y ? sign(vec2.x) : sign(vec2.y));
}

#pragma warning( enable: 4008 )

#endif