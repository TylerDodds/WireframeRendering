// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using System.Linq;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Utility functions for grouping triangles in a mesh.
    /// </summary>
    internal static class TriangleGroupUtilities
    {
        /// <summary>
        /// Determines the set of <see cref="DecoupledGrouping"/> of edges and triangles that do not share any common edges.
        /// </summary>
        /// <param name="meshInformation">The mesh information.</param>
        /// <returns>The set of <see cref="DecoupledGrouping"/>.</returns>
        internal static List<DecoupledGrouping> GetDecoupledTriangleGroupings(MeshInformation meshInformation)
        {
            HashSet<Edge> edgesLeft = meshInformation.GetEdges();
            List<DecoupledGrouping> triangleGroupings = new List<DecoupledGrouping>();
            while (edgesLeft.Count > 0)
            {
                Edge firstEdge = edgesLeft.First();
                HashSet<Triangle> trianglesConnectedToEdge = new HashSet<Triangle>();
                HashSet<Edge> edgesConnectedToEdge = new HashSet<Edge>();
                void RemoveConnectedEdges(Edge edge)
                {
                    if (edgesLeft.Contains(edge))
                    {
                        edgesLeft.Remove(edge);
                        foreach (Triangle triangle in edge)
                        {
                            if (!trianglesConnectedToEdge.Contains(triangle))
                            {
                                trianglesConnectedToEdge.Add(triangle);
                                foreach (Edge connectedTriangleEdge in triangle)
                                {
                                    edgesConnectedToEdge.Add(connectedTriangleEdge);
                                    RemoveConnectedEdges(connectedTriangleEdge);
                                }
                            }
                        }
                    }
                }
                RemoveConnectedEdges(firstEdge);

                triangleGroupings.Add(new DecoupledGrouping(trianglesConnectedToEdge, edgesConnectedToEdge));
            }
            return triangleGroupings;
        }
    }
}
