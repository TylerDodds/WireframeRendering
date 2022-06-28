// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.Enums;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature
{
    /// <summary>
    /// Custom <see cref="ScriptableRendererFeature"/> for Wireframe rendering.
    /// </summary>
    public class WireframeRenderingFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// The <see cref="WireframeRenderingFeatureSettings"/>.
        /// </summary>
        public WireframeRenderingFeatureSettings Settings = WireframeRenderingFeatureSettings.GetDefault();

        /// <inheritdoc/>
        public override void Create()
        {
            switch (Settings.WireframeType)
            {
                case WireframeType.TextureCoordinates:
                    InitializeTextureCoordinateMaterialFrontBackPasses();
                    break;
                case WireframeType.GeometryShader:
                    InitializeGeometryShaderMaterialFrontBackPasses();
                    break;
                case WireframeType.None:
                default:
                    InitializeNoWireframeFrontBackPasses();
                    break;
            }
        }

        /// <summary>
        /// Initializes <see cref="_inFrontMaterial"/>, <see cref="_inBehindMaterial"/>, <see cref="_wireframePassBehind"/> and <see cref="_wireframePassInFront"/> when not rendering wireframes.
        /// </summary>
        private void InitializeNoWireframeFrontBackPasses()
        {
            _inFrontMaterial = null;
            _inBehindMaterial = null;
            _wireframePassBehind = null;
            _wireframePassInFront = null;
        }

        /// <summary>
        /// Initializes <see cref="_inFrontMaterial"/>, <see cref="_inBehindMaterial"/>, <see cref="_wireframePassBehind"/> and <see cref="_wireframePassInFront"/> when using geometry shader.
        /// </summary>
        private void InitializeGeometryShaderMaterialFrontBackPasses()
        {
            Shader wireframeGeometryShader = Shader.Find("Hidden/PixelinearAccelerator/Wireframe/URP Wireframe Unlit (Using Geometry Shader)");
            _inFrontMaterial = null;
            _inBehindMaterial = null;
            _wireframePassInFront = null;
            _wireframePassBehind = null;
            if (wireframeGeometryShader != null)
            {
                Vector4 ndcOutsideViewFactors = new Vector4(Settings.ViewportEdgeWidthTaperStart, Settings.ViewportEdgeWidthTaperEnd, Settings.ViewportEdgeAlphaFadeStart, Settings.ViewportEdgeAlphaFadeEnd);
                if (Settings.InFrontWireframe)
                {
                    _inFrontMaterial = new Material(wireframeGeometryShader);
                    float haloingWidth = Settings.FrontSettings.WorldSpace ? Settings.HaloingWidthWorld : Settings.HaloingWidthPx;
                    InFrontBehindType inFrontBehindType = Settings.BehindSamePassAsFront ? InFrontBehindType.All : InFrontBehindType.InFront;
                    bool depthFade = Settings.BehindSamePassAsFront ? Settings.BehindDepthFade : false;
                    float depthFadeDistance = Settings.BehindSamePassAsFront ? Settings.DepthFadeDistance : 0f;
                    ConfigureWireframeGeometryMaterial(_inFrontMaterial, Settings.FrontSettings, inFrontBehindType, Settings.Haloing, haloingWidth, depthFade, depthFadeDistance, Settings.InFrontDepthCutoff, ndcOutsideViewFactors);
                }
                if (Settings.InBehindWireframe && ! Settings.BehindSamePassAsFront)
                {
                    _inBehindMaterial = new Material(wireframeGeometryShader);
                    ConfigureWireframeGeometryMaterial(_inBehindMaterial, Settings.BehindSettings, InFrontBehindType.Behind, false, 0f, Settings.BehindDepthFade, Settings.DepthFadeDistance, Settings.InFrontDepthCutoff, ndcOutsideViewFactors);
                }
            }

            string profilerTag = nameof(WireframeRenderingFeature);
            string[] shaderTags = new string[0];
            int layerMask = Settings.LayerMask;
            int stencilReference = Settings.StencilReferenceValue;
            RenderObjects.CustomCameraSettings cameraSettings = new RenderObjects.CustomCameraSettings();

            //Note that we use RenderQueueType.Transparent to get the appropriate object sorting for transparent cases.
            if(_inFrontMaterial != null)
            {
                _wireframePassInFront = new RenderObjectsPass(profilerTag, RenderPassEvent.AfterRenderingSkybox, shaderTags, RenderQueueType.Transparent, layerMask, cameraSettings);
                _wireframePassInFront.overrideMaterial = _inFrontMaterial;
                _wireframePassInFront.SetDetphState(false, CompareFunction.Always);
                StencilOp frontPassOp = Settings.Haloing ? StencilOp.Replace : StencilOp.Keep;
                _wireframePassInFront.SetStencilState(stencilReference, CompareFunction.Always, frontPassOp, StencilOp.Keep, StencilOp.Keep);
            }
            
            if(_inBehindMaterial != null)
            {
                _wireframePassBehind = new RenderObjectsPass(profilerTag, RenderPassEvent.AfterRenderingSkybox, shaderTags, RenderQueueType.Transparent, layerMask, cameraSettings);
                _wireframePassBehind.overrideMaterial = _inBehindMaterial;
                _wireframePassBehind.SetDetphState(false, CompareFunction.Always);
                _wireframePassBehind.SetStencilState(stencilReference, CompareFunction.NotEqual, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            }
        }

        /// <summary>
        /// Initializes <see cref="_inFrontMaterial"/>, <see cref="_inBehindMaterial"/>, <see cref="_wireframePassBehind"/> and <see cref="_wireframePassInFront"/> when using texture coordinates.
        /// </summary>
        private void InitializeTextureCoordinateMaterialFrontBackPasses()
        {
            Shader wireframeShader = Shader.Find("Hidden/PixelinearAccelerator/Wireframe/URP Wireframe Unlit (From TexCoords)");
            _inFrontMaterial = null;
            _inBehindMaterial = null;
            _wireframePassInFront = null;
            _wireframePassBehind = null;
            if (wireframeShader != null)
            {
                if (Settings.InFrontWireframe)
                {
                    _inFrontMaterial = new Material(wireframeShader);
                    bool depthFade = Settings.BehindSamePassAsFront ? Settings.BehindDepthFade : false;
                    float depthFadeDistance = Settings.BehindSamePassAsFront ? Settings.DepthFadeDistance : 0f;
                    ConfigureWireframeTextureCoordinatesMaterial(_inFrontMaterial, Settings.FrontSettings, Settings.Haloing, Settings.HaloingWidthPx, depthFade, depthFadeDistance);
                }
                if (Settings.InBehindWireframe && !Settings.BehindSamePassAsFront)
                {
                    _inBehindMaterial = new Material(wireframeShader);
                    ConfigureWireframeTextureCoordinatesMaterial(_inBehindMaterial, Settings.BehindSettings, false, 0f, Settings.BehindDepthFade, Settings.DepthFadeDistance);
                }
            }

            string profilerTag = nameof(WireframeRenderingFeature);
            string[] shaderTags = new string[0];
            int layerMask = Settings.LayerMask;
            int stencilReference = Settings.StencilReferenceValue;
            RenderObjects.CustomCameraSettings cameraSettings = new RenderObjects.CustomCameraSettings();

            if (_inFrontMaterial != null)
            {
                //Note that we use RenderQueueType.Opaque since we are rendering objects meant to be opaque (and which write depth), even if the materials use alpha blending.
                _wireframePassInFront = new RenderObjectsPass(profilerTag, RenderPassEvent.AfterRenderingSkybox, shaderTags, RenderQueueType.Opaque, layerMask, cameraSettings);
                _wireframePassInFront.overrideMaterial = _inFrontMaterial;
                CompareFunction depthCompareFunction = Settings.BehindSamePassAsFront ? CompareFunction.Always : CompareFunction.LessEqual;
                _wireframePassInFront.SetDetphState(false, depthCompareFunction);
                StencilOp frontPassOp = Settings.Haloing ? StencilOp.Replace : StencilOp.Keep;
                _wireframePassInFront.SetStencilState(stencilReference, CompareFunction.Always, frontPassOp, StencilOp.Keep, StencilOp.Keep);
            }

            if (_inBehindMaterial != null)
            {
                _wireframePassBehind = new RenderObjectsPass(profilerTag, RenderPassEvent.AfterRenderingSkybox, shaderTags, RenderQueueType.Opaque, layerMask, cameraSettings);
                _wireframePassBehind.overrideMaterial = _inBehindMaterial;
                _wireframePassBehind.SetDetphState(false, CompareFunction.Greater);
                _wireframePassBehind.SetStencilState(stencilReference, CompareFunction.NotEqual, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            }
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Draw in-front wireframes first if using haloing, otherwise draw the behind wireframes first, followed by the in-front ones.
            if (!Settings.WireframeType.IsNone())
            {
                if (Settings.Haloing)
                {
                    if (_wireframePassInFront != null && Settings.InFrontWireframe)
                    {
                        renderer.EnqueuePass(_wireframePassInFront);
                    }
                    if (_wireframePassBehind != null && Settings.InBehindWireframe)
                    {
                        renderer.EnqueuePass(_wireframePassBehind);
                    }
                }
                else
                {
                    if (_wireframePassBehind != null && Settings.InBehindWireframe)
                    {
                        renderer.EnqueuePass(_wireframePassBehind);
                    }
                    if (_wireframePassInFront != null && Settings.InFrontWireframe)
                    {
                        renderer.EnqueuePass(_wireframePassInFront);
                    }
                }
            }
        }

        /// <summary>
        /// Configuers a wireframe shader material (using geometry shader method) from the given settings.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="edgeSettings">The settings.</param>
        /// <param name="haloing">If haloing is being used for this Material.</param>
        /// <param name="haloingWidth">The haloing width to be used.</param>
        /// <param name="behindDepthFade">If depth fade behind objects is being used for this Material.</param>
        /// <param name="depthFadeDistance">The distance length used for depth fading.</param>
        /// <param name="inFrontBehindType">Type of depth rendering used by material.</param>
        /// <param name="inFrontDepthCutoff">Depth cutoff between in-front and behind objects rendering.</param>
        /// <param name="ndcOutsideViewFactors">NDC outside view fractions to start and end falloff of width and alpha.</param>
        private void ConfigureWireframeGeometryMaterial(Material material, WireframeEdgeSettings edgeSettings, InFrontBehindType inFrontBehindType, bool haloing, float haloingWidth, bool behindDepthFade, float depthFadeDistance, float inFrontDepthCutoff, Vector4 ndcOutsideViewFactors)
        {
            material.SetColor("_EdgeColor", edgeSettings.Color);
            if (edgeSettings.WorldSpace)
            {
                material.SetFloat("_Width", edgeSettings.WidthWorld);
            }
            else
            {
                material.SetFloat("_Width", edgeSettings.WidthPx);
            }
            material.SetFloat("_FalloffWidth", edgeSettings.FalloffWidthPx);

            SetMaterialKeyword(material, "WIREFRAME_CLIP", haloing);
            material.SetFloat("_WireframeClip", haloing ? 1 : 0);
            material.SetFloat("_WireframeCutoff", haloingWidth);

            SetMaterialKeyword(material, "WIREFRAME_DASH", edgeSettings.Dash);
            material.SetFloat("_WireframeDash", edgeSettings.Dash ? 1 : 0);
            if (edgeSettings.WorldSpace)
            {
                material.SetFloat("_DashLength", edgeSettings.DashLengthWorld);
                material.SetFloat("_EmptyLength", edgeSettings.EmptyLengthWorld);
            }
            else
            {
                material.SetFloat("_DashLength", edgeSettings.DashLengthPx);
                material.SetFloat("_EmptyLength", edgeSettings.EmptyLengthPx);
            }

            SetMaterialKeyword(material, "WIREFRAME_OVERSHOOT", edgeSettings.Overshoot);
            material.SetFloat("_Overshoot", edgeSettings.Overshoot ? 1 : 0);
            if (edgeSettings.WorldSpace)
            {
                material.SetFloat("_OvershootLength", edgeSettings.OvershootWorld);
            }
            else
            {
                material.SetFloat("_OvershootLength", edgeSettings.OvershootPx);
            }

            if(Settings.WireframeType == WireframeType.GeometryShader)
            {
                bool useObjectNormals = !(Settings.ObjectNormalsImported && edgeSettings.WorldSpace) ? false : edgeSettings.UseObjectNormals;
                SetMaterialKeyword(material, "WIREFRAME_USE_OBJECT_NORMALS", useObjectNormals);
                material.SetFloat("_UseObjectNormals", useObjectNormals ? 1 : 0);

                SetMaterialKeyword(material, "_WIREFRAME_CONTOUR_EDGES_NOTIMPORTED", !Settings.ContourEdgesImported);
                SetMaterialKeyword(material, "_WIREFRAME_CONTOUR_EDGES_SHOWN", Settings.ContourEdgesImported && edgeSettings.ShowContourEdges);
                SetMaterialKeyword(material, "_WIREFRAME_CONTOUR_EDGES_NOTSHOWN", Settings.ContourEdgesImported && !edgeSettings.ShowContourEdges);
                material.SetFloat("_Wireframe_Contour_Edges", Settings.ContourEdgesImported ? (edgeSettings.ShowContourEdges ? 1 : 2) : 0);
            }

            SetMaterialKeyword(material, "WIREFRAME_TEXTURE", edgeSettings.ApplyTexture);
            material.SetFloat("_ApplyTexture", edgeSettings.ApplyTexture ? 1 : 0);

            if (edgeSettings.ApplyTexture && edgeSettings.Texture != null)
            {
                material.SetTexture("_WireframeTex", edgeSettings.Texture);
                float aspect = edgeSettings.Texture == null ? 1f : edgeSettings.Texture.width / (float)edgeSettings.Texture.height;
                if (edgeSettings.WorldSpace)
                {
                    material.SetFloat("_TexLength", edgeSettings.KeepTextureAspectRatio ? edgeSettings.WidthWorld * aspect : edgeSettings.TextureLengthWorld);
                }
                else
                {
                    material.SetFloat("_TexLength", edgeSettings.KeepTextureAspectRatio ? edgeSettings.WidthPx * aspect : edgeSettings.TextureLengthPx);
                }
            }

            SetMaterialKeyword(material, "WIREFRAME_WORLD", edgeSettings.WorldSpace);
            material.SetFloat("_WorldSpaceReference", edgeSettings.WorldSpace ? 1 : 0);

            SetMaterialKeyword(material, "WIREFRAME_BEHIND_DEPTH_FADE", behindDepthFade);
            material.SetFloat("_BehindDepthFade", behindDepthFade ? 1 : 0);
            if (behindDepthFade)
            {
                material.SetFloat("_DepthFadeDistance", depthFadeDistance);
            }

            SetMaterialKeyword(material, "_WIREFRAME_DEPTH_INFRONT", inFrontBehindType == InFrontBehindType.InFront);
            SetMaterialKeyword(material, "_WIREFRAME_DEPTH_BEHIND", inFrontBehindType == InFrontBehindType.Behind);
            SetMaterialKeyword(material, "_WIREFRAME_DEPTH_ALL", inFrontBehindType == InFrontBehindType.All);
            material.SetFloat("_Wireframe_Depth", inFrontBehindType == InFrontBehindType.All ? 2 : (inFrontBehindType == InFrontBehindType.Behind ? 1 : 0));

            material.SetFloat("_InFrontDepthCutoff", inFrontDepthCutoff);
            material.SetVector("_NdcMinMaxCutoffForWidthAndAlpha", ndcOutsideViewFactors);
        }

        /// <summary>
        /// Configuers a wireframe shader material (using texture coordinate method) from the given settings.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="edgeSettings">The settings.</param>
        /// <param name="haloing">If haloing is being used for this Material.</param>
        /// <param name="haloingWidth">The haloing width to be used.</param>
        /// <param name="behindDepthFade">If depth fade behind objects is being used for this Material.</param>
        /// <param name="depthFadeDistance">The distance length used for depth fading.</param>
        private void ConfigureWireframeTextureCoordinatesMaterial(Material material, WireframeEdgeSettings edgeSettings, bool haloing, float haloingWidth, bool behindDepthFade, float depthFadeDistance)
        {
            material.SetColor("_EdgeColor", edgeSettings.Color);
            if (edgeSettings.WorldSpace)
            {
                material.SetFloat("_Width", edgeSettings.WidthWorld);
            }
            else
            {
                material.SetFloat("_Width", edgeSettings.WidthPx);
            }
            material.SetFloat("_FalloffWidth", edgeSettings.FalloffWidthPx);

            SetMaterialKeyword(material, "WIREFRAME_CLIP", haloing);
            material.SetFloat("_WireframeClip", haloing ? 1 : 0);
            material.SetFloat("_WireframeCutoff", haloingWidth);

            SetMaterialKeyword(material, "WIREFRAME_DASH", edgeSettings.Dash);
            material.SetFloat("_WireframeDash", edgeSettings.Dash ? 1 : 0);
            if (edgeSettings.WorldSpace)
            {
                material.SetFloat("_DashLength", edgeSettings.DashLengthWorld);
                material.SetFloat("_EmptyLength", edgeSettings.EmptyLengthWorld);
            }
            else
            {
                material.SetFloat("_DashLength", edgeSettings.DashLengthPx);
                material.SetFloat("_EmptyLength", edgeSettings.EmptyLengthPx);
            }

            SetMaterialKeyword(material, "WIREFRAME_TEXTURE", edgeSettings.ApplyTexture);
            material.SetFloat("_ApplyTexture", edgeSettings.ApplyTexture ? 1 : 0);

            if (edgeSettings.ApplyTexture && edgeSettings.Texture != null)
            {
                material.SetTexture("_WireframeTex", edgeSettings.Texture);
                float aspect = edgeSettings.Texture == null ? 1f : edgeSettings.Texture.width / (float)edgeSettings.Texture.height;
                if (edgeSettings.WorldSpace)
                {
                    material.SetFloat("_TexLength", edgeSettings.KeepTextureAspectRatio ? edgeSettings.WidthWorld * aspect: edgeSettings.TextureLengthWorld);
                }
                else
                {
                    material.SetFloat("_TexLength", edgeSettings.KeepTextureAspectRatio ? edgeSettings.WidthPx * aspect : edgeSettings.TextureLengthPx);
                }
            }

            SetMaterialKeyword(material, "WIREFRAME_WORLD", edgeSettings.WorldSpace);
            material.SetFloat("_WorldSpaceReference", edgeSettings.WorldSpace ? 1 : 0);

            SetMaterialKeyword(material, "WIREFRAME_BEHIND_DEPTH_FADE", behindDepthFade);
            material.SetFloat("_BehindDepthFade", behindDepthFade ? 1 : 0);
            if(behindDepthFade)
            {
                material.SetFloat("_DepthFadeDistance", depthFadeDistance);
            }

            SetMaterialKeyword(material, "WIREFRAME_FRESNEL_EFFECT", edgeSettings.Fresnel);
            material.SetFloat("_WireframeFresnelEffect", edgeSettings.Fresnel ? 1 : 0);

            SetMaterialKeyword(material, $"UV{Settings.UvChannel}", true);
        }

        /// <summary>
        /// Sets a material's keyword enabled or disabled.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="name">The keyword.</param>
        /// <param name="enabled">If the keyword should be enabled.</param>
        private static void SetMaterialKeyword(Material material, string name, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(name);
            }
            else
            {
                material.DisableKeyword(name);
            }
        }

        /// <summary>
        /// In-Front / Behind depth type.
        /// </summary>
        private enum InFrontBehindType
        {
            InFront,
            Behind,
            All,
        }

        private RenderObjectsPass _wireframePassInFront;
        private RenderObjectsPass _wireframePassBehind;
        private Material _inFrontMaterial;
        private Material _inBehindMaterial;
    }

}
