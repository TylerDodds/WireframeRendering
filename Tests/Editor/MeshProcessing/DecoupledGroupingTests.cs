// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;
using System.Linq;

namespace PixelinearAccelerator.WireframeRendering.EditorTests
{
    internal class DecoupledGroupingTests
    {
        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for a Quad.</summary>
        [Test]
        public void QuadMeshDecoupledTest()
        {
            Mesh mesh = MeshExamples.GetQuadMesh(1f, 1f);
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(mesh);

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.That(decoupledGroupings.Count == 1, "One Decoupled Grouping");
            DecoupledGrouping grouping = decoupledGroupings[0];
            Assert.That(grouping.Edges.Count == 5, "Five Edges");
            Assert.That(grouping.Triangles.Count == 2, "Two Triangles");
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for a watertight cube mesh with no seam.</summary>
        [Test]
        public void CubeMeshNoSeamDecoupledTest()
        {
            Mesh mesh = MeshExamples.GetNonSeamCubeMesh();
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(mesh);

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "# Decoupled Grouping");
            foreach (DecoupledGrouping grouping in decoupledGroupings)
            {
                AssertNumberEdgesTriangles(grouping, 18, 12);
            }
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for the build-in Cube.</summary>
        [Test]
        public void BuiltInCubeMeshDecoupledTest()
        {
            MeshLibrary meshHolder = MeshExamples.GetMeshLibrary();
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(meshHolder.BuiltInCube);

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(6, decoupledGroupings.Count, "# Decoupled Grouping");
            foreach (DecoupledGrouping grouping in decoupledGroupings)
            {
                AssertNumberEdgesTriangles(grouping, 5, 2);
            }
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for the built-in sphere.</summary>
        [Test]
        public void BuiltInSphereMeshDecoupledTest()
        {
            MeshLibrary meshHolder = MeshExamples.GetMeshLibrary();
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(meshHolder.BuiltInSphere.WithOnlyVerticesAndTriangles());

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(9, decoupledGroupings.Count, "# Decoupled Grouping");
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for the built-in capsule.</summary>
        [Test]
        public void BuiltInCapsuleMeshDecoupledTest()
        {
            MeshLibrary meshHolder = MeshExamples.GetMeshLibrary();
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(meshHolder.BuiltInCapsule.WithOnlyVerticesAndTriangles());

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(7, decoupledGroupings.Count, "# Decoupled Grouping");
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for the built-in cylinder.</summary>
        [Test]
        public void BuiltInCylinderMeshDecoupledTest()
        {
            MeshLibrary meshHolder = MeshExamples.GetMeshLibrary();
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(meshHolder.BuiltInCylinder.WithOnlyVerticesAndTriangles());

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(5, decoupledGroupings.Count, "# Decoupled Grouping");
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for the Small Cylinder example mesh.</summary>
        [Test]
        public void SmallCylinderMeshDecoupledTest()
        {
            MeshLibrary meshHolder = MeshExamples.GetMeshLibrary();
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(meshHolder.SmallCylinder);

            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(3, decoupledGroupings.Count, "# Decoupled Grouping");
            decoupledGroupings = decoupledGroupings.OrderBy(g => g.Triangles.Count).ToList();
            AssertNumberEdgesTriangles(decoupledGroupings[0], 9, 4);
            AssertNumberEdgesTriangles(decoupledGroupings[1], 9, 4);
            AssertNumberEdgesTriangles(decoupledGroupings[2], 24, 12);
        }

        ///<summary>Tests <see cref="DecoupledGrouping"/> generation for the Circle1 example mesh.</summary>
        [Test]
        public void Circle1MeshDecoupledTest()
        {
            MeshInformation meshInformation = MeshHelper.GetMeshInformation(MeshExamples.GetMeshLibrary().Circle1);
            List<DecoupledGrouping> decoupledGroupings = TriangleGroupUtilities.GetDecoupledTriangleGroupings(meshInformation);
            Assert.AreEqual(1, decoupledGroupings.Count, "Decoupled Grouping");
            AssertNumberEdgesTriangles(decoupledGroupings[0], 181, 110);
        }

        /// <summary>
        /// Asserts that the <paramref name="grouping"/> has the given <paramref name="numEdges"/> and <paramref name="numTriangles"/>.
        /// </summary>
        /// <param name="grouping">The <see cref="DecoupledGrouping"/>.</param>
        /// <param name="numEdges">The number of edges.</param>
        /// <param name="numTriangles">The number of triangles.</param>
        private static void AssertNumberEdgesTriangles(DecoupledGrouping grouping, int numEdges, int numTriangles)
        {
            Assert.AreEqual(numEdges, grouping.Edges.Count, "# Edges");
            Assert.AreEqual(numTriangles, grouping.Triangles.Count, "# Triangles");
        }
    }
}
