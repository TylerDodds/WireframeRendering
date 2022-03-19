// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using NUnit.Framework;
using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;

namespace PixelinearAccelerator.WireframeRendering.EditorTests
{
    internal class BoundaryCycleTests
    {
        ///<summary>Tests <see cref="BoundaryEdgeCycle"/> generation for a Quad.</summary>
        [Test]
        public void QuadBoundaryCycleTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetQuadMesh(1f, 1f));
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "Decoupled Grouping");
            BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, _raiseWarning);
            AssertBoundaryCycleCounts(boundaryEdgeCycle, 4, 2, 2);
        }

        ///<summary>Tests <see cref="BoundaryEdgeCycle"/> generation for a watertight cube.</summary>
        [Test]
        public void WatertightCubeBoundaryCycleTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetNonSeamCubeMesh());
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "Decoupled Grouping");
            BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, _raiseWarning);
            Assert.IsNull(boundaryEdgeCycle);
        }

        ///<summary>Tests <see cref="BoundaryEdgeCycle"/> generation for the build-in Cube.</summary>
        [Test]
        public void BuiltInCubeBoundaryCycleTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().BuiltInCube);
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
            Assert.AreEqual(6, decoupledGroupings.Count, "Decoupled Grouping");
            foreach(DecoupledGrouping grouping in decoupledGroupings)
            {
                BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(grouping, meshInformation, _edgeAngleCutoff, _raiseWarning);
                AssertBoundaryCycleCounts(boundaryEdgeCycle, 4, 2, 2);
            }
        }

        ///<summary>Tests <see cref="BoundaryEdgeCycle"/> generation for the Circle1 example circle mesh.</summary>
        [Test]
        public void Circle1BoundaryCycleTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().Circle1);
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "Decoupled Grouping");
            foreach (DecoupledGrouping grouping in decoupledGroupings)
            {
                BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(grouping, meshInformation, _edgeAngleCutoff, _raiseWarning);
                AssertBoundaryCycleCounts(boundaryEdgeCycle, 32, 0, 32);
                BoundaryEdgeCycle boundaryEdgeCycle2 = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(grouping, meshInformation, 89f, _raiseWarning);
                AssertBoundaryCycleCounts(boundaryEdgeCycle2, 32, 0, 0);
            }
        }

        ///<summary>Tests <see cref="BoundaryEdgeCycle"/> generation for the TwoTriangleBowtie example circle mesh.</summary>
        [Test]
        public void TwoTriangleBowtieBoundaryCycleTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetTwoTriangleBowtieMesh());
            
            List<DecoupledGrouping> decoupledGroupings2 = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedEdges);
            foreach (DecoupledGrouping grouping in decoupledGroupings2)
            {
                BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(grouping, meshInformation, _edgeAngleCutoff, _raiseWarning);
                AssertBoundaryCycleCounts(boundaryEdgeCycle, 3, 3, 0);
            }

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedVertices);
            foreach (DecoupledGrouping grouping in decoupledGroupings)
            {
                BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(grouping, meshInformation, _edgeAngleCutoff, _raiseWarning);
                Assert.IsNull(boundaryEdgeCycle);
            }
        }

        /// <summary>
        /// Asserts that the <paramref name="boundaryEdgeCycle"/> has the appropriate <paramref name="numEdges"/>, <paramref name="numSharedTriangle"/> and <paramref name="numLargeAngles"/>.
        /// </summary>
        /// <param name="boundaryEdgeCycle">The <see cref="BoundaryEdgeCycle"/>.</param>
        /// <param name="numEdges">The number of edges.</param>
        /// <param name="numSharedTriangle">The number of shared-triangle edge neighbours.</param>
        /// <param name="numLargeAngles">The number of large-angle edge neighbours.</param>
        private static void AssertBoundaryCycleCounts(BoundaryEdgeCycle boundaryEdgeCycle, int numEdges, int numSharedTriangle, int numLargeAngles)
        {
            Assert.IsNotNull(boundaryEdgeCycle);
            Assert.AreEqual(numEdges, boundaryEdgeCycle.Edges.Count, "Boundary Edges");
            Assert.AreEqual(numSharedTriangle, boundaryEdgeCycle.SharedTriangleIndices.Count, "Shared Triangles");
            Assert.AreEqual(numLargeAngles, boundaryEdgeCycle.AngleDifferenceIndices.Count, "Large Edge Angles");
        }

        private List<DecoupledGrouping> GetDecoupledGroupings(MeshInformation meshInformation)
        {
            return TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedEdges);
        }

        private static float _edgeAngleCutoff = 10f;
        private static System.Action<string> _raiseWarning = warning => Assert.Fail(warning);
    }
}
