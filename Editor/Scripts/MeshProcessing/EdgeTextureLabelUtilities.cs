// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Utility functions for assigning wireframe texture labels to edges in a mesh.
    /// </summary>
    internal static class EdgeTextureLabelUtilities
    {
        /// <summary>
        /// Gets groups of edges with the same <see cref="TextureLabel"/>.
        /// </summary>
        /// <param name="decoupledGrouping">A contiguous set of triangles and their edges.</param>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="angleCutoffDegrees">Number of degrees between edges before they are considered to have different angles for wireframe texture generation.</param>
        /// <param name="raiseWarning"><see cref="System.Action"/> to raise a warning encountered during processing.</param>
        /// <returns>The set of <see cref="IEdgeGroup"/>s.</returns>
        internal static IReadOnlyList<IEdgeGroup> GetEdgeGroups(DecoupledGrouping decoupledGrouping, MeshInformation meshInformation, float angleCutoffDegrees, System.Action<string> raiseWarning)
        {
            BoundaryEdgeCycle boundaryEdgeCycle = GetBoundaryEdgeCycle(decoupledGrouping, meshInformation, angleCutoffDegrees, raiseWarning);

            if (boundaryEdgeCycle != null)
            {
                BoundaryGrouping boundaryGrouping = TriangleGroupUtilities.GetBoundaryGrouping(decoupledGrouping, boundaryEdgeCycle);
                EdgeGroupResults edgeGroupResults = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
                if(!edgeGroupResults.HasValue)
                {
                    if (edgeGroupResults.CalculationTimedOut)
                    {
                        raiseWarning($"From a boundary cycle containing {boundaryGrouping.Edges.Count} edges, no texture labels were assignable within {edgeGroupResults.TimeoutSeconds} seconds.");
                    }
                    else
                    {
                        raiseWarning($"From a boundary cycle containing {boundaryGrouping.Edges.Count} edges, no texture labels were assignable under any conditions.");
                    }
                }
                return edgeGroupResults.EdgeGroups;
            }
            else
            {
                raiseWarning("Boundary edges do not form a loop. Cannot assign wireframe coordinates in this unsupported case.");
                return null;
            }
        }

        //Note that for any triangle, we would want a wireframe edge to have one of the texture components be 1 on both vertices, but 0 on the other vertex of the triangle (or else the entire triangle would be filled in).
        //As we traverse boundary loops, some triangles will end up with two edges that are wireframe (boundary) edges.
        //The triangle would look something like this (being the bottom-right corner of a surface, with boundary edges on the bottom and right):
        //   /|
        //  / |
        // /__|
        //In this case the bottom edge B and right edge R require different texture labels. Furthermore, B's neighbouring boundary edge must have a different texture label than R, or else the leftmost vertex would end up with a value of 1
        //for the same texture component as the two vertices in edge R. Similarly, R's neighbouring boundary edge must have a different texture label than B.

        //There is one additional restriction on texture labels, which occurs when two edges don't share a triangle, but do have a large physical angle difference between them, and their shared vertex is only part of one other edge. 
        //This occurs in the case of a corner of a boundary edge loop, but one where edges are not shared. Something like this (being the bottom-left corner):
        // |  /
        // | /
        // |/____
        //This means that the screen-space direction of each edge varies significantly, and when these edges share the same texture label, a wireframe of width W perpendicular to each line in screen space would not intersect that other edge in the same spot.
        //If instead different texture labels are assigned to these edges, the shared vertex would act, for each triangle, as the lone texture coordinate of that type set to 1 (assuming further-out neighbors share the texture label of their neighbors shown here).
        //In this case, the triangle has a texture component with a value of 1 for a single vertex, which will lead to a wireframe just at that vertex, 
        //but it would have in the above case a screen-space derivative more similar to the main texture coordinate in the neighbouring triangle.

        //Note that groups of at least two consecutive edges that don't share triangle or have significant angle differences will satisfy the above criteria from edges from neighboring groups.

        /// <summary>
        /// Gets groups of edges with the same <see cref="TextureLabel"/>.
        /// </summary>
        /// <param name="boundaryEdgeCycle">The <see cref="BoundaryEdgeCycle"/> representing a loop of boundary <see cref="Edge"/>s.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <param name="meshInformation">The mesh information.</param>
        /// <returns>The set of <see cref="IEdgeGroup"/>s.</returns>
        internal static EdgeGroupResults GetBoundaryCycleEdgeGroups(BoundaryEdgeCycle boundaryEdgeCycle, BoundaryGrouping boundaryGrouping, MeshInformation meshInformation)
        {
            List<Edge> boundaryEdges = boundaryEdgeCycle.Edges;

            Dictionary<int, float> edgeIndexToEdgeAngleDictionary = boundaryEdgeCycle.AngleDifferenceIndices.ToDictionary(pair => pair.index, pair => pair.angle);

            List<GroupCutIndex> groupCutEdgeIndices = boundaryEdgeCycle.SharedTriangleIndices.Select(index => new GroupCutIndex(index, GroupCutType.SameTriangle))
                .Concat(boundaryEdgeCycle.AngleDifferenceIndices.Select(indexAndAngle => new GroupCutIndex(indexAndAngle.index, GroupCutType.EdgeAngle))).OrderBy(p => p.EdgeIndex).ToList();

            EdgeGroupResults edgeGroups = GetEdgeGroupsIncludingVirtualCuts(boundaryEdges, groupCutEdgeIndices, boundaryGrouping, meshInformation);

            while(!edgeGroups.HasValue && !edgeGroups.CalculationTimedOut && groupCutEdgeIndices.Count > 0)
            {
                int edgeIndexToRemove = GetEdgeIndexToRemove(groupCutEdgeIndices, edgeIndexToEdgeAngleDictionary);
                groupCutEdgeIndices.RemoveAll(value => value.EdgeIndex == edgeIndexToRemove);

                edgeGroups = GetEdgeGroupsIncludingVirtualCuts(boundaryEdges, groupCutEdgeIndices, boundaryGrouping, meshInformation);
            }

            return edgeGroups;
        }

        /// <summary>
        /// Gets the edge index of the least-significant cut to remove from <paramref name="groupCutEdgeIndices"/> in cases where no solution can be found.
        /// </summary>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="edgeIndexToEdgeAngleDictionary">A dictionary of edge indices to edge cut angles, for relevant edge cuts.</param>
        /// <returns>The edge index of the least-significant cut</returns>
        private static int GetEdgeIndexToRemove(List<GroupCutIndex> groupCutEdgeIndices, Dictionary<int, float> edgeIndexToEdgeAngleDictionary)
        {
            int edgeIndexToRemove;
            int groupCutEdgeIndicesCount = groupCutEdgeIndices.Count;
            Vector2Int GetNeighbouringGroupSizes(int edgeIndex)
            {
                int indexInGroupCutEdgeIndices = groupCutEdgeIndices.FindIndex(pair => pair.EdgeIndex == edgeIndex);
                int indexOneAfterCyclic = PeriodicUtilities.GetIndexPeriodic(indexInGroupCutEdgeIndices + 1, groupCutEdgeIndicesCount);
                int indexOneBeforeCyclic = PeriodicUtilities.GetIndexPeriodic(indexInGroupCutEdgeIndices - 1, groupCutEdgeIndicesCount);
                GroupCutIndex groupCutPrev = groupCutEdgeIndices[indexOneBeforeCyclic];
                GroupCutIndex groupCutNext = groupCutEdgeIndices[indexOneAfterCyclic];
                int groupSizePrev = PeriodicUtilities.GetIndexDistance(groupCutPrev.EdgeIndex, edgeIndex, groupCutEdgeIndicesCount);
                int groupSizeNext = PeriodicUtilities.GetIndexDistance(edgeIndex, groupCutNext.EdgeIndex, groupCutEdgeIndicesCount);
                return new Vector2Int(groupSizePrev, groupSizeNext);
            }
            int NumberOfOneSizedGroupsRemoved(int edgeIndex)
            {
                Vector2Int neighbouringGroupSizes = GetNeighbouringGroupSizes(edgeIndex);
                return (neighbouringGroupSizes.x < 2 ? 1 : 0) + (neighbouringGroupSizes.y < 2 ? 1 : 0);
            }
            int GetTotalNeighbouringGroupSizes(int edgeIndex)
            {
                Vector2Int neighbouringGroupSizes = GetNeighbouringGroupSizes(edgeIndex);
                return neighbouringGroupSizes.x + neighbouringGroupSizes.y;
            }
			
            IEnumerable<GroupCutIndex> edgeAngleCuts = groupCutEdgeIndices.Where(pair => pair.CutType == GroupCutType.EdgeAngle);
            if (edgeAngleCuts.Any())
            {
                edgeIndexToRemove = edgeAngleCuts.Select(pair => (pair.EdgeIndex, angle: edgeIndexToEdgeAngleDictionary[pair.EdgeIndex])).OrderBy(values => values.angle)
                    .ThenByDescending(values => NumberOfOneSizedGroupsRemoved(values.EdgeIndex))
                    .ThenBy(values => GetTotalNeighbouringGroupSizes(values.EdgeIndex))
                    .First().EdgeIndex;
            }
            else if(_removeSameTriangleGroupCuts)
            {
                //At least one left of GroupCutType.SameTriangle
                edgeIndexToRemove = groupCutEdgeIndices
                    .OrderByDescending(values => NumberOfOneSizedGroupsRemoved(values.EdgeIndex))
                    .ThenBy(values => GetTotalNeighbouringGroupSizes(values.EdgeIndex))
                    .First().EdgeIndex;
            }
            else
            {
                edgeIndexToRemove = -1;
            }
            return edgeIndexToRemove;
        }

        /// <summary>
        /// Gets the set of <see cref="EdgeGroup"/>s and the corresponding <see cref="GroupCutType"/>s between <see cref="EdgeGroup"/>s and their neighbours.
        /// </summary>
        /// <param name="boundaryEdges">The list of boundary <see cref="Edge"/>s in a cycle.</param>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <param name="meshInformation">The <see cref="MeshInformation"/>.</param>
        /// <param name="edgeGroups">The set of <see cref="EdgeGroup"/>s.</param>
        /// <param name="groupCuts">The <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="edgeGroupVertexInformation">The <see cref="EdgeGroupVertexInformation"/> for the <paramref name="edgeGroups"/>.</param>
        private static void GetEdgeGroupsAndGroupCutTypes(List<Edge> boundaryEdges, List<GroupCutIndex> groupCutEdgeIndices, BoundaryGrouping boundaryGrouping, MeshInformation meshInformation, out List<EdgeGroup> edgeGroups, out List<GroupCutType> groupCuts, out EdgeGroupVertexInformation edgeGroupVertexInformation)
        {
            bool anyCuts = groupCutEdgeIndices.Any();
            int boundaryEdgesCount = boundaryEdges.Count;
            List<int> groupSizes = anyCuts ?
                groupCutEdgeIndices.Zip(groupCutEdgeIndices.Prepend(groupCutEdgeIndices.Last()), (next, prev) => PeriodicUtilities.GetIndexDistance(prev.EdgeIndex, next.EdgeIndex, boundaryEdgesCount)).ToList()
                : new List<int> { boundaryEdgesCount };

            int[] startIndices = anyCuts ? groupCutEdgeIndices.Select(i => PeriodicUtilities.GetIndexPeriodic(i.EdgeIndex + 1, boundaryEdgesCount)).OrderBy(i => i).ToArray() : new int[] { 0 };
            groupCuts = anyCuts ? groupCutEdgeIndices.Select(pair => pair.CutType).ToList() : new List<GroupCutType>() { GroupCutType.Virtual};

            //Build edge groups, but we don't need to include Connections here, since they're guaranteed by the order of the cycle.
            edgeGroups = new List<EdgeGroup>();
            for (int groupIndex = 0; groupIndex < startIndices.Length; groupIndex++)
            {
                int startIndex = startIndices[groupIndex];
                int groupSize = groupSizes[groupIndex];
                EdgeGroup group = new EdgeGroup();
                for (int i = startIndex; i < startIndex + groupSize; i++)
                {
                    group.Edges.Add(boundaryEdges[i]);
                }
                edgeGroups.Add(group);
            }

            Dictionary<int, List<int>> vertexEdgeGroupIndicesDictionary = new Dictionary<int, List<int>>();

            HashSet<int> distinctVertexIndicesOfTrianglesOnEdges = boundaryGrouping.VertexIndicesOfTrianglesCompletelyTouchingBoundaryEdges;
            if (distinctVertexIndicesOfTrianglesOnEdges.Any())
            {
                Dictionary<Edge, int> edgeGroupIndices = new Dictionary<Edge, int>();
                for (int edgeGroupIndex = 0; edgeGroupIndex < edgeGroups.Count; edgeGroupIndex++)
                {
                    EdgeGroup edgeGroup = edgeGroups[edgeGroupIndex];
                    foreach (Edge edge in edgeGroup.Edges)
                    {
                        edgeGroupIndices[edge] = edgeGroupIndex;
                    }
                }

                foreach (int vertexIndex in distinctVertexIndicesOfTrianglesOnEdges)
                {
                    List<int> edgeIndices = new List<int>();
                    Vertex vertex = meshInformation.GetVertex(vertexIndex);
                    foreach (Edge e in vertex)
                    {
                        if (edgeGroupIndices.ContainsKey(e))
                        {
                            edgeIndices.Add(edgeGroupIndices[e]);
                        }
                    }

                    //By construction, there will always be edge indices
                    vertexEdgeGroupIndicesDictionary[vertexIndex] = edgeIndices;
                }
            }

            edgeGroupVertexInformation = new EdgeGroupVertexInformation(vertexEdgeGroupIndicesDictionary, meshInformation);
        }

        /// <summary>
        /// Gets a list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.
        /// </summary>
        /// <param name="boundaryEdges">The list of boundary <see cref="Edge"/>s in a cycle.</param>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <param name="meshInformation">The mesh information.</param>
        /// <returns>A list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.</returns>
        private static EdgeGroupResults GetEdgeGroupsIncludingVirtualCuts(List<Edge> boundaryEdges, List<GroupCutIndex> groupCutEdgeIndices, BoundaryGrouping boundaryGrouping, MeshInformation meshInformation)
        {
            return GetEdgeGroupsIncludingVirtualCuts(boundaryEdges, groupCutEdgeIndices, boundaryGrouping, meshInformation, 4);
        }

        /// <summary>
        /// Gets a list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.
        /// </summary>
        /// <param name="boundaryEdges">The list of boundary <see cref="Edge"/>s in a cycle.</param>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <returns>A list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.</returns>
        private static EdgeGroupResults GetEdgeGroupsIncludingVirtualCuts(List<Edge> boundaryEdges, List<GroupCutIndex> groupCutEdgeIndices, BoundaryGrouping boundaryGrouping, MeshInformation meshInformation, int maxNumLabels)
        {
            GetEdgeGroupsAndGroupCutTypes(boundaryEdges, groupCutEdgeIndices, boundaryGrouping, meshInformation, out List<EdgeGroup> edgeGroups, out List<GroupCutType> groupCuts, out EdgeGroupVertexInformation edgeGroupVertexInformation);
            EdgeTextureLabelBacktracking.BacktrackResults backtrackResults = GetTextureLabelsForFixedGroupCuts(edgeGroups, groupCuts, edgeGroupVertexInformation, boundaryGrouping, maxNumLabels);
            int numBoundaryEdges = boundaryEdges.Count;

            if (!backtrackResults.HasResult)
            {
                groupCutEdgeIndices = new List<GroupCutIndex>(groupCutEdgeIndices);//Need to clone so that original group isn't affected
            }

            while (!backtrackResults.HasResult && groupCutEdgeIndices.Count <= numBoundaryEdges)
            {
                int numGroupCutIndices = groupCutEdgeIndices.Count;
                if (numGroupCutIndices == 0)
                {
                    //NB Don't expect this case, but add virtual cut so the single resulting group starts at index 0
                    groupCutEdgeIndices.Add(new GroupCutIndex(numBoundaryEdges - 1, GroupCutType.Virtual));
                }
                else if (numGroupCutIndices == 1)
                {
                    groupCutEdgeIndices.Add(new GroupCutIndex(PeriodicUtilities.GetIndexPeriodic(groupCutEdgeIndices[0].EdgeIndex + 2, numBoundaryEdges), GroupCutType.Virtual));
                }
                else
                {
                    bool addedGroupCutEdgeIndex = TryAddBestVirtualGroupCut(groupCutEdgeIndices, numBoundaryEdges, backtrackResults.RejectedAnyBasedOnTriangles);
                    if (!addedGroupCutEdgeIndex)
                    {
                        break;
                    }
                }

                //NB Need to reorder groupCutIndices, and note that groups will always start at zero, since it should always have cut at the last index (no matter which are added)
                groupCutEdgeIndices.Sort((first, second) => first.EdgeIndex.CompareTo(second.EdgeIndex));
                GetEdgeGroupsAndGroupCutTypes(boundaryEdges, groupCutEdgeIndices, boundaryGrouping, meshInformation, out edgeGroups, out groupCuts, out edgeGroupVertexInformation);
                backtrackResults = GetTextureLabelsForFixedGroupCuts(edgeGroups, groupCuts, edgeGroupVertexInformation, boundaryGrouping, maxNumLabels);
            }

            if (backtrackResults.HasResult)
            {
                List<TextureLabel> labels = backtrackResults.Labels;
                for (int i = 0; i < labels.Count; i++)
                {
                    edgeGroups[i].TextureLabel = labels[i];
                }
                return new EdgeGroupResults(edgeGroups);
            }
            else
            {
                return new EdgeGroupResults(backtrackResults.RejectedBasedOnTimeout, backtrackResults.RejectionTimeoutSeconds);
            }
        }

        /// <summary>
        /// Tries to add to <paramref name="groupCutEdgeIndices"/> a <see cref="GroupCutType.Virtual"/> cut best suited to allow assigning of texture labels satisfying <paramref name="groupCutEdgeIndices"/> constraints.
        /// </summary>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="numBoundaryEdges">The number of boundary edges.</param>
        /// <param name="addAnyVirtualCutsPossible">If any possible cuts should be added.</param>
        /// <returns>If a <see cref="GroupCutIndex"/> of <see cref="GroupCutType.Virtual"/> could be added.</returns>
        private static bool TryAddBestVirtualGroupCut(List<GroupCutIndex> groupCutEdgeIndices, int numBoundaryEdges, bool addAnyVirtualCutsPossible)
        {
            int numGroupCutIndices = groupCutEdgeIndices.Count;
            if(numGroupCutIndices < 2)
            {
                throw new System.ArgumentException($"{nameof(groupCutEdgeIndices)} should have at least two entries in {nameof(TryAddBestVirtualGroupCut)}, {numGroupCutIndices} given.");
            }
            List<(GroupCutIndex ChosenCut, int Distance, bool NextNeighbourChosen)> relevantCutDistanceAndNext = new List<(GroupCutIndex, int, bool)>();
            GroupCutIndex prev = groupCutEdgeIndices.Last();
            GroupCutIndex curr = groupCutEdgeIndices[0];
            int prevDistance = PeriodicUtilities.GetIndexDistance(prev.EdgeIndex, curr.EdgeIndex, numBoundaryEdges);
            for (int i = 0; i < numGroupCutIndices; i++)
            {
                int indexOfNextGroupCut = PeriodicUtilities.GetIndexPeriodic(i + 1, numGroupCutIndices);
                GroupCutIndex next = groupCutEdgeIndices[indexOfNextGroupCut];
                int nextDistance = PeriodicUtilities.GetIndexDistance(curr.EdgeIndex, next.EdgeIndex, numBoundaryEdges);

                if (next.CutType == GroupCutType.Virtual && prev.CutType != GroupCutType.Virtual && prevDistance > 1)
                {
                    relevantCutDistanceAndNext.Add((prev, prevDistance, false));

                }
                else if (next.CutType != GroupCutType.Virtual && prev.CutType == GroupCutType.Virtual && nextDistance > 1)
                {
                    relevantCutDistanceAndNext.Add((next, nextDistance, true));
                }
                else
                {
                    //Choose based on furthest distance
                    bool chooseNext = nextDistance > prevDistance;
                    GroupCutIndex toChoose = chooseNext ? next : prev;
                    int chosenDistance = chooseNext ? nextDistance : prevDistance;
                    relevantCutDistanceAndNext.Add((toChoose, chosenDistance, chooseNext));
                }

                prev = curr;
                curr = next;
                prevDistance = nextDistance;
            }

            //Note that we only care about pairs with Distance > 1, since the rest already have two neighbouring cuts of Distance 1
            IEnumerable<(GroupCutIndex Cut, GroupCutIndex ChosenCut, int Distance, bool NextNeighbourChosen)> cutsAndDistancesPaired = groupCutEdgeIndices
                .Zip(relevantCutDistanceAndNext, (groupCutIndex, chosen) => (Cut: groupCutIndex, chosen.ChosenCut, chosen.Distance, chosen.NextNeighbourChosen))
                .Where(pair => pair.Distance > 1);

            bool added = false;
            if (cutsAndDistancesPaired.Any())
            {
                (GroupCutIndex Cut, GroupCutIndex ChosenCut, int Distance, bool NextNeighbourChosen) cutToAddNear = cutsAndDistancesPaired
                .Aggregate((Cut: new GroupCutIndex(-1, GroupCutType.Virtual), ChosenCut: new GroupCutIndex(-1, GroupCutType.Virtual), Distance: -1, NextChosen: false),
                    (min, next) => min.Cut.CutType == GroupCutType.Virtual && next.Cut.CutType != GroupCutType.Virtual ? next :
                        min.ChosenCut.CutType == GroupCutType.Virtual && next.ChosenCut.CutType != GroupCutType.Virtual ? next :
                        next.Distance > min.Distance ? next : min);

                if (!addAnyVirtualCutsPossible && cutToAddNear.Cut.CutType == GroupCutType.Virtual && cutToAddNear.ChosenCut.CutType == GroupCutType.Virtual)
                {
                    //If best cut is towards another virtual cut, adding another label to the group in between won't affect any non-Virtual GroupCutType restrictions
                }
                else
                {
                    int distanceToCut = Mathf.Min(2, cutToAddNear.Distance - 1);
                    int cutIndex = PeriodicUtilities.GetIndexPeriodic(cutToAddNear.Cut.EdgeIndex + (cutToAddNear.NextNeighbourChosen ? distanceToCut : -distanceToCut), numBoundaryEdges);
                    groupCutEdgeIndices.Add(new GroupCutIndex(cutIndex, GroupCutType.Virtual));
                    added = true;
                }
            }
            else
            {
                //There were no cuts left that have any neighbors that are not cuts, so nothing else to add, even if addCutsBetweenVirtualCuts
            }

            return added;
        }

        /// <summary>
        /// Gets <see cref="EdgeTextureLabelBacktracking.BacktrackResults"/> with a list of <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="edgeGroups"/> with a fixed set of <paramref name="groupCuts"/>.
        /// </summary>
        /// <param name="edgeGroups">The set of <see cref="EdgeGroup"/>s.</param>
        /// <param name="groupCuts">The <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="edgeGroupVertexInformation">The <see cref="EdgeGroupVertexInformation"/> for the <paramref name="edgeGroups"/>.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <returns><see cref="EdgeTextureLabelBacktracking.BacktrackResults"/> with a list of <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="edgeGroups"/>.</returns>
        private static EdgeTextureLabelBacktracking.BacktrackResults GetTextureLabelsForFixedGroupCuts(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, EdgeGroupVertexInformation edgeGroupVertexInformation, BoundaryGrouping boundaryGrouping, int maxNumLabels)
        {
            List<TextureLabel> textureLabels = GroupSizesAtLeastTwoTextureLabels(edgeGroups, maxNumLabels);
            EdgeTextureLabelBacktracking.BacktrackResults backtrackResults = new EdgeTextureLabelBacktracking.BacktrackResults(textureLabels, false);
            if (backtrackResults.HasResult)
            {
                backtrackResults.RejectedAnyBasedOnTriangles = EdgeTextureLabelBacktracking.BacktrackRejectIfTriangleHasAllOneLabelOrNewVertexLabelAssigned(edgeGroups, groupCuts, textureLabels, edgeGroupVertexInformation, boundaryGrouping);
            }

            if (!backtrackResults.HasResult)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                backtrackResults = EdgeTextureLabelBacktracking.BacktrackTextureLabels(edgeGroups, groupCuts, edgeGroupVertexInformation, boundaryGrouping, maxNumLabels, new List<TextureLabel>(), stopwatch);
            }

            return backtrackResults;
        }

        /// <summary>
        /// Assigns <see cref="TextureLabel"/>s in the more-straightforward case that all <paramref name="edgeGroups"/> have size at least two.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <returns>A list of <see cref="TextureLabel"/> per <see cref="EdgeGroup"/>, if one satisfies the constraints.</returns>
        private static List<TextureLabel> GroupSizesAtLeastTwoTextureLabels(List<EdgeGroup> edgeGroups, int maxNumLabels)
        {
            List<TextureLabel> textureLabels = null;
            bool allGroupsSize2 = edgeGroups.All(eg => eg.Edges.Count >= 2);
            int edgeGroupsCount = edgeGroups.Count;
            if (allGroupsSize2 && (maxNumLabels >= 3 || edgeGroupsCount % 2 == 0))
            {
                textureLabels = new List<TextureLabel>();
                //In this case, we just need neighbouring groups to have different labels. Can do this with just two labels if the number of groups is even, otherwise, odd.
                for (int i = 0; i < edgeGroupsCount; i++)
                {
                    textureLabels.Add(i % 2 == 0 ? TextureLabel.First : TextureLabel.Second);
                }
                if (edgeGroupsCount % 2 != 0)
                {
                    textureLabels[edgeGroupsCount - 1] = TextureLabel.Third;
                }
            }
            return textureLabels;
        }

        /// <summary>
        /// Gets a <see cref="BoundaryEdgeCycle"/> from the boundary <see cref="Edge"/> of the <paramref name="decoupledGrouping"/> that belong to only one triangle.
        /// </summary>
        /// <param name="decoupledGrouping">A contiguous set of triangles and their edges.</param>
        /// <param name="meshInformation">The mesh information.</param>
        /// <param name="angleCutoffDegrees">Number of degrees between edges before they are considered to have different angles for wireframe texture generation.</param>
        /// <param name="raiseWarning"><see cref="System.Action"/> to raise a warning encountered during processing.</param>
        /// <returns>A <see cref="BoundaryEdgeCycle"/>, if one exists; null otherwise.</returns>
        internal static BoundaryEdgeCycle GetBoundaryEdgeCycle(DecoupledGrouping decoupledGrouping, MeshInformation meshInformation, float angleCutoffDegrees, System.Action<string> raiseWarning)
        {
            Dictionary<int, HashSet<Edge>> edgesPerTriangleCount = new Dictionary<int, HashSet<Edge>>();
            foreach(Edge edge in decoupledGrouping.Edges)
            {
                DictionaryUtil.AddListItem(edgesPerTriangleCount, edge.TriangleCount, edge);
            }
            if(edgesPerTriangleCount.Keys.Any(k => k > 2))
            {
                raiseWarning("Some edges belong to more than two triangles. Expect them to belong to one triangle for boundary edges and two triangles for surface edges, for watertight meshes with seams.");
            }

            BoundaryEdgeCycle boundaryEdgeCycle;
            HashSet<Edge> boundaryEdgesUnordered = edgesPerTriangleCount.ContainsKey(1) ? edgesPerTriangleCount[1] : null;
            if (boundaryEdgesUnordered != null && boundaryEdgesUnordered.Count > 0)
            {
                Edge startingEdge = boundaryEdgesUnordered.First();

                boundaryEdgeCycle = new BoundaryEdgeCycle();

                Edge currEdge = startingEdge;
                int numBoundaryEdges = boundaryEdgesUnordered.Count;
                HashSet<Edge> addedEdges = new HashSet<Edge>();

                for(int i = 0; i < numBoundaryEdges; i++)
                {
                    boundaryEdgeCycle.Edges.Add(currEdge);
                    addedEdges.Add(currEdge);
                    Vertex vertexFirst = meshInformation.GetVertex(currEdge.FirstIndex);
                    Vertex vertexSecond = meshInformation.GetVertex(currEdge.SecondIndex);
                    //Note that in some cases there may be connected edges, but that don't form a triangle with the given edge, and so don't show up in the boundaryEdgesUnordered
                    //Additionally, note that there may be cases where one of the potential edges was already added to the list, so we'd like to ignore those.
                    Edge[] connectedBoundaryEdges = vertexFirst.Concat(vertexSecond).Distinct()
                        .Where(e => e.TriangleCount == 1 && boundaryEdgesUnordered.Contains(e))
                        .Where(e => !addedEdges.Contains(e) || (e == startingEdge && i == numBoundaryEdges - 1))
                        .ToArray();
                    if (connectedBoundaryEdges.Length > 1 && currEdge != startingEdge)
                    {
                        //The set of connected boundary edges splits here, so we don't have a cycle.
                        boundaryEdgeCycle = null;
                        break;
                    }
                    else if (connectedBoundaryEdges.Length == 0)
                    {
                        //The set of connected boundary edges ends here, so we don't have a cycle.
                        boundaryEdgeCycle = null;
                        break;
                    }
                    else
                    {
                        Edge connectedEdge = connectedBoundaryEdges[0];
                        Triangle[] sharedTriangles = connectedEdge.Intersect(currEdge).ToArray();
                        Triangle intersectionTriangle = sharedTriangles.FirstOrDefault();
                        if(sharedTriangles.Length > 1)
                        {
                            raiseWarning($"Two edges given by {currEdge} and {connectedEdge} are found together in more than one triangle, which should not be physically possible.");
                            boundaryEdgeCycle = null;
                            break;
                        }
                        else if (intersectionTriangle != null)
                        {
                            boundaryEdgeCycle.SharedTriangleIndices.Add(i);
                        }
                        else
                        {
                            float edgeAngleDegrees = meshInformation.GetEdgeAngleDegrees(connectedEdge, currEdge);
                            if(edgeAngleDegrees > angleCutoffDegrees)
                            {
                                boundaryEdgeCycle.AngleDifferenceIndices.Add((i, edgeAngleDegrees));
                            }
                        }

                        currEdge = connectedEdge;
                    }
                }
                if(currEdge != startingEdge && boundaryEdgeCycle != null)
                {
                    //Didn't get a full cycle connecting back to the first edge.
                    boundaryEdgeCycle = null;
                }
            }
            else
            {
                //No boundary edges at all, so we don't have a cycle.
                //This could happen on fully watertight meshes without any sharp edges/seams, so we just won't expect any wireframe to be drawn by our system.
                boundaryEdgeCycle = null;
            }

            if(boundaryEdgeCycle != null)
            {
                //Shift cycle so a group starts at zero
                int indexOfShift = boundaryEdgeCycle.SharedTriangleIndices.Count > 0 ? boundaryEdgeCycle.SharedTriangleIndices[0] + 1 :
                                    boundaryEdgeCycle.AngleDifferenceIndices.Count > 0 ? boundaryEdgeCycle.AngleDifferenceIndices[0].index + 1 : 0;
                int numBoundaryEdges = boundaryEdgeCycle.Edges.Count;
                boundaryEdgeCycle.Edges = boundaryEdgeCycle.Edges.Select((edge, index) => (edge, index: PeriodicUtilities.GetIndexPeriodic(index - indexOfShift, numBoundaryEdges)))
                    .OrderBy(p => p.index).Select(p => p.edge).ToList();
                boundaryEdgeCycle.AngleDifferenceIndices = boundaryEdgeCycle.AngleDifferenceIndices.Select(i => (PeriodicUtilities.GetIndexPeriodic(i.index - indexOfShift, numBoundaryEdges), i.angle)).ToList();
                boundaryEdgeCycle.AngleDifferenceIndices.Sort();
                boundaryEdgeCycle.SharedTriangleIndices = boundaryEdgeCycle.SharedTriangleIndices.Select(i => PeriodicUtilities.GetIndexPeriodic(i - indexOfShift,numBoundaryEdges)).ToList();
                boundaryEdgeCycle.SharedTriangleIndices.Sort();
            }
            return boundaryEdgeCycle;
        }

        /// <summary>
        /// Marks a delineation between groups at a given edge index and <see cref="GroupCutType"/>.
        /// </summary>
        private struct GroupCutIndex
        {
            public int EdgeIndex;
            public GroupCutType CutType;

            public GroupCutIndex(int edgeIndex, GroupCutType cutType)
            {
                EdgeIndex = edgeIndex;
                CutType = cutType;
            }

            public override string ToString() => $"{EdgeIndex} {CutType}";
        }

        /// <summary>
        /// If <see cref="GroupCutType.SameTriangle"/> should be removed in the last attempts to find any sort of solution.
        /// </summary>
        private static readonly bool _removeSameTriangleGroupCuts = false;
    }
}
