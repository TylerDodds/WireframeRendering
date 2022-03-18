// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Indices the reason for considering neighbouring edges to be of different groups for texture labeling reasons.
    /// </summary>
    internal enum GroupCutType
    {
        SameTriangle,
        EdgeAngle,
        Virtual,
    }

    /// <summary>
    /// A struct contains four booleans.
    /// </summary>
    internal struct FourBools
    {
        public bool x;
        public bool y;
        public bool z;
        public bool w;

        public FourBools(bool x, bool y, bool z, bool w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public FourBools ComponentwiseAnd(FourBools other)
        {
            return new FourBools(x && other.x, y && other.y, z && other.z, w && other.w);
        }
    }

    /// <summary>
    /// Results when getting the set of <see cref="EdgeGroup"/>s of a <see cref="BoundaryEdgeCycle"/>.
    /// </summary>
    internal class EdgeGroupResults
    {
        public bool HasValue => EdgeGroups != null;

        public IReadOnlyList<IEdgeGroup> EdgeGroups { get; }
        public bool CalculationTimedOut { get; }
        public double TimeoutSeconds { get; }

        public EdgeGroupResults(List<EdgeGroup> edgeGroups)
        {
            EdgeGroups = edgeGroups;
            CalculationTimedOut = false;
            TimeoutSeconds = 0;
        }

        public EdgeGroupResults(bool timeout, double timeoutSeconds)
        {
            EdgeGroups = null;
            CalculationTimedOut = timeout;
            TimeoutSeconds = timeoutSeconds;
        }
    }
}
