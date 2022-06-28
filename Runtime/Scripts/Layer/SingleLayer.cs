// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

namespace PixelinearAccelerator.WireframeRendering.Runtime.Layer
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Represents a single Layer number.
    /// </summary>
    [Serializable]
    public struct SingleLayer
    {
        [SerializeField]
        int m_layer;

        /// <summary>The Layer's Index</summary>
        public int Index
        {
            get => m_layer;
            set => m_layer = value;
        }

        /// <summary>The Layer's Mask value</summary>
        public int Mask => 1 << m_layer;

        /// <summary>The Layer's Name</summary>
        public string Name
        {
            get => LayerMask.LayerToName(m_layer);
            set => m_layer = LayerMask.NameToLayer(value);
        }

        /// <summary>The Layer as integer.</summary>
        public static implicit operator int(SingleLayer layer)
        {
            return layer.Index;
        }

        /// <summary>The Layer from the integer layer value</summary>
        public static implicit operator SingleLayer(int layer)
        {
            return new SingleLayer() { Index = layer };
        }

        /// <summary>Creates a <see cref="SingleLayer"/> with the given layer index.</summary>
        public SingleLayer(int index)
        {
            m_layer = index;
        }

        /// <summary>Creates a <see cref="SingleLayer"/> with the given layer name.</summary>
        public SingleLayer(string name)
        {
            m_layer = LayerMask.NameToLayer(name);
        }
    }
}
