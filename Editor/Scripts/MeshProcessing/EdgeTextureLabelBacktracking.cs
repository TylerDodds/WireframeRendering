// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Backtracking functions to assign <see cref="TextureLabel"/>s.
    /// </summary>
    internal static class EdgeTextureLabelBacktracking
    {
        /// <summary>
        /// Performs backtracking search to get a <see cref="TextureLabel"/> assignment for the <paramref name="edgeGroups"/> that satisfies the constraints.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="groupCuts">The type of <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="edgeGroupVertexInformation">The <see cref="EdgeGroupVertexInformation"/> for these <paramref name="edgeGroups"/>.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <param name="maxNumLabels">The maximum number of <see cref="TextureLabel"/> values (# of uv coordinates) to use.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <param name="stopwatch">Stopwatch for time spent getting edge groups.</param>
        /// <returns>A list of <see cref="TextureLabel"/> per <see cref="EdgeGroup"/>, if one satisfies the constraints.</returns>
        internal static BacktrackResults BacktrackTextureLabels(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, EdgeGroupVertexInformation edgeGroupVertexInformation, BoundaryGrouping boundaryGrouping, int maxNumLabels, List<TextureLabel> partial, Stopwatch stopwatch)
        {
            if(BacktrackRejectTextureLabels(edgeGroups, groupCuts, boundaryGrouping.Edges.Count, partial))
            {
                return new BacktrackResults(null, false);
            }
            else if(BacktrackRejectIfTriangleHasAllOneLabelOrNewVertexLabelAssigned(edgeGroups, groupCuts, partial, edgeGroupVertexInformation, boundaryGrouping))
            {
                return new BacktrackResults(null, true);
            }
            else if(BacktrackAcceptTextureLabelsIfNotRejected(edgeGroups, partial))
            {
                return new BacktrackResults(partial, false);
            }
            else if(stopwatch.Elapsed.TotalSeconds > _backtrackTimeCutoffSeconds)
            {
                return new BacktrackResults(null, false)
                {
                    RejectedBasedOnTimeout = true,
                    RejectionTimeoutSeconds = stopwatch.Elapsed.TotalSeconds,
                };
            } 
            else
            {
                
                bool anyExtensionRejectedDueToTriangleConstraint = false;
                List<TextureLabel> extension = BacktrackGenerateFirstExtension(edgeGroups, partial);
                while(extension != null)
                {
                    BacktrackResults backTracked = BacktrackTextureLabels(edgeGroups, groupCuts, edgeGroupVertexInformation, boundaryGrouping, maxNumLabels, extension, stopwatch);
                    if(backTracked.HasResult)
                    {
                        return backTracked;
                    }
                    else
                    {
                        anyExtensionRejectedDueToTriangleConstraint |= backTracked.RejectedAnyBasedOnTriangles;
                    }
                    extension = BacktrackGenerateNextExtension(maxNumLabels, extension);
                }
                return new BacktrackResults(null, anyExtensionRejectedDueToTriangleConstraint);
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
        /// Returns if the <paramref name="partial"/> solution should be rejected, based on if there are any triangles that would have all three vertices with a same TextureLabel assigned.
        /// Also rejects if any vertex already had a <see cref="TextureLabel"/> assigned to it before solving for these <paramref name="edgeGroups"/>, and would have a new label assigned.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="groupCuts">The type of <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <param name="edgeGroupVertexInformation">The <see cref="EdgeGroupVertexInformation"/> for these <paramref name="edgeGroups"/>.</param>
        /// <param name="boundaryGrouping">Boundary edges and triangles touching only boundary edge vertices.</param>
        /// <returns>If the <paramref name="partial"/> solution should be rejected.</returns>
        internal static bool BacktrackRejectIfTriangleHasAllOneLabelOrNewVertexLabelAssigned(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, List<TextureLabel> partial, EdgeGroupVertexInformation edgeGroupVertexInformation, BoundaryGrouping boundaryGrouping)
        {
            bool reject = false;

            if (partial != null)
            {
                int numPartial = partial.Count;

                (TextureLabelAssigned Initial, TextureLabelAssigned Final) GetTextureLabelCoverage(int vertexIndex)
                {
                    Vertex vertex = edgeGroupVertexInformation.MeshInformation.GetVertex(vertexIndex);
                    TextureLabelAssigned initial = vertex.TextureLabelsAssigned;
                    TextureLabelAssigned labelCoverage = initial;
                    List<int> edgeGroupIndicesTouchingThisVertex = edgeGroupVertexInformation.VertexEdgeGroupIndicesDictionary[vertexIndex];
                    foreach (int edgeGroupIndex in edgeGroupIndicesTouchingThisVertex)
                    {
                        if (edgeGroupIndex < numPartial && edgeGroupIndex >= 0)
                        {
                            labelCoverage.AddTextureLabel(partial[edgeGroupIndex]);
                            //NB Won't do early-stopping in case all four components are true, since this is likely rare in most cases.
                        }
                    }
                    return (initial, labelCoverage);
                }

                Dictionary<int, (TextureLabelAssigned Initial, TextureLabelAssigned Final)> vertexIndicesLabelCoverage = edgeGroupVertexInformation.VertexEdgeGroupIndicesDictionary.ToDictionary(pair => pair.Key, pair => GetTextureLabelCoverage(pair.Key));
                foreach(KeyValuePair<int, (TextureLabelAssigned Initial, TextureLabelAssigned Final)> pair in vertexIndicesLabelCoverage)
                {
                    (TextureLabelAssigned Initial, TextureLabelAssigned Final) = pair.Value;
                    bool finalHasAssignedANewLabel = Initial.Any && Initial.ComponentwiseXOr(Final).Any;//Since we know that any components True in Initial will also be True in Final, we can use XOr here to check.
                    if(finalHasAssignedANewLabel)
                    {
                        reject = true;
                        break;
                    }
                }
                if (!reject)
                {
                    foreach (Triangle triangle in boundaryGrouping.TrianglesCompletelyTouchingBoundaryEdges)
                    {
                        TextureLabelAssigned firstCoverageFinal = vertexIndicesLabelCoverage[triangle.Index1].Final;
                        TextureLabelAssigned secondCoverageFinal = vertexIndicesLabelCoverage[triangle.Index2].Final;
                        TextureLabelAssigned thirdCoverageFinal = vertexIndicesLabelCoverage[triangle.Index3].Final;
                        TextureLabelAssigned totalCoverageFinal = firstCoverageFinal.ComponentwiseAnd(secondCoverageFinal).ComponentwiseAnd(thirdCoverageFinal);
                        if (totalCoverageFinal.Any)
                        {
                            reject = true;
                            break;
                        }
                    }
                }
            }

            return reject;
        }

        /// <summary>
        /// Returns if the <paramref name="partial"/> solution should be rejected based on the constraints of the <paramref name="groupCuts"/>.
        /// </summary>
        /// <param name="edgeGroups">The <see cref="EdgeGroup"/>s of the boundary cycle, in order.</param>
        /// <param name="groupCuts">The type of <see cref="GroupCutType"/> from one <see cref="EdgeGroup"/> to the next.</param>
        /// <param name="numBoundaryEdges">The number of boundary edges.</param>
        /// <param name="partial">The partial solution assigning a <see cref="TextureLabel"/> to each <see cref="EdgeGroup"/>.</param>
        /// <returns>If the <paramref name="partial"/> solution should be rejected based on the constraints of the <paramref name="groupCuts"/>.</returns>
        private static bool BacktrackRejectTextureLabels(List<EdgeGroup> edgeGroups, List<GroupCutType> groupCuts, int numBoundaryEdges, List<TextureLabel> partial)
        {
            bool reject = false;
            int numEdgeGroups = edgeGroups.Count;
            int numPartial = partial.Count;
            if(numBoundaryEdges <= 1 || numPartial <= 1)
            {
                return false;
            }
            bool moreThanTwoEdges = numBoundaryEdges > 2;
            bool moreThanThreeEdges = numBoundaryEdges > 3;
            int currentIndex = numPartial - 1;
            List<int> indicesToCheck = new List<int>() { currentIndex - 1 };
            if(currentIndex > 1 && edgeGroups[currentIndex - 1].Edges.Count <= 1)
            {
                indicesToCheck.Add(currentIndex - 2);//Check two groups back if needed
            }
            if(numPartial == numEdgeGroups)
            {
                indicesToCheck.Add(currentIndex);//Don't needd to check currentIndex until it has more neighbours after wrapping to start of cycle
                indicesToCheck.Add(0);//If we've wrapped around, need to re-check first group whose neighbour in the cycle is the last group
                if(edgeGroups[0].Edges.Count <= 1)
                {
                    indicesToCheck.Add(1);//And also need to check the next group if it's two edges away from last group
                }
            }
            foreach (int indexToCheck in indicesToCheck)
            {
                GroupCutType currentGroupCutType = groupCuts[indexToCheck];
                if(currentGroupCutType == GroupCutType.Virtual)
                {
                    //No restrictions on GroupCutType.Virtual
                    continue;
                }
                int indexNext = PeriodicUtilities.GetIndexPeriodic(indexToCheck + 1, numEdgeGroups);
                //Labels on neighbouring groups must be different for any GroupCutType except GroupCutType.Virtual
                if (indexNext < numPartial && partial[indexNext] == partial[indexToCheck])
                {
                    return true;
                }
                if(currentGroupCutType == GroupCutType.SameTriangle)
                {
                    int indexPrev = PeriodicUtilities.GetIndexPeriodic(indexToCheck - 1, numEdgeGroups);
                    int indexAfterNext = PeriodicUtilities.GetIndexPeriodic(indexToCheck + 2, numEdgeGroups);
                    bool needTestPrev = indexPrev < numPartial && edgeGroups[indexToCheck].Edges.Count < 2;
                    indexPrev = needTestPrev ? indexPrev : indexToCheck;
                    bool needTestAfterNext = indexNext < numPartial && edgeGroups[indexNext].Edges.Count < 2;
                    indexAfterNext = needTestAfterNext ? indexAfterNext : indexNext;
                    //Set indexPrev, indexTwoGreater to indices of groups that hold the previous edge and edge after next, respectively
                    //If the groups are the same then we'd already reject based on the direct neighbors test above

                    //Ensure that it's not the exact same edge by checking totdal # of edges before comparing TextureLabels
                    if (moreThanTwoEdges && indexPrev < numPartial && indexNext < numPartial && indexPrev != indexNext && partial[indexPrev] == partial[indexNext])
                    {
                        return true;
                    }
                    if (moreThanTwoEdges && indexAfterNext < numPartial && partial[indexAfterNext] == partial[indexToCheck])
                    {
                        return true;
                    }
                    if (moreThanThreeEdges && indexPrev < numPartial && indexAfterNext < numPartial && partial[indexAfterNext] == partial[indexPrev])
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
        /// Results of backtracking to determine texture labels.
        /// </summary>
        internal class BacktrackResults
        {
            /// <summary>
            /// If a satisfactory labeling was found.
            /// </summary>
            public bool HasResult => Labels != null && !RejectedAnyBasedOnTriangles;
            /// <summary>
            /// The labels satisfying the group cut constraints, if found.
            /// </summary>
            public List<TextureLabel> Labels;
            /// <summary>
            /// If any of the rejected partial solutions were due to triangle constraint (where all three vertices have a common texture coordinate assigned).
            /// </summary>
            public bool RejectedAnyBasedOnTriangles;
            /// <summary>
            /// If no satisfactory solution was found within the given timeout.
            /// </summary>
            public bool RejectedBasedOnTimeout;
            /// <summary>
            /// The time in seconds after which the solution was rejected, if applicable.
            /// </summary>
            public double RejectionTimeoutSeconds;

            public BacktrackResults(List<TextureLabel> labels, bool rejectedAnyBasedOnTriangles)
            {
                Labels = labels;
                RejectedAnyBasedOnTriangles = rejectedAnyBasedOnTriangles;
            }
        }

        private static readonly double _backtrackTimeCutoffSeconds = 60;
    }
}
