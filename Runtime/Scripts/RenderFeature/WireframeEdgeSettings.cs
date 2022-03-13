// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature
{
    /// <summary>
    /// Settings for rendering wireframe edges.
    /// </summary>
    [Serializable]
    public class WireframeEdgeSettings
    {
        /// <summary>
        /// The color of the wireframe edge.
        /// </summary>
        public Color Color = Color.black;

        /// <summary>
        /// The width of the wireframe edge in px.
        /// </summary>
        [Range(0, 20)]
        public float WidthPx = 10;

        /// <summary>
        /// The width of the wireframe edge in world-space.
        /// </summary>
        [Range(0, 0.25f)]
        public float WidthWorld = 0.05f;

        /// <summary>
        /// The falloff width of the wireframe edge.
        /// </summary>
        [Range(0, 1)]
        public float FalloffWidthPx = 1;

        /// <summary>
        /// If the edge should be dashed.
        /// </summary>
        public bool Dash = false;

        /// <summary>
        /// The length of the wireframe dash in px;
        /// </summary>
        [Range(0, 500f)]
        public float DashLengthPx = 200f;

        /// <summary>
        /// The empty length between dashes in px.
        /// </summary>
        [Range(0, 500f)]
        public float EmptyLengthPx= 200f;

        /// <summary>
        /// The length of the wireframe dash in world space.
        /// </summary>
        [Range(0, 1f)]
        public float DashLengthWorld = 0.2f;

        /// <summary>
        /// The empty length between dashes in world space.
        /// </summary>
        [Range(0, 1f)]
        public float EmptyLengthWorld = 0.2f;

        /// <summary>
        /// If the edge should have a texture applied.
        /// </summary>
        public bool ApplyTexture = false;

        /// <summary>
        /// The edge's texture.
        /// </summary>
        public Texture2D Texture = null;

        /// <summary>
        /// If length of the wireframe texture should be chosen to match the <see cref="Texture"/>'s aspect ratio.
        /// </summary>
        public bool KeepTextureAspectRatio = true;

        /// <summary>
        /// The length of the wireframe texture in px;
        /// </summary>
        [Range(0, 500f)]
        public float TextureLengthPx = 200f;

        /// <summary>
        /// The length of the wireframe texture in world space;
        /// </summary>
        [Range(0, 1)]
        public float TextureLengthWorld = 0.2f;
        
        /// <summary>
        /// If edge sizes should be done in world space.
        /// </summary>
        public bool WorldSpace = false;

        /// <summary>
        /// If a Fresnel-like edge should be drawn.
        /// </summary>
        public bool Fresnel = false;
    }
}