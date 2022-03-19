// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// A vertex in a mesh.
    /// </summary>
    internal class Vertex : IEnumerable<Edge>
    {
        /// <summary>The Index of this Vertex.</summary>
        public int Index;

        /// <summary>Whether each of the four texture labels has been assigned to this vertex.</summary>
        public TextureLabelAssigned TextureLabelsAssigned => _textureLabelsAssigned;

        private TextureLabelAssigned _textureLabelsAssigned = new TextureLabelAssigned(false, false, false, false);
        private HashSet<Edge> _edges = new HashSet<Edge>();
        private HashSet<Triangle> _triangles = new HashSet<Triangle>();

        /// <summary>
        /// Creates a <see cref="Vertex"/> with the given index.
        /// </summary>
        /// <param name="index">The vertex's index.</param>
        public Vertex(int index)
        {
            Index = index;
        }

        internal void AddTextureLabel(TextureLabel textureLabel)
        {
            _textureLabelsAssigned.AddTextureLabel(textureLabel);
        }

        /// <summary>Adds an <see cref="Edge"/> that contains this <see cref="Vertex"/>.</summary>
        /// <param name="edge">The <see cref="Edge"/> to add.</param>
        internal void AddEdge(Edge edge)
        {
            _edges.Add(edge);
        }

        /// <summary>Adds an <see cref="Triangle"/> that contains this <see cref="Vertex"/>.</summary>
        /// <param name="triangle">The <see cref="Triangle"/> to add.</param>
        internal void AddTriangle(Triangle triangle)
        {
            _triangles.Add(triangle);
        }

        ///<summary>Returns an enumerator that iterates through the <see cref="Triangle"/>s that contain this <see cref="Vertex"/>.</summary>
        public IEnumerable<Triangle> GetTriangles()
        {
            return _triangles;
        }

        ///<summary>Returns an enumerator that iterates through the <see cref="Edge"/>s that contain this <see cref="Vertex"/>.</summary>
        public IEnumerator<Edge> GetEnumerator()
        {
            return ((IEnumerable<Edge>)_edges).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Edge>)_edges).GetEnumerator();
        }

        public override string ToString()
        {
            return Index.ToString();
        }
    }

    /// <summary>
    /// An edge of a <see cref="Triangle"/> in a mesh. Consists of two vertex indices, ordered so the smaller vertex index comes first.
    /// </summary>
    internal class Edge : IEnumerable<Triangle>
    {
        /// <summary> First vertex index. </summary>
        public int FirstIndex;
        /// <summary> Second vertex index. </summary>
        public int SecondIndex;

        /// <summary> The number of <see cref="Triangle"/>s that contain this <see cref="Edge"/>. </summary>
        public int TriangleCount => _triangles.Count;

        private HashSet<Triangle> _triangles = new HashSet<Triangle>();

        public Edge(int v1, int v2)
        {
            bool forwards = v2 >= v1;
            FirstIndex = forwards ? v1 : v2;
            SecondIndex = forwards ? v2 : v1;
        }

        /// <summary>Adds a <see cref="Triangle"/> that contains this <see cref="Edge"/>.</summary>
        /// <param name="triangle">The <see cref="Triangle"/> to add.</param>
        internal void AddTriangle(Triangle triangle)
        {
            _triangles.Add(triangle);
        }

        ///<summary>Returns an enumerator that iterates through the indices of this <see cref="Edge"/>.</summary>
        public IEnumerable<int> GetIndices()
        {
            yield return FirstIndex;
            yield return SecondIndex;
        }

        ///<summary>Returns an enumerator that iterates through the <see cref="Triangle"/>s that contain this <see cref="Edge"/>.</summary>
        public IEnumerator<Triangle> GetEnumerator()
        {
            return ((IEnumerable<Triangle>)_triangles).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Triangle>)_triangles).GetEnumerator();
        }

        public override string ToString()
        {
            return $"{FirstIndex} {SecondIndex}";
        }

    }

    /// <summary>
    /// A triangle in a mesh. Consists of three vertex indices and the three associated <see cref="Edge"/>s.
    /// </summary>
    internal class Triangle : IEnumerable<Edge>
    {
        ///<summary>The first vertex index.</summary>
        public int Index1 { get; }
        ///<summary>The second vertex index.</summary>
        public int Index2 { get; }
        ///<summary>The third vertex index.</summary>
        public int Index3 { get; }

        public Edge FirstEdge => _edges[0];
        public Edge SecondEdge => _edges[1];
        public Edge ThirdEdge => _edges[2];

        private Edge[] _edges = new Edge[3];

        /// <summary>
        /// Creates a triangles with the given <paramref name="indices"/>.
        /// </summary>
        /// <param name="indices">The triangle's indices.</param>
        public Triangle(Vector3Int indices) : this(indices.x, indices.y, indices.z)
        { }

        public Triangle(int index1, int index2, int index3)
        {
            Index1 = index1;
            Index2 = index2;
            Index3 = index3;
        }

        /// <summary>
        /// All <see cref="Edge"/>s that touch vertices of this triangle, but are not a part of it.
        /// </summary>
        internal IReadOnlyList<Edge> ConnectedEdges => _connectedEdges;

        /// <summary>
        /// Gets the indices of the triangle.
        /// </summary>
        /// <returns>The indices of the triangle.</returns>
        public IEnumerable<int> GetIndices()
        {
            yield return Index1;
            yield return Index2;
            yield return Index3;
            yield break;
        }


        ///<summary>Gets the pairs of vertex indices of each edge, ordered so the smaller vertex index comes first.</summary>
        public (Vector2Int edge1, Vector2Int edge2, Vector2Int edge3) GetOrderedEdgesIndices()
        {
            Vector2Int edge1 = GetOrderedEdgeIndices(Index1, Index2);
            Vector2Int edge2 = GetOrderedEdgeIndices(Index2, Index3);
            Vector2Int edge3 = GetOrderedEdgeIndices(Index3, Index1);
            return (edge1, edge2, edge3);
        }

        /// <summary>
        /// Sets the <see cref="Edge"/> representation for the given <see cref="EdgeIndex"/>.
        /// </summary>
        /// <param name="edge">The instance of the <see cref="Edge"/> class.</param>
        /// <param name="edgeIndex">Which of the three edges to set.</param>
        internal void SetEdge(Edge edge, EdgeIndex edgeIndex)
        {
            switch (edgeIndex)
            {
                case EdgeIndex.One:
                    _edges[0] = edge;
                    break;
                case EdgeIndex.Two:
                    _edges[1] = edge;
                    break;
                case EdgeIndex.Three:
                    _edges[2] = edge;
                    break;
            }
        }

        internal void SetConnectedEdges(IEnumerable<Edge> connectedEdges)
        {
            _connectedEdges.Clear();
            _connectedEdges.AddRange(connectedEdges);
        }

        /// <summary>
        /// Gets a <see cref="Vector2Int"/> with the input integer indices <paramref name="a"/> and <paramref name="b"/> ordered.
        /// </summary>
        /// <param name="a">The first index.</param>
        /// <param name="b">The second index.</param>
        /// <returns>The indices, ordered.</returns>
        private static Vector2Int GetOrderedEdgeIndices(int a, int b)
        {
            return new Vector2Int(Mathf.Min(a, b), Mathf.Max(a, b));
        }

        ///<summary>Returns an enumerator that iterates through the <see cref="Edge"/>s of the triangle.</summary>
        public IEnumerator<Edge> GetEnumerator()
        {
            return ((IEnumerable<Edge>)_edges).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Edge>)_edges).GetEnumerator();
        }

        public override string ToString()
        {
            return $"{Index1} {Index2} {Index3}";
        }

        private List<Edge> _connectedEdges = new List<Edge>();
    }

    /// <summary>
    /// Enumeration of possible edge indicees of a triangle.
    /// </summary>
    internal enum EdgeIndex
    {
        One,
        Two,
        Three
    }
}
