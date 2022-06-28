// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.GraphicsInfo;
using System;

namespace PixelinearAccelerator.WireframeRendering.Runtime.Enums
{
    /// <summary>
    /// Defines the way in which wireframes will be rendered, including shader requirements and any additional data that needs to be generated on model import.
    /// </summary>
    [Serializable]
    public enum WireframeType
    {
        None = 0,
        Default = 1,
        TextureCoordinates = 2,
        GeometryShader = 3,
    }

    /// <summary>
    /// Extension method for <see cref="WireframeType"/>.
    /// </summary>
    public static class WireframeTypeExtensions
    {
        /// <summary>
        /// Gets if the value is <see cref="WireframeType.None"/>
        /// </summary>
        /// <param name="wireframeType">The wireframe generation type.</param>
        /// <returns>If the value is <see cref="WireframeType.None"/>.</returns>
        public static bool IsNone(this WireframeType wireframeType) => wireframeType == WireframeType.None;

        /// <summary>
        /// Resolves <see cref="WireframeType.Default"/> based on expected system support.
        /// </summary>
        /// <param name="wireframeType">The wireframe generation type.</param>
        /// <returns>The <see cref="WireframeType"/> with <see cref="WireframeType.Default"/> resolved.</returns>
        public static WireframeType ResolveDefault(this WireframeType wireframeType)
        {
            if (wireframeType == WireframeType.Default)
            {
                if (SystemGraphicsInfo.LikelySupportsGeometryShaders)
                {
                    wireframeType = WireframeType.GeometryShader;
                }
                else
                {
                    wireframeType = WireframeType.TextureCoordinates;
                }
            }
            return wireframeType;
        }
    }
}