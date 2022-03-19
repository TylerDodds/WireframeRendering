// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

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
        public WireframeRenderingFeatureSettings Settings = WireframeRenderingFeatureSettings.Default;

        /// <inheritdoc/>
        public override void Create()
        {
            Shader wireframeShader = Shader.Find("Hidden/PixelinearAccelerator/Wireframe/URP Wireframe Unlit (From TexCoords)");
            if (wireframeShader != null)
            {
                if (Settings.InFrontWireframe)
                {
                    _inFrontMaterial = new Material(wireframeShader);
                    ConfigureWireframeMaterial(_inFrontMaterial, Settings.FrontSettings, Settings.Haloing, Settings.HaloingWidth, false, 0f);
                }
                if (Settings.InBehindWireframe)
                {
                    _inBehindMaterial = new Material(wireframeShader);
                    ConfigureWireframeMaterial(_inBehindMaterial, Settings.BehindSettings, false, 0f, Settings.BehindDepthFade, Settings.DepthFadeDistance);
                }
            }

            string profilerTag = nameof(WireframeRenderingFeature);
            string[] shaderTags = new string[0];
            int layerMask = Settings.LayerMask;
            int stencilReference = Settings.StencilReferenceValue;
            RenderObjects.CustomCameraSettings cameraSettings = new RenderObjects.CustomCameraSettings();

            //Note that we use RenderQueueType.Opaque since we are rendering objects meant to be opaque (and which write depth), even if the materials use alpha blending.
            _wireframePassInFront = new RenderObjectsPass(profilerTag, RenderPassEvent.AfterRenderingOpaques, shaderTags, RenderQueueType.Opaque, layerMask, cameraSettings);
            _wireframePassInFront.overrideMaterial = _inFrontMaterial;
            _wireframePassInFront.SetDetphState(false, CompareFunction.LessEqual);
            StencilOp frontPassOp = Settings.Haloing ? StencilOp.Replace: StencilOp.Keep;
            _wireframePassInFront.SetStencilState(stencilReference, CompareFunction.Always, frontPassOp, StencilOp.Keep, StencilOp.Keep);

            _wireframePassBehind = new RenderObjectsPass(profilerTag, RenderPassEvent.AfterRenderingOpaques, shaderTags, RenderQueueType.Opaque, layerMask, cameraSettings);
            _wireframePassBehind.overrideMaterial = _inBehindMaterial;
            _wireframePassBehind.SetDetphState(false, CompareFunction.Greater);
            _wireframePassBehind.SetStencilState(stencilReference, CompareFunction.NotEqual, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);

            // Configures where the render pass should be injected.
            _wireframePassInFront.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            _wireframePassBehind.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Draw in-front wireframes first if using haloing, otherwise draw the behind wireframes first, followed by the in-front ones.
            if(Settings.Haloing)
            {
                if (Settings.InFrontWireframe)
                {
                    renderer.EnqueuePass(_wireframePassInFront);
                }
                if (Settings.InBehindWireframe)
                {
                    renderer.EnqueuePass(_wireframePassBehind);
                }
            }
            else
            {
                if (Settings.InBehindWireframe)
                {
                    renderer.EnqueuePass(_wireframePassBehind);
                }
                if (Settings.InFrontWireframe)
                {
                    renderer.EnqueuePass(_wireframePassInFront);
                }
            }
        }

        /// <summary>
        /// Configues a wireframe shader material from the given settings.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="edgeSettings">The settings.</param>
        /// <param name="haloing">If haloing is being used for this Material.</param>
        /// <param name="haloingWidth">The haloing width to be used.</param>
        /// <param name="behindDepthFade">If depth fade behind objects is being used for this Material.</param>
        /// <param name="depthFadeDistance">The distance length used for depth fading.</param>
        private void ConfigureWireframeMaterial(Material material, WireframeEdgeSettings edgeSettings, bool haloing, float haloingWidth, bool behindDepthFade, float depthFadeDistance)
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

        private RenderObjectsPass _wireframePassInFront;
        private RenderObjectsPass _wireframePassBehind;
        private Material _inFrontMaterial;
        private Material _inBehindMaterial;
    }

}
