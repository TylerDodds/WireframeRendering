// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.EditorTests
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> holding a library of <see cref="Mesh"/>es.
    /// </summary>
    internal class MeshLibrary : ScriptableObject
    {
        public Mesh BuiltInCube;
        public Mesh BuiltInCapsule;
        public Mesh BuiltInCylinder;
        public Mesh BuiltInSphere;

        public Mesh SmallCylinder;
        public Mesh Circle1;
        public Mesh TenSidedPoly1;
        public Mesh OneGroupCutEachType1;
        public Mesh Circle2;
        public Mesh CircleFan1;
        public Mesh CircleFan2;
        public Mesh CircleFan3;
        public Mesh CircleFan4;
    }
}
