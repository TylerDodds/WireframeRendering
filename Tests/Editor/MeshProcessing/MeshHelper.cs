// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEngine;
using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;

namespace PixelinearAccelerator.WireframeRendering.EditorTests
{
    /// <summary>
    /// Helper class for setting up <see cref="Mesh"/>es for testing.
    /// </summary>
    internal static class MeshHelper
    {
        /// <summary>
        /// Returns a version of <paramref name="mesh"/> with only vertices and triangles.
        /// </summary>
        /// <param name="mesh">A <see cref="Mesh"/>.</param>
        /// <returns>A new mesh.</returns>
        internal static Mesh WithOnlyVerticesAndTriangles(this Mesh mesh)
        {
            Mesh newMesh = new Mesh();
            newMesh.SetVertices(mesh.vertices);
            newMesh.SetTriangles(mesh.triangles, 0);
            newMesh.Optimize();
            return newMesh;
        }

        /// <summary>
        /// Gets the <see cref="MeshInformation"/> corresponding to the <paramref name="mesh"/>.
        /// </summary>
        /// <param name="mesh">The <see cref="Mesh"/>.</param>
        /// <returns>The <see cref="MeshInformation"/>.</returns>
        internal static MeshInformation GetMeshInformation(Mesh mesh)
        {
            MeshInformation meshInformation = new MeshInformation(mesh);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                meshInformation.UpdateMeshInformation(i);
            }
            return meshInformation;
        }
    }
}
