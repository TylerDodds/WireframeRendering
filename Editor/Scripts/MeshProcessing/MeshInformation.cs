// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Information about the relationships of vertices, edges, and triangles in a <see cref="Mesh"/>.
    /// </summary>
    internal class MeshInformation
    {
        /// <summary>
        /// Constructor for <see cref="MeshInformation"/> from a <paramref name="mesh"/>.
        /// </summary>
        /// <param name="mesh">The <see cref="Mesh"/>.</param>
        public MeshInformation(Mesh mesh)
        {
            Mesh = mesh;

            RefreshAllVertices();
        }

        /// <summary>
        /// Gets the angle in degrees (0 to 90) between two edges.
        /// </summary>
        /// <param name="first">The first edge.</param>
        /// <param name="second">The second edge.</param>
        /// <returns>The angle.</returns>
        internal float GetEdgeAngleDegrees(Edge first, Edge second)
        {
            Vector3 firstDir = GetEdgeDirection(first);
            Vector3 secondDir = GetEdgeDirection(second);
            float angle = Vector3.Angle(firstDir, secondDir);
            return Mathf.Min(180f - angle, angle);
        }

        /// <summary>
        /// Gets the <see cref="Edge"/>s of the mesh.
        /// </summary>
        /// <returns>The set of unique <see cref="Edge"/>.</returns>
        internal HashSet<Edge> GetEdges() => new HashSet<Edge>(_edges.Values);

        /// <summary>
        /// Gets the <see cref="Vertex"/> at the given index.
        /// </summary>
        /// <param name="index">The index of the <see cref="Vertex"/>.</param>
        /// <returns>The <see cref="Vertex"/>.</returns>
        internal Vertex GetVertex(int index) => _vertices[index];

        /// <summary>
        /// Updates the information with the triangles of a given submesh index.
        /// </summary>
        /// <param name="submeshIndex">The index of the submesh.</param>
        internal void UpdateMeshInformation(int submeshIndex)
        {
            UpdateTriangleIndices(submeshIndex);
            AddNewTrianglesAndEdges();
        }

        /// <summary>
        /// Gets the direction of the <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The <see cref="Edge"/>.</param>
        /// <returns>The direction of the <paramref name="edge"/>.</returns>
        private Vector3 GetEdgeDirection(Edge edge)
        {
            Vector3 dir = Vector3.zero;
            if (_vertexPositions != null && edge.FirstIndex < _vertexPositions.Length && edge.SecondIndex < _vertexPositions.Length)
            {
                Vector3 start = _vertexPositions[edge.FirstIndex];
                Vector3 end = _vertexPositions[edge.SecondIndex];
                dir = (end - start).normalized;
            }
            return dir;
        }

        /// <summary>
        /// Updates the <see cref="_triangleIndices"/> based on the given submesh index.
        /// </summary>
        /// <param name="submeshIndex">The index of the submesh.</param>
        private void UpdateTriangleIndices(int submeshIndex)
        {
            Mesh.GetTriangles(_triangleIndices, submeshIndex);
        }

        /// <summary>
        /// Adds <see cref="Triangle"/> and <see cref="Edge"/> data structures based on the <see cref="_triangleIndices"/>.
        /// </summary>
        private void AddNewTrianglesAndEdges()
        {
            int numTriangleIndices = _triangleIndices.Count;
            for (int index = 0; index < numTriangleIndices; index += 3)
            {
                Vector3Int triangleIndexTriplet = new Vector3Int(_triangleIndices[index], _triangleIndices[index + 1], _triangleIndices[index + 2]);
                Triangle triangle = new Triangle(triangleIndexTriplet);
                (Vector2Int edgeIndices1, Vector2Int edgeIndices2, Vector2Int edgeIndices3) = triangle.GetOrderedEdgesIndices();
                void TryAddEdge(Vector2Int edgeIndices, EdgeIndex edgeIndex)
                {
                    if (!_edges.ContainsKey(edgeIndices))
                    {
                        Edge newEdge = new Edge(edgeIndices.x, edgeIndices.y);
                        _edges[edgeIndices] = newEdge;

                    }
                    Edge edge = _edges[edgeIndices];
                    edge.AddTriangle(triangle);
                    triangle.SetEdge(edge, edgeIndex);
                    _vertices[edge.FirstIndex].AddEdge(edge);
                    _vertices[edge.SecondIndex].AddEdge(edge);
                }
                TryAddEdge(edgeIndices1, EdgeIndex.One);
                TryAddEdge(edgeIndices2, EdgeIndex.Two);
                TryAddEdge(edgeIndices3, EdgeIndex.Three);
                _triangles.Add(triangle);
            }
            HashSet<Edge> connectedEdges = new HashSet<Edge>();
            foreach(Triangle triangle in _triangles)
            {
                connectedEdges.Clear();
                void TryAddEdge(Edge e)
                {
                    if(!connectedEdges.Contains(e) && e != triangle.FirstEdge && e != triangle.SecondEdge && e != triangle.ThirdEdge)
                    {
                        connectedEdges.Add(e);
                    }
                }
                void TryAddEdges(int vertexIndex)
                {
                    foreach(Edge e in _vertices[vertexIndex])
                    {
                        TryAddEdge(e);
                    }
                }
                TryAddEdges(triangle.Index1);
                TryAddEdges(triangle.Index2);
                TryAddEdges(triangle.Index3);
                triangle.SetConnectedEdges(connectedEdges);
            }
        }

        /// <summary>
        /// Clears the <see cref="_vertices"/> based on the number of vertices in the mesh.
        /// </summary>
        private void RefreshAllVertices()
        {
            _vertices.Clear();
            for (int i = 0; i < Mesh.vertexCount; i++)
            {
                _vertices.Add(new Vertex(i));
            }
            _vertexPositions = Mesh.vertices;
        }

        /// <summary>
        /// The <see cref="UnityEngine.Mesh"/>.
        /// </summary>
        public Mesh Mesh { get; }

        private List<int> _triangleIndices = new List<int>();
        private Vector3[] _vertexPositions = null;

        private Dictionary<Vector2Int, Edge> _edges = new Dictionary<Vector2Int, Edge>();
        private HashSet<Triangle> _triangles = new HashSet<Triangle>();
        private List<Vertex> _vertices = new List<Vertex>();
    }
}
