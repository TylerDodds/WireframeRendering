// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
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
                return GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            }
            else
            {
                raiseWarning("Boundary edges do not form a loop. Attempting to assign wireframe coordinates in this unsupported case.");
                return GetEdgeGroupsWhenNoBoundaryCycle(decoupledGrouping, meshInformation);
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
        /// <returns>The set of <see cref="IEdgeGroup"/>s.</returns>
        internal static IReadOnlyList<IEdgeGroup> GetBoundaryCycleEdgeGroups(BoundaryEdgeCycle boundaryEdgeCycle)
        {
            List<Edge> boundaryEdges = boundaryEdgeCycle.Edges;

            Dictionary<int, float> edgeIndexToEdgeAngleDictionary = boundaryEdgeCycle.AngleDifferenceIndices.ToDictionary(pair => pair.index, pair => pair.angle);

            List<GroupCutIndex> groupCutEdgeIndices = boundaryEdgeCycle.SharedTriangleIndices.Select(index => new GroupCutIndex(index, GroupCutType.SameTriangle))
                .Concat(boundaryEdgeCycle.AngleDifferenceIndices.Select(indexAndAngle => new GroupCutIndex(indexAndAngle.index, GroupCutType.EdgeAngle))).OrderBy(p => p.EdgeIndex).ToList();

            List<EdgeGroup> edgeGroups = GetEdgeGroups(boundaryEdges, groupCutEdgeIndices);

            while(edgeGroups == null && groupCutEdgeIndices.Count > 0)
            {
                int edgeIndexToRemove = GetEdgeIndexToRemove(groupCutEdgeIndices, edgeIndexToEdgeAngleDictionary);
                int numElementsRemoved = groupCutEdgeIndices.RemoveAll(value => value.EdgeIndex == edgeIndexToRemove);
                if(numElementsRemoved < 1)
                {
                    throw new System.InvalidOperationException($"Did not find a {nameof(EdgeGroup)} cut to remove from the {groupCutEdgeIndices.Count} possible.");
                }

                edgeGroups = GetEdgeGroups(boundaryEdges, groupCutEdgeIndices);
            }

            if (edgeGroups == null)
            {
                throw new System.InvalidOperationException($"From a boundary cycle containing {boundaryEdges.Count} edges, no texture labels were assignable under any conditions.");
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
                int indexOneAfterCyclic = GetIndexPeriodic(indexInGroupCutEdgeIndices + 1, groupCutEdgeIndicesCount);
                int indexOneBeforeCyclic = GetIndexPeriodic(indexInGroupCutEdgeIndices - 1, groupCutEdgeIndicesCount);
                GroupCutIndex groupCutPrev = groupCutEdgeIndices[indexOneBeforeCyclic];
                GroupCutIndex groupCutNext = groupCutEdgeIndices[indexOneAfterCyclic];
                int groupSizePrev = GetIndexDistance(groupCutPrev.EdgeIndex, edgeIndex, groupCutEdgeIndicesCount);
                int groupSizeNext = GetIndexDistance(edgeIndex, groupCutNext.EdgeIndex, groupCutEdgeIndicesCount);
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
            else
            {
                //At least one left of GroupCutType.SameTriangle
                edgeIndexToRemove = groupCutEdgeIndices
                    .OrderByDescending(values => NumberOfOneSizedGroupsRemoved(values.EdgeIndex))
                    .ThenBy(values => GetTotalNeighbouringGroupSizes(values.EdgeIndex))
                    .First().EdgeIndex;
            }
            return edgeIndexToRemove;
        }

        /// <summary>
        /// Gets the set of <see cref="EdgeGroup"/>s and the corresponding <see cref="GroupCutType"/>s between <see cref="EdgeGroup"/>s and their neighbours.
        /// </summary>
        /// <param name="boundaryEdges">The list of boundary <see cref="Edge"/>s in a cycle.</param>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="edgeGroups">The set of <see cref="EdgeGroup"/>s.</param>
        /// <param name="groupCuts">The <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        private static void GetEdgeGroupsAndGroupCutTypes(List<Edge> boundaryEdges, List<GroupCutIndex> groupCutEdgeIndices, out List<EdgeGroup> edgeGroups, out List<GroupCutType> groupCuts)
        {
            bool anyCuts = groupCutEdgeIndices.Any();
            int boundaryEdgesCount = boundaryEdges.Count;
            List<int> groupSizes = anyCuts ?
                groupCutEdgeIndices.Zip(groupCutEdgeIndices.Prepend(groupCutEdgeIndices.Last()), (next, prev) => GetIndexDistance(prev.EdgeIndex, next.EdgeIndex, boundaryEdgesCount)).ToList()
                : new List<int> { boundaryEdgesCount };

            int[] startIndices = anyCuts ? groupCutEdgeIndices.Select(i => GetIndexPeriodic(i.EdgeIndex + 1, boundaryEdgesCount)).OrderBy(i => i).ToArray() : new int[] { 0 };
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
        }

        /// <summary>
        /// Gets a list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.
        /// </summary>
        /// <param name="boundaryEdges">The list of boundary <see cref="Edge"/>s in a cycle.</param>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <returns>A list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.</returns>
        private static List<EdgeGroup> GetEdgeGroups(List<Edge> boundaryEdges, List<GroupCutIndex> groupCutEdgeIndices)
        {
            List<EdgeGroup> edgeGroups = null;

            if (edgeGroups == null)
            {
                edgeGroups = GetEdgeGroups(boundaryEdges, groupCutEdgeIndices, 2);
            }
            if (edgeGroups == null)
            {
                edgeGroups = GetEdgeGroups(boundaryEdges, groupCutEdgeIndices, 3);
            }
            if (edgeGroups == null)
            {
                edgeGroups = GetEdgeGroups(boundaryEdges, groupCutEdgeIndices, 4);
            }

            return edgeGroups;
        }

        /// <summary>
        /// Gets a list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="groupCutEdgeIndices"/>.
        /// </summary>
        /// <param name="boundaryEdges">The list of boundary <see cref="Edge"/>s in a cycle.</param>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <returns>A list of <see cref="EdgeGroup"/>s with <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="edgeGroups"/>.</returns>
        private static List<EdgeGroup> GetEdgeGroups(List<Edge> boundaryEdges, List<GroupCutIndex> groupCutEdgeIndices, int maxNumLabels)
        {

            GetEdgeGroupsAndGroupCutTypes(boundaryEdges, groupCutEdgeIndices, out List<EdgeGroup> edgeGroups, out List<GroupCutType> groupCuts);
            List<TextureLabel> textureLabels = GetTextureLabelsForFixedGroupCuts(edgeGroups, groupCuts, maxNumLabels);
            int numBoundaryEdges = boundaryEdges.Count;

            if(textureLabels == null)
            {
                groupCutEdgeIndices = new List<GroupCutIndex>(groupCutEdgeIndices);//Need to clone so that original group isn't affected
            }

            while (textureLabels == null && groupCutEdgeIndices.Count <= numBoundaryEdges)
            {
                int numGroupCutIndices = groupCutEdgeIndices.Count;
                if (numGroupCutIndices == 0)
                {
                    //NB Don't expect this case, but add virtual cut so the single resulting group starts at index 0
                    groupCutEdgeIndices.Add(new GroupCutIndex(numBoundaryEdges - 1, GroupCutType.Virtual));
                }
                else if (numGroupCutIndices == 1)
                {
                    groupCutEdgeIndices.Add(new GroupCutIndex(GetIndexPeriodic(groupCutEdgeIndices[0].EdgeIndex + 2, numBoundaryEdges), GroupCutType.Virtual));
                }
                else
                {
                    bool addedGroupCutEdgeIndex = TryAddBestVirtualGroupCut(groupCutEdgeIndices, numBoundaryEdges);
                    if(!addedGroupCutEdgeIndex)
                    {
                        break;
                    }
                }

                //NB Need to reorder groupCutIndices, and note that groups will always start at zero, since it should always have cut at the last index (no matter which are added)
                groupCutEdgeIndices.Sort((first, second) => first.EdgeIndex.CompareTo(second.EdgeIndex));
                GetEdgeGroupsAndGroupCutTypes(boundaryEdges, groupCutEdgeIndices, out edgeGroups, out groupCuts);
                textureLabels = GetTextureLabelsForFixedGroupCuts(edgeGroups, groupCuts, maxNumLabels);
            }

            if(textureLabels != null)
            {
                for (int i = 0; i < textureLabels.Count; i++)
                {
                    edgeGroups[i].TextureLabel = textureLabels[i];
                }
                return edgeGroups;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Tries to add to <paramref name="groupCutEdgeIndices"/> a <see cref="GroupCutType.Virtual"/> cut best suited to allow assigning of texture labels satisfying <paramref name="groupCutEdgeIndices"/> constraints.
        /// </summary>
        /// <param name="groupCutEdgeIndices">The set of indices where <see cref="GroupCutType"/> are assigned to boundary loop edges.</param>
        /// <param name="numBoundaryEdges">The number of boundary edges.</param>
        /// <returns>If a <see cref="GroupCutIndex"/> of <see cref="GroupCutType.Virtual"/> could be added.</returns>
        private static bool TryAddBestVirtualGroupCut(List<GroupCutIndex> groupCutEdgeIndices, int numBoundaryEdges)
        {
            int numGroupCutIndices = groupCutEdgeIndices.Count;
            List<(GroupCutIndex ChosenCut, int Distance, bool NextNeighbourChosen)> relevantCutDistanceAndNext = new List<(GroupCutIndex, int, bool)>();
            GroupCutIndex prev = groupCutEdgeIndices.Last();
            GroupCutIndex curr = groupCutEdgeIndices[0];
            int prevDistance = GetIndexDistance(prev.EdgeIndex, curr.EdgeIndex, numBoundaryEdges);
            for (int i = 0; i < numGroupCutIndices; i++)
            {
                int indexOfNextGroupCut = GetIndexPeriodic(i + 1, numGroupCutIndices);
                GroupCutIndex next = groupCutEdgeIndices[indexOfNextGroupCut];
                int nextDistance = GetIndexDistance(curr.EdgeIndex, next.EdgeIndex, numBoundaryEdges);

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

            (GroupCutIndex Cut, GroupCutIndex ChosenCut, int Distance, bool NextNeighbourChosen) cutToAddNear = groupCutEdgeIndices
                .Zip(relevantCutDistanceAndNext, (groupCutIndex, chosen) => (Cut: groupCutIndex, chosen.ChosenCut, chosen.Distance, chosen.NextNeighbourChosen))
                .Aggregate((Cut: new GroupCutIndex(-1, GroupCutType.Virtual), ChosenCut: new GroupCutIndex(-1, GroupCutType.Virtual), Distance: int.MaxValue, NextChosen: false),
                    (min, next) => min.Cut.CutType == GroupCutType.Virtual && next.Cut.CutType != GroupCutType.Virtual ? next :
                        min.ChosenCut.CutType == GroupCutType.Virtual && next.ChosenCut.CutType != GroupCutType.Virtual ? next :
                        next.Distance > min.Distance ? next : min);

            bool added = false;
            if (cutToAddNear.Cut.CutType == GroupCutType.Virtual && cutToAddNear.ChosenCut.CutType == GroupCutType.Virtual)
            {
                //If best cut is towards another virtual cut, adding another label to the group in between won't affect any non-Virtual GroupCutType restrictions
            }
            else if (cutToAddNear.Distance < 2)
            {
                //Nothing to do if the cut is a neighbour!
            }
            else
            {
                int distanceToCut = Mathf.Min(2, cutToAddNear.Distance - 1);
                int cutIndex = GetIndexPeriodic(cutToAddNear.Cut.EdgeIndex + (cutToAddNear.NextNeighbourChosen ? distanceToCut : -distanceToCut), numBoundaryEdges);
                groupCutEdgeIndices.Add(new GroupCutIndex(cutIndex, GroupCutType.Virtual));
                added = true;
            }
            return added;
        }

        /// <summary>
        /// Gets a list of <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="edgeGroups"/> with a fixed set of <paramref name="groupCuts"/>.
        /// </summary>
        /// <param name="edgeGroups">The set of <see cref="EdgeGroup"/>s.</param>
        /// <param name="groupCuts">The <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <returns>A list of <see cref="TextureLabel"/>s that satisfy the constraints of the given <paramref name="edgeGroups"/>.</returns>
        private static List<TextureLabel> GetTextureLabelsForFixedGroupCuts(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, int maxNumLabels)
        {

            List<TextureLabel> textureLabels = GroupSizesAtLeastTwoTextureLabels(edgeGroups, maxNumLabels);
            if (textureLabels == null)
            {
                textureLabels = BacktrackTextureLabels(edgeGroups, groupCuts, maxNumLabels, new List<TextureLabel>() { TextureLabel.First });
            }

            return textureLabels;
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
        /// Performs backtracking search to get a <see cref="TextureLabel"/> assignment for the <paramref name="edgeGroups"/> that satisfies the constraints.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="groupCuts">The type of <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <returns>A list of <see cref="TextureLabel"/> per <see cref="EdgeGroup"/>, if one satisfies the constraints.</returns>
        private static List<TextureLabel> BacktrackTextureLabels(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, int maxNumLabels, List<TextureLabel> partial)
        {
            if(BacktrackRejectTextureLabels(edgeGroups, groupCuts, partial))
            {
                return null;
            }
            else if(BacktrackAcceptTextureLabelsIfNotRejected(edgeGroups, partial))
            {
                return partial;
            }
            else
            {
                List<TextureLabel> extension = BacktrackGenerateFirstExtension(edgeGroups, partial);
                while(extension != null)
                {
                    List<TextureLabel> backTracked = BacktrackTextureLabels(edgeGroups, groupCuts, maxNumLabels, extension);
                    if(backTracked != null)
                    {
                        return backTracked;
                    }
                    extension = BacktrackGenerateNextExtension(maxNumLabels, extension);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets an extension of <paramref name="partial"/> with an additional <see cref="TextureLabel"/>, unless one is assigned for each of the <paramref name="edgeGroups"/>.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <returns>An extension of <paramref name="partial"/> with an additional <see cref="TextureLabel"/>, unless one is assigned for each of the <paramref name="edgeGroups"/>.</returns>
        private static List<TextureLabel> BacktrackGenerateFirstExtension(List<EdgeGroup> edgeGroups, List<TextureLabel> partial)
        {
            return partial.Count <= edgeGroups.Count ? partial.Append(TextureLabel.First).ToList() : null;
        }

        /// <summary>
        /// Return an alteration of <paramref name="partial"/> with a different <see cref="TextureLabel"/> at the last entry.
        /// </summary>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <returns>An alteration of <paramref name="partial"/> with a different <see cref="TextureLabel"/> at the last entry.</returns>
        private static List<TextureLabel> BacktrackGenerateNextExtension(int maxNumLabels, List<TextureLabel> partial)
        {
            TextureLabel lastLabel = partial[partial.Count - 1];
            if((int)lastLabel < maxNumLabels)
            {
                List<TextureLabel> textureLabels = new List<TextureLabel>(partial);
                textureLabels[textureLabels.Count - 1] = lastLabel + 1;
                return textureLabels;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns if the <paramref name="partial"/> solution should be rejected based on the constraints of the <paramref name="groupCuts"/>.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="groupCuts">The type of <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <returns>If the <paramref name="partial"/> solution should be rejected based on the constraints of the <paramref name="groupCuts"/>.</returns>
        private static bool BacktrackRejectTextureLabels(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, List<TextureLabel> partial)
        {
            bool reject = false;
            int numEdgeGroups = edgeGroups.Count;
            int numPartial = partial.Count;
            for (int indexCurr = numPartial - 1; indexCurr >= 0; indexCurr--)
            {
                GroupCutType currentGroupCutType = groupCuts[indexCurr];
                if(currentGroupCutType == GroupCutType.Virtual)
                {
                    //No restrictions on GroupCutType.Virtual
                    continue;
                }
                TextureLabel label = partial[indexCurr];
                int indexNext = GetIndexPeriodic(indexCurr + 1, numEdgeGroups);
                //Labels on neighbouring groups must be different for any GroupCutType except GroupCutType.Virtual
                if (indexNext < numPartial && partial[indexNext] == partial[indexCurr])
                {
                    return true;
                }
                if(currentGroupCutType == GroupCutType.SameTriangle)
                {
                    int indexPrev = GetIndexPeriodic(indexCurr - 1, numEdgeGroups);
                    int indexAfterNext = GetIndexPeriodic(indexCurr + 2, numEdgeGroups);
                    bool needTestPrev = indexPrev < numPartial && edgeGroups[indexCurr].Edges.Count < 2;
                    indexPrev = needTestPrev ? indexPrev : indexCurr;
                    bool needTestAfterNext = indexNext < numPartial && edgeGroups[indexNext].Edges.Count < 2;
                    indexAfterNext = needTestAfterNext ? indexAfterNext : indexNext;
                    //Set indexPrev, indexTwoGreater to indices of groups that hold the previous edge and edge after next, respectively
                    //If the groups are the same then we'd already reject based on the direct neighbors test above

                    if (indexPrev < numPartial && indexNext < numPartial && partial[indexPrev] == partial[indexNext])
                    {
                        return true;
                    }
                    if (indexAfterNext < numPartial && partial[indexAfterNext] == partial[indexCurr])
                    {
                        return true;
                    }
                    if (indexPrev < numPartial && indexAfterNext < numPartial && partial[indexAfterNext] == partial[indexPrev])
                    {
                        return true;
                    }
                }
            }
            return reject;
        }

        /// <summary>
        /// Return if the list of labels <paramref name="partial"/> should be accepted, when it was not rejected as a partial match.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <returns>If the list of labels <paramref name="partial"/> should be accepted.</returns>
        private static bool BacktrackAcceptTextureLabelsIfNotRejected(List<EdgeGroup> edgeGroups, List<TextureLabel> partial)
        {
            return partial.Count == edgeGroups.Count;
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
            Dictionary<int, List<Edge>> edgesPerTriangleCount = new Dictionary<int, List<Edge>>();
            foreach(Edge edge in decoupledGrouping.Edges)
            {
                DictionaryUtil.AddListItem(edgesPerTriangleCount, edge.TriangleCount, edge);
            }
            if(edgesPerTriangleCount.Keys.Any(k => k > 2))
            {
                raiseWarning("Some edges belong to more than two triangles. Expect them to belong to one triangle for boundary edges and two triangles for surface edges, for watertight meshes with seams.");
            }

            BoundaryEdgeCycle boundaryEdgeCycle;
            List<Edge> boundaryEdgesUnordered = edgesPerTriangleCount.ContainsKey(1) ? edgesPerTriangleCount[1] : null;
            if (boundaryEdgesUnordered != null && boundaryEdgesUnordered.Count > 0)
            {
                Edge startingEdge = boundaryEdgesUnordered[0];

                boundaryEdgeCycle = new BoundaryEdgeCycle()
                {
                    Edges = new List<Edge>() { },
                    SharedTriangleIndices = new List<int>(),
                };

                Edge currEdge = startingEdge;
                Edge prevEdge = currEdge;
                int numBoundaryEdges = boundaryEdgesUnordered.Count;
                for(int i = 0; i < numBoundaryEdges; i++)
                {
                    boundaryEdgeCycle.Edges.Add(currEdge);
                    Vertex vertexFirst = meshInformation.GetVertex(currEdge.FirstIndex);
                    Vertex vertexSecond = meshInformation.GetVertex(currEdge.SecondIndex);
                    Edge[] connectedBoundaryEdges = vertexFirst.Concat(vertexSecond).Distinct().Where(e => e != currEdge && e != prevEdge && e.TriangleCount == 1).ToArray();
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

                        prevEdge = currEdge;
                        currEdge = connectedEdge;
                    }
                }
                if(currEdge != startingEdge)
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
                boundaryEdgeCycle.Edges = boundaryEdgeCycle.Edges.Select((edge, index) => (edge, index: GetIndexPeriodic(index - indexOfShift, numBoundaryEdges)))
                    .OrderBy(p => p.index).Select(p => p.edge).ToList();
                boundaryEdgeCycle.AngleDifferenceIndices = boundaryEdgeCycle.AngleDifferenceIndices.Select(i => (GetIndexPeriodic(i.index - indexOfShift, numBoundaryEdges), i.angle)).ToList();
                boundaryEdgeCycle.AngleDifferenceIndices.Sort();
                boundaryEdgeCycle.SharedTriangleIndices = boundaryEdgeCycle.SharedTriangleIndices.Select(i => GetIndexPeriodic(i - indexOfShift,numBoundaryEdges)).ToList();
                boundaryEdgeCycle.SharedTriangleIndices.Sort();
            }
            return boundaryEdgeCycle;
        }

        /// <summary>
        /// Gets groups of edges with the same <see cref="TextureLabel"/>. Uses heuristic labelling in the case that the <paramref name="decoupledGrouping"/> boundary edges don't form a cycle.
        /// </summary>
        /// <param name="decoupledGrouping">A contiguous set of triangles and their edges.</param>
        /// <param name="meshInformation">The mesh information.</param>
        /// <returns>The set of <see cref="EdgeGroup"/>s.</returns>
        private static List<EdgeGroup> GetEdgeGroupsWhenNoBoundaryCycle(DecoupledGrouping decoupledGrouping, MeshInformation meshInformation)
        {
            List<EdgeGroup> edgeGroups = new List<EdgeGroup>();
            HashSet<Edge> processedEdges = new HashSet<Edge>();
            foreach (Edge startingEdge in decoupledGrouping.Edges)
            {
                TraverseBoundaryNoCycle(startingEdge, null, edgeGroups, processedEdges, meshInformation);
            }

            HashSet<EdgeGroup> processedGroups = new HashSet<EdgeGroup>();
            foreach (EdgeGroup edgeGroup in edgeGroups)
            {
                if (!processedGroups.Contains(edgeGroup))
                {
                    TraverseEdgeGroup(edgeGroup, TextureLabel.None, processedGroups);
                }
            }

            return edgeGroups;
        }

        /// <summary>
        /// Adds <see cref="EdgeGroup"/>s while traversing around a boundary edges.
        /// </summary>
        /// <param name="startingEdge">The edge to start traversing from.</param>
        /// <param name="previousEdgeGroup">The previous <see cref="EdgeGroup"/> connected to <paramref name="startingEdge"/>.</param>
        /// <param name="edgeGroups">The list of <see cref="EdgeGroup"/>.</param>
        /// <param name="processed">The set of processed <see cref="Edge"/>s.</param>
        /// <param name="meshInformation">The mesh information.</param>
        private static void TraverseBoundaryNoCycle(Edge startingEdge, EdgeGroup previousEdgeGroup, List<EdgeGroup> edgeGroups, HashSet<Edge> processed, MeshInformation meshInformation)
        {
            if (!processed.Contains(startingEdge))
            {
                processed.Add(startingEdge);
                int triangleCount = startingEdge.TriangleCount;

                if (triangleCount == 1)
                {
                    EdgeGroup edgeGroup = previousEdgeGroup;
                    if (edgeGroup == null)
                    {
                        edgeGroup = new EdgeGroup();
                        edgeGroups.Add(edgeGroup);
                    }
                    edgeGroup.Edges.Add(startingEdge);

                    Vertex vertexFirst = meshInformation.GetVertex(startingEdge.FirstIndex);
                    Vertex vertexSecond = meshInformation.GetVertex(startingEdge.SecondIndex);
                    Edge[] connectedBoundaryEdges = vertexFirst.Concat(vertexSecond).Distinct().Where(e => e != startingEdge && !processed.Contains(e) && e.TriangleCount == 1).ToArray();
                    //NB Even if we expect only one edge here, we'll just continue traversing otherwise, since we're already in the unsupported case of no boundary edge cycle.
                    foreach (Edge connectedEdge in connectedBoundaryEdges)
                    {
                        Triangle[] sharedTriangles = connectedEdge.Intersect(startingEdge).ToArray();
                        Triangle intersectionTriangle = sharedTriangles.FirstOrDefault();
                        //NB Although we expect at most one intersectionTriangle here, we'll just add an EdgeConnection based on the first, ignoring any doubled triangles, since we're already in the unsupported case of no boundary edge cycle.
                        if (intersectionTriangle != null)
                        {
                            int intersectionVertex = startingEdge.GetIndices().Intersect(connectedEdge.GetIndices()).First();
                            EdgeGroup newEdgeGroup = new EdgeGroup();
                            edgeGroups.Add(newEdgeGroup);
                            edgeGroup.Connections.Add(new EdgeConnection(startingEdge, meshInformation.GetVertex(intersectionVertex), intersectionTriangle, connectedEdge, newEdgeGroup));
                            TraverseBoundaryNoCycle(connectedEdge, newEdgeGroup, edgeGroups, processed, meshInformation);
                        }
                        else
                        {
                            //NB note that we won't do ege-to-edge angle tests here, since we're already in the unsupported case of no boundary edge cycle.
                            TraverseBoundaryNoCycle(connectedEdge, edgeGroup, edgeGroups, processed, meshInformation);
                        }
                    }
                }
                else if (triangleCount > 2)
                {
                    Debug.Log($"Edge from vertices {startingEdge.FirstIndex} to {startingEdge.SecondIndex} is found in {triangleCount} triangles. This is expected to be 1 for boundary and 2 for surface edges.");
                }
            }
        }

        /// <summary>
        /// Traverses an <see cref="EdgeGroup"/>'s connections to assign <see cref="TextureLabel"/>s.
        /// </summary>
        /// <param name="edgeGroup">The <see cref="EdgeGroup"/>.</param>
        /// <param name="connectedGroupLabel">The <see cref="TextureLabel"/> of the <see cref="EdgeGroup"/> connected to <paramref name="edgeGroup"/>.</param>
        /// <param name="processed">The set of processed <see cref="EdgeGroup"/>s.</param>
        private static void TraverseEdgeGroup(EdgeGroup edgeGroup, TextureLabel connectedGroupLabel, HashSet<EdgeGroup> processed)
        {
            AssignEdgeLabel(edgeGroup, connectedGroupLabel);

            if (!processed.Contains(edgeGroup))
            {
                processed.Add(edgeGroup);
                foreach (var c in edgeGroup.Connections)
                {
                    TraverseEdgeGroup(c.OtherGroup, edgeGroup.TextureLabel, processed);
                }
            }
        }

        /// <summary>
        /// Assigns a <see cref="TextureLabel"/> to an <see cref="EdgeGroup"/>.
        /// </summary>
        /// <param name="edgeGroup">The <see cref="EdgeGroup"/>.</param>
        /// <param name="connectedGroupLabel">The <see cref="TextureLabel"/> of the <see cref="EdgeGroup"/> connected to <paramref name="edgeGroup"/>.</param>
        private static void AssignEdgeLabel(EdgeGroup edgeGroup, TextureLabel connectedGroupLabel)
        {
            const TextureLabel baseCaseLabel = TextureLabel.First;
            TextureLabel currentLabel = edgeGroup.TextureLabel;
            if (connectedGroupLabel == TextureLabel.None)
            {
                if (currentLabel == TextureLabel.None)//Base case
                {
                    edgeGroup.TextureLabel = baseCaseLabel;
                }
                else
                {
                    throw new System.ArgumentException($"Edge group already has label {currentLabel}, but is being assigned label of {connectedGroupLabel}.", nameof(edgeGroup));
                }
            }
            else
            {
                TextureLabel alternateLabel = GetAlternateLabel(connectedGroupLabel);
                if (currentLabel == TextureLabel.None)
                {
                    edgeGroup.TextureLabel = alternateLabel;
                }
                else if (currentLabel == connectedGroupLabel)
                {
                    edgeGroup.TextureLabel = GetConflictLabel(connectedGroupLabel, alternateLabel);
                }
                else if (currentLabel == alternateLabel)
                {
                    //This group wants to be re-labelled with the same label, so there's nothing to do.
                }
                else
                {
                    //In this case, the current label is not equal to the otherGroupLabel, so we don't need ot change anything, even if it's not equal to what would be the alternateLabel.
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="TextureLabel"/> that is different from two labels.
        /// </summary>
        /// <param name="label1">The first label.</param>
        /// <param name="label2">The second label.</param>
        /// <returns>The <see cref="TextureLabel"/>.</returns>
        private static TextureLabel GetConflictLabel(TextureLabel label1, TextureLabel label2)
        {
            bool inOrder = label1 <= label2;
            TextureLabel first = inOrder ? label1 : label2;
            TextureLabel second = inOrder ? label2 : label1;
            TextureLabel ret = TextureLabel.None;
            switch (first)
            {
                case TextureLabel.First:
                    if (label2 == TextureLabel.Second)
                    {
                        ret = TextureLabel.Third;
                    }
                    else if (label2 == TextureLabel.Third)
                    {
                        ret = TextureLabel.Second;
                    }
                    break;
                case TextureLabel.Second:
                    if (label2 == TextureLabel.Third)
                    {
                        ret = TextureLabel.First;
                    }
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Gets the <see cref="TextureLabel"/> that alternates from the given label.
        /// </summary>
        /// <param name="textureLabel">The <see cref="TextureLabel"/>.</param>
        /// <returns>The alternate <see cref="TextureLabel"/>.</returns>
        private static TextureLabel GetAlternateLabel(TextureLabel textureLabel)
        {
            TextureLabel ret = TextureLabel.None;
            switch (textureLabel)
            {
                case TextureLabel.None:
                    ret = TextureLabel.None;
                    break;
                case TextureLabel.First:
                    ret = TextureLabel.Second;
                    break;
                case TextureLabel.Second:
                    ret = TextureLabel.First;
                    break;
                case TextureLabel.Third:
                    ret = TextureLabel.First;
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Get an <paramref name="index"/> wrapped cyclically to a given <paramref name="period"/>.
        /// </summary>
        /// <param name="index">The unwrapped index.</param>
        /// <param name="period'">The period of wrapping.</param>
        /// <returns>The wrapped index.</returns>
        private static int GetIndexPeriodic(int index, int period) => (index + period) % period;

        /// <summary>
        /// Gets the periodically-wrapped distance from the <paramref name="first"/> index to the <paramref name="second"/>, 
        /// where distance is measured by the number of increasing steps from <paramref name="first"/> to <paramref name="second"/>,
        /// wrapping at the given <paramref name="period"/> if necessary.
        /// </summary>
        /// <param name="first">The first index.</param>
        /// <param name="second">The second index.</param>
        /// <param name="period'">The period of wrapping.</param>
        /// <returns>The distance between indices.</returns>
        private static int GetIndexDistance(int first, int second, int period) => second > first ? second - first : period - (first - second);

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
        /// Indices the reason for considering neighbouring edges to be of different groups for texture labeling reasons.
        /// </summary>
        private enum GroupCutType
        {
            SameTriangle,
            EdgeAngle,
            Virtual,
        }
    }
}
