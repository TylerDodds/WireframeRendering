// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// Information about wireframe uv generation to be serialized as custom data for the asset importer.
    /// </summary>
    [Serializable]
    internal struct WireframeInfo
    {
        public bool ShouldGenerate;

        public WireframeInfo(bool shouldGenerate)
        {
            ShouldGenerate = shouldGenerate;
        }
    }
}
