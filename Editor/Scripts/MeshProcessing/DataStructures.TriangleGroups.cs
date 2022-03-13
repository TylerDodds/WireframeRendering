// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// A group of <see cref="Triangle"/>s and their associated <see cref="Edge"/>s that form a group
    /// that shares no edges with other triangles.
    /// </summary>
    internal class DecoupledGrouping
    {
        /// <summary>The set of <see cref="Triangle"/>s in the group.</summary>
        public HashSet<Triangle> Triangles;
        /// <summary>The set of <see cref="Edge"/>s in the group.</summary>
        public HashSet<Edge> Edges;

        /// <summary>
        /// Creates a <see cref="DecoupledGrouping"/> with the given <paramref name="triangles"/> and <paramref name="edges"/>.
        /// </summary>
        /// <param name="triangles">The triangles of the grouping.</param>
        /// <param name="edges">The edges of the grouping.</param>
        public DecoupledGrouping(HashSet<Triangle> triangles, HashSet<Edge> edges)
        {
            Triangles = triangles;
            Edges = edges;
        }
    }
}
