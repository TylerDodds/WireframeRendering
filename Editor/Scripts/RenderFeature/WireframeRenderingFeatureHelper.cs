// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature;
using UnityEditor;
using System;
using UnityEngine;
using PixelinearAccelerator.WireframeRendering.Editor.Settings;
using UnityEngine.Rendering.Universal;
using System.Linq;
using System.Collections.Generic;

namespace PixelinearAccelerator.WireframeRendering.Editor.RenderFeature
{
    /// <summary>
    /// Helper functions for for <see cref="WireframeRenderingFeature"/>.
    /// </summary>
    internal static class WireframeRenderingFeatureHelper
    {
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSelectRendererFeature, priority = -99)]
        public static void SelectRendererFeature()
        {
            TraverseCurrentURPAssetForWireframeRenderingFeatures((urpAsset, scriptableRenderer, wireframeRendererFeatures) => Selection.SetActiveObjectWithContext(wireframeRendererFeatures[0], null), true);
        }

        /// <summary>
        /// Fills a list of <see cref="UniversalRenderPipelineAsset"/> that use the given <paramref name="wireframeRenderingFeature"/>.
        /// </summary>
        /// <param name="wireframeRenderingFeature">The <see cref="WireframeRenderingFeature"/>.</param>
        /// <param name="universalRenderPipelineAssets">A <see cref="List{UniversalRenderPipelineAsset}"/> to fill.</param>
        internal static void FillPipelineAssetListsIfHasFeature(WireframeRenderingFeature wireframeRenderingFeature, HashSet<UniversalRenderPipelineAsset> universalRenderPipelineAssets)
        {
            universalRenderPipelineAssets.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(UniversalRenderPipelineAsset)}");
            IEnumerable<string> paths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g));
            IEnumerable<UniversalRenderPipelineAsset> assets = paths.Select(p => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(p)).Where(a => a != null);
            foreach(UniversalRenderPipelineAsset asset in assets)
            {
                TranverseRenderersForWireframeRenderingFeatures(asset, (sr, wrfs) =>
                {
                    if (wrfs.Contains(wireframeRenderingFeature))
                    {
                        universalRenderPipelineAssets.Add(asset);
                    }
                }, false);
            }
        }

        /// <summary>
        /// Fills a list of <see cref="ScriptableRenderer"/> that use the given <paramref name="wireframeRenderingFeature"/>.
        /// </summary>
        /// <param name="wireframeRenderingFeature">The <see cref="WireframeRenderingFeature"/>.</param>
        /// <param name="scriptableRendererDatas">A <see cref="List{ScriptableRendererData}"/> to fill.</param>
        internal static void FillScriptableRendererListIfHasFeature(WireframeRenderingFeature wireframeRenderingFeature, HashSet<ScriptableRendererData> scriptableRendererDatas)
        {
            scriptableRendererDatas.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableRendererData)}");
            IEnumerable<string> paths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g));
            IEnumerable<ScriptableRendererData> assets = paths.Select(p => AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(p)).Where(a => a != null);
            foreach (ScriptableRendererData asset in assets)
            {
                TranverseRenderersForWireframeRenderingFeatures(asset, (wrfs) =>
                {
                    if (wrfs.Contains(wireframeRenderingFeature))
                    {
                        scriptableRendererDatas.Add(asset);
                    }
                });
            }
        }

        /// <summary>
        /// Looks through the current <see cref="UniversalRenderPipelineAsset"/> and performs <paramref name="onFoundFeaturesPerRenderer"/> action for each found <see cref="WireframeRenderingFeature"/>.
        /// </summary>
        /// <param name="onFoundFeaturesPerRenderer">The action to perform.</param>
        /// <param name="breakAfterFirst">If traversal should stop after the first match is found.</param>
        private static void TraverseCurrentURPAssetForWireframeRenderingFeatures(Action<UniversalRenderPipelineAsset, ScriptableRenderer, WireframeRenderingFeature[]> onFoundFeaturesPerRenderer, bool breakAfterFirst)
        {
            UnityEngine.Rendering.RenderPipelineAsset renderPipelineAsset = QualitySettings.renderPipeline;
            if (renderPipelineAsset != null && renderPipelineAsset is UniversalRenderPipelineAsset urpPiplineAsset)
            {
                TranverseRenderersForWireframeRenderingFeatures(urpPiplineAsset, (scriptableRenderer, wireframeRendererFeatures) => onFoundFeaturesPerRenderer(urpPiplineAsset, scriptableRenderer, wireframeRendererFeatures), breakAfterFirst);
            }
        }

        /// <summary>
        /// Looks through the <paramref name="urpPiplineAsset"/> and performs <paramref name="onFoundFeaturesPerRenderer"/> action for each found <see cref="WireframeRenderingFeature"/>.
        /// </summary>
        /// <param name="urpPiplineAsset">A <see cref="UniversalRenderPipelineAsset"/>.</param>
        /// <param name="onFoundFeaturesPerRenderer">The action to perform.</param>
        /// <param name="breakAfterFirst">If traversal should stop after the first match is found.</param>
        private static void TranverseRenderersForWireframeRenderingFeatures(UniversalRenderPipelineAsset urpPiplineAsset, Action<ScriptableRenderer, WireframeRenderingFeature[]> onFoundFeaturesPerRenderer, bool breakAfterFirst)
        {
            ScriptableRendererData[] rendererDatas = urpPiplineAsset.GetType().GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .GetValue(urpPiplineAsset) as ScriptableRendererData[];
            if (rendererDatas != null)
            {
                for (int index = 0; index < rendererDatas.Length; index++)
                {
                    ScriptableRenderer scriptableRenderer = urpPiplineAsset.GetRenderer(index);
                    if (scriptableRenderer != null)
                    {
                        List<ScriptableRendererFeature> listOfFeatures = scriptableRenderer.GetType().GetProperty("rendererFeatures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .GetValue(scriptableRenderer) as List<ScriptableRendererFeature>;
                        if (listOfFeatures != null)
                        {
                            WireframeRenderingFeature[] wireframeFeatures = listOfFeatures.OfType<WireframeRenderingFeature>().ToArray();
                            if (wireframeFeatures.Length > 0)
                            {
                                onFoundFeaturesPerRenderer(scriptableRenderer, wireframeFeatures);
                                if (breakAfterFirst)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Looks through the <paramref name="urpPiplineAsset"/> and performs <paramref name="onFoundFeaturesPerRenderer"/> action for each found <see cref="WireframeRenderingFeature"/>.
        /// </summary>
        /// <param name="urpPiplineAsset">A <see cref="UniversalRenderPipelineAsset"/>.</param>
        /// <param name="onFoundFeaturesPerRenderer">The action to perform.</param>
        private static void TranverseRenderersForWireframeRenderingFeatures(ScriptableRendererData scriptableRendererData, Action<WireframeRenderingFeature[]> onFoundFeaturesPerRenderer)
        {
            List<ScriptableRendererFeature> listOfFeatures = scriptableRendererData.rendererFeatures;
            if (listOfFeatures != null)
            {
                WireframeRenderingFeature[] wireframeFeatures = listOfFeatures.OfType<WireframeRenderingFeature>().ToArray();
                if (wireframeFeatures.Length > 0)
                {
                    onFoundFeaturesPerRenderer(wireframeFeatures);
                }
            }
        }
    }
}
