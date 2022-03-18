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
        /// <summary>Wireframe texture labeling test for a single triangle.</summary>
        [Test]
        public void SingleTriangleTextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetSingleTriangleMesh());
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");
            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(3, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(3, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(3, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(3, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for a Quad.</summary>
        [Test]
        public void QuadTextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetQuadMesh(1f, 1f));
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");
            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(4, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
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
                GetBoundaryCycleAndGrouping(grouping, meshInformation, _edgeAngleCutoff, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
                IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
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

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(32, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(32, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.IsTrue(1 == edgeGroups.Where((g, i) => i % 2 == 0).Select(e => e.TextureLabel).Distinct().Count(), "Even edge groups share texture label");
            Assert.IsTrue(1 == edgeGroups.Where((g, i) => i % 2 == 1).Select(e => e.TextureLabel).Distinct().Count(), "Odd edge groups share texture label");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 89f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(32, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
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

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, _edgeAngleCutoff, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(8, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(10, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(4, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 89f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Count, "# Edge Groups");
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
        }

        /// <summary>Wireframe texture labeling test for the OneGroupCutEachType1 example mesh.</summary>
        [Test]
        public void OneGroupCutEachType1TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().OneGroupCutEachType1);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 45f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(32, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(1, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(1, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(3, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(3, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for the Circle2 example mesh.</summary>
        [Test]
        public void Circle2TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().Circle2);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 90f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(8, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(4, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(4, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for the CircleFan1 example mesh.</summary>
        [Test]
        public void CircleFan1TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().CircleFan1);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 90f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(8, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(2, edgeGroups.Count, "# Edge Groups");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 5f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(8, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(6, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(4, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(8, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for the CircleFan2 example mesh.</summary>
        [Test]
        public void CircleFan2TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().CircleFan2);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 90f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(7, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(2, edgeGroups.Count, "# Edge Groups");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 5f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(7, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(5, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(4, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(7, edgeGroups.Count, "# Edge Groups");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 60f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(7, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(2, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(1, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(3, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(3, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for the CircleFan3 example mesh.</summary>
        [Test]
        public void CircleFan3TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().CircleFan3);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 90f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(0, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            //Note that normally with no SharedTriangleIndices or AngleDifferenceIndices we just have one group with one texture label, but not so due to triangle constraint
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(2, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for the CircleFan4 example mesh.</summary>
        [Test]
        public void CircleFan4TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().CircleFan4);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 90f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(16, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(0, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.IsNotNull(edgeGroups);
            //Note that we need to have addAnyVirtualCutsPossible enabled in TryAddBestVirtualGroupCut to add in-between virtual cuts to get any labeling, which is done correctly via BacktrackResults.RejectedAnyBasedOnTriangles
            Assert.AreEqual(3, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(6, edgeGroups.Count, "# Edge Groups");
        }

        private static IReadOnlyList<IEdgeGroup> GetBoundaryCycleEdgeGroups(BoundaryEdgeCycle boundaryEdgeCycle, BoundaryGrouping boundaryGrouping, MeshInformation meshInformation)
        {
            EdgeGroupResults edgeGroupResults = EdgeTextureLabelUtilities.GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            return edgeGroupResults.EdgeGroups;
        }

        private static void GetBoundaryCycleAndGrouping(DecoupledGrouping decoupledGrouping, MeshInformation meshInformation, float edgeAngleCutoff, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping)
        {
            boundaryEdgeCycle = EdgeTextureLabelUtilities.GetBoundaryEdgeCycle(decoupledGrouping, meshInformation, edgeAngleCutoff, _raiseWarning);
            boundaryGrouping = TriangleGroupUtilities.GetBoundaryGrouping(decoupledGrouping, boundaryEdgeCycle);
        }

        private static float _edgeAngleCutoff = 10f;
        private static System.Action<string> _raiseWarning = warning => Assert.Fail(warning);
    }
}
