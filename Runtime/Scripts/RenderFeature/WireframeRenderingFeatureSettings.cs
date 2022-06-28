// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.Enums;
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
        /// If in-behind wireframe should be done in the same pass as the front-face pass.
        /// </summary>
        public bool BehindSamePassAsFront => InBehindWireframe && BehindLooksLikeFront && (WireframeType == WireframeType.GeometryShader || WireframeType == WireframeType.TextureCoordinates);

        /// <summary>
        /// If in-behind wireframe should look the same as in-front wireframe for Geometry Shader type.
        /// </summary>
        public bool BehindLooksLikeFront = false;

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
        public bool Haloing => InFrontWireframe && InBehindWireframe && HaloingWhenBothFrontAndBack && !BehindSamePassAsFront;

        /// <summary>
        /// If haloing should be used when both front and back wireframes are drawn.
        /// </summary>
        public bool HaloingWhenBothFrontAndBack;

        /// <summary>
        /// The width of the haloing effect in px.
        /// </summary>
        [Range(0, 20)]
        public float HaloingWidthPx = 5;

        /// <summary>
        /// The width of the haloing effect in world-space units.
        /// </summary>
        [Range(0, 0.25f)]
        public float HaloingWidthWorld = 0.05f;

        /// <summary>
        /// The stencil reference value used for the haloing effect.
        /// </summary>
        [Range(0, 15)]
        [Tooltip("The stencil buffer reference value used for the haloing effect.")]
        public int StencilReferenceValue = 15;

        /// <summary>
        /// The cutoff depth between in front and behind wireframe types.
        /// </summary>
        [Range(0, 0.25f)]
        [Tooltip("The cutoff depth between in-front-of and behind-objects wireframe types.")]
        public float InFrontDepthCutoff = 0.005f;

        /// <summary>
        /// The viewport fraction past the edge where width tapering begins.
        /// </summary>
        [Range(-0.1f, 0.1f)]
        [Tooltip("The viewport fraction past the edge where width tapering begins.")]
        public float ViewportEdgeWidthTaperStart = -0.01f;

        /// <summary>
        /// The viewport fraction past the edge where width tapering ends.
        /// </summary>
        [Range(-0.1f, 0.1f)]
        [Tooltip("The viewport fraction past the edge where width tapering ends.")]
        public float ViewportEdgeWidthTaperEnd = 0.01f;

        /// <summary>
        /// The viewport fraction past the edge where alpha fading begins.
        /// </summary>
        [Range(-0.1f, 0.1f)]
        [Tooltip("The viewport fraction past the edge where alpha fading begins.")]
        public float ViewportEdgeAlphaFadeStart = -0.001f;

        /// <summary>
        /// The viewport fraction past the edge where alpha fading ends.
        /// </summary>
        [Range(-0.1f, 0.1f)]
        [Tooltip("The viewport fraction past the edge where alpha fading ends.")]
        public float ViewportEdgeAlphaFadeEnd = 0.001f;

        /// <summary>
        /// The UV channel where wireframe coordinates are set.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private int _uvChannel;//Note that this property is set by string name in its Editor

        /// <summary>
        /// The UV channel where wireframe coordinates are set.
        /// </summary>
        public int UvChannel => _uvChannel;

        /// <summary>
        /// The type of wireframe generation.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private WireframeType _wireframeType;//Note that this property is set by string name in its Editor

        /// <summary>
        /// The type of wireframe generation.
        /// </summary>
        public WireframeType WireframeType => _wireframeType;

        /// <summary>
        /// If object-space normals are imported and available for use.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private bool _objectNormalsImported = false;//Note that this property is set by string name in its Editor

        /// <summary>
        /// If object-space normals are imported and available for use.
        /// </summary>
        public bool ObjectNormalsImported => _objectNormalsImported;

        /// <summary>
        /// If contour edge information is imported and available for use.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private bool _contourEdgesImported = false;//Note that this property is set by string name in its Editor

        /// <summary>
        /// If contour edge information is imported and available for use.
        /// </summary>
        public bool ContourEdgesImported => _contourEdgesImported;

        

        /// <summary>
        /// Default values for <see cref="WireframeRenderingFeatureSettings"/>.
        /// </summary>
        public static WireframeRenderingFeatureSettings GetDefault() => new WireframeRenderingFeatureSettings()
        {
            InFrontWireframe = true,
            FrontSettings = new WireframeEdgeSettings()
            {
                Color = Color.black,
                WidthPx = 10,
                FalloffWidthPx = 1,
                Dash = false,
                Overshoot = false,
            },
            InBehindWireframe = true,
            BehindSettings = new WireframeEdgeSettings()
            {
                Color = new Color(0, 0, 0, 0.5f),
                WidthPx = 5,
                FalloffWidthPx = 1,
                Dash = false,
                Overshoot = false,
            },
            HaloingWhenBothFrontAndBack = true,
            HaloingWidthPx = 5,
            _uvChannel = 3,
            _wireframeType = WireframeType.TextureCoordinates,
        };
    }
}