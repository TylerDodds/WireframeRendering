// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// A group of <see cref="Edge"/>s that share a common <see cref="TextureLabel"/>.
    /// </summary>
    internal interface IEdgeGroup
    {
        /// <summary>The <see cref="Edge"/>s in the group.</summary>
        List<Edge> Edges { get; }
        /// <summary> The common <see cref="TextureLabel"/> assigned to this group.</summary>
        TextureLabel TextureLabel { get; }
    }

    /// <summary>
    /// Vertex information for an <see cref="EdgeGroup"/>, including a mapping from vertex indices to their index in the edge group list.
    /// </summary>
    internal class EdgeGroupVertexInformation
    {
        public EdgeGroupVertexInformation(Dictionary<int, List<int>> vertexEdgeGroupIndicesDictionary, MeshInformation meshInformation)
        {
            VertexEdgeGroupIndicesDictionary = vertexEdgeGroupIndicesDictionary;
            MeshInformation = meshInformation;
        }

        /// <summary>
        /// Dictionary mapping vertices' connected edges to their indices in the edge group list.
        /// </summary>
        public Dictionary<int, List<int>> VertexEdgeGroupIndicesDictionary { get; }
        /// <summary>
        /// The <see cref="MeshInformation"/>.
        /// </summary>
        public MeshInformation MeshInformation { get; }
    }

    /// <summary>
    /// A group of <see cref="Edge"/>s that consist of the boundary of a <see cref="DecoupledGrouping"/> of triangles that create a loop (cycle),
    /// along with indices marking important information for <see cref="TextureLabel"/> generation along that loop.
    /// </summary>
    internal class BoundaryEdgeCycle
    {
        /// <summary>
        /// List of <see cref="Edge"/>s in the cycle, in order.
        /// </summary>
        public List<Edge> Edges = new List<Edge>();

        /// <summary>
        /// Indices of <see cref="Edges"/> that share a triangle with the next in the list.
        /// </summary>
        public List<int> SharedTriangleIndices = new List<int>();

        /// <summary>
        /// Indices of <see cref="Edges"/> that have a non-insignificant angle different with the next in the list, along with their angles.
        /// </summary>
        public List<(int index, float angle)> AngleDifferenceIndices = new List<(int index, float angle)>();

    }

    /// <summary>
    /// A group of <see cref="Edge"/>s that share a common <see cref="TextureLabel"/>, and the <see cref="EdgeConnection"/> to other <see cref="EdgeGroup"/>s.
    /// </summary>
    internal class EdgeGroup : IEdgeGroup
    {
        /// <summary>The <see cref="Edge"/>s in the group.</summary>
        public List<Edge> Edges { get; } = new List<Edge>();
        /// <summary>The <see cref="EdgeConnection"/> coming from this group.</summary>
        public List<EdgeConnection> Connections { get; } = new List<EdgeConnection>();
        /// <summary> The common <see cref="TextureLabel"/> assigned to this group.</summary>
        public TextureLabel TextureLabel { get; set; } = TextureLabel.None;
    }

    /// <summary>
    /// Designates the connection between from an <see cref="Edge"/> to another <see cref="Edge"/> in a different <see cref="EdgeGroup"/>.
    /// </summary>
    internal class EdgeConnection
    {
        /// <summary>The <see cref="Edge"/> in which the connection starts.</summary>
        public Edge StartingEdge;
        /// <summary>The <see cref="Vertex"/> that the edges share.</summary>
        public Vertex Vertex;
        /// <summary>The <see cref="Triangle"/> in which both edges are found.</summary>
        public Triangle Triangle;
        /// <summary>The <see cref="Edge"/> that is connected to.</summary>
        public Edge OtherEdge;
        /// <summary>The <see cref="EdgeGroup"/> that the other <see cref="Edge"/> belongs to.</summary>
        public EdgeGroup OtherGroup;

        public EdgeConnection(Edge startingEdge, Vertex vertex, Triangle triangle, Edge otherEdge, EdgeGroup otherGroup)
        {
            StartingEdge = startingEdge;
            Vertex = vertex;
            Triangle = triangle;
            OtherEdge = otherEdge;
            OtherGroup = otherGroup;
        }
    }

    /// <summary>
    /// When marking an Edge with the texture coordinates used to indicate which edges to draw as wireframe, indices which of the three possible components are to be used.
    /// </summary>
    internal enum TextureLabel
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
    }
}
