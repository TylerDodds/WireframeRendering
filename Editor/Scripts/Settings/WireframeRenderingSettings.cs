// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature;

namespace PixelinearAccelerator.WireframeRendering.Editor.Settings
{
    /// <summary>
    /// Settings asset for wireframe rendering.
    /// </summary>
    internal class WireframeRenderingSettings : ScriptableObject
    {
        public const string k_MyCustomSettingsPath = "Assets/Settings/WireframeRenderingSettings.asset";

        [Delayed]
        public string DirectorySuffixForAutomaticGeneration = "_Wireframe";

        public bool AllowUserConfigurationOfUvGeneration = true;

        [Delayed]
        public int UvChannel = 3;

        [Delayed]
        public float AngleCutoffDegrees = 45;

        public bool DoNotWeldVertices = true;

        public bool ShowProjectItemInfo = true;

        internal static WireframeRenderingSettings Settings
        {
            get
            {
                if(_settings == null)
                {
                    _settings = GetOrCreateSettings();
                }
                return _settings;
            }
        }
        private static WireframeRenderingSettings _settings = null;

        /// <summary>
        /// Gets or creates the serialized <see cref="WireframeRenderingSettings"/>.
        /// </summary>
        /// <returns></returns>
        private static WireframeRenderingSettings GetOrCreateSettings()
        {
            WireframeRenderingSettings settings = AssetDatabase.LoadAssetAtPath<WireframeRenderingSettings>(k_MyCustomSettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<WireframeRenderingSettings>();
                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }

    /// <summary>
    /// Utility class for <see cref="WireframeRenderingSettings"/>, including settings and menu paths.
    /// </summary>
    static class WireframeRenderingSettingsUtil
    {
        [MenuItem(MenuPathOpenSettings, priority = -100)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(WireframeRenderingSettingsPath);
        }

        public const string MenuPathBase = "Tools/Wireframe Rendering/";
        public const string MenuNameOpenSettings = "Open Settings";
        public const string MenuPathOpenSettings = MenuPathBase + MenuNameOpenSettings;
        public const string MenuNameClearUVInformation = "Clear UV Coordinate Generation Information For Selected Models";
        public const string MenuPathClearUVInformation = MenuPathBase + MenuNameClearUVInformation;
        public const string MenuNameSetUvInformationGenerate = "Generate UV Coordinates For Selected Models";
        public const string MenuPathSetUvInformationGenerate = MenuPathBase + MenuNameSetUvInformationGenerate;
        public const string MenuNameSetUvInformationDoNotGenerate = "Do Not Generate UV Coordinates For Selected Models";
        public const string MenuPathSetUvInformationDoNotGenerate = MenuPathBase + MenuNameSetUvInformationDoNotGenerate;
        public const string MenuNameSelectRendererFeature = "Select Renderer Feature";
        public const string MenuPathSelectRendererFeature = MenuPathBase + MenuNameSelectRendererFeature;

        /// <summary>
        /// SettingsProvider path for wireframe rendering settings.
        /// </summary>
        public const string WireframeRenderingSettingsPath = "Project/WireframeRenderingSettings";
    }

    /// <summary>
    /// Class to register <see cref="WireframeRenderingSettings"/> with the IMGUI drawing framework.
    /// </summary>
    static class WireframeRenderingSettingsIMGUIRegister
    {
        /// <summary>
        /// Creates the WireframeRendering <see cref="SettingsProvider"/>.
        /// </summary>
        /// <returns>The <see cref="SettingsProvider"/>.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateWireframeRenderingSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider(WireframeRenderingSettingsUtil.WireframeRenderingSettingsPath, SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Wireframe Rendering",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    bool reimportModelsPressed = false;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.HelpBox("Settings changes will only affect newly-imported or reimported models.", MessageType.Info);
                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Space(12);
                            reimportModelsPressed = GUILayout.Button("Reimport Models");
                        }
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    WireframeRenderingSettings settings = WireframeRenderingSettings.Settings;
                    SerializedObject serializedSettings = new SerializedObject(settings);
                    EditorGUILayout.LabelField("UV Generation Status in Project Window", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.ShowProjectItemInfo)), new GUIContent("Show"));
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("UV Generation Details", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.UvChannel)), new GUIContent($"UV Channel ({MinChannel} to {MaxChannel})", "The UV Channel Where Wireframe Texture Coordinates Will Be Generated"));
                    int previousChannel = settings.UvChannel;
                    bool channelChanged = EditorGUI.EndChangeCheck();
                    if(channelChanged)
                    {
                        serializedSettings.FindProperty(nameof(settings.UvChannel)).intValue = Mathf.Clamp(serializedSettings.FindProperty(nameof(settings.UvChannel)).intValue, MinChannel, MaxChannel);
                    }
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.AngleCutoffDegrees)), new GUIContent($"Sharp Edge Angle (\u00B0)", 
                        "Adjacent edges that differ in angle greater less than this value are treated as a group for wireframe texture coordinate generation."));
                    bool angleCutoffChanged = EditorGUI.EndChangeCheck();
                    if (angleCutoffChanged)
                    {
                        serializedSettings.FindProperty(nameof(settings.AngleCutoffDegrees)).floatValue= Mathf.Clamp(serializedSettings.FindProperty(nameof(settings.AngleCutoffDegrees)).floatValue, 0f, 90f);
                    }
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.DoNotWeldVertices)), new GUIContent($"Do not Weld Vertices", "Disable Weld Vertices in model import settings for models with Wireframe Texture Coordinates generated."));
                    bool weldVerticesCanged = EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Automated UV Generation", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.DirectorySuffixForAutomaticGeneration)), new GUIContent("Directory Suffix", "Directories Ending With This Will Automatically Add Wireframe UVs to Imported Models."));
                    bool suffixChanged = EditorGUI.EndChangeCheck();
                    string previousSuffix = settings.DirectorySuffixForAutomaticGeneration;
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(UvUserConfigurationMessage, MessageType.Info);
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("User Configurable");
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.AllowUserConfigurationOfUvGeneration)), GUIContent.none);
                        GUILayout.FlexibleSpace();
                    }
                    bool userConfigurationSettingChanged = EditorGUI.EndChangeCheck();

                    serializedSettings.ApplyModifiedProperties();


                    if(reimportModelsPressed)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Do you want to reimport all models?", DialogOk, DialogCancel))
                        {
                            ReimportAllModels();
                        }
                    }
                    else if(channelChanged && previousChannel != settings.UvChannel)
                    {
                        SetRendererFeatureUvChannel(settings.UvChannel);
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new channel of UV coordinate generation?", DialogOk, DialogCancel))
                        {
                            UpdateChannelAndReimportModels(settings.UvChannel, settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(angleCutoffChanged)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new Angle Cutoff behavior?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(weldVerticesCanged)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new Weld Vertices behavior?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(suffixChanged && previousSuffix != settings.DirectorySuffixForAutomaticGeneration)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Do you want to reimport all models affected by the directory suffix change?", DialogOk, DialogCancel))
                        {
                            ReimportAllModels(previousSuffix, settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(userConfigurationSettingChanged)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new method of assigning UV coordinate generation?", DialogOk, DialogCancel))
                        {
                            ReimportModelsOnUserConfigurationAllowed(settings);
                        }
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>()
            };

            return provider;
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and sets the wireframe uv channel to be used.
        /// </summary>
        /// <param name="uvChannel">The wireframe uv channel to be used.</param>
        private static void SetRendererFeatureUvChannel(int uvChannel)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(WireframeRenderingFeature)}");
            string[] paths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToArray();
            foreach(string path in paths)
            {
                WireframeRenderingFeature feature = AssetDatabase.LoadAssetAtPath<WireframeRenderingFeature>(path);
                if(feature != null)
                {
                    SerializedObject serializedObject = new SerializedObject(feature);
                    SerializedProperty settingsProperty = serializedObject.FindProperty(nameof(feature.Settings));
                    SerializedProperty channelProperty = settingsProperty.FindPropertyRelative("_uvChannel"); 
                    channelProperty.intValue = uvChannel;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        /// <summary>
        /// Reimports relevant models with wireframe info or with appropriate <paramref name="directorySuffix"/>.
        /// </summary>
        private static void ReimportWireframeModels(string directorySuffix)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if (assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    Importer.WireframeMeshPostprocessor.WireframeInfo wireframeInfo = Importer.WireframeMeshPostprocessor.GetWireframeInfoFromUserData(assetImporter, out bool couldNotParse);
                    if((!couldNotParse && wireframeInfo.GenerateWireframeUvs) || Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, directorySuffix))
                    {
                        modelImporter.SaveAndReimport();
                    }
                }
            }
        }

        /// <summary>
        /// Updates UV channel and reimports relevant models.
        /// </summary>
        private static void UpdateChannelAndReimportModels(int channel, string directorySuffix)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if (assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    bool hadInfoAndWasUpdated = Importer.WireframeMeshPostprocessor.TryUpdateWireframeInfo(channel, modelImporter);
                    if(!hadInfoAndWasUpdated && Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, directorySuffix))
                    {
                        modelImporter.SaveAndReimport();
                    }
                }
            }
        }

        /// <summary>
        /// Reimport all models from user request (those in relevant directories or with wireframe information).
        /// </summary>
        private static void ReimportAllModels(params string[] directorySuffixRestrictions)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if (assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    if(directorySuffixRestrictions.Length == 0 || directorySuffixRestrictions.Any(ds => Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, ds)))
                    {
                        modelImporter.SaveAndReimport();
                    }
                }
            }
        }

        /// <summary>
        /// Reimport models based on changes to setting allowing user configuration of UV generation.
        /// </summary>
        /// <param name="settings">The <see cref="WireframeRenderingSettings"/>.</param>
        private static void ReimportModelsOnUserConfigurationAllowed(WireframeRenderingSettings settings)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if(assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    if(settings.AllowUserConfigurationOfUvGeneration)
                    {
                        //We want to re-import files in the directories that used to have UVS auto-generated, to raise any issues immediately (if assetData field is already being used).
                        if (Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, settings.DirectorySuffixForAutomaticGeneration))
                        {
                            modelImporter.SaveAndReimport();
                        }
                    }
                    else
                    {
                        //In this case, we want to go through *every* ModelImporter with userData, try to parse the WireframeInfo, and clear the field if we could successfully parse.
                        //This will allow other tools to start to use that field.
                        Importer.WireframeMeshPostprocessor.ClearWireframeUVCoordinateInformation(modelImporter, false, out bool couldNotParse);
                        if (couldNotParse)
                        {
                            //There's already information in that field, so we don't clear it, but we still need to reimport the model, since it won't be done above.
                            if (Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, settings.DirectorySuffixForAutomaticGeneration))
                            {
                                modelImporter.SaveAndReimport();
                            }
                        }
                    }
                }
            }
        }

        private const int MinChannel = 0;
        private const int MaxChannel = 7;
        private const string ModelSearchFilter = "t:Model";
        private const string DialogOk = "Yes";
        private const string DialogCancel = "No";
        private const string UvUserConfigurationMessage = "In addition to automatic generation from directory suffix, user configuration of wireframe UV generation from the Tools menu requires use of the AssetImporter userData field.\n" +
                                "If other packages, assets, or code requires this field, you can disable User Configurable to restrict UV generation to only check directory suffix.";
    }
}
