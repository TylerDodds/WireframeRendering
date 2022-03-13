// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEditor;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.EditorTests
{
    /// <summary>
    /// Example meshes.
    /// </summary>
    internal static class MeshExamples
    {
        /// <summary>
        /// Gets the test <see cref="MeshLibrary"/> from the Asset Database.
        /// </summary>
        /// <returns>The test <see cref="MeshLibrary"/>.</returns>
        public static MeshLibrary GetMeshLibrary()
        {
            string assetPath = "Packages/com.pixelinearaccelerator.wireframe-rendering/Tests/ExampleMeshes/MeshLibrary.asset";
            return AssetDatabase.LoadAssetAtPath<MeshLibrary>(assetPath);
        }

        /// <summary>
        /// Gets a Quad <see cref="Mesh"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>A quad mesh.</returns>
        public static Mesh GetQuadMesh(float width, float height)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, height, 0),
                new Vector3(width, height, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;

            return mesh;
        }

        /// <summary>
        /// Gets a watertight Cube <see cref="Mesh"/> with no seams/sharp edges.
        /// </summary>
        /// <returns>The mesh.</returns>
        public static Mesh GetNonSeamCubeMesh()
        {
            Vector3[] vertices = 
            {
            new Vector3 (0, 0, 0),
            new Vector3 (1, 0, 0),
            new Vector3 (1, 1, 0),
            new Vector3 (0, 1, 0),
            new Vector3 (0, 1, 1),
            new Vector3 (1, 1, 1),
            new Vector3 (1, 0, 1),
            new Vector3 (0, 0, 1),
            };

            int[] triangles = 
            {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
            };

            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
