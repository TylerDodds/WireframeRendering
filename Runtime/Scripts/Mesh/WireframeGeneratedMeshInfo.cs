// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Runtime.Mesh
{
    /// <summary>
    /// Holds data needed to identify connections between source models and generated wireframe meshes.
    /// </summary>
    public class WireframeGeneratedMeshInfo : ScriptableObject
    {
        /// <summary>
        /// The GUID of the source model.
        /// </summary>
        public string ReferenceGuid
        {
            get => _referenceGuid;
            set => _referenceGuid = value;
        }

        [SerializeField]
        private string _referenceGuid = string.Empty;
    }
}
