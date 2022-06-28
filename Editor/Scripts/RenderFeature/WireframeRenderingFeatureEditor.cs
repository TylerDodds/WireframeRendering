// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature;
using UnityEditor;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PixelinearAccelerator.WireframeRendering.Editor.Settings;
using PixelinearAccelerator.WireframeRendering.Runtime.Enums;

namespace PixelinearAccelerator.WireframeRendering.Editor.RenderFeature
{
    /// <summary>
    /// <see cref="CustomEditor"/> for <see cref="WireframeRenderingFeature"/>.
    /// </summary>
    [CustomEditor(typeof(WireframeRenderingFeature))]
    internal class WireframeRenderingFeatureEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            RefreshUrpAssetsAndScriptableRenderers();

            //Also set the uv channel for the WireframeRenderingFeatureSettings
            WireframeRenderingFeature feature = target as WireframeRenderingFeature;
            using (SerializedObject serializedObject = new SerializedObject(feature))
            {
                SerializedProperty settingsProperty = serializedObject.FindProperty(nameof(feature.Settings));

                SerializedProperty uvChannelProperty = settingsProperty.FindPropertyRelative("_uvChannel");
                uvChannelProperty.intValue = WireframeRenderingSettings.Settings.UvChannel;

                SerializedProperty wireframeTypeProperty = settingsProperty.FindPropertyRelative("_wireframeType");
                wireframeTypeProperty.enumValueIndex = (int)WireframeRenderingSettings.Settings.WireframeTypeToUse;

                if (SetLayerMaskFromOverallWireframeRenderingSettings)
                {
                    SerializedProperty layerMaskProperty = settingsProperty.FindPropertyRelative(nameof(feature.Settings.LayerMask));
                    layerMaskProperty.intValue = WireframeRenderingSettings.Settings.DefaultLayer.Mask;
                }

                SerializedProperty objectNormalsImportedProperty = settingsProperty.FindPropertyRelative("_objectNormalsImported");
                objectNormalsImportedProperty.boolValue = WireframeRenderingSettings.Settings.ImportObjectNormals;

                SerializedProperty contourEdgesImportedProperty = settingsProperty.FindPropertyRelative("_contourEdgesImported");
                contourEdgesImportedProperty.boolValue = WireframeRenderingSettings.Settings.ImportContourEdges;

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            WireframeRenderingFeature feature = target as WireframeRenderingFeature;
            WireframeRenderingFeatureSettings settings = feature.Settings;
            SerializedObject serializedObject = new SerializedObject(feature);
            SerializedProperty settingsProperty = serializedObject.FindProperty(nameof(feature.Settings));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup(nameof(feature.Settings.WireframeType), feature.Settings.WireframeType);
            }
            if (settings.WireframeType == WireframeType.GeometryShader)
            {
                using (new IndentLevel())
                {
                    ShowDepthTextureWarningGUI(settings, settingsProperty);
                }
            }
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(SetLayerMaskFromOverallWireframeRenderingSettings))
            {
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.LayerMask)));
                if (settings.WireframeType == WireframeType.GeometryShader)
                {
                    using (new IndentLevel())
                    {
                        ShowScriptableRendererLayerMaskWarningGUI(settings, settingsProperty);
                    }
                }
            }

            if (settings.WireframeType == WireframeType.TextureCoordinates)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(nameof(feature.Settings.UvChannel), feature.Settings.UvChannel);
                }
            }

            if (feature.Settings.WireframeType == WireframeType.GeometryShader)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("_objectNormalsImported"));
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("_contourEdgesImported"));
                }
            }

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.InFrontWireframe)));
            if(settings.InFrontWireframe)
            {
                using (new IndentLevel())
                {
                    SerializedProperty frontSettingsProperty = settingsProperty.FindPropertyRelative(nameof(settings.FrontSettings));
                    WireframeEdgeSettingsInspectorGUI(frontSettingsProperty, settings.FrontSettings, settings.WireframeType, settings.ObjectNormalsImported, settings.ContourEdgesImported);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.InBehindWireframe)));
            if (settings.WireframeType == WireframeType.TextureCoordinates|| settings.WireframeType == WireframeType.GeometryShader)
            {
                if (settings.InBehindWireframe)
                {
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.BehindLooksLikeFront)), new GUIContent("Behind Looks Like In-Front"));
                }
            }
            if (settings.InBehindWireframe)
            {
                using (new IndentLevel())
                {
                    if(!settings.BehindSamePassAsFront)
                    {
                        SerializedProperty behindSettingsProperty = settingsProperty.FindPropertyRelative(nameof(settings.BehindSettings));
                        WireframeEdgeSettingsInspectorGUI(behindSettingsProperty, settings.BehindSettings, settings.WireframeType, settings.ObjectNormalsImported, settings.ContourEdgesImported);
                    }
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.BehindDepthFade)), new GUIContent("Fade With Depth"));
                    if(settings.BehindDepthFade)
                    {
                        using (new IndentLevel())
                        {
                            if(settings.WireframeType == WireframeType.TextureCoordinates)
                            {
                                ShowDepthTextureWarningGUI(settings, settingsProperty);
                            }
                            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.DepthFadeDistance)));
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            if (settings.InFrontWireframe && settings.InBehindWireframe && !settings.BehindSamePassAsFront)
            {
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.HaloingWhenBothFrontAndBack)), new GUIContent(nameof(settings.Haloing)));
                if (settings.Haloing)
                {
                    using (new IndentLevel())
                    {
                        if (settings.WireframeType == WireframeType.TextureCoordinates)
                        {
                            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.HaloingWidthPx)));
                        }
                        else if(settings.WireframeType == WireframeType.GeometryShader)
                        {
                            if(settings.FrontSettings.WorldSpace)
                            {
                                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.HaloingWidthWorld)));
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.HaloingWidthPx)));
                            }
                        }
                        if (settings.WireframeType == WireframeType.TextureCoordinates || settings.WireframeType == WireframeType.GeometryShader)
                        {
                            _haloingAdvancedFoldout = EditorGUILayout.ToggleLeft("Advanced Options", _haloingAdvancedFoldout);
                            if (_haloingAdvancedFoldout)
                            {
                                using (new IndentLevel())
                                {
                                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.StencilReferenceValue)));
                                }
                            }
                        }
                    }
                }
            }

            if(settings.WireframeType == WireframeType.GeometryShader)
            {
                EditorGUILayout.Space();
                _geometryAdvancedFoldout = EditorGUILayout.Foldout(_geometryAdvancedFoldout, "Advanced", true);
                if(_geometryAdvancedFoldout)
                {
                    using (new IndentLevel())
                    {
                        EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.InFrontDepthCutoff)));
                        EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.ViewportEdgeWidthTaperStart)));
                        EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.ViewportEdgeWidthTaperEnd)));
                        EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.ViewportEdgeAlphaFadeStart)));
                        EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.ViewportEdgeAlphaFadeEnd)));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Shows HelpBox displaying status of depth texture enabled in URP assets.
        /// </summary>
        /// <param name="settings">The <see cref="WireframeRenderingFeatureSettings"/>.</param>
        /// <param name="settingsProperty">The <see cref="SerializedProperty"/> corresponding to the <paramref name="settings"/>.</param>
        private void ShowDepthTextureWarningGUI(WireframeRenderingFeatureSettings settings, SerializedProperty settingsProperty)
        {
            if (UrpAssets.All(ua => ua.supportsCameraDepthTexture))
            {
                EditorGUILayout.HelpBox(UrpDepthTextureMessageSatisfied, MessageType.None);
            }
            else
            {
                foreach (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset in UrpAssets)
                {
                    if (!asset.supportsCameraDepthTexture)
                    {
                        EditorGUILayout.HelpBox($"{UrpDepthTextureMessageError}: {AssetDatabase.GetAssetPath(asset)}", MessageType.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// Shows HelpBox displaying status of proer LayerMask in ScriptableRendererData.
        /// </summary>
        /// <param name="settings">The <see cref="WireframeRenderingFeatureSettings"/>.</param>
        /// <param name="settingsProperty">The <see cref="SerializedProperty"/> corresponding to the <paramref name="settings"/>.</param>
        private void ShowScriptableRendererLayerMaskWarningGUI(WireframeRenderingFeatureSettings settings, SerializedProperty settingsProperty)
        {
            if(ScriptableRendererDatas.OfType<UnityEngine.Rendering.Universal.ForwardRendererData>().All(frd => LayersDisabled(frd, settings)))
            {
                EditorGUILayout.HelpBox(ForwardRendererLayerMaskSatisfied, MessageType.None);
            }
            else
            {
                foreach(UnityEngine.Rendering.Universal.ForwardRendererData forwardRendererData in ScriptableRendererDatas.OfType<UnityEngine.Rendering.Universal.ForwardRendererData>())
                {
                    if(!LayersDisabled(forwardRendererData, settings))
                    {
                        EditorGUILayout.HelpBox($"{ForwardRendererLayerMaskError}: {AssetDatabase.GetAssetPath(forwardRendererData)}", MessageType.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// Returns if the <paramref name="forwardRendererData"/> layer masks correctly ignore the wireframe layer given in the <paramref name="settings"/>.
        /// </summary>
        /// <param name="forwardRendererData">The <see cref="UnityEngine.Rendering.Universal.ForwardRendererData"/></param>
        /// <param name="settings">The <see cref="WireframeRenderingFeatureSettings"/>.</param>
        private static bool LayersDisabled(UnityEngine.Rendering.Universal.ForwardRendererData forwardRendererData, WireframeRenderingFeatureSettings settings)
        {
            return (forwardRendererData.opaqueLayerMask & settings.LayerMask) == 0 && (forwardRendererData.transparentLayerMask & settings.LayerMask) == 0;
        }

        /// <summary>
        /// Draws inspector GUI for <see cref="WireframeEdgeSettings"/>.
        /// </summary>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> corresponding to the <paramref name="edgeSettings"/>.</param>
        /// <param name="edgeSettings">The edge settings.</param>
        /// <param name="wireframeType">The wireframe rendering type.</param>
        /// <param name="objectNormalsImported">If object normals are imported</param>
        /// <param name="contourEdgesImported">If contour edge information is imported</param>
        private void WireframeEdgeSettingsInspectorGUI(SerializedProperty serializedProperty, WireframeEdgeSettings edgeSettings, WireframeType wireframeType, bool objectNormalsImported, bool contourEdgesImported)
        {
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.Color)));
            if(edgeSettings.WorldSpace)
            {
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.WidthWorld)));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.WidthPx)));
            }
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.FalloffWidthPx)));
            if(wireframeType == WireframeType.GeometryShader)
            {
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.Overshoot)));
                if (edgeSettings.Overshoot)
                {
                    using (new IndentLevel())
                    {
                        if (edgeSettings.WorldSpace)
                        {
                            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.OvershootWorld)));
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.OvershootPx)));
                        }
                    }
                }
            }
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.Dash)));
            if (edgeSettings.Dash)
            {
                using (new IndentLevel())
                {
                    if (edgeSettings.WorldSpace)
                    {
                        EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.DashLengthWorld)));
                        EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.EmptyLengthWorld)));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.DashLengthPx)));
                        EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.EmptyLengthPx)));
                    }
                }
            }
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.ApplyTexture)));
            if (edgeSettings.ApplyTexture)
            {
                using (new IndentLevel())
                {
                    EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.Texture)));
                    EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.KeepTextureAspectRatio)));
                    if (!edgeSettings.KeepTextureAspectRatio)
                    {
                        if (edgeSettings.WorldSpace)
                        {
                            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.TextureLengthWorld)));
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.TextureLengthPx)));
                        }
                    }
                }
            }
            if(wireframeType == WireframeType.GeometryShader)
            {
                if(contourEdgesImported)
                {
                    EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.ShowContourEdges)));
                }
            }
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.WorldSpace)));
            if(wireframeType == WireframeType.GeometryShader)
            {
                if(edgeSettings.WorldSpace && objectNormalsImported)
                {
                    using (new IndentLevel())
                    {
                        EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.UseObjectNormals)));
                    }
                }
            }
            if (wireframeType == WireframeType.TextureCoordinates)
            {
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.Fresnel)));
            }
        }

        /// <summary>
        /// If the LayerMask of this <see cref="WireframeRenderingFeature"/> should be set based on the <see cref="WireframeRenderingSettings"/>.
        /// </summary>
        private static bool SetLayerMaskFromOverallWireframeRenderingSettings
        {
            get => WireframeRenderingSettings.Settings.WireframeTypeToUse == WireframeType.GeometryShader;
        }

        /// <summary>
        /// Helper class for increasing <see cref="EditorGUI.indentLevel"/> within a <code>using</code> scope.
        /// </summary>
        private class IndentLevel : IDisposable
        {
            public IndentLevel()
            {
                EditorGUI.indentLevel++;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// A set of <see cref="UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"/> using this renderer feature.
        /// </summary>
        private HashSet<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset> UrpAssets
        {
            get
            {
                if(_urpAssets == null)
                {
                    RefreshUrpAssetsAndScriptableRenderers();
                    
                }
                return _urpAssets;
            }
        }
        private HashSet<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset> _urpAssets = null;

        /// <summary>
        /// A set of <see cref="UnityEngine.Rendering.Universal.ScriptableRenderer"/> using this renderer feature.
        /// </summary>
        private HashSet<UnityEngine.Rendering.Universal.ScriptableRendererData> ScriptableRendererDatas
        {
            get
            {
                if (_scriptableRendererDatas == null)
                {
                    RefreshUrpAssetsAndScriptableRenderers();

                }
                return _scriptableRendererDatas;
            }
        }
        private HashSet<UnityEngine.Rendering.Universal.ScriptableRendererData> _scriptableRendererDatas = null;

        /// <summary>
        /// Refreshes the <see cref="UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"/> and <see cref="UnityEngine.Rendering.Universal.ScriptableRenderer"/> using this renderer feature.
        /// </summary>
        private void RefreshUrpAssetsAndScriptableRenderers()
        {
            if (_urpAssets == null)
            {
                _urpAssets = new HashSet<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
                WireframeRenderingFeatureHelper.FillPipelineAssetListsIfHasFeature(target as WireframeRenderingFeature, _urpAssets);
            }
            if (_scriptableRendererDatas == null)
            {
                _scriptableRendererDatas = new HashSet<UnityEngine.Rendering.Universal.ScriptableRendererData>();
                WireframeRenderingFeatureHelper.FillScriptableRendererListIfHasFeature(target as WireframeRenderingFeature, _scriptableRendererDatas);
            }
        }


        private bool _haloingAdvancedFoldout = false;
        private bool _geometryAdvancedFoldout = false;

        private const string UrpDepthTextureMessageSatisfied = "URP Assets Using This Feature Have Camera Depth Texture Enabled";
        private const string UrpDepthTextureMessageError = "URP Asset Using This Feature Does Not Have Camera Depth Texture Enabled";
        private const string ForwardRendererLayerMaskSatisfied = "Forward Renderer Data Assets Using This Feature Do Not Render Chosen Layer Normally";
        private const string ForwardRendererLayerMaskError = "Forward Renderer Data Asset Using This Feature Also Renders Chosen Layer Normally (Opaque/Transparent Layer Mask setting)";
    }
}
