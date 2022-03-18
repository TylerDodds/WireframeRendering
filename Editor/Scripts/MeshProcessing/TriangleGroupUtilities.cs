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

        /// <summary>
        /// Gets a <see cref="BoundaryGrouping"/> that includes triangles, and their vertices, whose vertices only touch boundary edges.
        /// </summary>
        /// <param name="decoupledGrouping">The <see cref="DecoupledGrouping"/> of triangles and edges.</param>
        /// <param name="boundaryEdgeCycle">The <see cref="BoundaryEdgeCycle"/> of the <paramref name="decoupledGrouping"/></param>
        /// <returns>The <see cref="BoundaryGrouping"/>.</returns>
        internal static BoundaryGrouping GetBoundaryGrouping(DecoupledGrouping decoupledGrouping, BoundaryEdgeCycle boundaryEdgeCycle)
        {
            List<Edge> boundaryEdges = boundaryEdgeCycle.Edges;
            HashSet<int> boundaryVertexIndices = new HashSet<int>(boundaryEdges.SelectMany(e => e.GetIndices()));
            IEnumerable<Triangle> potentialTriangles = boundaryEdges.SelectMany(e => e).Distinct();
            IEnumerable<Triangle> trianglesWithAllVerticesTouchingBoundaryEdges = potentialTriangles.Where(t => t.GetIndices().All(i => boundaryVertexIndices.Contains(i)));
            IEnumerable<int> verticesOfTrianglesWithAllVerticesTouchingBoundaryEdges = trianglesWithAllVerticesTouchingBoundaryEdges.SelectMany(t => t.GetIndices()).Distinct();

            BoundaryGrouping boundaryGrouping = new BoundaryGrouping(new HashSet<Edge>(boundaryEdges), new HashSet<Triangle>(trianglesWithAllVerticesTouchingBoundaryEdges), new HashSet<int>(verticesOfTrianglesWithAllVerticesTouchingBoundaryEdges));
            return boundaryGrouping;
        }
    }
}
