// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Runtime.GraphicsInfo
{
    /// <summary>
    /// System graphics information needed for wireframe rendering.
    /// </summary>
    public static class SystemGraphicsInfo
    {
        /// <summary>
        /// If the system's shader level and graphics API likely supports geometry shaders.
        /// </summary>
        public static bool LikelySupportsGeometryShaders
        {
            get
            {
                //Consider https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html, which defines geometry support in Target 4.0 (Metal excepted)
                int shaderLevel = SystemInfo.graphicsShaderLevel;
                UnityEngine.Rendering.GraphicsDeviceType device = SystemInfo.graphicsDeviceType;
                bool geometryShadersProbablySupported = shaderLevel >= 40 && device != UnityEngine.Rendering.GraphicsDeviceType.Metal;
                return geometryShadersProbablySupported;
            }
        }
    }
}
