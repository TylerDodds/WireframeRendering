// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using System.Linq;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// A group of <see cref="Triangle"/>s and their associated <see cref="Edge"/>s that form a decoupled group from other triangles.
    /// </summary>
    internal class DecoupledGrouping
    {
        /// <summary>The set of <see cref="Triangle"/>s in the group.</summary>
        public HashSet<Triangle> Triangles;
        /// <summary>The set of <see cref="Edge"/>s in the group.</summary>
        public HashSet<Edge> Edges;
        /// <summary>The set of vertex indices in the group.</summary>
        public HashSet<int> Vertices;
        /// <summary>The <see cref="DecoupledGroupType"/>.</summary>
        public DecoupledGroupType DecoupledGroupType;

        /// <summary>
        /// Creates a <see cref="DecoupledGrouping"/> with the given <paramref name="triangles"/> and <paramref name="edges"/>.
        /// </summary>
        /// <param name="triangles">The triangles of the grouping.</param>
        /// <param name="edges">The edges of the grouping.</param>
        /// <param name="decoupledGroupType">The type of connection between triangles</param>
        public DecoupledGrouping(HashSet<Triangle> triangles, HashSet<Edge> edges, DecoupledGroupType decoupledGroupType)
        {
            Triangles = triangles;
            Edges = edges;
            Vertices = new HashSet<int>(triangles.SelectMany(t => t.GetIndices()));
            DecoupledGroupType = decoupledGroupType;
        }
    }

    /// <summary>
    /// A grouping of boundary <see cref="Edges"/>, along with <see cref="Triangle"/>s (and their vertices) whose vertices all touch a boundary edge.
    /// </summary>
    internal class BoundaryGrouping
    {
        public HashSet<Triangle> TrianglesCompletelyTouchingBoundaryEdges;
        public HashSet<int> VertexIndicesOfTrianglesCompletelyTouchingBoundaryEdges;
        public HashSet<Edge> Edges;

        public BoundaryGrouping(HashSet<Edge> edges, HashSet<Triangle> trianglesCompletelyTouchingBoundaryEdges, HashSet<int> vertexIndicesOfTrianglesCompletelyTouchingBoundaryEdges)
        {
            TrianglesCompletelyTouchingBoundaryEdges = trianglesCompletelyTouchingBoundaryEdges;
            VertexIndicesOfTrianglesCompletelyTouchingBoundaryEdges = vertexIndicesOfTrianglesCompletelyTouchingBoundaryEdges;
            Edges = edges;
        }
    }

    /// <summary>
    /// Describes the type of connection between triangles in a <see cref="DecoupledGrouping"/>.
    /// </summary>
    internal enum DecoupledGroupType
    {
        SharedEdges,
        SharedVertices,
    }
}
