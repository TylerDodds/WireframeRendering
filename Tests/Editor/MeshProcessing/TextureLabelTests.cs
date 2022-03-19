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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List <DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            List<DecoupledGrouping> decoupledGroupings = GetDecoupledGroupings(meshInformation);
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
            
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedEdges);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");

            GetBoundaryCycleAndGrouping(decoupledGroupings[0], meshInformation, 90f, out BoundaryEdgeCycle boundaryEdgeCycle, out BoundaryGrouping boundaryGrouping);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(0, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            //Note that normally with no SharedTriangleIndices or AngleDifferenceIndices we just have one group with one texture label, but not so due to triangle constraint
            IReadOnlyList<IEdgeGroup> edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(2, edgeGroups.Count, "# Edge Groups");

            List<DecoupledGrouping> decoupledGroupings2 = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedVertices);
            Assert.AreEqual(1, decoupledGroupings2.Count, "# Decoupled Grouping");
            Assert.IsTrue(decoupledGroupings2[0].Edges.SetEquals(decoupledGroupings[0].Edges), "Edges of Decoupled Grouping Styles Are Same");
            Assert.IsTrue(decoupledGroupings2[0].Triangles.SetEquals(decoupledGroupings[0].Triangles), "Triangles of Decoupled Grouping Styles Are Same");
            GetBoundaryCycleAndGrouping(decoupledGroupings2[0], meshInformation, 90f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(10, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(0, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.AreEqual(2, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(4, edgeGroups.Count, "# Edge Groups");//Note that we end up with different groupings here based on the order of how we try to split up the boundary group
        }

        /// <summary>Wireframe texture labeling test for the CircleFan4 example mesh.</summary>
        [Test]
        public void CircleFan4TextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().CircleFan4);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedEdges);
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

            List<DecoupledGrouping> decoupledGroupings2 = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedVertices);
            Assert.AreEqual(1, decoupledGroupings2.Count, "# Decoupled Grouping");
            Assert.IsTrue(decoupledGroupings2[0].Edges.SetEquals(decoupledGroupings[0].Edges), "Edges of Decoupled Grouping Styles Are Same");
            Assert.IsTrue(decoupledGroupings2[0].Triangles.SetEquals(decoupledGroupings[0].Triangles), "Triangles of Decoupled Grouping Styles Are Same");
            GetBoundaryCycleAndGrouping(decoupledGroupings2[0], meshInformation, 90f, out boundaryEdgeCycle, out boundaryGrouping);
            Assert.AreEqual(16, boundaryEdgeCycle.Edges.Count, "# Boundary Edges");
            Assert.AreEqual(0, boundaryEdgeCycle.SharedTriangleIndices.Count, "# Shared Triangles");
            Assert.AreEqual(0, boundaryEdgeCycle.AngleDifferenceIndices.Count, "# Angle Differences");
            edgeGroups = GetBoundaryCycleEdgeGroups(boundaryEdgeCycle, boundaryGrouping, meshInformation);
            Assert.IsNotNull(edgeGroups);
            //Note that we need to have addAnyVirtualCutsPossible enabled in TryAddBestVirtualGroupCut to add in-between virtual cuts to get any labeling, which is done correctly via BacktrackResults.RejectedAnyBasedOnTriangles
            Assert.AreEqual(3, edgeGroups.Select(e => e.TextureLabel).Distinct().Count(), "# Texture Labels");
            Assert.AreEqual(5, edgeGroups.Count, "# Edge Groups");
        }

        /// <summary>Wireframe texture labeling test for the TwoTriangleBowtie example mesh.</summary>
        [Test]
        public void TwoTriangleBowtieTextureLabelingTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetTwoTriangleBowtieMesh());
            List<DecoupledGrouping> decoupledGroupingsVertex = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedVertices);
            Assert.AreEqual(1, decoupledGroupingsVertex.Count, "# Decoupled Grouping (by shared vertices)");

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedEdges);
            Assert.AreEqual(2, decoupledGroupings.Count, "# Decoupled Grouping (by shared edges)");

            int uvChannel = 3;
            WireframeTextureCoordinateGenerator wireframeTextureCoordinateGenerator = new WireframeTextureCoordinateGenerator();
            wireframeTextureCoordinateGenerator.DecoupleDisconnectedPortions(meshInformation.Mesh, uvChannel, _edgeAngleCutoff, _raiseWarning);

            List<UnityEngine.Vector4> uvs = new List<UnityEngine.Vector4>();
            meshInformation.Mesh.GetUVs(uvChannel, uvs);

            Assert.IsTrue(uvs.All(uv => (uv.x == 0 || uv.x == 1) && (uv.y == 0 || uv.y == 1) && (uv.z == 0 || uv.z == 1) && (uv.w == 0 || uv.w == 1)), "All UV Components 0 or 1");
            Assert.IsTrue(uvs.All(uv => uv.magnitude > 0.5), "All Wireframe UVs Nonzero");
            List<TextureLabelAssigned> labelAssignments = uvs.Select(uv => new TextureLabelAssigned(uv.x > 0.5, uv.y > 0.5, uv.z > 0.5, uv.w > 0.5)).ToList();

            IEnumerable<List<int>> groupedTriangleIndices = meshInformation.Mesh.triangles.Select((i, index) => (i, index)).GroupBy(p => p.index / 3).Select(g => g.Select(p => p.i).ToList());
            foreach(List<int> triangle in groupedTriangleIndices)
            {
                List<TextureLabelAssigned> assignments = triangle.Select(i => labelAssignments[i]).ToList();
                Assert.AreEqual(3, assignments.Count, "# Label Assignments Per Triangle");
                TextureLabelAssigned totalAssignment = assignments[0].ComponentwiseAnd(assignments[1]).ComponentwiseAnd(assignments[2]);
                Assert.IsFalse(totalAssignment.Any, "All three vertices in triangle with same texture component assignment");

                Assert.IsTrue(assignments[0].ComponentwiseOr(assignments[1]).Any, "Edge vertices share a texture component assignment");
                Assert.IsTrue(assignments[1].ComponentwiseOr(assignments[2]).Any, "Edge vertices share a texture component assignment");
                Assert.IsTrue(assignments[2].ComponentwiseOr(assignments[0]).Any, "Edge vertices share a texture component assignment");
            }
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

        private List<DecoupledGrouping> GetDecoupledGroupings(MeshInformation meshInformation)
        {
            return TriangleGroupUtilities.GetDecoupledGroupings(meshInformation, DecoupledGroupType.SharedEdges);
        }

        private static readonly float _edgeAngleCutoff = 10f;
        private static readonly System.Action<string> _raiseWarning = warning => Assert.Fail(warning);
    }
}
