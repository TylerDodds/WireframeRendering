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
        /// Determines the set of <see cref="DecoupledGrouping"/> of edges and triangles that do not share any common vertices or edges, depending on <paramref name="decoupledGroupType"/>.
        /// </summary>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="decoupledGroupType">The type of decoupling.</param>
        /// <param name="trianglesInGroupings">Only these <see cref="Triangle"/> will be considered in the groupings.</param>
        /// <returns>The set of <see cref="DecoupledGrouping"/>.</returns>
        internal static List<DecoupledGrouping> GetDecoupledGroupings(MeshInformation meshInformation, DecoupledGroupType decoupledGroupType, IEnumerable<Triangle> trianglesInGroupings)
        {
            List<DecoupledGrouping> groups;
            switch (decoupledGroupType)
            {
                case DecoupledGroupType.SharedEdges:
                    groups = GetDecoupledGroupingsBasedOnConnectedEdges(meshInformation, trianglesInGroupings);
                    break;
                case DecoupledGroupType.SharedVertices:
                    groups = GetDecoupledGroupingsBasedOnConnectedVertices(meshInformation, trianglesInGroupings);
                    break;
                default:
                    groups = null;
                    break;
            }
            return groups;
        }

        /// <summary>
        /// Determines the set of <see cref="DecoupledGrouping"/> of edges and triangles that do not share any common vertices or edges, depending on <paramref name="decoupledGroupType"/>.
        /// </summary>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="decoupledGroupType">The type of decoupling.</param>
        /// <returns>The set of <see cref="DecoupledGrouping"/>.</returns>
        internal static List<DecoupledGrouping> GetDecoupledGroupings(MeshInformation meshInformation, DecoupledGroupType decoupledGroupType)
        {
            return GetDecoupledGroupings(meshInformation, decoupledGroupType, null);
        }

        /// <summary>
        /// Determines the set of <see cref="DecoupledGrouping"/> of edges and triangles that do not share any common vertices.
        /// </summary>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="trianglesInGroupings">The <see cref="Triangle"/> in the groupings. If null, instead use the triangles from the <paramref name="meshInformation"/>.</param>
        /// <returns>The set of <see cref="DecoupledGrouping"/>.</returns>
        private static List<DecoupledGrouping> GetDecoupledGroupingsBasedOnConnectedVertices(MeshInformation meshInformation, IEnumerable<Triangle> trianglesInGroupings)
        {
            HashSet<Triangle> trianglesLeft = trianglesInGroupings != null ? new HashSet<Triangle>(trianglesInGroupings) : meshInformation.GetTriangles();
            List<DecoupledGrouping> triangleGroupings = new List<DecoupledGrouping>();
            while (trianglesLeft.Count > 0)
            {
                Triangle firstTriangle = trianglesLeft.First();
                HashSet<Triangle> trianglesConnectedToTriangle = new HashSet<Triangle>();
                void RemoveTrianglesConnectedByVertices(Triangle triangle)
                {
                    if (trianglesLeft.Contains(triangle))
                    {
                        trianglesLeft.Remove(triangle);
                        foreach (int vertexIndex in triangle.GetIndices())
                        {
                            Vertex vertex = meshInformation.GetVertex(vertexIndex);
                            foreach(Triangle connectedTriangle in vertex.GetTriangles())
                            {
                                if(!trianglesConnectedToTriangle.Contains(connectedTriangle))
                                {
                                    trianglesConnectedToTriangle.Add(connectedTriangle);
                                    RemoveTrianglesConnectedByVertices(connectedTriangle);
                                }
                            }
                        }
                    }
                }
                RemoveTrianglesConnectedByVertices(firstTriangle);

                HashSet<Edge> edgesInTriangles = new HashSet<Edge>(trianglesConnectedToTriangle.SelectMany(t => t));
                triangleGroupings.Add(new DecoupledGrouping(trianglesConnectedToTriangle, edgesInTriangles, DecoupledGroupType.SharedVertices));
            }
            return triangleGroupings;
        }

        /// <summary>
        /// Determines the set of <see cref="DecoupledGrouping"/> of edges and triangles that do not share any common edges.
        /// </summary>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="trianglesInGroupings">The <see cref="Triangle"/> in the groupings. If null, instead use the triangles from the <paramref name="meshInformation"/>.</param>
        /// <returns>The set of <see cref="DecoupledGrouping"/>.</returns>
        private static List<DecoupledGrouping> GetDecoupledGroupingsBasedOnConnectedEdges(MeshInformation meshInformation, IEnumerable<Triangle> trianglesInGroupings)
        {
            HashSet<Edge> edgesLeft = trianglesInGroupings != null ? new HashSet<Edge>(trianglesInGroupings.SelectMany(t => t)) : meshInformation.GetEdges();
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

                triangleGroupings.Add(new DecoupledGrouping(trianglesConnectedToEdge, edgesConnectedToEdge, DecoupledGroupType.SharedEdges));
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
