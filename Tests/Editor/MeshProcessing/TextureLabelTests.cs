// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using NUnit.Framework;
using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;
using System.Linq;

namespace PixelinearAccelerator.WireframeRendering.EditorTests
{
    /// <summary>
    /// Tests for generation of <see cref="EdgeGroup"/>s with appropriate wireframe texture labels.
    /// </summary>
    internal class TextureLabelTests
    {
        /// <summary>Wireframe texture labeling test for a Quad.</summary>
        [Test]
        public void QuadTextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetQuadMesh(1f, 1f));
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");
            BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, _raiseWarning);
            Assert.AreEqual(4, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            IReadOnlyList<IEdgeGroup> edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            Assert.AreEqual(4, edgeGroups.Count);
            Assert.AreEqual(4, edgeGroups.Select(e => e.TextureLabel).Distinct().Count());
        }

        /// <summary>Wireframe texture labeling test for the build-in cube.</summary>
        [Test]
        public void BuiltInCubeTextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().BuiltInCube);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(6, decoupledGroupings.Count, "# Decoupled Grouping");
            foreach (DecoupledGrouping grouping in decoupledGroupings)
            {
                BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(grouping, meshInformation, _edgeAngleCutoff, _raiseWarning);
                IReadOnlyList<IEdgeGroup> edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
                Assert.AreEqual(4, edgeGroups.Count, "# Edge Groups");
                Assert.AreEqual(4, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Distinct Texture Labels");
            }
        }

        /// <summary>Wireframe texture labeling test for the Circle1 example mesh.</summary>
        [Test]
        public void Circle1TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().Circle1);
            List <DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, _raiseWarning);
            Assert.AreEqual(32, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            IReadOnlyList<IEdgeGroup> edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            Assert.AreEqual(32, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.IsTrue(1 == edgeGroups.Where((g, i) => i % 2 == 0).Select(e => e.TextureLabel).Distinct().Count(), "Even edge groups share texture label");
            Assert.IsTrue(1 == edgeGroups.Where((g, i) => i % 2 == 1).Select(e => e.TextureLabel).Distinct().Count(), "Odd edge groups share texture label");

            boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, 89f, _raiseWarning);
            Assert.AreEqual(32, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            Assert.AreEqual(1, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(1, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
        }

        /// <summary>Wireframe texture labeling test for the TenSidedPoly1 example mesh.</summary>
        [Test]
        public void TenSidedPoly1TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().TenSidedPoly1);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, _raiseWarning);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(8, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            Assert.AreEqual(10, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(4, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");

            boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, 89f, _raiseWarning);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            Assert.AreEqual(2, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
        }

        /// <summary>Wireframe texture labeling test for the OneGroupCutEachType1 example mesh.</summary>
        [Test]
        public void OneGroupCuteEachType1TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().OneGroupCutEachType1);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            BoundaryEdgeCycle boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGroupings[0], meshInformation, 45f, _raiseWarning);
            Assert.AreEqual(32, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(1, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(1, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle);
            Assert.AreEqual(3, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(3, edgeGroups.Count, "# Edge Groups");
        }

        private static float _edgeAngleCutoff = 10f;
        private static System.Action<string> _raiseWarning = warning => Assert.Fail(warning);
    }
}
