// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature
{

    /// <summary>
    /// Setings for the <see cref="WireframeRenderingFeature"/>.
    /// </summary>
    [Serializable]
    public class WireframeRenderingFeatureSettings
    {
        /// <summary>
        /// The <see cref="LayerMask"/> of objects to render.
        /// </summary>
        public LayerMask LayerMask;

        /// <summary>
        /// If wireframes on front of objects should be drawn.
        /// </summary>
        public bool InFrontWireframe;

        /// <summary>
        /// Settings for wireframe edges drawn on front of objects.
        /// </summary>
        public WireframeEdgeSettings FrontSettings;

        /// <summary>
        /// If wireframes behind objects should be drawn.
        /// </summary>
        public bool InBehindWireframe;

        /// <summary>
        /// Settings for wireframe edges drawn behind of objects.
        /// </summary>
        public WireframeEdgeSettings BehindSettings;

        /// <summary>
        /// If edges behind objects should fade with depth.
        /// </summary>
        public bool BehindDepthFade = false;

        /// <summary>
        /// The distance length used for depth fading.
        /// </summary>
        [Range(0, 50)]
        public float DepthFadeDistance = 10.0f;

        /// <summary>
        /// Return if haloing should be used, based on <see cref="HaloingWhenBothFrontAndBack"/> settings and if front and back wireframes are drawn.
        /// </summary>
        public bool Haloing => InFrontWireframe && InBehindWireframe && HaloingWhenBothFrontAndBack;

        /// <summary>
        /// If haloing should be used when both front and back wireframes are drawn.
        /// </summary>
        public bool HaloingWhenBothFrontAndBack;

        /// <summary>
        /// The width of the haloing effect.
        /// </summary>
        [Range(0, 20)]
        public float HaloingWidth = 5;

        /// <summary>
        /// The stencil reference value used for the haloing effect.
        /// </summary>
        [Range(0, 15)]
        [Tooltip("The stencil buffer reference value used for the haloing effect.")]
        public int StencilReferenceValue = 15;

        /// <summary>
        /// The UV channel where wireframe coordinates are set.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private int _uvChannel;

        /// <summary>
        /// The UV channel where wireframe coordinates are set.
        /// </summary>
        public int UvChannel => _uvChannel;

        /// <summary>
        /// Default values for <see cref="WireframeRenderingFeatureSettings"/>.
        /// </summary>
        public static WireframeRenderingFeatureSettings Default = new WireframeRenderingFeatureSettings()
        {
            InFrontWireframe = true,
            FrontSettings = new WireframeEdgeSettings()
            {
                Color = Color.black,
                WidthPx = 10,
                FalloffWidthPx = 1,
                Dash = false,
            },
            InBehindWireframe = true,
            BehindSettings = new WireframeEdgeSettings()
            {
                Color = new Color(0, 0, 0, 0.5f),
                WidthPx = 5,
                FalloffWidthPx = 1,
                Dash = false,
            },
            HaloingWhenBothFrontAndBack = true,
            HaloingWidth = 5,
        };
    }
}