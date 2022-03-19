// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature;
using UnityEditor;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PixelinearAccelerator.WireframeRendering.Editor.Settings;

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
            RefreshUrpAssets();

            //Also set the uv channel for the WireframeRenderingFeatureSettings
            WireframeRenderingFeature feature = target as WireframeRenderingFeature;
            using (SerializedObject serializedObject = new SerializedObject(feature))
            {
                SerializedProperty settingsProperty = serializedObject.FindProperty(nameof(feature.Settings));

                SerializedProperty uvChannelProperty = settingsProperty.FindPropertyRelative("_uvChannel");
                uvChannelProperty.intValue = WireframeRenderingSettings.Settings.UvChannel;
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

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.LayerMask)));

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.InFrontWireframe)));
            if(settings.InFrontWireframe)
            {
                using (new IndentLevel())
                {
                    SerializedProperty frontSettingsProperty = settingsProperty.FindPropertyRelative(nameof(settings.FrontSettings));
                    WireframeEdgeSettingsInspectorGUI(frontSettingsProperty, settings.FrontSettings);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.InBehindWireframe)));
            if (settings.InBehindWireframe)
            {
                using (new IndentLevel())
                {
                    SerializedProperty behindSettingsProperty = settingsProperty.FindPropertyRelative(nameof(settings.BehindSettings));
                    WireframeEdgeSettingsInspectorGUI(behindSettingsProperty, settings.BehindSettings);
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.BehindDepthFade)), new GUIContent("Fade With Depth"));
                    if(settings.BehindDepthFade)
                    {
                        using (new IndentLevel())
                        {
                            if(UrpAssets.All(ua => ua.supportsCameraDepthTexture))
                            {
                                EditorGUILayout.HelpBox(UrpDepthTextureMessageSatisfied, MessageType.None);
                            }
                            else
                            {
                                foreach(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset in UrpAssets)
                                {
                                    if (!asset.supportsCameraDepthTexture)
                                    {
                                        EditorGUILayout.HelpBox($"{UrpDepthTextureMessageError}: {AssetDatabase.GetAssetPath(asset)}", MessageType.Warning);
                                    }
                                }
                            }

                            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.DepthFadeDistance)));
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            if (settings.InFrontWireframe && settings.InBehindWireframe)
            {
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.HaloingWhenBothFrontAndBack)), new GUIContent(nameof(settings.Haloing)));
                if (settings.HaloingWhenBothFrontAndBack)
                {
                    using (new IndentLevel())
                    {
                        EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative(nameof(settings.HaloingWidth)));
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

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(nameof(feature.Settings.UvChannel), feature.Settings.UvChannel);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws inspector GUI for <see cref="WireframeEdgeSettings"/>.
        /// </summary>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> corresponding to the <paramref name="edgeSettings"/>.</param>
        /// <param name="edgeSettings">The edge settings.</param>
        private void WireframeEdgeSettingsInspectorGUI(SerializedProperty serializedProperty, WireframeEdgeSettings edgeSettings)
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
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.WorldSpace)));
            EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative(nameof(edgeSettings.Fresnel)));
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
        /// A list of <see cref="UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"/> using this renderer feature.
        /// </summary>
        internal List<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset> UrpAssets
        {
            get
            {
                if(_urpAssets == null)
                {
                    _urpAssets = new List<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
                    RefreshUrpAssets();
                    
                }
                return _urpAssets;
            }
        }
        private List<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset> _urpAssets = null;

        /// <summary>
        /// Refreshes the <see cref="UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"/> using this renderer feature.
        /// </summary>
        private void RefreshUrpAssets()
        {
            if (_urpAssets == null)
            {
                _urpAssets = new List<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
            }
            WireframeRenderingFeatureHelper.FillPipelineAssetListsIfHasFeature(target as WireframeRenderingFeature, _urpAssets);
        }


        private bool _haloingAdvancedFoldout = false;

        private const string UrpDepthTextureMessageSatisfied = "URP Assets Using This Feature Have Camera Depth Texture Enabled";
        private const string UrpDepthTextureMessageError = "URP Asset Using This Feature Does Not Have Camera Depth Texture Enabled";
    }
}
