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
    /// A struct holding whether each of four texture labels is assigned.
    /// </summary>
    internal struct TextureLabelAssigned
    {
        public bool First;
        public bool Second;
        public bool Third;
        public bool Fourth;

        public TextureLabelAssigned(bool first, bool second, bool third, bool fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }

        /// <summary>
        /// Records the given <paramref name="textureLabel"/> as assigned.
        /// </summary>
        /// <param name="textureLabel">The <see cref="TextureLabel"/>.</param>
        public void AddTextureLabel(TextureLabel textureLabel)
        {
            switch (textureLabel)
            {
                case TextureLabel.First:
                    First = true;
                    break;
                case TextureLabel.Second:
                    Second = true;
                    break;
                case TextureLabel.Third:
                    Third = true;
                    break;
                case TextureLabel.Fourth:
                    Fourth = true;
                    break;
            }
        }

        /// <summary>
        /// Takes the component-wise And operation with the <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The component-wise And operation.</returns>
        public TextureLabelAssigned ComponentwiseAnd(TextureLabelAssigned other)
        {
            return new TextureLabelAssigned(First && other.First, Second && other.Second, Third && other.Third, Fourth && other.Fourth);
        }

        /// <summary>
        /// Takes the component-wise Or operation with the <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The component-wise Or operation.</returns>
        public TextureLabelAssigned ComponentwiseOr(TextureLabelAssigned other)
        {
            return new TextureLabelAssigned(First || other.First, Second || other.Second, Third || other.Third, Fourth || other.Fourth);
        }

        /// <summary>
        /// Takes the component-wise XOr operation with the <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The component-wise XOr operation.</returns>
        public TextureLabelAssigned ComponentwiseXOr(TextureLabelAssigned other)
        {
            return new TextureLabelAssigned(First ^ other.First, Second ^ other.Second, Third ^ other.Third, Fourth ^ other.Fourth);
        }

        /// <summary>
        /// If all components are True.
        /// </summary>
        public bool All => First && Second && Third && Fourth;

        /// <summary>
        /// If any component is True.
        /// </summary>
        public bool Any => First || Second || Third || Fourth;

        public override string ToString()
        {
            return $"{First} {Second} {Third} {Fourth}";
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
