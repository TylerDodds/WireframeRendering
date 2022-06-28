// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;
using System.Collections.Generic;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// Utility class for geometry functions
    /// </summary>
    internal static class GeometryUtils
    {
        /// <summary>
        /// Gets the average of a set of <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vectors">The set of vectors.</param>
        public static Vector3 GetAverageVector3(IEnumerable<Vector3> vectors)
        {
            Vector3 total = Vector3.zero;
            int count = 0;
            foreach (Vector3 vector in vectors)
            {
                count++;
                total += vector;
            }
            return count > 0 ? total / count : total;
        }

        /// <summary>
        /// Simple method for determining the average of a set of normal vectors.
        /// </summary>
        /// <param name="normals">The normal vectors.</param>
        public static Vector3 GetAverageNormalSimple(IEnumerable<Vector3> normals)
        {
            return GetAverageVector3(normals).normalized;
        }

        /// <summary>
        /// Gets the triangle's normal vector.
        /// </summary>
        /// <param name="triangle">The triangle.</param>
        /// <param name="vertexPositions">The vertex positions</param>
        public static Vector3 GetTriangleNormal(Triangle triangle, IReadOnlyList<Vector3> vertexPositions)
        {
            Vector3 a = vertexPositions[triangle.Index1];
            Vector3 b = vertexPositions[triangle.Index2];
            Vector3 c = vertexPositions[triangle.Index3];

            return Vector3.Cross((b - a).normalized, (c - a).normalized).normalized;
        }
    }
}
