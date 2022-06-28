// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// Data needed to generate a wireframe mesh on import.
    /// </summary>
    [Serializable]
    internal class WireframeGeneratedMeshData
    {
        /// <summary>
        /// The GUID of the source model.
        /// </summary>
        public string ReferenceGuid;

        public WireframeGeneratedMeshData(string referenceGuid)
        {
            ReferenceGuid = referenceGuid;
        }
    }
}
