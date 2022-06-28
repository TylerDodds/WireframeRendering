// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Sets up wireframe rendering of a <see cref="Mesh"/> by assigning the appropriate texture coordinates.
    /// </summary>
    internal class WireframeTextureCoordinateGenerator
    {
        /// <summary>
        /// Sets the wireframe texture coordinates based on boundary edges of decoupled portions of the mesh.
        /// </summary>
        /// <param name="mesh">The mesh to apply wireframe texture coordinates to.</param> 
        /// <param name="channel">The channel of the texture coordinates.</param>
        /// <param name="angleCutoffDegrees">Number of degrees between edges before they are considered to have different angles for wireframe texture generation.</param>
        /// <param name="raiseWarning">Action to raise a warning message.</param>
        public void DecoupleDisconnectedPortions(Mesh mesh, int channel, float angleCutoffDegrees, Action<string> raiseWarning)
        {
            _raiseWarning = raiseWarning;
            if(_raiseWarning == null)
            {
                _raiseWarning = w => { };
            }
            _meshInformation = MeshInformation.CreateAndUpdateFromSubmeshTriangleTopology(mesh);
            SetBoundaryUVs(channel, angleCutoffDegrees);
        }

        /// <summary>
        /// Sets the wireframe texture coordinates based on boundary edges of the mesh.
        /// </summary>
        /// <param name="channel">The channel of the texture coordinates.</param>
        /// <param name="angleCutoffDegrees">Number of degrees between edges before they are considered to have different angles for wireframe texture generation.</param>
        private void SetBoundaryUVs(int channel, float angleCutoffDegrees)
        {
            //For each decoupled group of edges, we expect most edges to belong to one or two triangles.
            //One triangle: it's a boundary (either because the mesh is not watertight, or edges are marked as creases and therefore vertices are split upon Unity's mesh import).
            //Two triangles: edge is part of a mesh surface.
            //Three or more triangles: edge is involved in the intersection of two surfaces. Mesh is clearly not watertight. This is probably unexpected in most cases.

            List<DecoupledGrouping> vertexSharedGroupings = TriangleGroupUtilities.GetDecoupledGroupings(_meshInformation, DecoupledGroupType.SharedVertices);

            Dictionary<DecoupledGrouping, IReadOnlyList<IEdgeGroup>> allEdgeGroups = new Dictionary<DecoupledGrouping, IReadOnlyList<IEdgeGroup>>();
            foreach (DecoupledGrouping vertexGrouping in vertexSharedGroupings)
            {
                List<DecoupledGrouping> groupings = TriangleGroupUtilities.GetDecoupledGroupings(_meshInformation, DecoupledGroupType.SharedEdges, vertexGrouping.Triangles);

                Dictionary<int, int> numGroupsEachVertexIsIn = new Dictionary<int, int>();
                foreach (DecoupledGrouping decoupledGrouping in groupings)
                {
                    foreach(int vertIndex in decoupledGrouping.Vertices)
                    {
                        if(!numGroupsEachVertexIsIn.ContainsKey(vertIndex))
                        {
                            numGroupsEachVertexIsIn[vertIndex] = 1;
                        }
                        else
                        {
                            numGroupsEachVertexIsIn[vertIndex] += 1;
                        }
                    }
                }

                HashSet<int> sharedVertices = new HashSet<int>(numGroupsEachVertexIsIn.Where(pair => pair.Value > 1).Select(pair => pair.Key));
                IOrderedEnumerable<DecoupledGrouping> orderedGroupings = groupings.OrderByDescending(g => g.Vertices.Count(v => sharedVertices.Contains(v))).ThenBy(g => g.Edges.Count);
                //NB This is just a rough ordering of groups that we want to solve. Groups that only have one shared vertex can be solved last, since they should be able to arrive
                //at the same solution as if they had no shared vetex, just up to a permutation of the labels.
                //Note that we'd need to do something like full backtracking over all possible labelings for the entire set of groupings to fully explore all possible solutions.
                //Since cases requiring this are extremely unlikely, we'll leave this as a future improvement.

                foreach (DecoupledGrouping decoupledGrouping in orderedGroupings)
                {
                    IReadOnlyList<IEdgeGroup> edgeGroups = EdgeTextureLabelUtilities.GetEdgeGroups(decoupledGrouping, _meshInformation, angleCutoffDegrees, _raiseWarning);
                    if(edgeGroups != null)
                    {
                        foreach(IEdgeGroup edgeGroup in edgeGroups)
                        {
                            foreach(int vertexIndex in edgeGroup.Edges.SelectMany(e => e.GetIndices()).Distinct())
                            {
                                Vertex vertex = _meshInformation.GetVertex(vertexIndex);
                                vertex.AddTextureLabel(edgeGroup.TextureLabel);
                            }
                        }
                        allEdgeGroups[decoupledGrouping] = edgeGroups;
                    }
                }
            }
            

            bool needThirdUvParameter = false;
            bool needFourthUvParameter = false;
            foreach (TextureLabel textureLabel in allEdgeGroups.SelectMany(groupPair => groupPair.Value).Select(group => group.TextureLabel))
            {
                needThirdUvParameter &= textureLabel == TextureLabel.Third;
                needFourthUvParameter &= textureLabel == TextureLabel.Fourth;
                if(needThirdUvParameter && needFourthUvParameter)
                {
                    break;
                }
            }

            if (!needThirdUvParameter && !needFourthUvParameter)
            {
                List<Vector2> uvs = GetUV2s(channel);
                SetBoundaryUVs(allEdgeGroups, (label, index) => uvs[index] = AssignOneToLabelledTextureComponent(label, uvs[index]));
                _meshInformation.Mesh.SetUVs(channel, uvs);
            }
            else if (!needFourthUvParameter)
            {
                List<Vector3> uvs = GetUV3s(channel);
                SetBoundaryUVs(allEdgeGroups, (label, index) => uvs[index] = AssignOneToLabelledTextureComponent(label, uvs[index]));
                _meshInformation.Mesh.SetUVs(channel, uvs);
            }
            else
            {
                List<Vector4> uvs = GetUV4s(channel);
                SetBoundaryUVs(allEdgeGroups, (label, index) => uvs[index] = AssignOneToLabelledTextureComponent(label, uvs[index]));
                _meshInformation.Mesh.SetUVs(channel, uvs);
            }
        }

        /// <summary>
        /// Gets the four-component texture coordinates in the given channel.
        /// </summary>
        /// <param name="channel">The uv channel.</param>
        /// <returns>The texture coordinates.</returns>
        private List<Vector4> GetUV4s(int channel)
        {
            List<Vector4> uvs = new List<Vector4>();
            _meshInformation.Mesh.GetUVs(channel, uvs);
            int vertexCount = _meshInformation.Mesh.vertexCount;
            if (uvs.Count < vertexCount)
            {
                uvs.AddRange(Enumerable.Repeat(new Vector4(0, 0, 0, 1), vertexCount - uvs.Count));
            }
            return uvs;
        }

        /// <summary>
        /// Gets the three-component texture coordinates in the given channel.
        /// </summary>
        /// <param name="channel">The uv channel.</param>
        /// <returns>The texture coordinates.</returns>
        private List<Vector3> GetUV3s(int channel)
        {
            List<Vector3> uvs = new List<Vector3>();
            _meshInformation.Mesh.GetUVs(channel, uvs);
            int vertexCount = _meshInformation.Mesh.vertexCount;
            if (uvs.Count < vertexCount)
            {
                uvs.AddRange(Enumerable.Repeat(Vector3.zero, vertexCount - uvs.Count));
            }
            return uvs;
        }

        /// <summary>
        /// Gets the two-component texture coordinates in the given channel.
        /// </summary>
        /// <param name="channel">The uv channel.</param>
        /// <returns>The texture coordinates.</returns>
        private List<Vector2> GetUV2s(int channel)
        {
            List<Vector2> uvs = new List<Vector2>();
            _meshInformation.Mesh.GetUVs(channel, uvs);
            int vertexCount = _meshInformation.Mesh.vertexCount;
            if (uvs.Count < vertexCount)
            {
                uvs.AddRange(Enumerable.Repeat(Vector2.zero, vertexCount - uvs.Count));
            }
            return uvs;
        }

        /// <summary>
        /// Assigns a value of 1 to the given component (x, y, z, w) of the texture coordinate.
        /// </summary>
        /// <param name="textureLabel">The <see cref="TextureLabel"/> indicating the component.</param>
        /// <param name="initialTextureCoords">The initial value of the texture coordinate.</param>
        /// <returns>The updated value of the texture coordinate.</returns>
        private static Vector4 AssignOneToLabelledTextureComponent(TextureLabel textureLabel, Vector4 initialTextureCoords)
        {
            Vector4 coords = initialTextureCoords;
            switch (textureLabel)
            {
                case TextureLabel.First:
                    coords.x = 1;
                    break;
                case TextureLabel.Second:
                    coords.y = 1;
                    break;
                case TextureLabel.Third:
                    coords.z = 1;
                    break;
                case TextureLabel.Fourth:
                    //Note that as per https://docs.unity3d.com/Manual/SL-VertexProgramInputs.html, the default w coordinate is set to 1 if the uv 
                    //contains fewer components than the vertex shader input needs, so we'll instead store 1 - texComponent in w, and undo this in the shader.
                    coords.w = 0;
                    break;
                case TextureLabel.None:
                default:
                    break;
            }
            return coords;
        }

        /// <summary>
        /// Set wireframe texture coordinate of vertices based on the <see cref="TextureLabel"/> of the edges they are a part of.
        /// </summary>
        /// <param name="edgeGroupings">The groups of <see cref="IEdgeGroup"/>s.</param>
        /// <param name="updateUvAtIndex">Action to update the UV value at a given vertex index.</param>
        private void SetBoundaryUVs(Dictionary<DecoupledGrouping, IReadOnlyList<IEdgeGroup>> edgeGroupings, Action<TextureLabel, int> updateUvAtIndex)
        {
            foreach (KeyValuePair<DecoupledGrouping, IReadOnlyList<IEdgeGroup>> pair in edgeGroupings)
            {
                foreach (IEdgeGroup edgeGroup in pair.Value)
                {
                    TextureLabel label = edgeGroup.TextureLabel;
                    foreach (Edge edge in edgeGroup.Edges)
                    {
                        updateUvAtIndex(label, edge.FirstIndex);
                        updateUvAtIndex(label, edge.SecondIndex);
                    }
                }
            }
        }

        private Action<string> _raiseWarning = null;
        private MeshInformation _meshInformation = null;
    }
}
